namespace FilterProvider

open System
open System.Threading
open Berlino.Filters
open NBitcoin
open Suave
open WebSocket

open Suave.Operators
open Suave.Filters
open Suave.RequestErrors

module Program =

    let handleWebSocket filterFetcher knownHashString =
          match uint256.TryParse knownHashString with
          | true, knownHash -> handShake (webSocketHandler filterFetcher knownHash)
          | false, _ -> NOT_FOUND "Invalid known hash"

    let webapp handleWebSocket : WebPart =
        choose [
            pathScan "/%s" handleWebSocket
        ]

    [<EntryPoint>]
    let main args =
        let config = Configuration.load "config.json" args
        let network = Configuration.network config
        let db = Database.connection config.DatabaseConnectionString

        let getFiltersFromDatabase = Database.get db 500
        let filterFetcher = fetchFilters getFiltersFromDatabase
        let webSocketHandler = handleWebSocket filterFetcher
        let app = webapp webSocketHandler
        use cts = new CancellationTokenSource()
        let conf = { defaultConfig with cancellationToken = cts.Token; logger = logger }
        startWebServer conf app
        0 // return an integer exit code
