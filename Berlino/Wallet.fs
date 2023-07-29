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


