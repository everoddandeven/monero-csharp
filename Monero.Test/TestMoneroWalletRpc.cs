using Monero.Common;
using Monero.Test.Utils;
using Monero.Wallet;
using Monero.Wallet.Common;

namespace Monero.Test;

public class MoneroWalletRpcFixture : MoneroWalletCommonFixture
{
    public MoneroWalletRpc walletRpc;

    public MoneroWalletRpcFixture() : base(TestUtils.GetWalletRpc(), TestUtils.GetDaemonRpc())
    {
        // Before All
        walletRpc = (MoneroWalletRpc)wallet;
    }
}

public class TestMoneroWalletRpc : TestMoneroWalletCommon, IClassFixture<MoneroWalletRpcFixture>
{
    public TestMoneroWalletRpc(MoneroWalletRpcFixture walletRpcFixture) : base(walletRpcFixture)
    {

    }

    #region Base Method Overrides

    protected override void CloseWallet(MoneroWallet wallet, bool save)
    {
        MoneroWalletRpc walletRpc = (MoneroWalletRpc)wallet;
        walletRpc.Close(save);
        try
        {
            TestUtils.StopWalletRpcProcess(walletRpc);
        }
        catch (Exception e)
        {
            throw new Exception("An error occurred while stopping monero wallet rpc process", e);
        }
    }

    protected override MoneroWalletRpc CreateWallet(MoneroWalletConfig config)
    {
        // assign defaults
        if (config == null) config = new MoneroWalletConfig();
        bool random = config.GetSeed() == null && config.GetPrimaryAddress() == null;
        if (config.GetPath() == null) config.SetPath(GenUtils.GetGuid());
        if (config.GetPassword() == null) config.SetPassword(TestUtils.WALLET_PASSWORD);
        if (config.GetRestoreHeight() == null && !random) config.SetRestoreHeight(0l);
        if (config.GetServer() == null && config.GetConnectionManager() == null) config.SetServer(daemon.GetRpcConnection());

        // create client connected to internal monero-wallet-rpc process
        bool offline = MoneroUtils.ParseUri(TestUtils.OFFLINE_SERVER_URI).ToString().Equals(config.GetServerUri());
        MoneroWalletRpc wallet = TestUtils.StartWalletRpcProcess(offline);

        // create wallet
        try
        {
            wallet.CreateWallet(config);
            wallet.SetDaemonConnection(wallet.GetDaemonConnection(), true, null); // set daemon as trusted
            if (wallet.IsConnectedToDaemon()) wallet.StartSyncing(TestUtils.SYNC_PERIOD_IN_MS_ULONG);
            return wallet;
        }
        catch (MoneroError e)
        {
            try { TestUtils.StopWalletRpcProcess(wallet); } catch (Exception e2) { throw new Exception("An error occurred while stopping monero wallet rpc process", e2); }
            throw;
        }
    }

    protected override List<string> GetSeedLanguages()
    {
        return ((MoneroWalletRpc)wallet).GetSeedLanguages();
    }

    protected override MoneroWallet GetTestWallet()
    {
        return TestUtils.GetWalletRpc();
    }

    protected override MoneroWallet OpenWallet(MoneroWalletConfig config)
    {
        // assign defaults
        if (config == null) config = new MoneroWalletConfig();
        if (config.GetPassword() == null) config.SetPassword(TestUtils.WALLET_PASSWORD);
        if (config.GetServer() == null && config.GetConnectionManager() == null) config.SetServer(daemon.GetRpcConnection());

        // create client connected to internal monero-wallet-rpc process
        bool offline = TestUtils.OFFLINE_SERVER_URI.Equals(config.GetServerUri());
        MoneroWalletRpc wallet = TestUtils.StartWalletRpcProcess(offline);

        // open wallet
        try
        {
            wallet.OpenWallet(config);
            wallet.SetDaemonConnection(wallet.GetDaemonConnection(), true, null); // set daemon as trusted
            if (wallet.IsConnectedToDaemon()) wallet.StartSyncing((ulong)TestUtils.SYNC_PERIOD_IN_MS);
            return wallet;
        }
        catch (MoneroError e)
        {
            try { TestUtils.StopWalletRpcProcess(wallet); } catch (Exception e2) { throw new Exception(e2.Message); }
            throw;
        }
    }

    protected override void DisposeInternal()
    {
        base.DisposeInternal();

        foreach (MoneroWalletRpc walletRpc in TestUtils.WALLET_PORT_OFFSETS.Keys)
        {
            try
            {
                TestUtils.StopWalletRpcProcess(walletRpc);
            }
            catch (Exception e)
            {
                throw new Exception("An error occurred while stopping monero wallet rpc process", e);
            }
        }

    }

