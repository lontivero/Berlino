namespace Berlino

open System
open NBitcoin

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

        type ScriptPubKeyInfo = {
            ScriptPubKey : Script
            Index : uint
            Generator: ScriptPubKeyDescriptor
            KeyPath : KeyPath
        }

        type ScriptPubKeyInfoSet = ScriptPubKeyInfo seq

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

        let deriveExtPubKeyForPurpose = memoize (
            fun (extPubKey : ExtPubKey, purpose) ->
                purpose |> purposeIndex |> extPubKey.Derive)

        let derive = memoize (
            fun (sg : ScriptPubKeyDescriptor, i : uint) ->
                deriveExtPubKeyForPurpose (sg.ExtPubKey, sg.Purpose)
                |> fun extPubKey -> extPubKey.Derive(i).PubKey
                |> getScriptPubKey sg.ScriptType
                |> fun scp -> {
                    ScriptPubKey = scp
                    Index = i
                    Generator = sg
                    KeyPath = sg.KeyPath.Derive(purposeIndex sg.Purpose).Derive(i)
                    })

        let deriveRange sg start count =
            seq {
                for i in start .. start + count - 1u do
                yield derive (sg, i)
            }

        let getScriptPubKeys indexedGenerators =
            Seq.collect (fun (sg, i) -> deriveRange sg 0u i) indexedGenerators

    [<RequireQualifiedAccess>]
    module Outputs =
        open ScriptPubKeyDescriptor

        [<CustomEquality; NoComparison>]
        type Output =
            {
                OutPoint : OutPoint
                Amount : Money
                ScriptPubKeyInfo : ScriptPubKeyInfo
                CreatedBy : Transaction
            }
            override this.GetHashCode () = this.OutPoint.GetHashCode()
            override this.Equals other =
                match other with
                | :? Output as o -> o.OutPoint.Equals this.OutPoint
                | _ -> false
            interface IEquatable<Output> with
                member this.Equals other = other.OutPoint.Equals this.OutPoint

        type OutputSet = Output seq

        let keyPath output = output.ScriptPubKeyInfo.KeyPath

        let spent = memoize (
            fun (outputs : OutputSet) ->
                let allInputsSpent =
                    outputs
                    |> Seq.collect (fun output -> output.CreatedBy.Inputs)
                    |> Seq.map (fun input -> input.PrevOut)
                    |> Seq.toList
                outputs
                |> Seq.filter (fun o -> allInputsSpent |> Seq.contains o.OutPoint))

        let unspent = memoize (
            fun (outputs : OutputSet) ->
                outputs
                |> Seq.except (spent outputs))

        let balance outputs =
            outputs
            |> unspent
            |> Seq.sumBy (fun x -> x.Amount)

        let discoverOutputs (scriptPubKeyInfoSet : ScriptPubKeyInfoSet) (tx : Transaction) =
            tx.Outputs
            |> Seq.indexed
            |> Seq.join scriptPubKeyInfoSet (fun (_, output) -> output.ScriptPubKey) (fun spki -> spki.ScriptPubKey)
            |> Seq.map (fun ((i, output), spkInfo) -> {
                OutPoint = OutPoint(tx, uint i)
                Amount = output.Value
                ScriptPubKeyInfo = spkInfo
                CreatedBy = tx
                })

    [<RequireQualifiedAccess>]
    module Knowledge =
        type Label = KeyPath * string

        let knownBy outpoint (metadata : Label list) (outputs : Outputs.OutputSet) =
            let outputsWithKnowledge =
                outputs
                |> Seq.join metadata Outputs.keyPath fst
                |> Seq.map (fun (output, (_, knownBy)) -> output, knownBy)
            let rec kb (outpoint : OutPoint) = seq {
                let o, theOneWhoKnowThisOne =
                    outputsWithKnowledge
                    |> Seq.find (fun (o,k) -> o.OutPoint = outpoint)
                yield theOneWhoKnowThisOne
                let thoseWhoKnowAncestors =
                    o.CreatedBy.Inputs
                    |> Seq.collect (fun i -> kb i.PrevOut)
                yield! thoseWhoKnowAncestors
                }
            kb outpoint |> Set.ofSeq

    open ScriptPubKeyDescriptor

    type Wallet = {
        Network : Network
        Descriptors : ScriptPubKeyDescriptor list
        Metadata : Knowledge.Label list
    }

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
            let script = derive (g, nextIndex)
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

            let metadata =
                newOutputs
                |> Seq.map (fun x -> x.ScriptPubKeyInfo.KeyPath)
                |> Seq.except (wallet.Metadata |> List.map fst)
                |> Seq.map (fun keypath -> keypath, "__discovered__")
                |> Seq.toList

            let newWallet = { wallet with Metadata = metadata @ wallet.Metadata }
            newOutputs, newWallet

    let processTransactions (txs : Transaction list) : WalletTransformer<Outputs.OutputSet> =
        fun wallet ->
            ((Seq.empty<Outputs.Output>, wallet), txs)
            ||> List.fold (fun (outputs, wallet) tx ->
                let discovery = state {
                    let! discoveredOutputs = processTransaction tx
                    return outputs |> Seq.append discoveredOutputs
                }
                discovery |> State.run wallet)

