namespace Berlino

open System
open NBitcoin
       
module Wallet =

    module ScriptPubKeyDescriptor =
        type ScriptPurpose =
            | Receiving
            | Change
            | CoinJoin

        type ScriptPubKeyDescriptor = {
            ExtPubKey: ExtPubKey
            Fingerprint : HDFingerprint
            KeyPath : KeyPath
            ScriptType: ScriptType
            Purpose: ScriptPurpose
            Gap : uint
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
            
