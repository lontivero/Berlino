namespace Berlino.Tests

open Berlino
open Berlino.Wallet
open Berlino.Wallet.ScriptPubKeyDescriptor
open Prelude
open NBitcoin
open Xunit

module Wallet =

    let createDestinations () = state {
        let! destination = getNextScriptPubKeyForReceiving ScriptType.P2WPKH "Lucas"
        let! change = getNextScriptPubKeyForChange ScriptType.P2WPKH "Pablo"
        return (destination, change)
    }

    let generateTransactionChain (destination : ScriptPubKeyInfo) (change : ScriptPubKeyInfo) = state {
        let tx0 = createFundingTransaction destination.ScriptPubKey
        let tx1 = createSpendingTransaction tx0 (Money.Coins 0.3m) change.ScriptPubKey

        return! processTransactions [tx0; tx1]
    }

    [<Fact>]
    let ``Can discover outputs and update metadata`` () =

        let newWallet = createNewWallet Network.Main

        let (destination, change), usedWallet = createDestinations () |> State.run newWallet
        let transactionChain = generateTransactionChain destination change

        let utxoExistingWallet, existingWallet  = transactionChain |> State.run usedWallet
        let utxoRecoveredWallet, recoveredWallet = transactionChain |> State.run newWallet
        Assert.Equal(Money.Coins 0.3m, Outputs.balance utxoExistingWallet)
        Assert.Equal<Outputs.Output seq>(utxoRecoveredWallet, utxoExistingWallet)
        Assert.Equal<Set<string * string>>(
            existingWallet.Metadata |> List.map (fun (k,v) -> k.ToString(), v) |> Set.ofList, Set [
                "84'/0'/0'/1/0", "Pablo"
                "84'/0'/0'/0/0", "Lucas"
            ] )
        Assert.Equal<Set<string * string>>(
            recoveredWallet.Metadata |> List.map (fun (k,v) -> k.ToString(), v) |> Set.ofList, Set [
                "84'/0'/0'/1/0", "__discovered__"
                "84'/0'/0'/0/0", "__discovered__"
            ] )

