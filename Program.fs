open System
open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

type ColumnRecord = {
    Diameter: float
    Length: float
    Load: float
    FireRating: string
}

let initialColumnData = [
    { Diameter = 101.9; Length = 959.0; Load = 959.0; FireRating = "cold" }
    { Diameter = 101.9; Length = 1959.0; Load = 2959.0; FireRating = "cold" }
    { Diameter = 101.9; Length = 2959.0; Load = 3959.0; FireRating = "cold" }
    { Diameter = 101.9; Length = 3959.0; Load = 4959.0; FireRating = "cold" }
]

// Function to query data based on user input
let queryData diameterValue lengthValue loadValue fireRatingValue =
    initialColumnData
    |> List.tryFind (fun record -> 
        record.Diameter = diameterValue && 
        record.Length = lengthValue && 
        record.Load = loadValue && 
        record.FireRating = fireRatingValue)

// Define the web app with error handling
let webApp (logger: ILogger) =
    choose [
        route "/" >=> htmlFile "wwwroot/index.html"
        route "/query" >=> fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let diameterValue = ctx.Request.Query.["diameter"].ToString()
                let lengthValue = ctx.Request.Query.["length"].ToString()
                let loadValue = ctx.Request.Query.["load"].ToString()
                let fireRatingValue = ctx.Request.Query.["fireRating"].ToString()

                logger.LogInformation($"Received parameters: diameterValue={diameterValue}, lengthValue={lengthValue}, loadValue={loadValue}, fireRatingValue={fireRatingValue}")

                let diameterParsed, diameterValueParsed = Double.TryParse(diameterValue)
                let lengthParsed, lengthValueParsed = Double.TryParse(lengthValue)
                let loadParsed, loadValueParsed = Double.TryParse(loadValue)

                if diameterParsed && lengthParsed && loadParsed then
                    let result = queryData diameterValueParsed lengthValueParsed loadValueParsed fireRatingValue

                    match result with
                    | Some res ->
                        logger.LogInformation($"Query result: {res}")
                        return! json {| result = res |} next ctx
                    | None ->
                        logger.LogInformation("No matching record found.")
                        return! json {| error = "No matching record found." |} next ctx
                else
                    logger.LogInformation("Invalid input.")
                    return! json {| error = "Invalid input. Please enter valid numbers for Diameter, Length, and Load." |} next ctx
            }
        route "/data" >=> fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                return! json initialColumnData next ctx
            }
    ]

// Configure services
let configureServices (services: IServiceCollection) =
    services.AddGiraffe() |> ignore
    services.AddLogging(fun builder ->
        builder.AddConsole() |> ignore
        builder.AddDebug() |> ignore) |> ignore

// Configure the HTTP request pipeline with error handling
let configureApp (app: IApplicationBuilder) =
    let logger = app.ApplicationServices.GetService<ILogger<obj>>()
    app.UseStaticFiles()
       .UseGiraffe(webApp logger)

// Configure and run the web host
[<EntryPoint>]
let main argv =
    Host.CreateDefaultBuilder(argv)
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .ConfigureServices(configureServices)
                .Configure(configureApp)
                |> ignore)
        .Build()
        .Run()
    0
