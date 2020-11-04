open System
open k8s
open k8s.Models

[<EntryPoint>]
let main argv =
    let config = KubernetesClientConfiguration.BuildConfigFromConfigFile()
    printfn "current context: %s" config.CurrentContext

    let client = new Kubernetes(config)

    let namespaces = client.ListNamespace()
    printfn ""
    printfn "namespaces"
    printfn "=========="
    namespaces.Items
    |> Seq.iter (fun ns -> printfn "%s" ns.Metadata.Name)

    let jobYml = """
    apiVersion: batch/v1
    kind: Job
    metadata:
      name: redis-job
    spec:
      completions: 10
      parallelism: 3
      template:
        metadata:
          name: redis-job
        spec:
          containers:
          - name: redis-job
            image: karlkim/redis-job
            command: ["./redis-job"]
          restartPolicy: OnFailure
    """

    let redisJob = Yaml.LoadFromString<V1Job>(jobYml)
    client.CreateNamespacedJob(
            body=redisJob,
            namespaceParameter="default") |> ignore

    let jobs = client.ListJobForAllNamespaces()
    printfn ""
    printfn "jobs"
    printfn "===="
    jobs.Items
    |> Seq.iter (fun j -> printfn "%s" j.Metadata.Name)

    let pods = client.ListPodForAllNamespaces()
    printfn ""
    printfn "pods"
    printfn "===="
    pods.Items
    |> Seq.filter (fun p -> p.Metadata.Namespace() = "default")
    |> Seq.iter (fun p -> printfn "%s" p.Metadata.Name)

    0

