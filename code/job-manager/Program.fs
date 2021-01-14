open Argu
open k8s
open k8s.Models
open StackExchange.Redis
open System
open System.Threading

type CLIArguments =
    | QueueName of queueName:string
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | QueueName _ -> "Name of the jobs queue"

let getJobConfig jobId numberOfJobs =
    let jobName = sprintf $"redis-job-%s{jobId}"
    sprintf $"""
    apiVersion: batch/v1
    kind: Job
    metadata:
      name: %s{jobName}
    spec:
      completions: %d{numberOfJobs}
      parallelism: 3
      template:
        metadata:
          name: redis-job
        spec:
          containers:
          - name: redis-job
            image: karlkim/redis-job
            command: ["./redis-job", "--jobname", "%s{jobName}"]
          restartPolicy: OnFailure
    """

[<EntryPoint>]
let main argv =
    async {
        let config = KubernetesClientConfiguration.BuildDefaultConfig()
        let client = new Kubernetes(config)

        let jobHandler (cm:ChannelMessage) =
            let message = cm.Message.ToString()
            match Int32.TryParse(message) with
            | false, _ -> ()
            | true, numberOfJobs ->
                let jobId = Guid.NewGuid().ToString().Substring(0,5)
                let jobConfig = getJobConfig jobId numberOfJobs
                let job = Yaml.LoadFromString<V1Job>(jobConfig)
                client.CreateNamespacedJob(
                    body=job,
                    namespaceParameter="default") |> ignore

        printfn "Job Manager Starts.."

        let namespaces = client.ListNamespace()
        printfn ""
        printfn "namespaces"
        printfn "=========="
        namespaces.Items
        |> Seq.iter (fun ns -> printfn "%s" ns.Metadata.Name)
        printfn ""

        let parser = ArgumentParser.Create<CLIArguments>(programName="job-manager")
        let argResults = parser.Parse(argv)
        let queueName = argResults.GetResult(<@QueueName@>, defaultValue="jobs")

        use redis = ConnectionMultiplexer.Connect("redis.storage")

        let sub = redis.GetSubscriber()

        printfn $"Subscribe to %s{queueName}.."
        let channel = RedisChannel(queueName, RedisChannel.PatternMode.Literal)
        let messageQueue = sub.Subscribe(channel, CommandFlags.FireAndForget)
        messageQueue.OnMessage(jobHandler)

        do! Tasks.Task.Delay(Timeout.Infinite) |> Async.AwaitTask
        return 0
    } |> Async.RunSynchronously |> int