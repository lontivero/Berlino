namespace Berlino.Tests

open Berlino
open Berlino.ScriptPubKeyDescriptor
open Prelude
open NBitcoin
open Xunit

module Wallet =

    let createDestinations () = state {
        let! destination = Wallet.getNextScriptPubKeyForReceiving ScriptPubKeyType.Segwit "Lucas"
        let! change = Wallet.getNextScriptPubKeyForChange ScriptPubKeyType.Segwit "Pablo"
        return (destination, change)
    }

    let generateTransactionChain (destination : ScriptPubKeyInfo) (change : ScriptPubKeyInfo) = state {
        let tx0 = createFundingTransaction destination.ScriptPubKey
        let tx1 = createSpendingTransaction tx0 (Money.Coins 0.3m) change.ScriptPubKey

        return! Wallet.processTransactions [tx0; tx1]
    }

    [<Fact>]
    let ``Can discover outputs and update metadata`` () =

        let newWallet = Wallet.createNew Network.Main

        let (destination, change), usedWallet = createDestinations () |> State.run newWallet
        let transactionChain = generateTransactionChain destination change

        let existingWalletTxs, existingWallet  = transactionChain |> State.run usedWallet
        let recoveredWalletTxs, recoveredWallet = transactionChain |> State.run newWallet
        Assert.Equal(Money.Coins 0.3m, Outputs.balance (existingWallet |> Wallet.getAllScriptPubKeys) existingWalletTxs)
        Assert.Equal(Money.Coins 0.3m, Outputs.balance (recoveredWallet |> Wallet.getAllScriptPubKeys) recoveredWalletTxs)
        Assert.Equal<TransactionSet>(recoveredWalletTxs, existingWalletTxs)
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

    [<Fact>]
    let ``Can compute entities who know about outputs`` () =
        state {
            let! destination, change = createDestinations ()
            let! relevantTransactions = generateTransactionChain destination change

            let! wallet = State.get
            let scriptPubKeys = wallet |> Wallet.getAllScriptPubKeys
            let outputs = Outputs.allOutputs scriptPubKeys relevantTransactions
            let firstOutput = outputs |> Seq.find (fun x -> x.ScriptPubKeyInfo = change) |> _.OutPoint;
            let knownBy = Knowledge.knownBy firstOutput wallet.Metadata scriptPubKeys relevantTransactions
            let expected = ["Lucas"; "Pablo"] |> Set.ofList
            Assert.Equal<Set<string>>(expected, knownBy)

            let secondOutput = outputs |> Seq.find (fun x -> x.ScriptPubKeyInfo = destination) |> _.OutPoint;
            let knownBy = Knowledge.knownBy secondOutput wallet.Metadata scriptPubKeys relevantTransactions
            Assert.Equal(["Lucas"], knownBy)
        } |> State.run (Wallet.createNew Network.Main)

    open Thoth.Json.Net

    [<Fact>]
    let ``Can (de)-serialize wallet`` () =
        state {
            let! _ = createDestinations () // just to generate the metadata

            let! wallet = State.get
            let serializedWallet = Encode.toString 0 (Wallet.Encode.wallet wallet)
            let deserializedWalletResult = Decode.fromString Wallet.Decode.wallet serializedWallet
            Assert.Equal (Ok wallet, deserializedWalletResult)
        } |> State.run (Wallet.createNew Network.Main)

