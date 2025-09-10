using Monero.Common;
using Monero.Wallet;
using Monero.Wallet.Common;

namespace Monero.Test.Utils;

public class WalletEqualityUtils
{
    public static void TestWalletEqualityOnChain(MoneroWallet w1, MoneroWallet w2)
    {
        TestUtils.WALLET_TX_TRACKER.Reset(); // all wallets need to wait for txs to confirm to reliably sync

        // wait for relayed txs associated with wallets to clear pool
        Assert.Equal(w1.IsConnectedToDaemon(), w2.IsConnectedToDaemon());
        if (w1.IsConnectedToDaemon()) TestUtils.WALLET_TX_TRACKER.WaitForWalletTxsToClearPool([w1, w2]);

        // sync the wallets until same height
        while (w1.GetHeight() != w2.GetHeight())
        {
            w1.Sync();
            w2.Sync();
        }

        // test that wallets are equal using only on-chain data
        Assert.Equal(w1.GetHeight(), w2.GetHeight());
        Assert.Equal(w1.GetSeed(), w2.GetSeed());
        Assert.Equal(w1.GetPrimaryAddress(), w2.GetPrimaryAddress());
        Assert.Equal(w1.GetPrivateViewKey(), w2.GetPrivateViewKey());
        Assert.Equal(w1.GetPrivateSpendKey(), w2.GetPrivateSpendKey());
        MoneroTxQuery txQuery = new MoneroTxQuery().SetIsConfirmed(true);
        TestTxWalletsEqualOnChain(w1.GetTxs(txQuery), w2.GetTxs(txQuery));
        txQuery.SetIncludeOutputs(true);
        TestTxWalletsEqualOnChain(w1.GetTxs(txQuery), w2.GetTxs(txQuery));  // fetch and compare outputs
        TestAccountsEqualOnChain(w1.GetAccounts(true), w2.GetAccounts(true));
        Assert.Equal(w1.GetBalance(), w2.GetBalance());
        Assert.Equal(w1.GetUnlockedBalance(), w2.GetUnlockedBalance());
        MoneroTransferQuery transferQuery = new MoneroTransferQuery().SetTxQuery(new MoneroTxQuery().SetIsConfirmed(true));
        TestTransfersEqualOnChain(w1.GetTransfers(transferQuery), w2.GetTransfers(transferQuery));
        MoneroOutputQuery outputQuery = new MoneroOutputQuery().SetTxQuery(new MoneroTxQuery().SetIsConfirmed(true));
        TestOutputWalletsEqualOnChain(w1.GetOutputs(outputQuery), w2.GetOutputs(outputQuery));
    }

    private static void TestAccountsEqualOnChain(List<MoneroAccount> accounts1, List<MoneroAccount> accounts2)
    {
        for (int i = 0; i < Math.Max(accounts1.Count, accounts2.Count); i++)
        {
            if (i < accounts1.Count && i < accounts2.Count)
            {
                TestAccountEqualOnChain(accounts1[i], accounts2[i]);
            }
            else if (i >= accounts1.Count)
            {
                for (int j = i; j < accounts2.Count; j++)
                {
                    Assert.Equal((ulong)0, accounts2[j].GetBalance()!);
                    Assert.True(accounts2[j].GetSubaddresses().Count >= 1);
                    foreach (MoneroSubaddress subaddress in accounts2[j].GetSubaddresses()) Assert.False(subaddress.IsUsed());
                }
                return;
            }
            else
            {
                for (int j = i; j < accounts1.Count; j++)
                {
                    Assert.Equal((ulong)0, accounts1[j].GetBalance()!);
                    Assert.True(accounts1[j].GetSubaddresses().Count >= 1);
                    foreach (MoneroSubaddress subaddress in accounts1[j].GetSubaddresses()) Assert.False(subaddress.IsUsed());
                }
                return;
            }
        }
    }

    private static void TestAccountEqualOnChain(MoneroAccount account1, MoneroAccount account2)
    {

        // nullify off-chain data for comparison
        List<MoneroSubaddress> subaddresses1 = account1.GetSubaddresses();
        List<MoneroSubaddress> subaddresses2 = account2.GetSubaddresses();
        account1.SetSubaddresses(null);
        account2.SetSubaddresses(null);
        account1.SetTag(null);
        account2.SetTag(null);

        // test account equality
        Assert.Equal(account1, account2);
        TestSubaddressesEqualOnChain(subaddresses1, subaddresses2);
    }