    #endregion

    #region Tests

    // Can create a wallet with a randomly generated seed
    [Fact]
    public void TestCreateWalletRandomRpc()
    {
        Assert.True(TEST_NON_RELAYS);

        // Create random wallet with defaults
        string path = GenUtils.GetGuid();
        MoneroWallet newWallet = CreateWallet(new MoneroWalletConfig().SetPath(path));
        string seed = newWallet.GetSeed();
        MoneroUtils.ValidateMnemonic(seed);
        Assert.NotEqual(TestUtils.SEED, seed);
        MoneroUtils.ValidateAddress(newWallet.GetPrimaryAddress(), TestUtils.NETWORK_TYPE);
        newWallet.Sync();  // very quick because restore height is chain height
        CloseWallet(newWallet);

        // Create random wallet with non defaults
        path = GenUtils.GetGuid();
        newWallet = CreateWallet(new MoneroWalletConfig().SetPath(path).SetLanguage("Spanish"));
        MoneroUtils.ValidateMnemonic(newWallet.GetSeed());
        Assert.NotEqual(seed, newWallet.GetSeed());
        seed = newWallet.GetSeed();
        MoneroUtils.ValidateAddress(newWallet.GetPrimaryAddress(), TestUtils.NETWORK_TYPE);

        // attempt to Create wallet which already exists
        try
        {
            CreateWallet(new MoneroWalletConfig().SetPath(path).SetLanguage("Spanish"));
        }
        catch (MoneroError e)
        {
            Assert.Equal(e.Message, "Wallet already exists: " + path);
            Assert.Equal(-21, (int)e.GetCode());
            Assert.Equal(seed, newWallet.GetSeed());
        }
        CloseWallet(newWallet);
    }

    // Can create a RPC wallet from a seed
    [Fact]
    public void TestCreateWalletFromSeedRpc()
    {
        Assert.True(TEST_NON_RELAYS);

        // Create wallet with seed and defaults
        string path = GenUtils.GetGuid();
        MoneroWallet wallet = CreateWallet(new MoneroWalletConfig().SetPath(path).SetSeed(TestUtils.SEED).SetRestoreHeight(TestUtils.FIRST_RECEIVE_HEIGHT));
        Assert.Equal(TestUtils.SEED, wallet.GetSeed());
        Assert.Equal(TestUtils.ADDRESS, wallet.GetPrimaryAddress());
        wallet.Sync();
        Assert.Equal(daemon.GetHeight(), wallet.GetHeight());
        List<MoneroTxWallet> txs = wallet.GetTxs();
        Assert.False(txs.Count == 0); // wallet is used
        Assert.Equal(TestUtils.FIRST_RECEIVE_HEIGHT, (ulong)txs[0].GetHeight());
        CloseWallet(wallet); // TODO: monero-wallet-rpc: if wallet is not closed, primary address will not change

        // Create wallet with non-defaults
        path = GenUtils.GetGuid();
        wallet = CreateWallet(new MoneroWalletConfig().SetPath(path).SetSeed(TestUtils.SEED).SetRestoreHeight(TestUtils.FIRST_RECEIVE_HEIGHT).SetLanguage("German").SetSeedOffset("my offset!").SetSaveCurrent(false));
        MoneroUtils.ValidateMnemonic(wallet.GetSeed());
        Assert.NotEqual(TestUtils.SEED, wallet.GetSeed()); // seed is different because of offset
        Assert.NotEqual(TestUtils.ADDRESS, wallet.GetPrimaryAddress());
        wallet.Sync();
        Assert.Equal(daemon.GetHeight(), wallet.GetHeight());
        Assert.True(wallet.GetTxs().Count == 0);  // wallet is not used
        CloseWallet(wallet);
    }

