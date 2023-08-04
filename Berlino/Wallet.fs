namespace Berlino

open System
open NBitcoin
open FSharpPlus
open Thoth.Json.Net

module Wallet =

    module ScriptPubKeyDescriptor =
        type ScriptPurpose =
            | Receiving
            | Change
            | CoinJoin

        let purposeIndex =
            function
            | Receiving -> 0u
            | Change -> 1u
            | CoinJoin -> 2u

        type ScriptPubKeyDescriptor = {
            ExtPubKey: ExtPubKey
            Fingerprint : HDFingerprint
            KeyPath : KeyPath
            ScriptType: ScriptType
            Purpose: ScriptPurpose
            Gap : uint
        }

        [<CustomEquality;CustomComparison>]
        type ScriptPubKeyInfo =
            {
                ScriptPubKey : Script
                Index : uint
                Generator: ScriptPubKeyDescriptor
                KeyPath : KeyPath
            }
            override this.GetHashCode () = this.ScriptPubKey.GetHashCode()
            override this.Equals obj =
                match obj with
                | :? ScriptPubKeyInfo as other -> compare this other = 0
                | _ -> false
            interface IComparable<ScriptPubKeyInfo> with
                member this.CompareTo other =
                    BytesComparer.Instance.Compare (this.ScriptPubKey.ToBytes(true), other.ScriptPubKey.ToBytes(true))
            interface IComparable with
                member this.CompareTo obj =
                    match obj with
                    | null -> 1
                    | :? ScriptPubKeyInfo as other -> (this :> IComparable<_>).CompareTo other
                    | _                            -> invalidArg "obj" "not a ScriptPubKeyInfo"

        let scriptPubKey spki = spki.ScriptPubKey

        type ScriptPubKeyInfoSet = Set<ScriptPubKeyInfo>

        let create (extPubKey : ExtPubKey) (fingerprint : HDFingerprint) (keyPath : KeyPath) scriptType scriptPurpose minGapLimit =
            {
                ExtPubKey = extPubKey
                Fingerprint = fingerprint
                ScriptType = scriptType
                Purpose = scriptPurpose
                KeyPath = keyPath
                Gap = minGapLimit
            }

        let getScriptPubKey scriptType (pubkey : PubKey) =
            match scriptType with
            | ScriptType.P2WPKH -> pubkey.GetScriptPubKey(ScriptPubKeyType.Segwit)
            | ScriptType.Taproot -> pubkey.GetScriptPubKey(ScriptPubKeyType.TaprootBIP86)
            | ScriptType.P2PKH -> pubkey.GetScriptPubKey(ScriptPubKeyType.Legacy)
            | _ -> failwith $"Unknown script type {scriptType}"


        let deriveExtPubKeyForPurpose =
            let memoizableFun (extPubKey : ExtPubKey) (purpose : ScriptPurpose) =
                purpose |> purposeIndex |> extPubKey.Derive
            memoizeN memoizableFun

        let derive =
            let memoizableFun (sg : ScriptPubKeyDescriptor) (i : uint) =
                deriveExtPubKeyForPurpose sg.ExtPubKey sg.Purpose
                |> fun extPubKey -> extPubKey.Derive(i).PubKey
                |> getScriptPubKey sg.ScriptType
                |> fun scp -> {
                    ScriptPubKey = scp
                    Index = i
                    Generator = sg
                    KeyPath = sg.KeyPath.Derive(purposeIndex sg.Purpose).Derive(i)
                    }
            memoizeN memoizableFun

        let deriveRange sg start count =
            seq {
                for i in start .. start + count - 1u do
                yield derive sg i
            }

        let getScriptPubKeys indexedGenerators =
            Seq.collect (fun (sg, i) -> deriveRange sg 0u i) indexedGenerators
            |> Set.ofSeq

        [<RequireQualifiedAccess>]
        module Decode =
            open Berlino.Serialization

            let scriptPurpose : Decoder<ScriptPurpose> =
                Decode.string |> Decode.andThen (function
                    | "receiving" -> Decode.succeed ScriptPurpose.Receiving
                    | "change"    -> Decode.succeed ScriptPurpose.Change
                    | "coinjoin"  -> Decode.succeed ScriptPurpose.CoinJoin
                    | purpose -> Decode.fail $"Unknown script's purpose '{purpose}'")

            let scriptType : Decoder<ScriptType> =
                Decode.string |> Decode.andThen (function
                    | "taproot" -> Decode.succeed ScriptType.Taproot
                    | "wpkh"    -> Decode.succeed ScriptType.P2WPKH
                    | scriptType -> Decode.fail $"Unknown script's type '{scriptType}'")

            let scriptPubKeyDescriptor (network : Network) : Decoder<ScriptPubKeyDescriptor> =
                Decode.object(fun get -> {
                    ExtPubKey = get.Required.Field "extPubKey" (Decode.extPubKey network)
                    Fingerprint = get.Required.Field "fingerprint" Decode.fingerprint
                    ScriptType = get.Required.Field "scriptType" scriptType
                    Purpose = get.Required.Field "scriptPurpose" scriptPurpose
                    KeyPath = get.Required.Field "keyPath" Decode.keyPath
                    Gap = get.Required.Field "gap" Decode.uint32
                })

        [<RequireQualifiedAccess>]
        module Encode =
            open Berlino.Serialization

            let scriptPurpose (scriptPurpose : ScriptPurpose) =
                match scriptPurpose with
                | ScriptPurpose.Receiving -> "receiving"
                | ScriptPurpose.Change -> "change"
                | ScriptPurpose.CoinJoin -> "coinjoin"
                |> Encode.string

            let scriptType (scriptType : ScriptType) =
                match scriptType with
                | ScriptType.Taproot -> "taproot"
                | ScriptType.P2WPKH -> "wpkh"
                | _ -> failwith $"Unsupported script type {scriptType}"
                |> Encode.string

            let scriptPubKeyDescriptor (network : Network) (d : ScriptPubKeyDescriptor) =
                Encode.object [
                    "extPubKey", Encode.extPubKey d.ExtPubKey network
                    "fingerprint", Encode.fingerprint d.Fingerprint
                    "scriptType", scriptType d.ScriptType
                    "scriptPurpose", scriptPurpose d.Purpose
                    "keyPath", Encode.keyPath d.KeyPath
                    "gap", Encode.uint32 d.Gap
                ]


    [<RequireQualifiedAccess>]
    module Outputs =
        open ScriptPubKeyDescriptor

        [<CustomEquality; CustomComparison>]
        type Output =
            {
                OutPoint : OutPoint
                Amount : Money
                ScriptPubKeyInfo : ScriptPubKeyInfo
                CreatedBy : Transaction
            }
            override this.GetHashCode () = this.OutPoint.GetHashCode()
            override this.Equals obj =
                match obj with
                | :? Output as other -> compare this other = 0
                | _ -> false
            interface IEquatable<Output> with
                member this.Equals other = other.OutPoint.Equals this.OutPoint
            interface IComparable<Output> with
                member this.CompareTo other =
                    match this.OutPoint.Hash.CompareTo other.OutPoint.Hash with
                    | 0 -> this.OutPoint.N.CompareTo other.OutPoint.N
                    | x -> x
            interface IComparable with
                member this.CompareTo obj =
                    match obj with
                    | null -> 1
                    | :? Output as other -> (this :> IComparable<_>).CompareTo other
                    | _                  -> invalidArg "obj" "not an Output"

        let outputAmount output = output.Amount
        let outputInputs output = output.CreatedBy.Inputs :> seq<TxIn>
        let inputPrevOut (input : TxIn) = input.PrevOut
        let outputPrevOuts output = output |> outputInputs |>> inputPrevOut
        let outputKeyPath output = output.ScriptPubKeyInfo.KeyPath
        let outputScriptPubKey output = output.ScriptPubKeyInfo.ScriptPubKey

        type OutputSet = Set<Output>

        let spent =
            let memoizableFun (outputs : OutputSet) =
                let allInputsSpent =
                    outputs
                    |> Seq.collect outputPrevOuts
                    |> Seq.cache

                outputs
                |> Set.filter (fun o -> allInputsSpent |> Seq.contains o.OutPoint)
            memoizeN memoizableFun

        let unspent (outputs : OutputSet) =
            outputs - (spent outputs)

        let balance outputs =
            outputs
            |> unspent
            |> Seq.sumBy outputAmount

        let discoverOutputs (scriptPubKeyInfoSet : ScriptPubKeyInfoSet) (tx : Transaction) =
            tx.Outputs
            |> Seq.indexed
            |> Seq.join scriptPubKeyInfoSet (fun (_, output) -> output.ScriptPubKey) scriptPubKey
            |> Seq.map (fun ((i, output), spkInfo) -> {
                OutPoint = OutPoint(tx, uint i)
                Amount = output.Value
                ScriptPubKeyInfo = spkInfo
                CreatedBy = tx
                })
            |> Set.ofSeq

    [<RequireQualifiedAccess>]
    module Knowledge =

        type Label = KeyPath * string

        let knownBy outpoint (metadata : Label list) (outputs : Outputs.OutputSet) =
            let outputsWithKnowledge =
                outputs
                |> Seq.join metadata Outputs.outputKeyPath fst
                |> Seq.map (fun (output, (_, knownBy)) -> output, knownBy)
            let rec kb (outpoint : OutPoint) = seq {
                let curOutput, theOneWhoKnowThisOne =
                    outputsWithKnowledge
                    |> Seq.find (fun (o,_) -> o.OutPoint = outpoint)
                yield theOneWhoKnowThisOne
                yield! curOutput |> Outputs.outputPrevOuts |> Seq.collect kb
                }
            kb outpoint |> Set.ofSeq

        [<RequireQualifiedAccess>]
        module Decode =
            open Berlino.Serialization

            let metadata : Decoder<(KeyPath * string) list> =
                Decode.string
                |> Decode.keyValuePairs
                |> Decode.map (List.collect (fun (k,v) -> [KeyPath k,v]))

        [<RequireQualifiedAccess>]
        module Encode =
            open Berlino.Serialization

            let metadata (metadata : Label list) =
                metadata
                |> List.map (fun (kp, l) -> kp.ToString(), Encode.string l)
                |> Encode.object


    open ScriptPubKeyDescriptor

    type Wallet = {
        Network : Network
        Descriptors : ScriptPubKeyDescriptor list
        Metadata : Knowledge.Label list
    }

    [<RequireQualifiedAccess>]
    module Decode =
        open Berlino.Serialization

        let wallet : Decoder<Wallet> =
            Decode.object (fun get ->
                let network = get.Required.Field "network" Decode.network
                {
                    Network = network
                    Descriptors = get.Required.Field "descriptors" (Decode.list (Decode.scriptPubKeyDescriptor network))
                    Metadata = get.Required.Field "metadata" Knowledge.Decode.metadata
                })

    [<RequireQualifiedAccess>]
    module Encode =
        open Berlino.Serialization

        let wallet (wallet : Wallet) =
            Encode.object [
                "network", wallet.Network |> Encode.network
                "descriptors", wallet.Descriptors |> List.map (Encode.scriptPubKeyDescriptor wallet.Network) |> Encode.list
                "metadata", wallet.Metadata |> Knowledge.Encode.metadata
             ]
    type WalletTransformer<'a> = State<Wallet, 'a>

    let recover (mnemonic : Mnemonic) (network : Network) =
        let masterExtKey = mnemonic.DeriveExtKey()
        let fingerprint = masterExtKey.Neuter().PubKey.GetHDFingerPrint();
        let segwitKeyPath = KeyPath.Parse("m/84'/0'/0'")
        let taprootKeyPath = KeyPath.Parse("m/86'/0'/0'")
        let segwitExtPubKey = masterExtKey.Derive(segwitKeyPath).Neuter()
        let taprootExtPubKey = masterExtKey.Derive(taprootKeyPath).Neuter()
        {
            Network = network
            Descriptors = [
                create segwitExtPubKey  fingerprint segwitKeyPath ScriptType.P2WPKH ScriptPurpose.Receiving 20u
                create segwitExtPubKey  fingerprint segwitKeyPath ScriptType.P2WPKH ScriptPurpose.Change 10u
                create taprootExtPubKey fingerprint taprootKeyPath ScriptType.Taproot ScriptPurpose.Receiving 20u
                create taprootExtPubKey fingerprint taprootKeyPath ScriptType.Taproot ScriptPurpose.Change 10u
                create taprootExtPubKey fingerprint taprootKeyPath ScriptType.Taproot ScriptPurpose.CoinJoin 30u
                ]
            Metadata = []
        }

    let createNewWallet network =
        let mnemonic = Mnemonic(Wordlist.English, WordCount.Twelve)
        recover mnemonic network

    let getNextScriptByKeyPath (keyPath : KeyPath) wallet =
        wallet.Metadata
        |> List.tryFindBack (fun (k, _) -> k = keyPath)
        |> Option.map (fun (k, _) -> k.Indexes[-1] + 1u)
        |> Option.defaultValue 0u

    let getAllScriptPubKeys (wallet : Wallet) =
        let generators =
            wallet.Descriptors
            // |> List.filter (fun g -> g.Purpose <> ScriptPurpose.Change)
            |> List.map (fun g -> g, g.Gap + (getNextScriptByKeyPath g.KeyPath wallet))
        getScriptPubKeys generators

    let getNextScriptPubKey scriptType purpose knownBy : WalletTransformer<ScriptPubKeyInfo> =
        fun (wallet : Wallet) ->
            let g =
                wallet.Descriptors
                |> List.find (fun g -> g.Purpose = purpose && g.ScriptType = scriptType)

            let nextIndex = getNextScriptByKeyPath g.KeyPath wallet
            let script = derive g nextIndex
            let newWallet = {
                wallet with
                  Metadata = (script.KeyPath, knownBy) :: wallet.Metadata
            }
            script, newWallet

    let getNextScriptPubKeyForReceiving scriptType knownBy : WalletTransformer<ScriptPubKeyInfo> =
        getNextScriptPubKey scriptType ScriptPurpose.Receiving knownBy

    let getNextScriptPubKeyForChange scriptType knownBy : WalletTransformer<ScriptPubKeyInfo> =
        getNextScriptPubKey scriptType ScriptPurpose.Change knownBy

    let getNextScriptPubKeyForCoinjoin scriptType : WalletTransformer<ScriptPubKeyInfo> =
        getNextScriptPubKey scriptType ScriptPurpose.CoinJoin ""

    let processTransaction tx : WalletTransformer<Outputs.OutputSet> =
        fun wallet ->
            let scriptPubKeys = getAllScriptPubKeys wallet
            let newOutputs = Outputs.discoverOutputs scriptPubKeys tx

            let withoutMetadata =
                newOutputs
                |> Seq.map Outputs.outputKeyPath
                |> Seq.except (wallet.Metadata |> List.map fst)
                |> Seq.map (fun keypath -> keypath, "__discovered__")
                |> Seq.toList

            let newWallet = { wallet with Metadata = withoutMetadata @ wallet.Metadata }
            newOutputs, newWallet

    let processTransactions (txs : Transaction list) : WalletTransformer<Outputs.OutputSet> =
        fun wallet ->
            ((Set.empty<Outputs.Output>, wallet), txs)
            ||> List.fold (fun (outputs, wallet) tx ->
                let discovery = state {
                    let! discoveredOutputs = processTransaction tx
                    return outputs |> Set.union discoveredOutputs
                }
                discovery |> State.run wallet)

