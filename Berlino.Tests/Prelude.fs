namespace Berlino.Tests

open NBitcoin

module Prelude =
    let createFundingTransaction (scriptPubKey : Script) =
        let tx = Transaction.Create Network.Main
        let _ = tx.Outputs.Add(Money.Coins(1m), scriptPubKey)
        tx

    let rec createSpendingTransaction fundingTx amount (changeScriptPubKey : Script) =
        let tx = Transaction.Create Network.Main
        let _ = tx.Inputs.Add(fundingTx, 0)
        let value = fundingTx.Outputs[0].Value;
        let _ = tx.Outputs.Add(amount, changeScriptPubKey)
        let _ = tx.Outputs.Add(value - amount, Script.Empty)
        tx