    // Can open wallets
    [Fact]
    public void TestOpenWallet()
    {
        Assert.True(TEST_NON_RELAYS);

        // create names of test wallets
        int numTestWallets = 3;
        List<string> names = new List<string>();
        for (int i = 0; i < numTestWallets; i++) names.Add(GenUtils.GetGuid());

        // create test wallets
        List<string> seeds = new List<string>();
        foreach (string name in names)
        {
            MoneroWalletRpc wallet = CreateWallet(new MoneroWalletConfig().SetPath(name));
            seeds.Add(wallet.GetSeed());
            CloseWallet(wallet, true);
        }

        // open test wallets
        List<MoneroWallet> wallets = new List<MoneroWallet>();
        for (int i = 0; i < numTestWallets; i++)
        {
            MoneroWallet wallet = OpenWallet(new MoneroWalletConfig().SetPath(names[i]).SetPassword(TestUtils.WALLET_PASSWORD));
            Assert.Equal(seeds[i], wallet.GetSeed());
            wallets.Add(wallet);
        }

        // attempt to re-open already opened wallet
        try
        {
            OpenWallet(new MoneroWalletConfig().SetPath(names[numTestWallets - 1]).SetPassword(TestUtils.WALLET_PASSWORD));
            Assert.Fail("Cannot open wallet which is already open");
        }
        catch (MoneroError e)
        {
            Assert.Equal(-1, (int)e.GetCode()); // -1 indicates wallet does not exist (or is open by another app)
        }

        // attempt to open non-existent
        try
        {
            OpenWallet(new MoneroWalletConfig().SetPath("btc_integrity").SetPassword(TestUtils.WALLET_PASSWORD));
            Assert.Fail("Cannot open non-existent wallet");
        }
        catch (MoneroError e)
        {
            Assert.Equal(-1, (int)e.GetCode());
        }

        // Close wallets
        foreach (MoneroWallet wallet in wallets) CloseWallet(wallet);
    }

    // Can indicate if multisig import is needed for correct balance information
    [Fact]
    public void TestIsMultisigNeeded()
    {
        Assert.True(TEST_NON_RELAYS);
        Assert.False(wallet.IsMultisigImportNeeded()); // TODO: test with multisig wallet
    }

    // Can tag accounts and query accounts by tag
    [Fact]
    public void TestAccountTags()
    {
        Assert.True(TEST_NON_RELAYS);

        // get accounts
        List<MoneroAccount> accounts = wallet.GetAccounts();
        Assert.True(accounts.Count >= 3, "Not enough accounts to test; run create account test");

        // tag some of the accounts
        MoneroAccountTag tag = new MoneroAccountTag("my_tag_" + GenUtils.GetGuid(), "my tag label", [0, 1]);
        wallet.TagAccounts(tag.GetTag(), tag.GetAccountIndices());

        // query accounts by tag
        List<MoneroAccount> taggedAccounts = wallet.GetAccounts(false, tag.GetTag());
        Assert.Equal(2, taggedAccounts.Count);
        Assert.Equal(0, (int)taggedAccounts[0].GetIndex());
        Assert.Equal(tag.GetTag(), taggedAccounts[0].GetTag());
        Assert.Equal(1, (int)taggedAccounts[1].GetIndex());
        Assert.Equal(tag.GetTag(), taggedAccounts[1].GetTag());

        // set tag label
        wallet.SetAccountTagLabel(tag.GetTag(), tag.GetLabel());

        // fetch tags and ensure new tag is contained
        List<MoneroAccountTag> tags = wallet.GetAccountTags();
        Assert.NotNull(tags.Find(x => x.Equals(tag)));

        // re-tag an account
        MoneroAccountTag tag2 = new MoneroAccountTag("my_tag_" + GenUtils.GetGuid(), "my tag label 2", [1]);
        wallet.TagAccounts(tag2.GetTag(), tag2.GetAccountIndices());
        List<MoneroAccount> taggedAccounts2 = wallet.GetAccounts(false, tag2.GetTag());
        Assert.Single(taggedAccounts2);
        Assert.True(1 == taggedAccounts2[0].GetIndex());
        Assert.Equal(tag2.GetTag(), taggedAccounts2[0].GetTag());

        // re-query original tag which only applies to one account now
        taggedAccounts = wallet.GetAccounts(false, tag.GetTag());
        Assert.Single(taggedAccounts);
        Assert.Equal(0, (int)taggedAccounts[0].GetIndex());
        Assert.Equal(tag.GetTag(), taggedAccounts[0].GetTag());

        // untag and query accounts
        wallet.UntagAccounts([0, 1]);
        Assert.Empty(wallet.GetAccountTags());
        try
        {
            wallet.GetAccounts(false, tag.GetTag());
            Assert.Fail("Should have thrown exception with unregistered tag");
        }
        catch (MoneroError e)
        {
            Assert.Equal(-1, (int)e.GetCode());
        }

        // test that non-existing tag returns no accounts
        try
        {
            wallet.GetAccounts(false, "non_existing_tag");
            Assert.Fail("Should have thrown exception with unregistered tag");
        }
        catch (MoneroError e)
        {
            Assert.Equal(-1, (int)e.GetCode());
        }
    }

