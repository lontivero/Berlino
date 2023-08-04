namespace Berlino

open System
open System.Collections.Generic
open System.Linq

[<AutoOpen>]
module Exception =

    let exnAsString (e : exn) = e.ToString()

[<AutoOpen; RequireQualifiedAccess>]
module Seq =
    let exceptBy excluded predicate ls =
        ls |> Seq.filter (fun x -> excluded |> Seq.exists (predicate x) |> not )

    let join (innerSequence : 'b seq) outerKeySelector innerKeySelector (outerSequence : 'a seq) =
        outerSequence.Join(innerSequence,
            Func<_, _>(outerKeySelector), Func<_, _>(innerKeySelector),
            fun outer inner -> (outer, inner))

type State<'s, 'a> = ('s -> 'a * 's)

[<RequireQualifiedAccess>]
module State =

    let inline run state x = let (f) = x in f state
    let get = (fun s -> s, s)
    let put newState = (fun _ -> (), newState)
    let map f s = (fun (state: 's) ->
        let x, state = run state s
        f x, state)

[<AutoOpen>]
module StateBuilder =
    type StateBuilder() =
        member this.Zero () = (fun s -> (), s)
        member this.Return x = (fun s -> x, s)
        member inline this.ReturnFrom (x: State<'s, 'a>) = x
        member this.Bind (x, f) : State<'s, 'b> =
            (fun state ->
                let (result: 'a), state = State.run state x
                State.run state (f result))
        member this.Combine (x1: State<'s, 'a>, x2: State<'s, 'b>) =
            (fun state ->
                let result, state = State.run state x1
                State.run state x2)
        member this.Delay f : State<'s, 'a> = f ()
        member this.For (seq, (f: 'a -> State<'s, 'b>)) =
            seq
            |> Seq.map f
            |> Seq.reduceBack (fun x1 x2 -> this.Combine (x1, x2))
        member this.While (f, x) =
            if f () then this.Combine (x, this.While (f, x))
            else this.Zero ()

    let state = StateBuilder()

[<AutoOpen; RequireQualifiedAccess>]
module Async =
    let CatchResult (computation: Async<'T>) : Async<Result<'T, exn>> = async {
        let! choice = computation |> Async.Catch

        match choice with
        | Choice1Of2 foo -> return (Ok foo)
        | Choice2Of2 err -> return (Error err)
    }

[<AutoOpen; RequireQualifiedAccess>]
module Result =
    let requiresOk r =
        match r with
        | Ok x -> x
        | Error e ->
            raise e

[<AutoOpen; RequireQualifiedAccess>]
module AsyncResult =
    let bind f (m : Async<Result<_,_>>) = async {
        let! x = m
        return x |> Result.bind f
    }

    let join (r : Async<Result<Result<_,_>,_>>) =
        bind id r

    let mapError mapper (r : Async<Result<_,_>>) = async {
        let! result = r
        return
            match result with
            | Ok x -> Ok x
            | Error e -> Error (mapper e)
    }

[<AutoOpen>]
module Runner =
    open FSharpPlus

    let loopWhile state predicate doWork =
        let rec loop state = async {
            if predicate state then
                let! newState = doWork state
                do! loop newState
        }
        loop state

    let forever state doWork =
        loopWhile state (fun _ -> true) doWork

