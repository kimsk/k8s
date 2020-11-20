module job_api.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe

open FSharp.Control.Tasks.V2.ContextInsensitive
open k8s
open k8s.Models


// ---------------------------------
// Models
// ---------------------------------

type Message =
    {
        Text : string
    }

let getJobConfig jobName jobId numberOfJobs =
    let jobName = sprintf "%s-%s" jobName jobId
    sprintf """
    apiVersion: batch/v1
    kind: Job
    metadata:
      name: %s
    spec:
      completions: %d
      parallelism: 3
      template:
        metadata:
          name: redis-job
        spec:
          containers:
          - name: redis-job
            image: karlkim/redis-job
            command: ["./redis-job", "--jobname", "%s"]
          restartPolicy: OnFailure
    """ jobName numberOfJobs jobName

[<CLIMutable>]
type Job =
    {
        JobName: string
        NumberOfJobs: int
    }

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open GiraffeViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "job_api" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]

    let partial () =
        h1 [] [ encodedText "job_api" ]

    let index (model : Message) =
        [
            partial()
            p [] [ encodedText model.Text ]
        ] |> layout

// ---------------------------------
// Web app
// ---------------------------------

let jobHandler (job:Job) =
    let config = KubernetesClientConfiguration.BuildDefaultConfig()
    let client = new Kubernetes(config)

    let jobId = Guid.NewGuid().ToString().Substring(0,5)
    let {JobName=jobName; NumberOfJobs=numberOfJobs} = job
    let jobConfig = getJobConfig jobName jobId numberOfJobs
    let job = Yaml.LoadFromString<V1Job>(jobConfig)
    client.CreateNamespacedJob(
            body=job,
            namespaceParameter="default") |> ignore

    let message = sprintf "Create job %s with %d completions!" jobName numberOfJobs
    let model     = { Text = message }
    let view      = Views.index model
    htmlView view

let indexHandler (name : string) =
    let greetings = sprintf "Hello %s, from Job API!" name
    let model     = { Text = greetings }
    let view      = Views.index model
    htmlView view

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler "world"
                routef "/hello/%s" indexHandler
            ]
        POST >=> routeBind<Job> "/jobs/{JobName}/{NumberOfJobs}" jobHandler
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.EnvironmentName with
    | "Development" -> app.UseDeveloperExceptionPage()
    | _ -> app.UseGiraffeErrorHandler(errorHandler))
        //.UseHttpsRedirection()
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0