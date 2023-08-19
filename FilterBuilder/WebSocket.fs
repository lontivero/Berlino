module WebSocket

open System
open Berlino
open System.Text
open Berlino.Filters
open FSharp.Control
open FSharpPlus.Data
open Microsoft.FSharp.Control
open NBitcoin
open FSharpPlus
open Suave
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket

type FiltersChunkFetcher = uint256 -> Async<Result<FilterData list, exn>>
type FiltersFetcher = uint256 -> AsyncSeq<FilterData list>

let fetchFilters (filterChunkGetter : FiltersChunkFetcher) blockId =
    let rec loop blockId = asyncSeq {
        let! filtersResult = filterChunkGetter blockId
        match filtersResult with
        | Ok [] ->
            ()
        | Ok filters ->
            yield filters
            let lastFilter = List.last filters
            yield! loop lastFilter.BlockHash
        | Error e ->
            Console.WriteLine ""
    }
    loop blockId


let webSocketHandler (fetchFilters : FiltersFetcher) (knownHash : uint256) (webSocket : WebSocket) (context: HttpContext) =

    let worker =
        MailboxProcessor<byte[] list>.Start(fun inbox ->
            let rec loop () = async {
                let! filters = inbox.Receive()
                do!
                    filters
                    |> List.map (fun filter ->
                        webSocket.send Binary (ByteSegment filter) true )
                    |> Async.Sequential
                    |> Async.Ignore

                return! loop ()
            }
            loop () )
    let send = worker.Post

    knownHash
    |> fetchFilters
    |> AsyncSeq.map (List.map wireSerialize)
    |> AsyncSeq.iter send
    |> Async.Start

    let emptyResponse = ByteSegment [||]
    let rec messageLoop () = socket {
        let! msg = webSocket.read()
        match msg with
        | Ping, _, _  -> do! webSocket.send Pong  emptyResponse true
        | Close, _, _ -> do! webSocket.send Close emptyResponse true
        | _ ->           return! messageLoop()
    }

    messageLoop ()


open Suave.Logging

let loggingOptions =
    { Literate.LiterateOptions.create() with
        getLogLevelText = function Verbose->"V" | Debug->"D" | Info->"I" | Warn->"W" | Error->"E" | Fatal->"F" }

let logger =
    LiterateConsoleTarget(
        name = [|"Provider"|],
        minLevel = Verbose,
        options = loggingOptions,
        outputTemplate = "[{level}] {timestampUtc:o} {message} [{source}]{exceptions}"
    ) :> Logger