    private static void TestSubaddressesEqualOnChain(List<MoneroSubaddress> subaddresses1, List<MoneroSubaddress> subaddresses2)
    {
        for (int i = 0; i < Math.Max(subaddresses1.Count, subaddresses2.Count); i++)
        {
            if (i < subaddresses1.Count && i < subaddresses2.Count)
            {
                TestSubaddressesEqualOnChain(subaddresses1[i], subaddresses2[i]);
            }
            else if (i >= subaddresses1.Count)
            {
                for (int j = i; j < subaddresses2.Count; j++)
                {
                    Assert.True(0 == (ulong)subaddresses2[j].GetBalance());
                    Assert.False(subaddresses2[j].IsUsed());
                }
                return;
            }
            else
            {
                for (int j = i; j < subaddresses1.Count; j++)
                {
                    Assert.True(0 == (ulong)subaddresses1[i].GetBalance());
                    Assert.False(subaddresses1[j].IsUsed());
                }
                return;
            }
        }
    }

    private static void TestSubaddressesEqualOnChain(MoneroSubaddress subaddress1, MoneroSubaddress subaddress2)
    {
        subaddress1.SetLabel(null); // nullify off-chain data for comparison
        subaddress2.SetLabel(null);
        Assert.Equal(subaddress1, subaddress2);
    }

    private static void TestTxWalletsEqualOnChain(List<MoneroTxWallet> txs1, List<MoneroTxWallet> txs2)
    {

        // nullify off-chain data for comparison
        var allTxs = new List<MoneroTxWallet>(txs1);

        allTxs.AddRange(txs2);

        foreach (MoneroTxWallet tx in allTxs)
        {
            tx.SetNote(null);
            if (tx.GetOutgoingTransfer() != null)
            {
                tx.GetOutgoingTransfer().SetAddresses(null);
            }
        }

        // compare txs
        Assert.Equal(txs1.Count, txs2.Count);
        foreach (MoneroTxWallet tx1 in txs1)
        {
            bool found = false;
            foreach (MoneroTxWallet tx2 in txs2)
            {
                if (tx1.GetHash().Equals(tx2.GetHash()))
                {

                    // transfer cached info if known for comparison
                    if (tx1.GetOutgoingTransfer() != null && tx1.GetOutgoingTransfer().GetDestinations() != null)
                    {
                        if (tx2.GetOutgoingTransfer() == null || tx2.GetOutgoingTransfer().GetDestinations() == null) TransferCachedInfo(tx1, tx2);
                    }
                    else if (tx2.GetOutgoingTransfer() != null && tx2.GetOutgoingTransfer().GetDestinations() != null)
                    {
                        TransferCachedInfo(tx2, tx1);
                    }

                    // test tx equality by merging
                    Assert.True(TestUtils.TxsMergeable(tx1, tx2), "Txs are not mergeable");
                    Assert.Equal(tx1, tx2);
                    found = true;

                    // test block equality except txs to ignore order
                    List<MoneroTx> blockTxs1 = tx1.GetBlock().GetTxs();
                    List<MoneroTx> blockTxs2 = tx2.GetBlock().GetTxs();
                    tx1.GetBlock().SetTxs(null);
                    tx2.GetBlock().SetTxs(null);
                    Assert.Equal(tx1.GetBlock(), tx2.GetBlock());
                    tx1.GetBlock().SetTxs(blockTxs1);
                    tx2.GetBlock().SetTxs(blockTxs2);
                }
            }
            Assert.True(found);  // each tx must have one and only one match
        }
    }