    // Can get addresses out of range of used accounts and subaddresses
    [Fact]
    public override void TestGetSubaddressAddressOutOfRange()
    {
        Assert.True(TEST_NON_RELAYS);
        List<MoneroAccount> accounts = wallet.GetAccounts(true);
        int accountIdx = accounts.Count - 1;
        int subaddressIdx = accounts[accountIdx].GetSubaddresses().Count;
        string address = wallet.GetAddress((uint)accountIdx, (uint)subaddressIdx);
        Assert.Null(address);
    }

    // Can save the wallet
    [Fact]
    public void TestSave()
    {
        Assert.True(TEST_NON_RELAYS);
        wallet.Save();
    }

    // Can close a wallet
    [Fact]
    public void TestClose()
    {
        Assert.True(TEST_NON_RELAYS);

        // create a test wallet
        string path = GenUtils.GetGuid();
        MoneroWalletRpc wallet = CreateWallet(new MoneroWalletConfig().SetPath(path));
        wallet.Sync();

        // close the wallet
        wallet.Close();

        // attempt to interact with the wallet
        try
        {
            wallet.GetHeight();
        }
        catch (MoneroError e)
        {
            Assert.Equal(-13, (int)e.GetCode());
            Assert.Equal("No wallet file", e.Message);
        }
        try
        {
            wallet.GetSeed();
        }
        catch (MoneroError e)
        {
            Assert.Equal(-13, (int)e.GetCode());
            Assert.Equal("No wallet file", e.Message);
        }
        try
        {
            wallet.Sync();
        }
        catch (MoneroError e)
        {
            Assert.Equal(-13, (int)e.GetCode());
            Assert.Equal("No wallet file", e.Message);
        }

        // re-open the wallet
        wallet.OpenWallet(path, TestUtils.WALLET_PASSWORD);
        wallet.Sync();
        Assert.Equal(daemon.GetHeight(), wallet.GetHeight());

        // close the wallet
        CloseWallet(wallet, true);
    }

    // Can stop the RPC server
    [Fact(Skip = "Disabled so server not actually stopped")]
    public void TestStop()
    {
        ((MoneroWalletRpc)wallet).Stop();
    }

    [Fact(Skip = "Disabled because importing key images deletes corresponding incoming transfers: https://github.com/monero-project/monero/issues/5812")]
    public override void TestImportKeyImages()
    {
        base.TestImportKeyImages();
    }

    [Fact(Skip = "monero-wallet-rpc does not support getting a height by date")]
    public override void TestGetHeightByDate()
    {
        base.TestGetHeightByDate();
    }

    [Fact(Skip = "monero-wallet-rpc does not support getting seed language")]
    public override void TestGetSeedLanguage()
    {
        base.TestGetSeedLanguage();
    }

    [Fact(Skip = "monero-wallet-rpc doesn't support getting public view key")]
    public override void TestGetPublicViewKey()
    {
        base.TestGetPublicViewKey();
    }

    [Fact(Skip = "monero-wallet-rpc doesn't support getting public spend key")]
    public override void TestGetPublicSpendKey()
    {
        base.TestGetPublicSpendKey();
    }

    [Fact(Skip = "monero-wallet-rpc does not support creating wallets with subaddress lookahead over rpc")]
    public override void TestSubaddressLookahead()
    {
        base.TestSubaddressLookahead();
    }

    #endregion

    #region RPC Specific Tests

    protected override void TestTxWallet(MoneroTxWallet tx, TxContext? ctx = null)
    {
        ctx = new TxContext(ctx);

        // run common tests
        base.TestTxWallet(tx, ctx);
    }

    protected override void TestInvalidTxHashError(MoneroError e)
    {
        base.TestInvalidTxHashError(e);
        Assert.Equal(-8, (int)e.GetCode());
    }

    protected override void TestInvalidTxKeyError(MoneroError e)
    {
        base.TestInvalidTxKeyError(e);
        Assert.Equal(-25, (int)e.GetCode());
    }

    protected override void TestInvalidAddressError(MoneroError e)
    {
        base.TestInvalidAddressError(e);
        Assert.Equal(-2, (int)e.GetCode());
    }

    protected override void TestInvalidSignatureError(MoneroError e)
    {
        base.TestInvalidSignatureError(e);
        Assert.Equal(-1, (int)e.GetCode()); // TODO: sometimes comes back bad, sometimes throws exception.  ensure txs come from different addresses?
    }

    protected override void TestNoSubaddressError(MoneroError e)
    {
        base.TestNoSubaddressError(e);
        Assert.Equal(-1, (int)e.GetCode());
    }

    protected override void TestSignatureHeaderCheckError(MoneroError e)
    {
        base.TestSignatureHeaderCheckError(e);
        Assert.Equal(-1, (int)e.GetCode());
    }

    #endregion

}
