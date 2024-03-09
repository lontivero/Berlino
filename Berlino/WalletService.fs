namespace Berlino

open NBitcoin
open ScriptPubKeyDescriptor
open Wallet

[<RequireQualifiedAccess>]
module WalletService =

    type Message =
        | MsgBlock of Block
        | MsgNewReceivingAddress of ScriptPubKeyType * string * AsyncReplyChannel<ScriptPubKeyInfo>
        | MsgNewChangeAddress of ScriptPubKeyType * string * AsyncReplyChannel<ScriptPubKeyInfo>
        | MsgNewConjoinAddress of ScriptPubKeyType * AsyncReplyChannel<ScriptPubKeyInfo>

    let startWallet wallet =
        MailboxProcessor<Message>.Start (fun inbox -> async {
            let rec loop wallet (transactionSet : TransactionSet) = async {
                let! msg = inbox.Receive()
                let newState = state {
                    match msg with
                    | MsgBlock block ->
                        return! processTransactions (block.Transactions |> List.ofSeq)
                    | MsgNewReceivingAddress (scriptType, knownBy, replyChannel) ->
                        let! scriptPubKeyInfo = getNextScriptPubKeyForReceiving scriptType knownBy
                        replyChannel.Reply scriptPubKeyInfo
                        return transactionSet
                    | MsgNewChangeAddress (scriptType, knownBy, replyChannel) ->
                        let! scriptPubKeyInfo = getNextScriptPubKeyForChange scriptType knownBy
                        replyChannel.Reply scriptPubKeyInfo
                        return transactionSet
                    | MsgNewConjoinAddress (scriptType, replyChannel) ->
                        let! scriptPubKeyInfo = getNextScriptPubKeyForCoinjoin scriptType
                        replyChannel.Reply scriptPubKeyInfo
                        return transactionSet
                }
                let transactionSet, wallet = newState |> State.run wallet
                return! loop wallet transactionSet
            }
            return! loop wallet []
        })

    let getNextScriptPubKeyForReceiving scriptType knownBy (ws : MailboxProcessor<Message>) =
        ws.PostAndAsyncReply (fun reply -> MsgNewReceivingAddress(scriptType, knownBy, reply))

    let getNextScriptPubKeyForChange scriptType knownBy (ws : MailboxProcessor<Message>) =
        ws.PostAndAsyncReply (fun reply -> MsgNewChangeAddress(scriptType, knownBy, reply))

    let getNextScriptPubKeyForCoinjoin scriptType (ws : MailboxProcessor<Message>) =
        ws.PostAndAsyncReply (fun reply -> MsgNewConjoinAddress(scriptType, reply))

    let processBlock block (ws : MailboxProcessor<Message>) =
        ws.Post (MsgBlock block)