    private static void TransferCachedInfo(MoneroTxWallet src, MoneroTxWallet tgt)
    {

        // fill in missing incoming transfers when sending from/to the same account
        if (src.GetIncomingTransfers() != null)
        {
            foreach (MoneroIncomingTransfer inTransfer in src.GetIncomingTransfers())
            {
                if (inTransfer.GetAccountIndex() == src.GetOutgoingTransfer().GetAccountIndex())
                {
                    tgt.GetIncomingTransfers().Add(inTransfer);
                }
            }

            tgt.GetIncomingTransfers().Sort(new MoneroIncomingTransferComparer());
        }

        // transfer info to outgoing transfer
        if (tgt.GetOutgoingTransfer() == null) tgt.SetOutgoingTransfer(src.GetOutgoingTransfer());
        else
        {
            tgt.GetOutgoingTransfer().SetDestinations(src.GetOutgoingTransfer().GetDestinations());
            tgt.GetOutgoingTransfer().SetAmount(src.GetOutgoingTransfer().GetAmount());
        }

        // transfer payment id if outgoing // TODO: monero-wallet-rpc does not provide payment id for outgoing transfer when cache missing https://github.com/monero-project/monero/issues/8378
        if (tgt.GetOutgoingTransfer() != null) tgt.SetPaymentId(src.GetPaymentId());
    }

    private static void TestTransfersEqualOnChain(List<MoneroTransfer> transfers1, List<MoneroTransfer> transfers2)
    {
        Assert.Equal(transfers1.Count, transfers2.Count);

        // test and collect transfers per transaction
        Dictionary<string, List<MoneroTransfer>> txsTransfers1 = new();
        Dictionary<string, List<MoneroTransfer>> txsTransfers2 = new();
        ulong? lastHeight = null;
        MoneroTxWallet lastTx1 = null;
        MoneroTxWallet lastTx2 = null;
        for (int i = 0; i < transfers1.Count; i++)
        {
            MoneroTransfer transfer1 = transfers1[i];
            MoneroTransfer transfer2 = transfers2[i];

            // transfers must have same height even if they don't belong to same tx (because tx ordering within blocks is not currently provided by wallet2)
            Assert.Equal((long)transfer1.GetTx().GetHeight(), (long)transfer2.GetTx().GetHeight());

            // transfers must be in ascending order by height
            if (lastHeight == null) lastHeight = transfer1.GetTx().GetHeight();
            else Assert.True(lastHeight <= transfer1.GetTx().GetHeight());

            // transfers must be consecutive per transaction
            if (lastTx1 != transfer1.GetTx())
            {
                Assert.False(txsTransfers1.ContainsKey(transfer1.GetTx().GetHash()));  // cannot be seen before
                lastTx1 = transfer1.GetTx();
            }
            if (lastTx2 != transfer2.GetTx())
            {
                Assert.False(txsTransfers2.ContainsKey(transfer2.GetTx().GetHash()));  // cannot be seen before
                lastTx2 = transfer2.GetTx();
            }

            // collect tx1 transfer
            List<MoneroTransfer> txTransfers1 = txsTransfers1.GetValueOrDefault(transfer1.GetTx().GetHash());
            if (txTransfers1 == null)
            {
                txTransfers1 = new List<MoneroTransfer>();
                txsTransfers1.Add(transfer1.GetTx().GetHash(), txTransfers1);
            }
            txTransfers1.Add(transfer1);

            // collect tx2 transfer
            List<MoneroTransfer> txTransfers2 = txsTransfers2.GetValueOrDefault(transfer2.GetTx().GetHash());
            if (txTransfers2 == null)
            {
                txTransfers2 = new List<MoneroTransfer>();
                txsTransfers2.Add(transfer2.GetTx().GetHash(), txTransfers2);
            }
            txTransfers2.Add(transfer2);
        }

        // compare collected transfers per tx for equality
        foreach (string txHash in txsTransfers1.Keys)
        {
            List<MoneroTransfer> txTransfers1 = txsTransfers1.GetValueOrDefault(txHash, []);
            List<MoneroTransfer> txTransfers2 = txsTransfers2.GetValueOrDefault(txHash, []);
            Assert.Equal(txTransfers1.Count, txTransfers2.Count);

            // normalize and compare transfers
            for (int i = 0; i < txTransfers1.Count; i++)
            {
                MoneroTransfer transfer1 = txTransfers1[i];
                MoneroTransfer transfer2 = txTransfers2[i];

                // normalize outgoing transfers
                if (transfer1 is MoneroOutgoingTransfer)
                {
                    MoneroOutgoingTransfer ot1 = (MoneroOutgoingTransfer)transfer1;
                    MoneroOutgoingTransfer ot2 = (MoneroOutgoingTransfer)transfer2;

                    // transfer destination info if known for comparison
                    if (ot1.GetDestinations() != null)
                    {
                        if (ot2.GetDestinations() == null) TransferCachedInfo(ot1.GetTx(), ot2.GetTx());
                    }
                    else if (ot2.GetDestinations() != null)
                    {
                        TransferCachedInfo(ot2.GetTx(), ot1.GetTx());
                    }

                    // nullify other local wallet data
                    ot1.SetAddresses(null);
                    ot2.SetAddresses(null);
                }

                // normalize incoming transfers
                else
                {
                    MoneroIncomingTransfer it1 = (MoneroIncomingTransfer)transfer1;
                    MoneroIncomingTransfer it2 = (MoneroIncomingTransfer)transfer2;
                    it1.SetAddress(null);
                    it2.SetAddress(null);
                }

                // compare transfer equality
                Assert.Equal(transfer1, transfer2);
            }
        }
    }

