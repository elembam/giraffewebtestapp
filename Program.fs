open System
open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http

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

// Define the web app
let webApp =
    choose [
        route "/" >=> htmlFile "wwwroot/index.html"
        route "/query" >=> fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let diameterValue = ctx.Request.Query.["diameter"].ToString()
                let lengthValue = ctx.Request.Query.["length"].ToString()
                let loadValue = ctx.Request.Query.["load"].ToString()
                let fireRatingValue = ctx.Request.Query.["fireRating"].ToString()

                let diameterParsed, diameterValueParsed = Double.TryParse(diameterValue)
                let lengthParsed, lengthValueParsed = Double.TryParse(lengthValue)
                let loadParsed, loadValueParsed = Double.TryParse(loadValue)

                if diameterParsed && lengthParsed && loadParsed then
                    let result = queryData diameterValueParsed lengthValueParsed loadValueParsed fireRatingValue

                    match result with
                    | Some res ->
                        return! json {| result = res |} next ctx
                    | None ->
                        return! json {| error = "No matching record found." |} next ctx
                else
                    return! json {| error = "Invalid input. Please enter valid numbers for Diameter, Length, and Load." |} next ctx
            }
        route "/data" >=> fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                return! json initialColumnData next ctx
            }
    ]

let configureServices (services: IServiceCollection) =
    services.AddGiraffe() |> ignore

let configureApp (app: IApplicationBuilder) =
    app.UseStaticFiles()
       .UseGiraffe(webApp)

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
