open Argu
open StackExchange.Redis

type CLIArguments =
    | [<Mandatory>] JobName of jobName:string
    | QueueName of queueName:string
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | JobName _ -> "Name of the job"
            | QueueName _ -> "Name of the items queue"


[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<CLIArguments>(programName="redis-job")
    let argResults = parser.Parse(argv)
    let jobName = argResults.GetResult(<@JobName@>)
    let queueName = argResults.GetResult(<@QueueName@>, defaultValue="items")

    printfn "%s consumes item from redis FIFO queue %s..." jobName queueName
    let muxer = ConnectionMultiplexer.Connect("redis.storage")
    let conn = muxer.GetDatabase()
    let items = RedisKey(queueName)

    let item = conn.ListLeftPop(items)
    if item.HasValue then
        let itemString = (item.ToString())
        let key = RedisKey(itemString)
        printfn "found item: %s.." itemString
        conn.StringIncrement(key) |> ignore
    else
        printfn "no item left.."

    printfn "redis-job done..."
    0 // return an integer exit code