    private static void TestOutputWalletsEqualOnChain(List<MoneroOutputWallet> outputs1, List<MoneroOutputWallet> outputs2)
    {
        Assert.Equal(outputs1.Count, outputs2.Count);

        // test and collect outputs per transaction
        Dictionary<string, List<MoneroOutputWallet>> txsOutputs1 = new();
        Dictionary<string, List<MoneroOutputWallet>> txsOutputs2 = new();
        ulong? lastHeight = null;
        MoneroTxWallet lastTx1 = null;
        MoneroTxWallet lastTx2 = null;
        for (int i = 0; i < outputs1.Count; i++)
        {
            MoneroOutputWallet output1 = outputs1[i];
            MoneroOutputWallet output2 = outputs2[i];

            // outputs must have same height even if they don't belong to same tx (because tx ordering within blocks is not currently provided by wallet2)
            Assert.Equal((long)output1.GetTx().GetHeight(), (long)output2.GetTx().GetHeight());

            // outputs must be in ascending order by height
            if (lastHeight == null) lastHeight = output1.GetTx().GetHeight();
            else Assert.True(lastHeight <= output1.GetTx().GetHeight());

            // outputs must be consecutive per transaction
            if (lastTx1 != output1.GetTx())
            {
                Assert.False(txsOutputs1.ContainsKey(output1.GetTx().GetHash()));  // cannot be seen before
                lastTx1 = output1.GetTx();
            }
            if (lastTx2 != output2.GetTx())
            {
                Assert.False(txsOutputs2.ContainsKey(output2.GetTx().GetHash()));  // cannot be seen before
                lastTx2 = output2.GetTx();
            }

            // collect tx1 output
            List<MoneroOutputWallet> txOutputs1 = txsOutputs1.GetValueOrDefault(output1.GetTx().GetHash(), []);
            if (txOutputs1 == null)
            {
                txOutputs1 = new List<MoneroOutputWallet>();
                txsOutputs1.Add(output1.GetTx().GetHash(), txOutputs1);
            }
            txOutputs1.Add(output1);

            // collect tx2 output
            List<MoneroOutputWallet> txOutputs2 = txsOutputs2.GetValueOrDefault(output2.GetTx().GetHash(), []);
            if (txOutputs2 == null)
            {
                txOutputs2 = new List<MoneroOutputWallet>();
                txsOutputs2.Add(output2.GetTx().GetHash(), txOutputs2);
            }
            txOutputs2.Add(output2);
        }

        // compare collected outputs per tx for equality
        foreach (string txHash in txsOutputs1.Keys)
        {
            List<MoneroOutputWallet> txOutputs1 = txsOutputs1.GetValueOrDefault(txHash, []);
            List<MoneroOutputWallet> txOutputs2 = txsOutputs2.GetValueOrDefault(txHash, []);
            Assert.Equal(txOutputs1.Count, txOutputs2.Count);

            // normalize and compare outputs
            for (int i = 0; i < txOutputs1.Count; i++)
            {
                MoneroOutput output1 = txOutputs1[i];
                MoneroOutput output2 = txOutputs2[i];
                Assert.Equal(output1.GetTx().GetHash(), output2.GetTx().GetHash());
                Assert.Equal(output1, output2);
            }
        }
    }

}
