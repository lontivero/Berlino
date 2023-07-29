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
    module Knowledge =
        type Label = KeyPath * string

    open ScriptPubKeyDescriptor

    type Wallet = {
        Network : Network
        Descriptors : ScriptPubKeyDescriptor list
        Metadata : Knowledge.Label list
    }

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

