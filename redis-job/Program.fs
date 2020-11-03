open StackExchange.Redis
open System.Threading

[<EntryPoint>]
let main argv =
    printfn "consume item from redis FIFO queue..."
    let muxer = ConnectionMultiplexer.Connect("redis.storage")
    let conn = muxer.GetDatabase()
    let jobs = RedisKey("jobs")

    while true do
        let item = conn.ListLeftPop(jobs)
        if item.HasValue then
            printfn "consume item: %s.." (item.ToString())
        else
            printfn "no item left.."

        Thread.Sleep(5000)

    printfn "redis-job done..."
    0 // return an integer exit code
