module WebSocket

open System
open Berlino
open System.Text
open Berlino.Filters
open FSharp.Control
open FSharpPlus.Data
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
            do! Async.Sleep (TimeSpan.FromSeconds 5)
            yield! loop blockId
        | Ok filters ->
            yield filters
            let lastFilter = List.last filters
            yield! loop (lastFilter.BlockHash)
        | Error e ->
            Console.WriteLine ""
    }
    loop blockId


let webSocketHandler (fetchFilters : FiltersFetcher) (taprootActivation : TaprootActivation) (webSocket : WebSocket) (context: HttpContext) =

    let worker =
        MailboxProcessor<byte[]>.Start(fun inbox ->
            let rec loop () = async {
                let! msg = inbox.Receive()
                let! _ = webSocket.send Binary (ByteSegment msg) true
                return! loop ()
            }
            loop () )
    let send = worker.Post

    let rec messageLoop () = socket {
        let! msg = webSocket.read()
        match msg with
        | Binary, data, true ->
            let parseBlockHash = Result.protect (fun (x : byte[]) -> uint256 x)
            let blockHashResult = parseBlockHash data
            match blockHashResult with
            | Error _ ->
                send (Encoding.UTF8.GetBytes "Invalid request")
            | Ok blockId ->
                blockId
                |> fetchFilters
                |> AsyncSeq.map (List.map wireSerialize)
                |> AsyncSeq.collect (AsyncSeq.ofSeq)
                |> AsyncSeq.iter send
                |> Async.Start

            return! messageLoop()
        | Close, _, _ ->
            let emptyResponse = [||] |> ByteSegment
            do! webSocket.send Close emptyResponse true
        | _ ->
            return! messageLoop()
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

