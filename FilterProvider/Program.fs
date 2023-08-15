namespace FilterProvider

open System
open System.Threading
open Berlino.Filters
open Suave
open WebSocket

open Suave.Operators
open Suave.Filters

module Program =

    let webapp filterFetcher (taprootActivation : TaprootActivation) : WebPart =
        choose [
            path "/" >=> handShake (webSocketHandler filterFetcher taprootActivation)
        ]

    [<EntryPoint>]
    let main args =
        let config = Configuration.load "config.json" args
        let network = Configuration.network config
        let db = Database.connection config.DatabaseConnectionString
        let taprootActivation = taprootActivation network

        let getFiltersFromDatabase blockId = Database.get db blockId
        let filterFetcher blockId = fetchFilters getFiltersFromDatabase blockId
        let app = webapp filterFetcher taprootActivation
        use cts = new CancellationTokenSource()
        let conf = { defaultConfig with cancellationToken = cts.Token; logger = logger }
        startWebServer conf app
        0 // return an integer exit code
