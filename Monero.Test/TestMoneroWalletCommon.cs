using Monero.Common;
using Monero.Daemon;
using Monero.Daemon.Common;
using Monero.Test.Utils;
using Monero.Wallet;
using Monero.Wallet.Common;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Monero.Test
{
    public abstract class TestMoneroWalletCommon
    {
        // test constants
        protected static readonly bool LITE_MODE = false;
        protected static readonly bool TEST_NON_RELAYS = true;
        protected static readonly bool TEST_RELAYS = true;
        protected static readonly bool TEST_NOTIFICATIONS = true;
        protected static readonly bool TEST_RESETS = false;
        private static readonly int MAX_TX_PROOFS = 25; // maximum number of transactions to _check for each proof, undefined to _check all
        private static readonly int SEND_MAX_DIFF = 60;
        private static readonly int SEND_DIVISOR = 10;
        private static readonly ulong NUM_BLOCKS_LOCKED = 10;

        // instance variables
        protected MoneroWallet wallet = new MoneroWalletRpc("");        // wallet instance to test
        protected MoneroDaemonRpc daemon;     // daemon instance to test

        protected MoneroDaemonRpc GetTestDaemon() { return TestUtils.GetDaemonRpc(); }
        protected abstract MoneroWallet GetTestWallet();
        protected MoneroWallet OpenWallet(string path, string password) { return OpenWallet(new MoneroWalletConfig().SetPath(path).SetPassword(password)); }
        protected abstract MoneroWallet OpenWallet(MoneroWalletConfig config);
        protected abstract MoneroWallet CreateWallet(MoneroWalletConfig config);
        protected void CloseWallet(MoneroWallet wallet) { CloseWallet(wallet, false); }
        protected abstract void CloseWallet(MoneroWallet wallet, bool save);
        protected abstract List<string> GetSeedLanguages();

        protected TestMoneroWalletCommon() {
            // stop mining
            MoneroMiningStatus status = daemon.GetMiningStatus();
            if (status.IsActive() == true) wallet.StopMining();
        }

        #region Begin Tests

        // Can create a random wallet
        [Fact]
        public void TestCreateWalletRandom()
        {
            Assert.True(TEST_NON_RELAYS);
            Exception? e1 = null;  // emulating Java "finally" but compatible with other languages
            try
            {

                // create random wallet
                MoneroWallet wallet = CreateWallet(new MoneroWalletConfig());
                string path = wallet.GetPath();
                Exception e2 = null;
                try
                {
                    MoneroUtils.ValidateAddress(wallet.GetPrimaryAddress(), TestUtils.NETWORK_TYPE);
                    MoneroUtils.ValidatePrivateViewKey(wallet.GetPrivateViewKey());
                    MoneroUtils.ValidatePrivateSpendKey(wallet.GetPrivateSpendKey());
                    MoneroUtils.ValidateMnemonic(wallet.GetSeed());
                    if (wallet.GetWalletType() != MoneroWalletType.RPC) Assert.True(MoneroWallet.DEFAULT_LANGUAGE == wallet.GetSeedLanguage());  // TODO monero-wallet-rpc: get seed language
                }
                catch (Exception e)
                {
                    e2 = e;
                }
                CloseWallet(wallet);
                if (e2 != null) throw e2;

                // attempt to create wallet at same path
                try
                {
                    CreateWallet(new MoneroWalletConfig().SetPath(path));
                    throw new Exception("Should have thrown error");
                }
                catch (Exception e)
                {
                    Assert.True("Wallet already exists: " + path == e.Message);
                }

                // attempt to create wallet with unknown language
                try
                {
                    CreateWallet(new MoneroWalletConfig().SetLanguage("english")); // TODO: support lowercase?
                    throw new Exception("Should have thrown error");
                }
                catch (Exception e)
                {
                    Assert.True("Unknown language: english" == e.Message);
                }
            }
            catch (Exception e)
            {
                e1 = e;
            }

            if (e1 != null) throw new Exception(e1.Message);
        }

        // Can create a wallet from a seed.
        [Fact]
        public void TestCreateWalletFromSeed()
        {
            Assert.True(TEST_NON_RELAYS);
            Exception? e1 = null;  // emulating Java "finally" but compatible with other languages
            try
            {

                // save for comparison
                string primaryAddress = this.wallet.GetPrimaryAddress();
                string privateViewKey = this.wallet.GetPrivateViewKey();
                string privateSpendKey = this.wallet.GetPrivateSpendKey();

                // recreate test wallet from seed
                MoneroWallet wallet = CreateWallet(new MoneroWalletConfig().SetSeed(TestUtils.SEED).SetRestoreHeight(TestUtils.FIRST_RECEIVE_HEIGHT));
                string path = wallet.GetPath();
                Exception e2 = null;
                try
                {
                    Assert.True(primaryAddress == wallet.GetPrimaryAddress());
                    Assert.True(privateViewKey == wallet.GetPrivateViewKey());
                    Assert.True(privateSpendKey == wallet.GetPrivateSpendKey());
                    Assert.True(TestUtils.SEED == wallet.GetSeed());
                    if (wallet.GetWalletType() != MoneroWalletType.RPC) Assert.True(MoneroWallet.DEFAULT_LANGUAGE == wallet.GetSeedLanguage());
                }
                catch (Exception e)
                {
                    e2 = e;
                }
                CloseWallet(wallet);
                if (e2 != null) throw e2;

                // attempt to create wallet with two missing words
                try
                {
                    string invalidMnemonic = "memoir desk algebra inbound innocent unplugs fully okay five inflamed giant factual ritual toyed topic snake unhappy guarded tweezers haunted inundate giant";
                    wallet = CreateWallet(new MoneroWalletConfig().SetSeed(invalidMnemonic).SetRestoreHeight(TestUtils.FIRST_RECEIVE_HEIGHT));
                }
                catch (Exception e)
                {
                    Assert.True("Invalid mnemonic" == e.Message);
                }

                // attempt to create wallet at same path
                try
                {
                    CreateWallet(new MoneroWalletConfig().SetPath(path));
                    throw new Exception("Should have thrown error");
                }
                catch (Exception e)
                {
                    Assert.True("Wallet already exists: " + path == e.Message);
                }
            }
            catch (Exception e)
            {
                e1 = e;
            }

            if (e1 != null) throw new Exception(e1.Message);
        }

        // Can create a wallet from a seed with a seed offset
        [Fact]
        public void TestCreateWalletFromSeedWithOffset()
        {
            Assert.True(TEST_NON_RELAYS);
            Exception e1 = null;  // emulating Java "finally" but compatible with other languages
            try
            {

                // create test wallet with offset
                MoneroWallet wallet = CreateWallet(new MoneroWalletConfig().SetSeed(TestUtils.SEED).SetRestoreHeight(TestUtils.FIRST_RECEIVE_HEIGHT).SetSeedOffset("my secret offset!"));
                Exception e2 = null;
                try
                {
                    MoneroUtils.ValidateMnemonic(wallet.GetSeed());
                    Assert.True(TestUtils.SEED == wallet.GetSeed());
                    MoneroUtils.ValidateAddress(wallet.GetPrimaryAddress(), TestUtils.NETWORK_TYPE);
                    Assert.True(TestUtils.ADDRESS != wallet.GetPrimaryAddress());
                    if (wallet.GetWalletType() != MoneroWalletType.RPC) Assert.True(MoneroWallet.DEFAULT_LANGUAGE == wallet.GetSeedLanguage());  // TODO monero-wallet-rpc: support
                }
                catch (Exception e)
                {
                    e2 = e;
                }
                CloseWallet(wallet);
                if (e2 != null) throw e2;
            }
            catch (Exception e)
            {
                e1 = e;
            }

            if (e1 != null) throw new Exception(e1.Message);
        }

        // Can create a wallet from keys
        [Fact]
        public void TestCreateWalletFromKeys()
        {
            Assert.True(TEST_NON_RELAYS);
            Exception e1 = null; // emulating Java "finally" but compatible with other languages
            try
            {

                // save for comparison
                string primaryAddress = this.wallet.GetPrimaryAddress();
                string privateViewKey = this.wallet.GetPrivateViewKey();
                string privateSpendKey = this.wallet.GetPrivateSpendKey();

                // recreate test wallet from keys
                MoneroWallet wallet = CreateWallet(new MoneroWalletConfig().SetPrimaryAddress(primaryAddress).SetPrivateViewKey(privateViewKey).SetPrivateSpendKey(privateSpendKey).SetRestoreHeight(daemon.GetHeight()));
                string path = wallet.GetPath();
                Exception e2 = null;
                try
                {
                    Assert.True(primaryAddress == wallet.GetPrimaryAddress());
                    Assert.True(privateViewKey == wallet.GetPrivateViewKey());
                    Assert.True(privateSpendKey == wallet.GetPrivateSpendKey());
                    if (!wallet.IsConnectedToDaemon()) MoneroUtils.Log(0, "WARNING: wallet created from keys is not connected to authenticated daemon"); // TODO monero-project: keys wallets not connected
                    Assert.True(wallet.IsConnectedToDaemon(), "Wallet created from keys is not connected to authenticated daemon");
                    if (wallet.GetWalletType() != MoneroWalletType.RPC)
                    {
                        MoneroUtils.ValidateMnemonic(wallet.GetSeed()); // TODO monero-wallet-rpc: cannot get seed from wallet created from keys?
                        Assert.True(MoneroWallet.DEFAULT_LANGUAGE == wallet.GetSeedLanguage());
                    }
                }
                catch (Exception e)
                {
                    e2 = e;
                }
                CloseWallet(wallet);
                if (e2 != null) throw e2;

                // recreate test wallet from spend _key
                if (wallet.GetWalletType() != MoneroWalletType.RPC)
                { // TODO monero-wallet-rpc: cannot create wallet from spend _key?
                    wallet = CreateWallet(new MoneroWalletConfig().SetPrivateSpendKey(privateSpendKey).SetRestoreHeight(daemon.GetHeight()));
                    e2 = null;
                    try
                    {
                        Assert.True(primaryAddress == wallet.GetPrimaryAddress());
                        Assert.True(privateViewKey == wallet.GetPrivateViewKey());
                        Assert.True(privateSpendKey == wallet.GetPrivateSpendKey());
                        if (!wallet.IsConnectedToDaemon()) MoneroUtils.Log(0, "WARNING: wallet created from keys is not connected to authenticated daemon"); // TODO monero-project: keys wallets not connected
                        Assert.True(wallet.IsConnectedToDaemon(), "Wallet created from keys is not connected to authenticated daemon");
                        if (wallet.GetWalletType() != MoneroWalletType.RPC)
                        {
                            MoneroUtils.ValidateMnemonic(wallet.GetSeed()); // TODO monero-wallet-rpc: cannot get seed from wallet created from keys?
                            Assert.True(MoneroWallet.DEFAULT_LANGUAGE == wallet.GetSeedLanguage());
                        }
                    }
                    catch (Exception e)
                    {
                        e2 = e;
                    }
                    CloseWallet(wallet);
                    if (e2 != null) throw e2;
                }

                // attempt to create wallet at same path
                try
                {
                    CreateWallet(new MoneroWalletConfig().SetPath(path));
                    throw new Exception("Should have thrown error");
                }
                catch (Exception e)
                {
                    Assert.True("Wallet already exists: " + path == e.Message);
                }
            }
            catch (Exception e)
            {
                e1 = e;
            }

            if (e1 != null) throw new Exception(e1.Message);
        }

        // Can create wallets with subaddress lookahead
        [Fact]
        public void TestSubaddressLookahead()
        {
            Assert.True(TEST_NON_RELAYS);
            Exception? e1 = null;  // emulating Java "finally" but compatible with other languages
            MoneroWallet? receiver = null;
            try
            {

                // create wallet with high subaddress lookahead
                receiver = CreateWallet(new MoneroWalletConfig().SetAccountLookahead(1).SetSubaddressLookahead(100000));

                // transfer funds to subaddress with high index
                wallet.CreateTx(new MoneroTxConfig()
                        .SetAccountIndex(0)
                        .AddDestination(receiver.GetSubaddress(0, 85000).GetAddress(), TestUtils.MAX_FEE)
                        .SetRelay(true));

                // observe unconfirmed funds
                Thread.Sleep(1000);
                receiver.Sync();
                Assert.True(receiver.GetBalance().CompareTo(0) > 0);
            }
            catch (Exception e)
            {
                e1 = e;
            }

            if (receiver != null) CloseWallet(receiver);
            if (e1 != null) throw new Exception(e1.Message);
        }

        // Can get the wallet's version
        [Fact]
        public void TestGetVersion()
        {
            Assert.True(TEST_NON_RELAYS);
            MoneroVersion version = wallet.GetVersion();
            Assert.True(version.GetNumber() != null);
            Assert.True(version.GetNumber() > 0);
            Assert.True(version.IsRelease() != null);
        }

        // Can get the wallet's path
        [Fact]
        public void TestGetPath()
        {
            Assert.True(TEST_NON_RELAYS);

            // create random wallet
            MoneroWallet wallet = CreateWallet(new MoneroWalletConfig());

            // set a random attribute
            string uuid = GenUtils.GetUUID();
            wallet.SetAttribute("uuid", uuid);

            // record the wallet's path then save and close
            string path = wallet.GetPath();
            CloseWallet(wallet, true);

            // re-open the wallet using its path
            wallet = OpenWallet(path, null);

            // test the attribute
            Assert.True(uuid == wallet.GetAttribute("uuid"));
            CloseWallet(wallet);
        }

        // Can set the daemon connection
        [Fact]
        public void TestSetDaemonConnection()
        {
            // create random wallet with default daemon connection
            MoneroWallet wallet = CreateWallet(new MoneroWalletConfig());
            Assert.True(new MoneroRpcConnection(TestUtils.DAEMON_RPC_URI, TestUtils.DAEMON_RPC_USERNAME, TestUtils.DAEMON_RPC_PASSWORD).Equals(wallet.GetDaemonConnection()));
            Assert.True(wallet.IsConnectedToDaemon()); // uses default localhost connection

            // set empty server uri
            wallet.SetDaemonConnection("");
            Assert.Null(wallet.GetDaemonConnection());
            Assert.False(wallet.IsConnectedToDaemon());

            // set offline server uri
            wallet.SetDaemonConnection(TestUtils.OFFLINE_SERVER_URI);
            Assert.True(new MoneroRpcConnection(TestUtils.OFFLINE_SERVER_URI, "", "").Equals(wallet.GetDaemonConnection()));
            Assert.False(wallet.IsConnectedToDaemon());

            // set daemon with wrong credentials
            wallet.SetDaemonConnection(TestUtils.DAEMON_RPC_URI, "wronguser", "wrongpass");
            Assert.True(new MoneroRpcConnection(TestUtils.DAEMON_RPC_URI, "wronguser", "wrongpass").Equals(wallet.GetDaemonConnection()));
            if ("".Equals(TestUtils.DAEMON_RPC_USERNAME) || TestUtils.DAEMON_RPC_USERNAME == null) Assert.True(wallet.IsConnectedToDaemon()); // TODO: monerod without authentication works with bad credentials?
            else Assert.False(wallet.IsConnectedToDaemon());

            // set daemon with authentication
            wallet.SetDaemonConnection(TestUtils.DAEMON_RPC_URI, TestUtils.DAEMON_RPC_USERNAME, TestUtils.DAEMON_RPC_PASSWORD);
            Assert.True(new MoneroRpcConnection(TestUtils.DAEMON_RPC_URI, TestUtils.DAEMON_RPC_USERNAME, TestUtils.DAEMON_RPC_PASSWORD).Equals(wallet.GetDaemonConnection()));
            Assert.True(wallet.IsConnectedToDaemon());

            // nullify daemon connection
            wallet.SetDaemonConnection((string)null);
            Assert.Null(wallet.GetDaemonConnection());
            wallet.SetDaemonConnection(TestUtils.DAEMON_RPC_URI);
            Assert.True(new MoneroRpcConnection(TestUtils.DAEMON_RPC_URI).Equals(wallet.GetDaemonConnection()));
            wallet.SetDaemonConnection((MoneroRpcConnection)null);
            Assert.Null(wallet.GetDaemonConnection());

            // set daemon uri to non-daemon
            wallet.SetDaemonConnection("www.Getmonero.org");
            Assert.True(new MoneroRpcConnection("www.Getmonero.org").Equals(wallet.GetDaemonConnection()));
            Assert.False(wallet.IsConnectedToDaemon());

            // set daemon to invalid uri
            wallet.SetDaemonConnection("abc123");
            Assert.False(wallet.IsConnectedToDaemon());

            // attempt to sync
            try
            {
                wallet.Sync();
                throw new Exception("Exception expected");
            }
            catch (MoneroError e)
            {
                Assert.True("Wallet is not connected to daemon" == e.Message);
            }
            finally
            {
                CloseWallet(wallet);
            }
        }

        // Can use a connection manager
        [Fact]
        public void TestConnectionManager()
        {

            // create connection manager with monerod connections
            MoneroConnectionManager connectionManager = new MoneroConnectionManager();
            MoneroRpcConnection connection1 = new MoneroRpcConnection(TestUtils.GetDaemonRpc().GetRpcConnection()).SetPriority(1);
            MoneroRpcConnection connection2 = new MoneroRpcConnection("localhost:48081").SetPriority(2);
            connectionManager.SetConnection(connection1);
            connectionManager.AddConnection(connection2);

            // create wallet with connection manager
            MoneroWallet wallet = CreateWallet(new MoneroWalletConfig().SetServerUri("").SetConnectionManager(connectionManager));
            Assert.True(TestUtils.GetDaemonRpc().GetRpcConnection() == wallet.GetDaemonConnection());
            Assert.True(wallet.IsConnectedToDaemon());

            // set manager's connection
            connectionManager.SetConnection(connection2);
            Thread.Sleep(TestUtils.AUTO_CONNECT_TIMEOUT_MS);
            Assert.True(connection2 == wallet.GetDaemonConnection());

            // reopen wallet with connection manager
            string path = wallet.GetPath();
            CloseWallet(wallet);
            wallet = OpenWallet(new MoneroWalletConfig().SetServerUri("").SetConnectionManager(connectionManager).SetPath(path));
            Assert.True(connection2 == wallet.GetDaemonConnection());

            // disconnect
            connectionManager.SetConnection((string)null);
            Assert.Null(wallet.GetDaemonConnection());
            Assert.False(wallet.IsConnectedToDaemon());

            // start polling connections
            connectionManager.StartPolling((ulong)TestUtils.SYNC_PERIOD_IN_MS);

            // test that wallet auto connects
            Thread.Sleep(TestUtils.AUTO_CONNECT_TIMEOUT_MS);
            Assert.True(connection1.Equals(wallet.GetDaemonConnection()));
            Assert.True(wallet.IsConnectedToDaemon());

            // test override with bad connection
            wallet.AddListener(new MoneroWalletListener());
            connectionManager.SetAutoSwitch(false);
            connectionManager.SetConnection("http://foo.bar.xyz");
            Assert.True("http://foo.bar.xyz" == wallet.GetDaemonConnection().GetUri());
            Assert.False(wallet.IsConnectedToDaemon());
            Thread.Sleep(5000);
            Assert.False(wallet.IsConnectedToDaemon());

            // set to another connection manager
            MoneroConnectionManager connectionManager2 = new MoneroConnectionManager();
            connectionManager2.SetConnection(connection2);
            wallet.SetConnectionManager(connectionManager2);
            Assert.True(connection2 == wallet.GetDaemonConnection());

            // unset connection manager
            wallet.SetConnectionManager(null);
            Assert.Null(wallet.GetConnectionManager());
            Assert.True(connection2 == wallet.GetDaemonConnection());

            // stop polling and close
            connectionManager.StopPolling();
            CloseWallet(wallet);
        }

        // Can get the seed
        [Fact]
        public void TestGetSeed()
        {
            Assert.True(TEST_NON_RELAYS);
            string seed = wallet.GetSeed();
            MoneroUtils.ValidateMnemonic(seed);
            Assert.True(TestUtils.SEED == seed);
        }

        // Can get the language of the seed
        [Fact]
        public void TestGetSeedLanguage()
        {
            Assert.True(TEST_NON_RELAYS);
            string language = wallet.GetSeedLanguage();
            Assert.True(MoneroWallet.DEFAULT_LANGUAGE == language);
        }

        // Can get a list of supported languages for the seed
        [Fact]
        public void TestGetSeedLanguages()
        {
            Assert.True(TEST_NON_RELAYS);
            List<string> languages = GetSeedLanguages();
            Assert.True(languages.Count > 0);
            foreach (string language in languages) Assert.True(language.Length > 0);
        }

        // Can get the private view _key
        [Fact]
        public void TestGetPrivateViewKey()
        {
            Assert.True(TEST_NON_RELAYS);
            string privateViewKey = wallet.GetPrivateViewKey();
            MoneroUtils.ValidatePrivateViewKey(privateViewKey);
        }

        // Can get the private spend _key
        [Fact]
        public void TestGetPrivateSpendKey()
        {
            Assert.True(TEST_NON_RELAYS);
            string privateSpendKey = wallet.GetPrivateSpendKey();
            MoneroUtils.ValidatePrivateSpendKey(privateSpendKey);
        }

        // Can get the public view _key
        [Fact]
        public void TestGetPublicViewKey()
        {
            Assert.True(TEST_NON_RELAYS);
            string publicViewKey = wallet.GetPublicViewKey();
            MoneroUtils.ValidatePrivateSpendKey(publicViewKey);
        }

        // Can get the public view _key
        [Fact]
        public void TestGetPublicSpendKey()
        {
            Assert.True(TEST_NON_RELAYS);
            string publicSpendKey = wallet.GetPublicSpendKey();
            MoneroUtils.ValidatePrivateSpendKey(publicSpendKey);
        }

        // Can get the primary address
        [Fact]
        public void TestGetPrimaryAddress()
        {
            Assert.True(TEST_NON_RELAYS);
            string primaryAddress = wallet.GetPrimaryAddress();
            MoneroUtils.ValidateAddress(primaryAddress, TestUtils.NETWORK_TYPE);
            Assert.True(wallet.GetAddress(0, 0) == primaryAddress);
        }

        // Can get the address of a subaddress at a specified account and subaddress index
        [Fact]
        public void TestGetSubaddressAddress()
        {
            Assert.True(TEST_NON_RELAYS);
            Assert.True(wallet.GetPrimaryAddress() == (wallet.GetSubaddress(0, 0)).GetAddress());
            foreach (MoneroAccount account in wallet.GetAccounts(true))
            {
                foreach (MoneroSubaddress subaddress in account.GetSubaddresses())
                {
                    Assert.True(subaddress.GetAddress() == wallet.GetAddress((uint)account.GetIndex(), (uint)subaddress.GetIndex()));
                }
            }
        }

        // Can get addresses out of range of used accounts and subaddresses
        [Fact]
        public void TestGetSubaddressAddressOutOfRange()
        {
            Assert.True(TEST_NON_RELAYS);
            List<MoneroAccount> accounts = wallet.GetAccounts(true);
            uint accountIdx = (uint)accounts.Count - 1;
            uint subaddressIdx = (uint)accounts[accounts.Count - 1].GetSubaddresses().Count;
            string address = wallet.GetAddress(accountIdx, subaddressIdx);
            Assert.NotNull(address);
            Assert.True(address.Length > 0);
        }

        // Can get the account and subaddress indices of an address
        [Fact]
        public void TestGetAddressIndices()
        {
            Assert.True(TEST_NON_RELAYS);

            // get last subaddress to test
            List<MoneroAccount> accounts = wallet.GetAccounts(true);
            int accountIdx = accounts.Count - 1;
            int subaddressIdx = accounts[accountIdx].GetSubaddresses().Count - 1;

            if (accountIdx < 0) throw new MoneroError("Wallet has no accounts");
            if (subaddressIdx < 0) throw new MoneroError("Wallet has no subaddresses");

            string address = wallet.GetAddress((uint)accountIdx, (uint)subaddressIdx);
            Assert.NotNull(address);

            // get address index
            MoneroSubaddress subaddress = wallet.GetAddressIndex(address);
            var subaddressAccountIdx = subaddress.GetAccountIndex();
            Assert.Equal((uint)accountIdx, subaddressAccountIdx);
            Assert.Equal((uint)subaddressIdx, subaddress.GetIndex());

            // test valid but unfound address
            string nonWalletAddress = TestUtils.GetExternalWalletAddress();
            try
            {
                subaddress = wallet.GetAddressIndex(nonWalletAddress);
                throw new Exception("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                Assert.Equal("Address doesn't belong to the wallet", e.Message);
            }

            // test invalid address
            try
            {
                subaddress = wallet.GetAddressIndex("this is definitely not an address");
                throw new Exception("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                Assert.Equal("Invalid address", e.Message);
            }
        }

        // Can get an integrated address given a payment id
        [Fact]
        public void TestGetIntegratedAddress()
        {
            Assert.True(TEST_NON_RELAYS);

            // save address for later comparison
            string address = wallet.GetPrimaryAddress();

            // test valid payment id
            string paymentId = "03284e41c342f036";
            MoneroIntegratedAddress integratedAddress = wallet.GetIntegratedAddress(null, paymentId);
            Assert.Equal(integratedAddress.GetStandardAddress(), address);
            Assert.Equal(integratedAddress.GetPaymentId(), paymentId);

            // test null payment id which generates a new one
            integratedAddress = wallet.GetIntegratedAddress();
            Assert.Equal(integratedAddress.GetStandardAddress(), address);
            Assert.False(integratedAddress.GetPaymentId().Length == 0);

            // test with primary address
            string primaryAddress = wallet.GetPrimaryAddress();
            integratedAddress = wallet.GetIntegratedAddress(primaryAddress, paymentId);
            Assert.Equal(integratedAddress.GetStandardAddress(), primaryAddress);
            Assert.Equal(integratedAddress.GetPaymentId(), paymentId);

            // test with subaddress
            if (wallet.GetSubaddresses(0).Count < 2) wallet.CreateSubaddress(0);
            string subaddress = wallet.GetSubaddress(0, 1).GetAddress();
            try
            {
                integratedAddress = wallet.GetIntegratedAddress(subaddress, null);
                throw new Exception("Getting integrated address from subaddress should have failed");
            }
            catch (MoneroError e)
            {
                Assert.Equal("Subaddress shouldn't be used", e.Message);
            }

            // test invalid payment id
            string invalidPaymentId = "invalid_payment_id_123456";
            try
            {
                integratedAddress = wallet.GetIntegratedAddress(null, invalidPaymentId);
                throw new Exception("Getting integrated address with invalid payment id " + invalidPaymentId + " should have thrown exception");
            }
            catch (MoneroError e)
            {
                Assert.Equal("Invalid payment ID: " + invalidPaymentId, e.Message);
            }
        }

        // Can decode an integrated address
        [Fact]
        public void TestDecodeIntegratedAddress()
        {
            Assert.True(TEST_NON_RELAYS);
            MoneroIntegratedAddress integratedAddress = wallet.GetIntegratedAddress(null, "03284e41c342f036");
            MoneroIntegratedAddress decodedAddress = wallet.DecodeIntegratedAddress(integratedAddress.ToString());
            Assert.Equal(integratedAddress, decodedAddress);

            // decode invalid address
            try
            {
                wallet.DecodeIntegratedAddress("bad address");
                throw new Exception("Should have failed decoding bad address");
            }
            catch (MoneroError err)
            {
                Assert.Equal("Invalid address", err.Message);
            }
        }

        // Can sync (without progress)
        // TODO: test syncing from start height
        [Fact]
        public void TestSyncWithoutProgress()
        {
            Assert.True(TEST_NON_RELAYS);
            ulong numBlocks = 100;
            ulong chainHeight = daemon.GetHeight();
            Assert.True(chainHeight >= numBlocks);
            MoneroSyncResult result = wallet.Sync(chainHeight - numBlocks);  // sync end of chain
            Assert.True(result.GetNumBlocksFetched() >= 0);
            Assert.NotNull(result.GetReceivedMoney());
        }

        // Is equal to a ground truth wallet according to on-chain data
        [Fact]
        public void TestWalletEqualityGroundTruth()
        {
            Assert.True(TEST_NON_RELAYS);
            TestUtils.WALLET_TX_TRACKER.WaitForWalletTxsToClearPool(wallet);
            MoneroWallet walletGt = TestUtils.CreateWalletGroundTruth(TestUtils.NETWORK_TYPE, TestUtils.SEED, null, TestUtils.FIRST_RECEIVE_HEIGHT);
            try
            {
                WalletEqualityUtils.TestWalletEqualityOnChain(walletGt, wallet);
            }
            finally
            {
                walletGt.Close();
            }
        }

        // Can get the current height that the wallet is synchronized to
        [Fact]
        public void TestGetHeight()
        {
            Assert.True(TEST_NON_RELAYS);
            ulong height = wallet.GetHeight();
            Assert.True(height >= 0);
        }

        // Can get a blockchain height by date
        [Fact]
        public void TestGetHeightByDate()
        {
            Assert.True(TEST_NON_RELAYS);

            // collect dates to test starting 100 days ago
            ulong DAY_MS = 24 * 60 * 60 * 1000;
            DateTime yesterday = DateTime.UtcNow.AddDays(-1); // TODO monero-project: today's date can throw exception as "in future" so we test up to yesterday
            List<DateTime> dates = new List<DateTime>();
            for (int i = 99; i >= 0; i--)
            {
                dates.Add(yesterday.AddDays(-i)); // subtract i days
            }

            // test heights by date
            ulong? lastHeight = null;
            foreach (DateTime date in dates)
            {
                ulong _height = wallet.GetHeightByDate(date.Year, date.Month, date.Day);
                Assert.True(_height >= 0);
                if (lastHeight != null) Assert.True(_height >= lastHeight);
                lastHeight = _height;
            }

            Assert.True(lastHeight >= 0);
            ulong height = wallet.GetHeight();
            Assert.True(height >= 0);

            // test future date
            try
            {
                DateTime tomorrow = yesterday.AddDays(2);
                wallet.GetHeightByDate(tomorrow.Year + 1900, tomorrow.Month + 1, tomorrow.Day);
                Assert.Fail("Expected exception on future date");
            }
            catch (MoneroError err)
            {
                Assert.Equal("specified date is in the future", err.Message);
            }
        }

        // Can get the locked and unlocked balances of the wallet, accounts, and subaddresses
        [Fact]
        public void TestGetAllBalances()
        {
            Assert.True(TEST_NON_RELAYS);

            // fetch accounts with all info as reference
            List<MoneroAccount> accounts = wallet.GetAccounts(true);

            // test that balances add up between accounts and wallet
            ulong accountsBalance = 0;
            ulong accountsUnlockedBalance = 0;
            foreach (MoneroAccount account in accounts)
            {
                accountsBalance += account.GetBalance();
                accountsUnlockedBalance += account.GetUnlockedBalance();

                // test that balances add up between subaddresses and accounts
                ulong subaddressesBalance = 0;
                ulong subaddressesUnlockedBalance = 0;
                foreach (MoneroSubaddress subaddress in account.GetSubaddresses())
                {
                    subaddressesBalance += subaddress.GetBalance() ?? 0;
                    subaddressesUnlockedBalance += subaddress.GetUnlockedBalance() ?? 0;

                    // test that balances are consistent with getAccounts() call
                    Assert.Equal((wallet.GetBalance(subaddress.GetAccountIndex(), subaddress.GetIndex())).ToString(), subaddress.GetBalance().ToString());
                    Assert.Equal((wallet.GetUnlockedBalance(subaddress.GetAccountIndex(), subaddress.GetIndex())).ToString(), subaddress.GetUnlockedBalance().ToString());
                }

                Assert.Equal((wallet.GetBalance(account.GetIndex())).ToString(), subaddressesBalance.ToString());
                Assert.Equal((wallet.GetUnlockedBalance(account.GetIndex())).ToString(), subaddressesUnlockedBalance.ToString());
            }

            TestUtils.TestUnsignedBigInteger(accountsBalance);
            TestUtils.TestUnsignedBigInteger(accountsUnlockedBalance);
            Assert.Equal((wallet.GetBalance()).ToString(), accountsBalance.ToString());
            Assert.Equal((wallet.GetUnlockedBalance()).ToString(), accountsUnlockedBalance.ToString());
        }

        // Can get accounts without subaddresses
        [Fact]
        public void TestGetAccountsWithoutSubaddresses()
        {
            Assert.True(TEST_NON_RELAYS);
            List<MoneroAccount> accounts = wallet.GetAccounts();
            Assert.False(accounts.Count == 0);
            foreach (MoneroAccount account in accounts)
            {
                TestAccount(account);
                Assert.Null(account.GetSubaddresses());
            }
        }

        // Can get accounts with subaddresses
        [Fact]
        public void TestGetAccountsWithSubaddresses()
        {
            Assert.True(TEST_NON_RELAYS);
            List<MoneroAccount> accounts = wallet.GetAccounts(true);
            Assert.False(accounts.Count == 0);
            foreach (MoneroAccount account in accounts)
            {
                TestAccount(account);
                Assert.False(account.GetSubaddresses().Count == 0);
            }
        }

        // Can get an account at a specified index
        [Fact]
        public void TestGetAccount()
        {
            Assert.True(TEST_NON_RELAYS);
            List<MoneroAccount> accounts = wallet.GetAccounts();
            Assert.False(accounts.Count == 0);
            foreach (MoneroAccount account in accounts)
            {
                TestAccount(account);

                uint? accountIdx = account.GetIndex();

                if (accountIdx == null)
                {
                    throw new Exception("Account index is null");
                }

                // test without subaddresses
                MoneroAccount retrieved = wallet.GetAccount((uint)accountIdx);
                Assert.Null(retrieved.GetSubaddresses());

                // test with subaddresses
                retrieved = wallet.GetAccount((uint)accountIdx, true);
                Assert.NotNull(retrieved.GetSubaddresses());
                Assert.False(retrieved.GetSubaddresses().Count == 0);
            }
        }

        // Can create a new account without a label
        [Fact]
        public void TestCreateAccountWithoutLabel()
        {
            Assert.True(TEST_NON_RELAYS);
            List<MoneroAccount> accountsBefore = wallet.GetAccounts();
            MoneroAccount createdAccount = wallet.CreateAccount();
            TestAccount(createdAccount);
            Assert.Equal(accountsBefore.Count, (wallet.GetAccounts()).Count - 1);
        }

        // Can create a new account with a label
        [Fact]
        public void TestCreateAccountWithLabel()
        {
            Assert.True(TEST_NON_RELAYS);

            // create account with label
            List<MoneroAccount> accountsBefore = wallet.GetAccounts();
            string label = Guid.NewGuid().ToString();
            MoneroAccount createdAccount = wallet.CreateAccount(label);
            TestAccount(createdAccount);
            Assert.Equal(accountsBefore.Count, wallet.GetAccounts().Count - 1);
            Assert.Equal(label, wallet.GetSubaddress((uint)createdAccount.GetIndex(), 0).GetLabel());

            // fetch and test account
            createdAccount = wallet.GetAccount((uint)createdAccount.GetIndex());
            TestAccount(createdAccount);

            // create account with same label
            createdAccount = wallet.CreateAccount(label);
            TestAccount(createdAccount);
            Assert.Equal(accountsBefore.Count, (wallet.GetAccounts()).Count - 2);
            Assert.Equal(label, wallet.GetSubaddress((uint)createdAccount.GetIndex(), 0).GetLabel());

            // fetch and test account
            createdAccount = wallet.GetAccount((uint)createdAccount.GetIndex());
            TestAccount(createdAccount);
        }

        // Can set account labels
        [Fact]
        public void TestSetAccountLabel()
        {
            // create account
            if (wallet.GetAccounts().Count < 2)
            {
                wallet.CreateAccount();
            }

            // set account label
            string label = GenUtils.GetUUID();
            wallet.SetAccountLabel(1, label);
            Assert.Equal(label, wallet.GetSubaddress(1, 0).GetLabel());
        }

        // Can get subaddresses at a specified account index
        [Fact]
        public void TestGetSubaddresses()
        {
            Assert.True(TEST_NON_RELAYS);
            List<MoneroAccount> accounts = wallet.GetAccounts();
            Assert.False(accounts.Count == 0);
            foreach (MoneroAccount account in accounts)
            {
                List<MoneroSubaddress> subaddresses = wallet.GetSubaddresses((uint)account.GetIndex());
                Assert.False(subaddresses.Count == 0);
                foreach (MoneroSubaddress subaddress in subaddresses)
                {
                    TestSubaddress(subaddress);
                    Assert.Equal(account.GetIndex(), subaddress.GetAccountIndex());
                }
            }
        }

        // Can get subaddresses at specified account and subaddress indices
        [Fact]
        public void TestGetSubaddressesByIndices()
        {
            Assert.True(TEST_NON_RELAYS);
            List<MoneroAccount> accounts = wallet.GetAccounts();
            Assert.False(accounts.Count == 0);
            foreach (MoneroAccount account in accounts)
            {

                // get subaddresses
                List<MoneroSubaddress> subaddresses = wallet.GetSubaddresses((uint)account.GetIndex());
                Assert.True(subaddresses.Count > 0);

                // remove a subaddress for query if possible
                if (subaddresses.Count > 1) subaddresses.RemoveAt(0);

                // get subaddress indices
                List<uint> subaddressIndices = new();
                foreach (MoneroSubaddress subaddress in subaddresses)
                {
                    subaddressIndices.Add((uint)subaddress.GetIndex());
                }

                Assert.True(subaddressIndices.Count > 0);

                // fetch subaddresses by indices
                List<MoneroSubaddress> fetchedSubaddresses = wallet.GetSubaddresses((uint)account.GetIndex(), subaddressIndices);

                // original subaddresses (minus one removed if applicable) is equal to fetched subaddresses
                Assert.Equal(subaddresses, fetchedSubaddresses);
            }
        }

        // Can get a subaddress at a specified account and subaddress index
        [Fact]
        public void TestGetSubaddressByIndex()
        {
            Assert.True(TEST_NON_RELAYS);
            List<MoneroAccount> accounts = wallet.GetAccounts();
            Assert.True(accounts.Count > 0);
            foreach (MoneroAccount account in accounts)
            {
                uint accountIdx = (uint)account.GetIndex();

                List<MoneroSubaddress> subaddresses = wallet.GetSubaddresses(accountIdx);
                Assert.True(subaddresses.Count > 0);

                foreach (MoneroSubaddress subaddress in subaddresses)
                {
                    TestSubaddress(subaddress);
                    uint subaddressIdx = (uint)subaddress.GetIndex();
                    Assert.Equal(subaddress, wallet.GetSubaddress(accountIdx, subaddressIdx));
                    Assert.Equal(subaddress, (wallet.GetSubaddresses(accountIdx, new List<uint>() { subaddressIdx })[0])); // test plural call with single subaddr number
                }
            }
        }

        // Can create a subaddress with and without a label
        [Fact]
        public void TestCreateSubaddress()
        {
            Assert.True(TEST_NON_RELAYS);

            // create subaddresses across accounts
            List<MoneroAccount> accounts = wallet.GetAccounts();
            if (accounts.Count < 2) wallet.CreateAccount();
            accounts = wallet.GetAccounts();
            Assert.True(accounts.Count > 1);
            for (uint accountIdx = 0; accountIdx < 2; accountIdx++)
            {
                // create subaddress with no label
                List<MoneroSubaddress> subaddresses = wallet.GetSubaddresses(accountIdx);
                MoneroSubaddress subaddress = wallet.CreateSubaddress(accountIdx);
                Assert.Null(subaddress.GetLabel());
                TestSubaddress(subaddress);
                List<MoneroSubaddress> subaddressesNew = wallet.GetSubaddresses(accountIdx);
                Assert.Equal(subaddressesNew.Count - 1, subaddresses.Count);
                Assert.Equal(subaddress, subaddressesNew[subaddressesNew.Count - 1]);

                // create subaddress with label
                subaddresses = wallet.GetSubaddresses(accountIdx);
                string uuid = GenUtils.GetUUID();
                subaddress = wallet.CreateSubaddress(accountIdx, uuid);
                Assert.Equal(uuid, subaddress.GetLabel());
                TestSubaddress(subaddress);
                subaddressesNew = wallet.GetSubaddresses(accountIdx);
                Assert.Equal(subaddresses.Count, subaddressesNew.Count - 1);
                Assert.Equal(subaddress, subaddressesNew[subaddressesNew.Count - 1]);
            }
        }

        // Can set subaddress labels
        [Fact]
        public void TestSetSubaddressLabel()
        {

            // create subaddresses
            while (wallet.GetSubaddresses(0).Count < 3)
            {
                wallet.CreateSubaddress(0);
            }

            // set subaddress labels
            for (uint subaddressIdx = 0; subaddressIdx < wallet.GetSubaddresses(0).Count; subaddressIdx++)
            {
                string label = GenUtils.GetUUID();
                wallet.SetSubaddressLabel(0, subaddressIdx, label);
                Assert.Equal(label, wallet.GetSubaddress(0, subaddressIdx).GetLabel());
            }
        }

        // Can get transactions in the wallet
        [Fact]
        public void TestGetTxsWallet()
        {
            Assert.True(TEST_NON_RELAYS);
            bool nonDefaultIncoming = false;
            List<MoneroTxWallet> txs = GetAndTestTxs(wallet, null, null, true);
            Assert.False(txs.Count == 0, "Wallet has no txs to test");
            Assert.True(TestUtils.FIRST_RECEIVE_HEIGHT == (ulong)txs[0].GetHeight(), "First _tx's restore height must match the restore height in TestUtils");

            // build test context
            TxContext ctx = new TxContext();
            ctx.Wallet = wallet;

            // test each transaction
            Dictionary<ulong, MoneroBlock> blockPerHeight = [];
            for (int i = 0; i < txs.Count; i++)
            {
                TestTxWallet(txs[i], ctx);

                // test merging equivalent txs
                MoneroTxWallet copy1 = txs[i].Clone();
                MoneroTxWallet copy2 = txs[i].Clone();
                if (copy1.IsConfirmed() == true) copy1.SetBlock(txs[i].GetBlock().Clone().SetTxs([copy1]));
                if (copy2.IsConfirmed() == true) copy2.SetBlock(txs[i].GetBlock().Clone().SetTxs([copy2]));
                MoneroTxWallet merged = copy1.Merge(copy2);
                TestTxWallet(merged, ctx);

                // find non-default incoming
                if (txs[i].GetIncomingTransfers() != null)
                { // TODO: txs1[i].IsIncoming()
                    foreach (MoneroIncomingTransfer transfer in txs[i].GetIncomingTransfers())
                    {
                        if (transfer.GetAccountIndex() != 0 && transfer.GetSubaddressIndex() != 0) nonDefaultIncoming = true;
                    }
                }

                // ensure unique block reference per height
                if (txs[i].IsConfirmed() == true)
                {
                    MoneroBlock block = blockPerHeight[(ulong)txs[i].GetHeight()];
                    if (block == null) blockPerHeight.Add((ulong)txs[i].GetHeight(), txs[i].GetBlock());
                    else
                    {
                        Assert.Equal(block, txs[i].GetBlock());
                        Assert.True(block == txs[i].GetBlock(), "Block references for same height must be same");
                    }
                }
            }

            // ensure non-default account and subaddress tested
            Assert.True(nonDefaultIncoming, "No incoming transfers found to non-default account and subaddress; run send-to-multiple tests first");
        }

        // Can get transactions by hash
        [Fact]
        public void TestGetTxsByHash()
        {
            Assert.True(TEST_NON_RELAYS);

            int maxNumTxs = 10;  // max number of txs to test

            // fetch all txs for testing
            List<MoneroTxWallet> txs = wallet.GetTxs();
            Assert.True(txs.Count > 1, "Test requires at least 2 txs to fetch by hash");

            // randomly pick a few for fetching by hash
            txs.Shuffle();
            txs = txs.GetRange(0, Math.Min(txs.Count, maxNumTxs));

            // test fetching by hash
            MoneroTxWallet fetchedTx = wallet.GetTx(txs[0].GetHash());
            Assert.Equal(txs[0].GetHash(), fetchedTx.GetHash());
            TestTxWallet(fetchedTx);

            // test fetching by hashes
            string txId1 = txs[0].GetHash();
            string txId2 = txs[1].GetHash();
            List<MoneroTxWallet> fetchedTxs = wallet.GetTxs([txId1, txId2]);
            Assert.Equal(2, fetchedTxs.Count);

            // test fetching by hashes as collection
            List<string> txHashes = new ();
            foreach (MoneroTxWallet tx in txs) txHashes.Add(tx.GetHash());
            fetchedTxs = wallet.GetTxs(txHashes);
            Assert.Equal(txs.Count, fetchedTxs.Count);
            for (int i = 0; i < txs.Count; i++)
            {
                Assert.Equal(txs[i].GetHash(), fetchedTxs[i].GetHash());
                TestTxWallet(fetchedTxs[i]);
            }

            // test fetching with missing _tx hashes
            string missingTxHash = "d01ede9cde813b2a693069b640c4b99c5adbdb49fbbd8da2c16c8087d0c3e320";
            txHashes.Add(missingTxHash);
            fetchedTxs = wallet.GetTxs(txHashes);
            Assert.Equal(txs.Count, fetchedTxs.Count);
            for (int i = 0; i < txs.Count; i++)
            {
                Assert.Equal(txs[i].GetHash(), fetchedTxs[i].GetHash());
                TestTxWallet(fetchedTxs[i]);
            }
        }

        // Can get transactions with additional configuration
        [Fact]
        public void TestGetTxsWithQuery()
        {
            Assert.True(TEST_NON_RELAYS);

            // get random transactions for testing
            List<MoneroTxWallet> randomTxs = GetRandomTransactions(wallet, null, 3, 5);
            foreach (MoneroTxWallet randomTx in randomTxs) TestTxWallet(randomTx, null);

            // get transactions by hash
            List<string> txHashes = new List<string>();
            foreach (MoneroTxWallet randomTx in randomTxs)
            {
                txHashes.Add(randomTx.GetHash());
                List<MoneroTxWallet> _txs = GetAndTestTxs(wallet, new MoneroTxQuery().SetHash(randomTx.GetHash()), null, true);
                Assert.Equal(_txs.Count, 1);
                MoneroTxWallet merged = _txs[0].Merge(randomTx.Clone()); // txs change with chain so _check mergeability
                TestTxWallet(merged, null);
            }

            // get transactions by hashes
            List<MoneroTxWallet> txs = GetAndTestTxs(wallet, new MoneroTxQuery().SetHashes(txHashes), null, null);
            Assert.Equal(txs.Count, randomTxs.Count);
            foreach (MoneroTxWallet tx in txs) Assert.True(txHashes.Contains(tx.GetHash()));

            // get transactions with an outgoing transfer
            TxContext ctx = new TxContext();
            ctx.HasOutgoingTransfer = true;
            txs = GetAndTestTxs(wallet, new MoneroTxQuery().SetIsOutgoing(true), ctx, true);
            foreach (MoneroTxWallet tx in txs)
            {
                Assert.True(tx.IsOutgoing());
                Assert.NotNull(tx.GetOutgoingTransfer());
                TestTransfer(tx.GetOutgoingTransfer(), null);
            }

            // get transactions without an outgoing transfer
            ctx.HasOutgoingTransfer = false;
            txs = GetAndTestTxs(wallet, new MoneroTxQuery().SetIsOutgoing(false), ctx, true);
            foreach (MoneroTxWallet tx in txs) Assert.Null(tx.GetOutgoingTransfer());

            // get transactions with incoming transfers
            ctx = new TxContext();
            ctx.HasIncomingTransfers = true;
            txs = GetAndTestTxs(wallet, new MoneroTxQuery().SetIsIncoming(true), ctx, true);
            foreach (MoneroTxWallet tx in txs)
            {
                Assert.True(tx.IsIncoming());
                Assert.True(tx.GetIncomingTransfers().Count > 0);
                foreach (MoneroIncomingTransfer transfer in tx.GetIncomingTransfers())
                {
                    TestTransfer(transfer, null);
                }
            }

            // get transactions without incoming transfers
            ctx.HasIncomingTransfers = false;
            txs = GetAndTestTxs(wallet, new MoneroTxQuery().SetIsIncoming(false), ctx, true);
            foreach (MoneroTxWallet tx in txs)
            {
                Assert.False(tx.IsIncoming());
                Assert.Null(tx.GetIncomingTransfers());
            }

            // get transactions associated with an account
            uint accountIdx = 1;
            txs = wallet.GetTxs(new MoneroTxQuery().SetTransferQuery(new MoneroTransferQuery().SetAccountIndex(accountIdx)));
            foreach (MoneroTxWallet tx in txs)
            {
                bool _found = false;
                if (tx.IsOutgoing() == true && tx.GetOutgoingTransfer().GetAccountIndex() == accountIdx) _found = true;
                else if (tx.GetIncomingTransfers() != null)
                {
                    foreach (MoneroTransfer transfer in tx.GetIncomingTransfers())
                    {
                        if (transfer.GetAccountIndex() == accountIdx)
                        {
                            _found = true;
                            break;
                        }
                    }
                }
                Assert.True(_found, ("Transaction is not associated with account " + accountIdx + ":\n" + tx.ToString()));
            }

            // get transactions with incoming transfers to an account
            txs = wallet.GetTxs(new MoneroTxQuery().SetTransferQuery(new MoneroTransferQuery().SetIsIncoming(true).SetAccountIndex(accountIdx)));
            foreach (MoneroTxWallet tx in txs)
            {
                Assert.True(tx.GetIncomingTransfers().Count > 0);
                bool _found = false;
                foreach (MoneroTransfer transfer in tx.GetIncomingTransfers())
                {
                    if (transfer.GetAccountIndex() == accountIdx)
                    {
                        _found = true;
                        break;
                    }
                }
                Assert.True(_found, "No incoming transfers to account " + accountIdx + " found:\n" + tx.ToString());
            }

            // get txs with manually built query that are confirmed and have an outgoing transfer from account 0
            ctx = new TxContext();
            ctx.HasOutgoingTransfer = true;
            MoneroTxQuery txQuery = new MoneroTxQuery();
            txQuery.SetIsConfirmed(true);
            txQuery.SetTransferQuery(new MoneroTransferQuery().SetAccountIndex(0).SetIsOutgoing(true));
            txs = GetAndTestTxs(wallet, txQuery, ctx, true);
            foreach (MoneroTxWallet tx in txs)
            {
                if (tx.IsConfirmed() != true) Console.WriteLine(tx);
                Assert.Equal(true, tx.IsConfirmed());
                Assert.True(tx.IsOutgoing());
                Assert.Equal(0, (int)tx.GetOutgoingTransfer().GetAccountIndex());
            }

            // get txs with outgoing transfers that have destinations to account 1
            txs = GetAndTestTxs(wallet, new MoneroTxQuery().SetTransferQuery(new MoneroTransferQuery().SetHasDestinations(true).SetAccountIndex(0)), null, null);
            foreach (MoneroTxWallet tx in txs)
            {
                Assert.True(tx.IsOutgoing());
                Assert.True(tx.GetOutgoingTransfer().GetDestinations().Count > 0);
            }

            // include outputs with transactions
            ctx = new TxContext();
            ctx.IncludeOutputs = true;
            txs = GetAndTestTxs(wallet, new MoneroTxQuery().SetIncludeOutputs(true), ctx, true);
            bool found = false;
            foreach (MoneroTxWallet tx in txs)
            {
                if (tx.GetOutputs() != null)
                {
                    Assert.True(tx.GetOutputs().Count > 0);
                    found = true;
                }
                else
                {
                    Assert.True(tx.IsOutgoing() == true || (tx.IsIncoming() == true && tx.IsConfirmed() != true)); // TODO: monero-wallet-rpc: return outputs for unconfirmed txs
                }
            }
            Assert.True(found, "No outputs found in txs");

            // get txs with input query // TODO: no inputs returned to filter
            //    MoneroTxWallet outgoingTx = wallet.GetTxs(new MoneroTxQuery().SetIsOutgoing(true))[0];
            //    List<MoneroTxWallet> outgoingTxs = wallet.GetTxs(new MoneroTxQuery().SetInputQuery(new MoneroOutputQuery().SetKeyImage(new MoneroKeyImage().SetHex(outgoingTx.GetInputsWallet()[0].GetKeyImage().GetHex()))));
            //    Assert.Equal(1, outgoingTxs.Count);
            //    Assert.Equal(outgoingTx.GetHash(), outgoingTxs[0].GetHash());

            // get txs with output query
            MoneroOutputQuery outputQuery = new MoneroOutputQuery().SetIsSpent(false).SetAccountIndex(1).SetSubaddressIndex(2);
            txs = wallet.GetTxs(new MoneroTxQuery().SetOutputQuery(outputQuery));
            Assert.False(txs.Count == 0);
            foreach (MoneroTxWallet tx in txs)
            {
                Assert.False(tx.GetOutputs() == null || tx.GetOutputs().Count == 0);
                found = false;
                foreach (MoneroOutputWallet output in tx.GetOutputsWallet())
                {
                    if (output.IsSpent() == false && output.GetAccountIndex() == 1 && output.GetSubaddressIndex() == 2)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) Assert.Fail("Tx does not contain specified output");
            }

            // get unlocked txs
            txs = wallet.GetTxs(new MoneroTxQuery().SetIsLocked(false));
            Assert.False(txs.Count == 0);
            foreach (MoneroTxWallet tx in txs)
            {
                Assert.False(tx.IsLocked());
            }

            // get confirmed transactions sent from/to same wallet with a transfer with destinations
            txs = wallet.GetTxs(new MoneroTxQuery().SetIsIncoming(true).SetIsOutgoing(true).SetIncludeOutputs(true).SetIsConfirmed(true).SetTransferQuery(new MoneroTransferQuery().SetHasDestinations(true)));
            Assert.False(txs.Count == 0);
            foreach (MoneroTxWallet tx in txs)
            {
                Assert.True(tx.IsIncoming());
                Assert.True(tx.IsOutgoing());
                Assert.True(tx.IsConfirmed());
                Assert.False(tx.GetOutputsWallet().Count == 0);
                Assert.NotNull(tx.GetOutgoingTransfer());
                Assert.NotNull(tx.GetOutgoingTransfer().GetDestinations());
                Assert.False(tx.GetOutgoingTransfer().GetDestinations().Count == 0);
            }
        }

        // Can get transactions by height
        [Fact]
        public void TestGetTxsByHeight()
        {
            Assert.True(TEST_NON_RELAYS);

            // get all confirmed txs for testing
            List<MoneroTxWallet> txs = wallet.GetTxs(new MoneroTxQuery().SetIsConfirmed(true));

            // collect all _tx heights
            List<ulong> txHeights = new List<ulong>();
            foreach (MoneroTxWallet tx in txs) txHeights.Add((ulong)tx.GetHeight());

            // get height that most txs occur at
            Dictionary<ulong, uint> heightCounts = CountNumInstances(txHeights);
            Assert.False(heightCounts.Count == 0, "Wallet has no confirmed txs; run send tests");
            HashSet<ulong> heightModes = GetModes(heightCounts);
            ulong modeHeight = heightModes.First();

            // fetch txs at mode height
            List<MoneroTxWallet> modeTxs = wallet.GetTxs(new MoneroTxQuery().SetHeight(modeHeight));
            Assert.Equal((int)heightCounts[modeHeight], modeTxs.Count);
            foreach (MoneroTxWallet tx in modeTxs)
            {
                Assert.Equal(modeHeight, tx.GetHeight());
            }

            // fetch txs at mode height by range
            List<MoneroTxWallet> modeTxsByRange = wallet.GetTxs(new MoneroTxQuery().SetMinHeight(modeHeight).SetMaxHeight(modeHeight));
            Assert.Equal(modeTxs.Count, modeTxsByRange.Count);
            Assert.Equal(modeTxs, modeTxsByRange);

            // fetch all txs by range
            Assert.Equal(txs, wallet.GetTxs(new MoneroTxQuery().SetMinHeight(txs.First().GetHeight()).SetMaxHeight(txs.Last().GetHeight())));

            // test some filtered by range
            {
                txs = wallet.GetTxs(new MoneroTxQuery().SetIsConfirmed(true));
                Assert.True(txs.Count > 0, "No transactions; run send to multiple test");

                // get and sort block heights in ascending order
                List<ulong> heights = new List<ulong>();
                foreach (MoneroTxWallet tx in txs)
                {
                    ulong? height = tx.GetBlock().GetHeight();
                    Assert.NotNull(height);
                    heights.Add((ulong)height);
                }

                heights.Sort();

                // pick minimum and maximum heights for filtering
                ulong minHeight = 0;//-1;
                ulong maxHeight = 0;//-1;
                if (heights.Count == 1)
                {
                    minHeight = 0;
                    maxHeight = heights.First() - 1;
                }
                else
                {
                    minHeight = heights.First() + 1;
                    maxHeight = heights.Last() - 1;
                }

                // Assert. some transactions filtered
                int unfilteredCount = txs.Count;
                txs = GetAndTestTxs(wallet, new MoneroTxQuery().SetMinHeight(minHeight).SetMaxHeight(maxHeight), null, true);
                Assert.True(txs.Count < unfilteredCount);
                foreach (MoneroTx tx in txs)
                {
                    ulong? height = tx.GetBlock().GetHeight();
                    Assert.True(height >= minHeight && height <= maxHeight);
                }
            }
        }

        // Can get transactions with payment id
        [Fact]
        public void TestGetTxsWithPaymentIds()
        {
            Assert.True(TEST_NON_RELAYS && !LITE_MODE);

            // get random transactions with payment ids for testing
            List<MoneroTxWallet> randomTxs = GetRandomTransactions(wallet, new MoneroTxQuery().SetHasPaymentId(true), 2, 5);
            Assert.False(randomTxs.Count == 0, "No txs with payment ids to test");
            foreach (MoneroTxWallet randomTx in randomTxs)
            {
                Assert.NotNull(randomTx.GetPaymentId());
            }

            // get transactions by payment id
            List<string> paymentIds = new List<string>();
            foreach (MoneroTxWallet tx in randomTxs) paymentIds.Add(tx.GetPaymentId());
            Assert.True(paymentIds.Count > 1);
            foreach (string paymentId in paymentIds)
            {
                List<MoneroTxWallet> _txs = GetAndTestTxs(wallet, new MoneroTxQuery().SetPaymentId(paymentId), null, null);
                Assert.True(_txs.Count > 0);
                Assert.NotNull(_txs[0].GetPaymentId());
                MoneroUtils.ValidatePaymentId(_txs[0].GetPaymentId());
            }

            // get transactions by payment hashes
            List<MoneroTxWallet> txs = GetAndTestTxs(wallet, new MoneroTxQuery().SetPaymentIds(paymentIds), null, null);
            foreach (MoneroTxWallet tx in txs)
            {
                Assert.True(paymentIds.Contains(tx.GetPaymentId()));
            }
        }

        // Returns all known fields of txs regardless of filtering
        [Fact]
        public void TestGetTxsFieldsWithFiltering()
        {
            Assert.True(TEST_NON_RELAYS);

            // fetch wallet txs
            List<MoneroTxWallet> txs = wallet.GetTxs(new MoneroTxQuery().SetIsConfirmed(true));
            foreach (MoneroTxWallet tx in txs)
            {

                // find _tx sent to same wallet with incoming transfer in different account than src account
                if (tx.GetOutgoingTransfer() == null || tx.GetIncomingTransfers() == null) continue;
                foreach (MoneroTransfer transfer in tx.GetIncomingTransfers())
                {
                    if (transfer.GetAccountIndex() == tx.GetOutgoingTransfer().GetAccountIndex()) continue;

                    // fetch _tx with filtering
                    List<MoneroTxWallet> filteredTxs = wallet.GetTxs(new MoneroTxQuery().SetTransferQuery(new MoneroTransferQuery().SetIsIncoming(true).SetAccountIndex(transfer.GetAccountIndex())));
                    var query = new MoneroTxQuery().SetHashes([tx.GetHash()]);

                    MoneroTxWallet? filteredTx = null;
                    foreach (var fTx in filteredTxs)
                    {
                        if (query.MeetsCriteria(fTx)) {
                            filteredTx = fTx;
                            break;
                        }
                    }

                    Assert.NotNull(filteredTx);

                    // txs should be the same (mergeable)
                    Assert.Equal(tx.GetHash(), filteredTx.GetHash());
                    tx.Merge(filteredTx);

                    // test is done
                    return;
                }
            }

            // test did not fully execute
            throw new Exception("Test requires _tx sent from/to different accounts of same wallet but none found; run send tests");
        }

        // Validates inputs when getting transactions
        [Fact]
        public void TestValidateInputsGetTxs()
        {
            Assert.True(TEST_NON_RELAYS && !LITE_MODE);

            // fetch random txs for testing
            List<MoneroTxWallet> randomTxs = GetRandomTransactions(wallet, null, 3, 5);

            // valid, invalid, and unknown _tx hashes for tests
            string txHash = randomTxs[0].GetHash();
            string invalidHash = "invalid_id";
            string unknownHash1 = "6c4982f2499ece80e10b627083c4f9b992a00155e98bcba72a9588ccb91d0a61";
            string unknownHash2 = "ff397104dd875882f5e7c66e4f852ee134f8cf45e21f0c40777c9188bc92e943";

            // fetch unknown _tx hash
            Assert.Null(wallet.GetTx(unknownHash1));

            // fetch unknown _tx hash using query
            Assert.Equal(0, wallet.GetTxs(new MoneroTxQuery().SetHash(unknownHash1)).Count);

            // fetch unknown _tx hash in collection
            List<MoneroTxWallet> txs = wallet.GetTxs([txHash, unknownHash1]);
            Assert.Equal(1, txs.Count);
            Assert.Equal(txHash, txs[0].GetHash());

            // fetch unknown _tx hashes in collection
            txs = wallet.GetTxs([txHash, unknownHash1, unknownHash2]);
            Assert.Equal(1, txs.Count);
            Assert.Equal(txHash, txs[0].GetHash());

            // fetch invalid hash
            Assert.Null(wallet.GetTx(invalidHash));

            // fetch invalid hash collection
            txs = wallet.GetTxs([txHash, invalidHash]);
            Assert.Equal(1, txs.Count);
            Assert.Equal(txHash, txs[0].GetHash());

            // fetch invalid hashes in collection
            txs = wallet.GetTxs([txHash, invalidHash, "invalid_hash_2"]);
            Assert.Equal(1, txs.Count);
            Assert.Equal(txHash, txs[0].GetHash());

            // test collection of invalid hashes
            txs = wallet.GetTxs(new MoneroTxQuery().SetHashes([txHash, invalidHash, "invalid_hash_2"]));
            Assert.Equal(1, txs.Count);
            foreach (MoneroTxWallet tx in txs) TestTxWallet(tx);
        }

        // Can get transfers in the wallet, accounts, and subaddresses
        [Fact]
        public void TestGetTransfers()
        {
            Assert.True(TEST_NON_RELAYS);

            // get all transfers
            GetAndTestTransfers(wallet, null, null, true);

            // get transfers by account index
            bool nonDefaultIncoming = false;
            foreach (MoneroAccount account in wallet.GetAccounts(true))
            {
                List<MoneroTransfer> accountTransfers = GetAndTestTransfers(wallet, new MoneroTransferQuery().SetAccountIndex(account.GetIndex()), null, null);
                foreach (MoneroTransfer transfer in accountTransfers) Assert.Equal(transfer.GetAccountIndex(), account.GetIndex());

                // get transfers by subaddress index
                List<MoneroTransfer> subaddressTransfers = new List<MoneroTransfer>();
                foreach (MoneroSubaddress subaddress in account.GetSubaddresses())
                {
                    List<MoneroTransfer> _transfers = GetAndTestTransfers(wallet, new MoneroTransferQuery().SetAccountIndex(subaddress.GetAccountIndex()).SetSubaddressIndex(subaddress.GetIndex()), null, null);
                    foreach (MoneroTransfer transfer in _transfers)
                    {

                        // test account and subaddress indices
                        Assert.Equal(subaddress.GetAccountIndex(), transfer.GetAccountIndex());
                        if (transfer.IsIncoming() == true)
                        {
                            MoneroIncomingTransfer inTransfer = (MoneroIncomingTransfer)transfer;
                            Assert.Equal(subaddress.GetIndex(), inTransfer.GetSubaddressIndex());
                            if (transfer.GetAccountIndex() != 0 && inTransfer.GetSubaddressIndex() != 0) nonDefaultIncoming = true;
                        }
                        else
                        {
                            MoneroOutgoingTransfer outTransfer = (MoneroOutgoingTransfer)transfer;
                            Assert.True(outTransfer.GetSubaddressIndices().Contains((uint)subaddress.GetIndex()));
                            if (transfer.GetAccountIndex() != 0)
                            {
                                foreach (int subaddrIdx in outTransfer.GetSubaddressIndices())
                                {
                                    if (subaddrIdx > 0)
                                    {
                                        nonDefaultIncoming = true;
                                        break;
                                    }
                                }
                            }
                        }

                        // don't add duplicates TODO monero-wallet-rpc: duplicate outgoing transfers returned foreach different subaddress indices, way to return outgoing subaddress indices?
                        bool found = false;
                        foreach (MoneroTransfer subaddressTransfer in subaddressTransfers)
                        {
                            if (transfer.ToString().Equals(subaddressTransfer.ToString()) && transfer.GetTx().GetHash().Equals(subaddressTransfer.GetTx().GetHash()))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found) subaddressTransfers.Add(transfer);
                    }
                }
                Assert.Equal(accountTransfers.Count, subaddressTransfers.Count);

                // collect unique subaddress indices
                HashSet<uint> subaddressIndices = new HashSet<uint>();
                foreach (MoneroTransfer transfer in subaddressTransfers)
                {
                    if (transfer.IsIncoming() == true) subaddressIndices.Add((uint)((MoneroIncomingTransfer)transfer).GetSubaddressIndex());
                    else
                    {
                        foreach (var subIdx in ((MoneroOutgoingTransfer)transfer).GetSubaddressIndices()) subaddressIndices.Add(subIdx);
                    }
                }

                // get and test transfers by subaddress indices
                List<MoneroTransfer> transfers = GetAndTestTransfers(wallet, new MoneroTransferQuery().SetAccountIndex(account.GetIndex()).SetSubaddressIndices(new List<uint>(subaddressIndices)), null, null);
                //if (transfers.Count != subaddressTransfers.Count) System.out.println("WARNING: outgoing transfers always from subaddress 0 (monero-wallet-rpc #5171)");
                Assert.Equal(subaddressTransfers.Count, transfers.Count); // TODO monero-wallet-rpc: these may not be equal because outgoing transfers are always from subaddress 0 (#5171) and/or incoming transfers from/to same account are occluded (#4500)
                foreach (MoneroTransfer transfer in transfers)
                {
                    Assert.Equal(transfer.GetAccountIndex(), account.GetIndex());
                    if (transfer.IsIncoming() == true) Assert.True(subaddressIndices.Contains((uint)((MoneroIncomingTransfer)transfer).GetSubaddressIndex()));
                    else
                    {
                        HashSet<uint> intersections = new HashSet<uint>(subaddressIndices);
                        intersections.IntersectWith(((MoneroOutgoingTransfer)transfer).GetSubaddressIndices());
                        Assert.True(intersections.Count > 0, "Subaddresses must overlap");
                    }
                }
            }

            // ensure transfer found with non-zero account and subaddress indices
            Assert.True(nonDefaultIncoming, "No transfers found in non-default account and subaddress; run send-to-multiple tests");
        }

        // Can get transfers with additional configuration
        [Fact]
        public void TestGetTransfersWithQuery()
        {
            Assert.True(TEST_NON_RELAYS);

            // get incoming transfers
            List<MoneroTransfer> transfers = GetAndTestTransfers(wallet, new MoneroTransferQuery().SetIsIncoming(true), null, true);
            foreach (MoneroTransfer transfer in transfers) Assert.True(transfer.IsIncoming());

            // get outgoing transfers
            transfers = GetAndTestTransfers(wallet, new MoneroTransferQuery().SetIsOutgoing(true), null, true);
            foreach (MoneroTransfer transfer in transfers) Assert.True(transfer.IsOutgoing());

            // get confirmed transfers to account 0
            transfers = GetAndTestTransfers(wallet, new MoneroTransferQuery().SetAccountIndex(0).SetTxQuery(new MoneroTxQuery().SetIsConfirmed(true)), null, true);
            foreach (MoneroTransfer transfer in transfers)
            {
                Assert.Equal(0, (int)transfer.GetAccountIndex());
                Assert.True(transfer.GetTx().IsConfirmed());
            }

            // get confirmed transfers to [1, 2]
            transfers = GetAndTestTransfers(wallet, new MoneroTransferQuery().SetAccountIndex(1).SetSubaddressIndex(2).SetTxQuery(new MoneroTxQuery().SetIsConfirmed(true)), null, true);
            foreach (MoneroTransfer transfer in transfers)
            {
                Assert.Equal(1, (int)transfer.GetAccountIndex());
                if (transfer.IsIncoming() == true) Assert.Equal(2, (int)((MoneroIncomingTransfer)transfer).GetSubaddressIndex());
                else Assert.True(((MoneroOutgoingTransfer)transfer).GetSubaddressIndices().Contains(2));
                Assert.True(transfer.GetTx().IsConfirmed());
            }

            // get transfers in the _tx pool
            transfers = GetAndTestTransfers(wallet, new MoneroTransferQuery().SetTxQuery(new MoneroTxQuery().SetInTxPool(true)), null, null);
            foreach (MoneroTransfer transfer in transfers)
            {
                Assert.Equal(true, transfer.GetTx().InTxPool());
            }

            // get random transactions
            List<MoneroTxWallet> txs = GetRandomTransactions(wallet, null, 3, 5);

            // get transfers with a _tx hash
            List<string> txHashes = new List<string>();
            foreach (MoneroTxWallet tx in txs)
            {
                txHashes.Add(tx.GetHash());
                transfers = GetAndTestTransfers(wallet, new MoneroTransferQuery().SetTxQuery(new MoneroTxQuery().SetHash(tx.GetHash())), null, true);
                foreach (MoneroTransfer transfer in transfers) Assert.Equal(tx.GetHash(), transfer.GetTx().GetHash());
            }

            // get transfers with _tx hashes
            transfers = GetAndTestTransfers(wallet, new MoneroTransferQuery().SetTxQuery(new MoneroTxQuery().SetHashes(txHashes)), null, true);
            foreach (MoneroTransfer transfer in transfers) Assert.True(txHashes.Contains(transfer.GetTx().GetHash()));

            // TODO: test that transfers with the same txHash have the same _tx reference

            // TODO: test transfers destinations

            // get transfers with pre-built query that are confirmed and have outgoing destinations
            MoneroTransferQuery transferQuery = new MoneroTransferQuery();
            transferQuery.SetIsOutgoing(true);
            transferQuery.SetHasDestinations(true);
            transferQuery.SetTxQuery(new MoneroTxQuery().SetIsConfirmed(true));
            transfers = GetAndTestTransfers(wallet, transferQuery, null, null);
            foreach (MoneroTransfer transfer in transfers)
            {
                Assert.Equal(true, transfer.IsOutgoing());
                Assert.True(((MoneroOutgoingTransfer)transfer).GetDestinations().Count > 0);
                Assert.Equal(true, transfer.GetTx().IsConfirmed());
            }

            // get incoming transfers to account 0 which has outgoing transfers (i.e. originated from the same wallet)
            transfers = wallet.GetTransfers(new MoneroTransferQuery().SetAccountIndex(1).SetIsIncoming(true).SetTxQuery(new MoneroTxQuery().SetIsOutgoing(true)));
            Assert.False(transfers.Count == 0);
            foreach (MoneroTransfer transfer in transfers)
            {
                Assert.True(transfer.IsIncoming());
                Assert.Equal(1, (int)transfer.GetAccountIndex());
                Assert.True(transfer.GetTx().IsOutgoing());
                Assert.Null(transfer.GetTx().GetOutgoingTransfer());
            }

            // get incoming transfers to a specific address
            string subaddress = wallet.GetAddress(1, 0);
            transfers = wallet.GetTransfers(new MoneroTransferQuery().SetIsIncoming(true).SetAddress(subaddress));
            Assert.True(transfers.Count > 0);
            foreach (MoneroTransfer transfer in transfers)
            {
                Assert.True(transfer is MoneroIncomingTransfer);
                Assert.True(1 == (uint)transfer.GetAccountIndex());
                Assert.True(0 == (uint) ((MoneroIncomingTransfer)transfer).GetSubaddressIndex());
                Assert.Equal(subaddress, ((MoneroIncomingTransfer)transfer).GetAddress());
            }
        }

        // Validates inputs when getting transfers
        [Fact]
        public void TestValidateInputsGetTransfers()
        {
            Assert.True(TEST_NON_RELAYS && !LITE_MODE);

            // test with invalid hash
            List<MoneroTransfer> transfers = wallet.GetTransfers(new MoneroTransferQuery().SetTxQuery(new MoneroTxQuery().SetHash("invalid_id")));
            Assert.Equal(0, transfers.Count);

            // test invalid hash in collection
            List<MoneroTxWallet> randomTxs = GetRandomTransactions(wallet, null, 3, 5);
            transfers = wallet.GetTransfers(new MoneroTransferQuery().SetTxQuery(new MoneroTxQuery().SetHashes([randomTxs[0].GetHash(), "invalid_id"])));
            Assert.True(transfers.Count > 0);
            MoneroTxWallet tx = transfers[0].GetTx();
            foreach (MoneroTransfer transfer in transfers) Assert.True(tx == transfer.GetTx());

            // test unused subaddress indices
            transfers = wallet.GetTransfers(new MoneroTransferQuery().SetAccountIndex(0).SetSubaddressIndices([1234907]));
            Assert.True(transfers.Count == 0);            
        }

        // Can get incoming and outgoing transfers using convenience methods
        [Fact]
        public void TestGetIncomingOutgoingTransfers()
        {
            Assert.True(TEST_NON_RELAYS);

            // get incoming transfers
            List<MoneroIncomingTransfer> inTransfers = wallet.GetIncomingTransfers();
            Assert.False(inTransfers.Count == 0);
            foreach (MoneroIncomingTransfer transfer in inTransfers)
            {
                Assert.True(transfer.IsIncoming());
                TestTransfer(transfer, null);
            }

            // get incoming transfers with query
            ulong? amount = inTransfers[0].GetAmount();
            uint? accountIdx = inTransfers[0].GetAccountIndex();
            uint? subaddressIdx = inTransfers[0].GetSubaddressIndex();
            inTransfers = wallet.GetIncomingTransfers(new MoneroTransferQuery().SetAmount(amount).SetAccountIndex(accountIdx).SetSubaddressIndex(subaddressIdx).SetTxQuery(new MoneroTxQuery().SetIsConfirmed(true)));
            Assert.False(inTransfers.Count == 0);
            foreach (MoneroIncomingTransfer transfer in inTransfers)
            {
                Assert.True(transfer.IsIncoming());
                Assert.Equal(amount, transfer.GetAmount());
                Assert.Equal(accountIdx, (uint)transfer.GetAccountIndex());
                Assert.Equal(subaddressIdx, (uint)transfer.GetSubaddressIndex());
                TestTransfer(transfer, null);
            }

            // get incoming transfers with contradictory query
            try
            {
                inTransfers = wallet.GetIncomingTransfers(new MoneroTransferQuery().SetIsIncoming(false));
            }
            catch (MoneroError e)
            {
                Assert.Equal("Transfer query contradicts getting incoming transfers", e.Message);
            }

            // get outgoing transfers
            List<MoneroOutgoingTransfer> outTransfers = wallet.GetOutgoingTransfers();
            Assert.False(outTransfers.Count == 0);
            foreach (MoneroOutgoingTransfer transfer in outTransfers)
            {
                Assert.True(transfer.IsOutgoing());
                TestTransfer(transfer, null);
            }

            // get outgoing transfers with query
            outTransfers = wallet.GetOutgoingTransfers(new MoneroTransferQuery().SetAccountIndex(accountIdx).SetSubaddressIndex(subaddressIdx));
            Assert.False(outTransfers.Count == 0);
            foreach (MoneroOutgoingTransfer transfer in outTransfers)
            {
                Assert.True(transfer.IsOutgoing());
                Assert.Equal(accountIdx, (uint)transfer.GetAccountIndex());
                Assert.True(transfer.GetSubaddressIndices().Contains((uint)subaddressIdx));
                TestTransfer(transfer, null);
            }

            // get outgoing transfers with contradictory query
            try
            {
                outTransfers = wallet.GetOutgoingTransfers(new MoneroTransferQuery().SetIsOutgoing(false));
            }
            catch (MoneroError e)
            {
                Assert.Equal("Transfer query contradicts getting outgoing transfers", e.Message);
            }
        }

        // Can get outputs in the wallet, accounts, and subaddresses
        [Fact]
        public void TestGetOutputs()
        {
            Assert.True(TEST_NON_RELAYS);

            // get all outputs
            GetAndTestOutputs(wallet, null, true);

            // get outputs for each account
            bool nonDefaultIncoming = false;
            List<MoneroAccount> accounts = wallet.GetAccounts(true);
            foreach (MoneroAccount account in accounts)
            {

                // determine if account is used
                bool isUsed = false;
                foreach (MoneroSubaddress subaddress in account.GetSubaddresses()) if (subaddress.IsUsed() == true) isUsed = true;

                // get outputs by account index
                List<MoneroOutputWallet> accountOutputs = GetAndTestOutputs(wallet, new MoneroOutputQuery().SetAccountIndex(account.GetIndex()), isUsed);
                foreach (MoneroOutputWallet output in accountOutputs) Assert.Equal(account.GetIndex(), output.GetAccountIndex());

                // get outputs by subaddress index
                List<MoneroOutputWallet> subaddressOutputs = new List<MoneroOutputWallet>();
                foreach (MoneroSubaddress subaddress in account.GetSubaddresses())
                {
                    List<MoneroOutputWallet> _outputs = GetAndTestOutputs(wallet, new MoneroOutputQuery().SetAccountIndex(account.GetIndex()).SetSubaddressIndex(subaddress.GetIndex()), subaddress.IsUsed());
                    foreach (MoneroOutputWallet output in _outputs)
                    {
                        Assert.Equal(subaddress.GetAccountIndex(), output.GetAccountIndex());
                        Assert.Equal(subaddress.GetIndex(), output.GetSubaddressIndex());
                        if (output.GetAccountIndex() != 0 && output.GetSubaddressIndex() != 0) nonDefaultIncoming = true;
                        subaddressOutputs.Add(output);
                    }
                }
                Assert.Equal(subaddressOutputs.Count, accountOutputs.Count);

                // get outputs by subaddress indices
                HashSet<uint> subaddressIndices = new HashSet<uint>();
                foreach (MoneroOutputWallet output in subaddressOutputs) subaddressIndices.Add((uint)output.GetSubaddressIndex());
                List<MoneroOutputWallet> outputs = GetAndTestOutputs(wallet, new MoneroOutputQuery().SetAccountIndex(account.GetIndex()).SetSubaddressIndices(new List<uint>(subaddressIndices)), isUsed);
                Assert.Equal(outputs.Count, subaddressOutputs.Count);
                foreach (MoneroOutputWallet output in outputs)
                {
                    Assert.Equal(account.GetIndex(), output.GetAccountIndex());
                    Assert.True(subaddressIndices.Contains((uint)output.GetSubaddressIndex()));
                }
            }

            // ensure output found with non-zero account and subaddress indices
            Assert.True(nonDefaultIncoming, "No outputs found in non-default account and subaddress; run send-to-multiple tests");
        }

        // Can get outputs with additional configuration
        [Fact]
        public void TestGetOutputsWithQuery()
        {
            Assert.True(TEST_NON_RELAYS);

            // get unspent outputs to account 0
            List<MoneroOutputWallet> outputs = GetAndTestOutputs(wallet, new MoneroOutputQuery().SetAccountIndex(0).SetIsSpent(false), null);
            foreach (MoneroOutputWallet output in outputs)
            {
                Assert.True(0 == (uint)output.GetAccountIndex());
                Assert.Equal(false, output.IsSpent());
            }

            // get spent outputs to account 1
            outputs = GetAndTestOutputs(wallet, new MoneroOutputQuery().SetAccountIndex(1).SetIsSpent(true), true);
            foreach (MoneroOutputWallet output in outputs)
            {
                Assert.Equal(1, (int)output.GetAccountIndex());
                Assert.Equal(true, output.IsSpent());
            }

            // get random transactions
            List<MoneroTxWallet> txs = GetRandomTransactions(wallet, new MoneroTxQuery().SetIsConfirmed(true), 3, 5);

            // get outputs with a _tx hash
            List<string> txHashes = new List<string>();
            foreach (MoneroTxWallet tx in txs)
            {
                txHashes.Add(tx.GetHash());
                outputs = GetAndTestOutputs(wallet, new MoneroOutputQuery().SetTxQuery(new MoneroTxQuery().SetHash(tx.GetHash())), true);
                foreach (MoneroOutputWallet output in outputs) Assert.Equal(output.GetTx().GetHash(), tx.GetHash());
            }

            // get outputs with _tx hashes
            outputs = GetAndTestOutputs(wallet, new MoneroOutputQuery().SetTxQuery(new MoneroTxQuery().SetHashes(txHashes)), true);
            foreach (MoneroOutputWallet output in outputs) Assert.True(txHashes.Contains(output.GetTx().GetHash()));

            // get confirmed outputs to specific subaddress with pre-built query
            uint accountIdx = 0;
            uint subaddressIdx = 1;
            MoneroOutputQuery outputQuery = new MoneroOutputQuery();
            outputQuery.SetAccountIndex(accountIdx).SetSubaddressIndex(subaddressIdx);
            outputQuery.SetTxQuery(new MoneroTxQuery().SetIsConfirmed(true));
            outputQuery.SetMinAmount(TestUtils.MAX_FEE);
            outputs = GetAndTestOutputs(wallet, outputQuery, true);
            foreach (MoneroOutputWallet output in outputs)
            {
                Assert.Equal(accountIdx, (uint)output.GetAccountIndex());
                Assert.Equal(subaddressIdx, (uint)output.GetSubaddressIndex());
                Assert.Equal(true, output.GetTx().IsConfirmed());
                Assert.True(output.GetAmount() >= TestUtils.MAX_FEE);
            }

            // get output by _key image
            string keyImage = outputs[0].GetKeyImage().GetHex();
            outputs = wallet.GetOutputs(new MoneroOutputQuery().SetKeyImage(new MoneroKeyImage(keyImage)));
            Assert.Equal(1, outputs.Count);
            Assert.Equal(keyImage, outputs[0].GetKeyImage().GetHex());

            // get outputs whose transaction is confirmed and has incoming and outgoing transfers
            outputs = wallet.GetOutputs(new MoneroOutputQuery().SetTxQuery(new MoneroTxQuery().SetIsConfirmed(true).SetIsIncoming(true).SetIsOutgoing(true).SetIncludeOutputs(true)));
            Assert.False(outputs.Count == 0);
            foreach (MoneroOutputWallet output in outputs)
            {
                Assert.True(output.GetTx().IsIncoming());
                Assert.True(output.GetTx().IsOutgoing());
                Assert.True(output.GetTx().IsConfirmed());
                Assert.False(output.GetTx().GetOutputsWallet().Count == 0);
                Assert.True(output.GetTx().GetOutputsWallet().Contains(output));
            }
        }

        // Validates inputs when getting wallet outputs
        [Fact]
        public void TestValidateInputsGetOutputs()
        {
            Assert.True(TEST_NON_RELAYS && !LITE_MODE);

            // test with invalid hash
            List<MoneroOutputWallet> outputs = wallet.GetOutputs(new MoneroOutputQuery().SetTxQuery(new MoneroTxQuery().SetHash("invalid_id")));
            Assert.True(0 == outputs.Count);

            // test invalid hash in collection
            List<MoneroTxWallet> randomTxs = GetRandomTransactions(wallet, new MoneroTxQuery().SetIsConfirmed(true).SetIncludeOutputs(true), 3, 5);
            foreach (MoneroTxWallet randomTx in randomTxs) Assert.False(randomTx.GetOutputs().Count == 0);
            outputs = wallet.GetOutputs(new MoneroOutputQuery().SetTxQuery(new MoneroTxQuery().SetHashes([randomTxs[0].GetHash(), "invalid_id"])));
            Assert.False(outputs.Count == 0);
            Assert.Equal(outputs.Count, randomTxs[0].GetOutputs().Count);
            MoneroTxWallet tx = outputs[0].GetTx();
            foreach (MoneroOutputWallet output in outputs) Assert.True(tx == output.GetTx());
        }

        // Can export outputs in hex format
        [Fact]
        public void TestExportOutputs()
        {
            Assert.True(TEST_NON_RELAYS);
            string outputsHex = wallet.ExportOutputs();
            Assert.NotNull(outputsHex);  // TODO: this will fail if wallet has no outputs; run these tests on new wallet
            Assert.True(outputsHex.Length > 0);

            // wallet exports outputs since last export by default
            outputsHex = wallet.ExportOutputs();
            string outputsHexAll = wallet.ExportOutputs(true);
            Assert.True(outputsHexAll.Length > outputsHex.Length);
        }

        // Can import outputs in hex format
        [Fact]
        public void TestImportOutputs()
        {
            Assert.True(TEST_NON_RELAYS);

            // export outputs hex
            string outputsHex = wallet.ExportOutputs();

            // import outputs hex
            if (outputsHex != null)
            {
                int numImported = wallet.ImportOutputs(outputsHex);
                Assert.True(numImported >= 0);
            }
        }

        // Has correct accounting across accounts, subaddresses, txs, transfers, and outputs
        [Fact]
        public void TestAccounting()
        {
            Assert.True(TEST_NON_RELAYS);

            // pre-fetch wallet balances, accounts, subaddresses, and txs
            ulong walletBalance = wallet.GetBalance();
            ulong walletUnlockedBalance = wallet.GetUnlockedBalance();
            List<MoneroAccount> accounts = wallet.GetAccounts(true);  // includes subaddresses

            // test wallet balance
            TestUtils.TestUnsignedBigInteger(walletBalance);
            TestUtils.TestUnsignedBigInteger(walletUnlockedBalance);
            Assert.True(walletBalance >= walletUnlockedBalance);

            // test that wallet balance equals sum of account balances
            ulong accountsBalance = 0;
            ulong accountsUnlockedBalance = 0;
            foreach (MoneroAccount account in accounts)
            {
                TestAccount(account); // test that account balance equals sum of subaddress balances
                accountsBalance += account.GetBalance();
                accountsUnlockedBalance += account.GetUnlockedBalance();
            }
            Assert.Equal(walletBalance, accountsBalance);
            Assert.Equal(walletUnlockedBalance, accountsUnlockedBalance);

            // balance may not equal sum of unspent outputs if unconfirmed txs
            // TODO monero-wallet-rpc: reason not to return unspent outputs on unconfirmed txs? then this isn't necessary
            List<MoneroTxWallet> txs = wallet.GetTxs();
            bool hasUnconfirmedTx = false;
            foreach (MoneroTxWallet tx in txs) if (tx.InTxPool() == true) hasUnconfirmedTx = true;

            // wallet balance is sum of all unspent outputs
            ulong walletSum = 0;
            foreach (MoneroOutputWallet output in wallet.GetOutputs(new MoneroOutputQuery().SetIsSpent(false))) walletSum += (ulong)output.GetAmount();
            if (!walletBalance.Equals(walletSum))
            {

                // txs may have changed in between calls so retry test
                walletSum = 0;
                foreach (MoneroOutputWallet output in wallet.GetOutputs(new MoneroOutputQuery().SetIsSpent(false))) walletSum += (ulong)output.GetAmount();
                if (!walletBalance.Equals(walletSum)) Assert.True(hasUnconfirmedTx, "Wallet balance must equal sum of unspent outputs if no unconfirmed txs");
            }

            // account balances are sum of their unspent outputs
            foreach (MoneroAccount account in accounts)
            {
                ulong accountSum = 0;
                List<MoneroOutputWallet> accountOutputs = wallet.GetOutputs(new MoneroOutputQuery().SetAccountIndex(account.GetIndex()).SetIsSpent(false));
                foreach (MoneroOutputWallet output in accountOutputs) accountSum += (uint)output.GetAmount();
                if (!account.GetBalance().Equals(accountSum)) Assert.True(hasUnconfirmedTx, "Account balance must equal sum of its unspent outputs if no unconfirmed txs");

                // subaddress balances are sum of their unspent outputs
                foreach (MoneroSubaddress subaddress in account.GetSubaddresses())
                {
                    ulong subaddressSum = 0;
                    List<MoneroOutputWallet> subaddressOutputs = wallet.GetOutputs(new MoneroOutputQuery().SetAccountIndex(account.GetIndex()).SetSubaddressIndex(subaddress.GetIndex()).SetIsSpent(false));
                    foreach (MoneroOutputWallet output in subaddressOutputs) subaddressSum += (uint)output.GetAmount();
                    if (!subaddress.GetBalance().Equals(subaddressSum)) Assert.True(hasUnconfirmedTx, "Subaddress balance must equal sum of its unspent outputs if no unconfirmed txs");
                }
            }
        }

        // Can checkReserve a transfer using the transaction's secret key and the destination
        [Fact]
        public void TestCheckTxKey()
        {
            Assert.True(TEST_NON_RELAYS);

            // get random txs that are confirmed and have outgoing destinations
            List<MoneroTxWallet> txs;
            try
            {
                txs = GetRandomTransactions(wallet, new MoneroTxQuery().SetIsConfirmed(true).SetTransferQuery(new MoneroTransferQuery().SetHasDestinations(true)), 1, MAX_TX_PROOFS);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("found with")) Assert.Fail("No txs with outgoing destinations found; run send Tests");
                throw e;
            }

            // Test good checks
            Assert.True(txs.Count > 0, "No transactions found with outgoing destinations");
            foreach (MoneroTxWallet _tx in txs)
            {
                string _key = wallet.GetTxKey(_tx.GetHash());
                Assert.False(_tx.GetOutgoingTransfer().GetDestinations().Count == 0);
                foreach (MoneroDestination dest in _tx.GetOutgoingTransfer().GetDestinations())
                {
                    MoneroCheckTx _check = wallet.CheckTxKey(_tx.GetHash(), _key, dest.GetAddress());
                    if (dest.GetAmount() > 0)
                    {
                        // TODO monero-wallet-rpc: indicates amount received amount is 0 despite transaction with transfer to this address
                        // TODO monero-wallet-rpc: returns 0-4 errors, not consistent
                        //        Assert.True(_check.GetReceivedAmount().CompareTo(BigInteger.valueOf(0)) > 0);
                        if (_check.GetReceivedAmount().Equals(0))
                        {
                            Console.WriteLine("WARNING: _key proof indicates no funds received despite transfer (txid=" + _tx.GetHash() + ", _key=" + _key + ", address=" + dest.GetAddress() + ", amount=" + dest.GetAmount() + ")");
                        }
                    }
                    else Assert.True(_check.GetReceivedAmount().Equals(0));
                    TestCheckTx(_tx, _check);
                }
            }

            // Test get _tx _key with invalid hash
            try
            {
                wallet.GetTxKey("invalid_tx_id");
                Assert.Fail("Should throw exception foreach invalid _key");
            }
            catch (MoneroError e)
            {
                TestInvalidTxHashError(e);
            }

            // Test _check with invalid _tx hash
            MoneroTxWallet tx = txs[0];
            string key = wallet.GetTxKey(tx.GetHash());
            MoneroDestination destination = tx.GetOutgoingTransfer().GetDestinations()[0];
            try
            {
                wallet.CheckTxKey("invalid_tx_id", key, destination.GetAddress());
                Assert.Fail("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                TestInvalidTxHashError(e);
            }

            // Test _check with invalid _key
            try
            {
                wallet.CheckTxKey(tx.GetHash(), "invalid_tx_key", destination.GetAddress());
                Assert.Fail("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                TestInvalidTxKeyError(e);
            }

            // Test _check with invalid address
            try
            {
                wallet.CheckTxKey(tx.GetHash(), key, "invalid_tx_address");
                throw new Exception("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                TestInvalidAddressError(e);
            }

            // Test _check with different address
            string? differentAddress = null;
            foreach (MoneroTxWallet aTx in wallet.GetTxs())
            {
                if (aTx.GetOutgoingTransfer() == null || aTx.GetOutgoingTransfer().GetDestinations() == null) continue;
                foreach (MoneroDestination aDestination in aTx.GetOutgoingTransfer().GetDestinations())
                {
                    if (!aDestination.GetAddress().Equals(destination.GetAddress()))
                    {
                        differentAddress = aDestination.GetAddress();
                        break;
                    }
                }
            }
            if (differentAddress == null) throw new Exception("Could not get a different outgoing address to Test; run send Tests");
            MoneroCheckTx check = wallet.CheckTxKey(tx.GetHash(), key, differentAddress);
            Assert.True(check.IsGood());
            Assert.True(check.GetReceivedAmount() >= 0);
            TestCheckTx(tx, check);
        }

        // Can prove a transaction by getting its sig
        [Fact]
        public void TestCheckTxProof()
        {
            Assert.True(TEST_NON_RELAYS);

            // get random txs with outgoing destinations
            List<MoneroTxWallet> txs;
            try
            {
                txs = GetRandomTransactions(wallet, new MoneroTxQuery().SetTransferQuery(new MoneroTransferQuery().SetHasDestinations(true)), 2, MAX_TX_PROOFS);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("found with")) Assert.Fail("No txs with outgoing destinations found; run send Tests");
                throw e;
            }

            // Test good checks with messages
            foreach (MoneroTxWallet _tx in txs)
            {
                foreach (MoneroDestination dest in _tx.GetOutgoingTransfer().GetDestinations())
                {
                    string sig = wallet.GetTxProof(_tx.GetHash(), dest.GetAddress(), "This transaction definitely happened.");
                    MoneroCheckTx _check = wallet.CheckTxProof(_tx.GetHash(), dest.GetAddress(), "This transaction definitely happened.", sig);
                    TestCheckTx(_tx, _check);
                }
            }

            // Test good checkReserve without message
            MoneroTxWallet tx = txs[0];
            MoneroDestination destination = tx.GetOutgoingTransfer().GetDestinations()[0];
            string signature = wallet.GetTxProof(tx.GetHash(), destination.GetAddress());
            MoneroCheckTx check = wallet.CheckTxProof(tx.GetHash(), destination.GetAddress(), null, signature);
            TestCheckTx(tx, check);

            // Test get proof with invalid hash
            try
            {
                wallet.GetTxProof("invalid_tx_id", destination.GetAddress());
                throw new Exception("Should throw exception for invalid key");
            }
            catch (MoneroError e)
            {
                TestInvalidTxHashError(e);
            }

            // Test checkReserve with invalid _tx hash
            try
            {
                wallet.CheckTxProof("invalid_tx_id", destination.GetAddress(), null, signature);
                Assert.Fail("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                TestInvalidTxHashError(e);
            }

            // Test checkReserve with invalid address
            try
            {
                wallet.CheckTxProof(tx.GetHash(), "invalid_tx_address", null, signature);
                Assert.Fail("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                TestInvalidAddressError(e);
            }

            // Test checkReserve with wrong message
            signature = wallet.GetTxProof(tx.GetHash(), destination.GetAddress(), "This is the right message");
            check = wallet.CheckTxProof(tx.GetHash(), destination.GetAddress(), "This is the wrong message", signature);
            Assert.Equal(check.IsGood(), false);
            TestCheckTx(tx, check);

            // Test checkReserve with wrong sig
            string wrongSignature = wallet.GetTxProof(txs[1].GetHash(), txs[1].GetOutgoingTransfer().GetDestinations()[0].GetAddress(), "This is the right message");
            try
            {
                check = wallet.CheckTxProof(tx.GetHash(), destination.GetAddress(), "This is the right message", wrongSignature);
                Assert.Equal(check.IsGood(), false);
            }
            catch (MoneroError e)
            {
                TestInvalidSignatureError(e);
            }

            // Test checkReserve with empty sig
            try
            {
                check = wallet.CheckTxProof(tx.GetHash(), destination.GetAddress(), "This is the right message", "");
                Assert.Equal(check.IsGood(), false);
            }
            catch (MoneroError e)
            {
                Assert.Equal("Must provide sig to checkReserve _tx proof", e.Message);
            }
        }

        // Can prove a spend using a generated signature and no destination public address
        [Fact]
        public void TestCheckSpendProof()
        {
            Assert.True(TEST_NON_RELAYS);

            // get random confirmed outgoing txs
            List<MoneroTxWallet> txs = GetRandomTransactions(wallet, new MoneroTxQuery().SetIsIncoming(false).SetInTxPool(false).SetIsFailed(false), 2, MAX_TX_PROOFS);
            foreach (MoneroTxWallet _tx in txs)
            {
                Assert.Equal(true, _tx.IsConfirmed());
                Assert.Null(_tx.GetIncomingTransfers());
                Assert.NotNull(_tx.GetOutgoingTransfer());
            }

            // test good checks with messages
            foreach (MoneroTxWallet _tx in txs)
            {
                string sig = wallet.GetSpendProof(_tx.GetHash(), "I am a message.");
                Assert.True(wallet.CheckSpendProof(_tx.GetHash(), "I am a message.", sig));
            }

            // test good checkReserve without message
            MoneroTxWallet tx = txs[0];
            string signature = wallet.GetSpendProof(tx.GetHash());
            Assert.True(wallet.CheckSpendProof(tx.GetHash(), null, signature));

            // test get proof with invalid hash
            try
            {
                wallet.GetSpendProof("invalid_tx_id");
                throw new Exception("Should throw exception foreach invalid key");
            }
            catch (MoneroError e)
            {
                TestInvalidTxHashError(e);
            }

            // test checkReserve with invalid _tx hash
            try
            {
                wallet.CheckSpendProof("invalid_tx_id", null, signature);
                throw new Exception("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                TestInvalidTxHashError(e);
            }

            // test checkReserve with invalid message
            signature = wallet.GetSpendProof(tx.GetHash(), "This is the right message");
            Assert.Equal(false, wallet.CheckSpendProof(tx.GetHash(), "This is the wrong message", signature));

            // test checkReserve with wrong sig
            signature = wallet.GetSpendProof(txs[1].GetHash(), "This is the right message");
            Assert.Equal(false, wallet.CheckSpendProof(tx.GetHash(), "This is the right message", signature));
        }

        // Can prove reserves in the wallet
        [Fact]
        public void TestGetReserveProofWallet()
        {
            Assert.True(TEST_NON_RELAYS);

            // get proof of entire wallet
            string signature = wallet.GetReserveProofWallet("Test message");

            // checkReserve proof of entire wallet
            MoneroCheckReserve check = wallet.CheckReserveProof(wallet.GetPrimaryAddress(), "Test message", signature);
            Assert.True(check.IsGood());
            TestCheckReserve(check);
            ulong balance = wallet.GetBalance();
            if (!balance.Equals(check.GetTotalAmount()))
            {  // TODO monero-wallet-rpc: this checkReserve Assert.Fails with unconfirmed txs
                List<MoneroTxWallet> unconfirmedTxs = wallet.GetTxs(new MoneroTxQuery().SetInTxPool(true));
                Assert.True(unconfirmedTxs.Count > 0, "Reserve amount must equal balance unless wallet has unconfirmed txs");
            }

            // Test different wallet address
            string differentAddress = TestUtils.GetExternalWalletAddress();
            try
            {
                wallet.CheckReserveProof(differentAddress, "Test message", signature);
                Assert.Fail("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                TestNoSubaddressError(e);
            }

            // Test subaddress
            try
            {
                wallet.CheckReserveProof((wallet.GetSubaddress(0, 1)).GetAddress(), "Test message", signature);
                Assert.Fail("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                TestNoSubaddressError(e);
            }

            // Test wrong message
            check = wallet.CheckReserveProof(wallet.GetPrimaryAddress(), "Wrong message", signature);
            Assert.Equal(false, check.IsGood());  // TODO: specifically Test reserve checks, probably separate objects
            TestCheckReserve(check);

            // Test wrong signature
            try
            {
                wallet.CheckReserveProof(wallet.GetPrimaryAddress(), "Test message", "wrong signature");
                Assert.Fail("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                TestSignatureHeaderCheckError(e);
            }
        }

        // Can prove reserves in an account
        [Fact]
        public void TestGetReserveProofAccount()
        {
            Assert.True(TEST_NON_RELAYS);

            // Test proofs of accounts
            int numNonZeroTests = 0;
            string msg = "Test message";
            List<MoneroAccount> accounts = wallet.GetAccounts();
            string? signature = null;
            foreach (MoneroAccount account in accounts)
            {
                if (account.GetBalance() > 0)
                {
                    ulong checkAmount = (account.GetBalance()) / 2;
                    signature = wallet.GetReserveProofAccount((uint)account.GetIndex(), checkAmount, msg);
                    MoneroCheckReserve checkReserve = wallet.CheckReserveProof(wallet.GetPrimaryAddress(), msg, signature);
                    Assert.True(checkReserve.IsGood());
                    TestCheckReserve(checkReserve);
                    Assert.True(checkReserve.GetTotalAmount() >= checkAmount);
                    numNonZeroTests++;
                }
                else
                {
                    try
                    {
                        wallet.GetReserveProofAccount((uint)account.GetIndex(), account.GetBalance(), msg);
                        throw new Exception("Should have thrown exception");
                    }
                    catch (MoneroError e)
                    {
                        Assert.Equal(-1, (int)e.GetCode());
                        try
                        {
                            wallet.GetReserveProofAccount((uint)account.GetIndex(), TestUtils.MAX_FEE, msg);
                            throw new Exception("Should have thrown exception");
                        }
                        catch (MoneroError e2)
                        {
                            Assert.Equal(-1, (int)e2.GetCode());
                        }
                    }
                }
            }
            Assert.True(numNonZeroTests > 1, "Must have more than one account with non-zero balance; run send-to-multiple Tests");

            // Test error when not enough balance foreach requested minimum reserve amount
            try
            {
                string proof = wallet.GetReserveProofAccount(0, accounts[0].GetBalance() + TestUtils.MAX_FEE, "Test message");
                Console.WriteLine("Account balance: " + wallet.GetBalance(0));
                Console.WriteLine("accounts[0] balance: " + accounts[0].GetBalance());
                MoneroCheckReserve reserve = wallet.CheckReserveProof(wallet.GetPrimaryAddress(), "Test message", proof);
                try
                {
                    wallet.GetReserveProofAccount(0, accounts[0].GetBalance() + TestUtils.MAX_FEE, "Test message");
                    throw new Exception("expecting this to succeed");
                }
                catch (Exception e)
                {
                    Assert.Equal("expecting this to succeed", e.Message);
                }
                //Console.WriteLine("Check reserve proof: " + JsonUtils.serialize(reserve));
                Assert.Fail("Should have thrown exception but got reserve proof: https://github.Com/monero-project/monero/issues/6595");
            }
            catch (MoneroError e)
            {
                Assert.Equal(-1, (int)e.GetCode());
            }

            // Test different wallet address
            string differentAddress = TestUtils.GetExternalWalletAddress();
            try
            {
                wallet.CheckReserveProof(differentAddress, "Test message", signature);
                Assert.Fail("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                Assert.Equal(-1, (int)e.GetCode());
            }

            // Test subaddress
            try
            {
                wallet.CheckReserveProof((wallet.GetSubaddress(0, 1)).GetAddress(), "Test message", signature);
                Assert.Fail("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                Assert.Equal(-1, (int)e.GetCode());
            }

            // Test wrong message
            MoneroCheckReserve check = wallet.CheckReserveProof(wallet.GetPrimaryAddress(), "Wrong message", signature);
            Assert.Equal(check.IsGood(), false); // TODO: specifically Test reserve checks, probably separate objects
            TestCheckReserve(check);

            // Test wrong signature
            try
            {
                wallet.CheckReserveProof(wallet.GetPrimaryAddress(), "Test message", "wrong signature");
                Assert.Fail("Should have thrown exception");
            }
            catch (MoneroError e)
            {
                Assert.Equal(-1, (int)e.GetCode());
            }
        }

        // Can get and set a transaction note
        [Fact]
        public void TestSetTxNote()
        {
            Assert.True(TEST_NON_RELAYS);
            List<MoneroTxWallet> txs = GetRandomTransactions(wallet, null, 1, 5);

            // set notes
            string uuid = GenUtils.GetUUID();
            for (int i = 0; i < txs.Count; i++)
            {
                wallet.SetTxNote(txs[i].GetHash(), uuid + i);
            }

            // get notes
            for (int i = 0; i < txs.Count; i++)
            {
                Assert.Equal(wallet.GetTxNote(txs[i].GetHash()), uuid + i);
            }
        }

        // Can get and set multiple transaction notes
        // TODO: why does getting cached txs take 2 seconds when should already be cached?
        [Fact]
        public void TestSetTxNotes()
        {
            Assert.True(TEST_NON_RELAYS);

            // set tx notes
            string uuid = GenUtils.GetUUID();
            List<MoneroTxWallet> txs = wallet.GetTxs();
            Assert.True(txs.Count >= 3, "Test requires 3 or more wallet transactions; run send tests");
            List<string> txHashes = new List<string>();
            List<string> txNotes = new List<string>();
            for (int i = 0; i < txHashes.Count; i++)
            {
                txHashes.Add(txs[i].GetHash());
                txNotes.Add(uuid + i);
            }
            wallet.SetTxNotes(txHashes, txNotes);

            // get tx notes
            txNotes = wallet.GetTxNotes(txHashes);
            for (int i = 0; i < txHashes.Count; i++)
            {
                Assert.Equal(uuid + i, txNotes[i]);
            }

            // TODO: test that get transaction has note
        }

        // Can export signed key images
        [Fact]
        public void TestExportKeyImages()
        {
            Assert.True(TEST_NON_RELAYS);
            List<MoneroKeyImage> images = wallet.ExportKeyImages(true);
            Assert.True(images.Count > 0, "No signed key images in wallet");
            foreach (MoneroKeyImage image in images)
            {
                Assert.True(image is MoneroKeyImage);
                Assert.True(image.GetHex().Length > 0);
                Assert.True(image.GetSignature().Length > 0);
            }

            // wallet exports key images since last export by default
            images = wallet.ExportKeyImages();
            List<MoneroKeyImage> imagesAll = wallet.ExportKeyImages(true);
            Assert.True(imagesAll.Count > images.Count);
        }

        // Can get new key images from the last import
        [Fact]
        public void TestGetNewKeyImagesFromLastImport()
        {
            Assert.True(TEST_NON_RELAYS);

            // get outputs hex
            string outputsHex = wallet.ExportOutputs();

            // import outputs hex
            if (outputsHex != null)
            {
                int numImported = wallet.ImportOutputs(outputsHex);
                Assert.True(numImported >= 0);
            }

            // get and test new key images from last import
            List<MoneroKeyImage> images = wallet.GetNewKeyImagesFromLastImport();
            if (images.Count == 0) Assert.Fail("No new key images in last import"); // TODO: these are already known to the wallet, so no new key images will be imported
            foreach (MoneroKeyImage image in images)
            {
                Assert.True(image.GetHex().Length > 0);
                Assert.True(image.GetSignature().Length > 0);
            }
        }

        // Can import key images
        // TODO monero-project: importing key images can cause erasure of incoming transfers per wallet2.Cpp:11957
        [Fact]
        public void TestImportKeyImages()
        {
            Assert.True(TEST_NON_RELAYS);
            List<MoneroKeyImage> images = wallet.ExportKeyImages();
            Assert.True(images.Count > 0, "Wallet does not have any key images; run send tests");
            MoneroKeyImageImportResult result = wallet.ImportKeyImages(images);
            Assert.True(result.GetHeight() > 0);

            // determine if non-zero spent and unspent amounts are expected
            List<MoneroTxWallet> txs = wallet.GetTxs(new MoneroTxQuery().SetIsOutgoing(true).SetIsConfirmed(true));
            ulong balance = wallet.GetBalance();
            bool hasSpent = txs.Count > 0;
            bool hasUnspent = balance > 0;

            // test amounts
            TestUtils.TestUnsignedBigInteger(result.GetSpentAmount(), hasSpent);
            TestUtils.TestUnsignedBigInteger(result.GetUnspentAmount(), hasUnspent);
        }

        // Supports view-only and offline wallets to create, sign, and submit transactions
        [Fact]
        public void TestViewOnlyAndOfflineWallets()
        {
            Assert.True(!LITE_MODE && (TEST_NON_RELAYS || TEST_RELAYS));

            // create view-only and offline wallets
            MoneroWallet viewOnlyWallet = CreateWallet(new MoneroWalletConfig().SetPrimaryAddress(wallet.GetPrimaryAddress()).SetPrivateViewKey(wallet.GetPrivateViewKey()).SetRestoreHeight(TestUtils.FIRST_RECEIVE_HEIGHT));
            MoneroWallet offlineWallet = CreateWallet(new MoneroWalletConfig().SetPrimaryAddress(wallet.GetPrimaryAddress()).SetPrivateViewKey(wallet.GetPrivateViewKey()).SetPrivateSpendKey(wallet.GetPrivateSpendKey()).SetServerUri(TestUtils.OFFLINE_SERVER_URI).SetRestoreHeight(0l));
            Assert.False(offlineWallet.IsConnectedToDaemon());
            viewOnlyWallet.Sync();

            // test tx signing with wallets
            try
            {
                CheckViewOnlyAndOfflineWallets(viewOnlyWallet, offlineWallet);
            }
            finally
            {
                CloseWallet(viewOnlyWallet);
                CloseWallet(offlineWallet);
            }
        }

        // Can sign and verify messages
        // TODO: test with view-only wallet
        [Fact]
        public void TestSignAndVerifyMessages()
        {
            Assert.True(TEST_NON_RELAYS);

            // message to sign and subaddresses to test
            string msg = "This is a super important message which needs to be signed and verified.";
            List<MoneroSubaddress> subaddresses = [new MoneroSubaddress(0, 0), new MoneroSubaddress(0, 1), new MoneroSubaddress(1, 0)];

            // test signing message with subaddresses
            foreach (MoneroSubaddress subaddress in subaddresses)
            {

                // sign and verify message with spend key
                string signature = wallet.SignMessage(msg, MoneroMessageSignatureType.SIGN_WITH_SPEND_KEY, (uint)subaddress.GetAccountIndex(), (uint)subaddress.GetIndex());
                MoneroMessageSignatureResult result = wallet.VerifyMessage(msg, wallet.GetAddress((uint)subaddress.GetAccountIndex(), (uint)subaddress.GetIndex()), signature);
                Assert.Equal(new MoneroMessageSignatureResult(true, false, MoneroMessageSignatureType.SIGN_WITH_SPEND_KEY, 2), result);

                // verify message with incorrect address
                result = wallet.VerifyMessage(msg, wallet.GetAddress(0, 2), signature);
                Assert.Equal(new MoneroMessageSignatureResult(false, null, null, null), result);

                // verify message with invalid address
                result = wallet.VerifyMessage(msg, "invalid address", signature);
                Assert.Equal(new MoneroMessageSignatureResult(false, null, null, null), result);

                // verify message with external address
                result = wallet.VerifyMessage(msg, TestUtils.GetExternalWalletAddress(), signature);
                Assert.Equal(new MoneroMessageSignatureResult(false, null, null, null), result);

                // sign and verify message with view key
                signature = wallet.SignMessage(msg, MoneroMessageSignatureType.SIGN_WITH_VIEW_KEY, (uint)subaddress.GetAccountIndex(), (uint)subaddress.GetIndex());
                result = wallet.VerifyMessage(msg, wallet.GetAddress((uint)subaddress.GetAccountIndex(), (uint)subaddress.GetIndex()), signature);
                Assert.Equal(new MoneroMessageSignatureResult(true, false, MoneroMessageSignatureType.SIGN_WITH_VIEW_KEY, 2), result);

                // verify message with incorrect address
                result = wallet.VerifyMessage(msg, wallet.GetAddress(0, 2), signature);
                Assert.Equal(new MoneroMessageSignatureResult(false, null, null, null), result);

                // verify message with external address
                result = wallet.VerifyMessage(msg, TestUtils.GetExternalWalletAddress(), signature);
                Assert.Equal(new MoneroMessageSignatureResult(false, null, null, null), result);

                // verify message with invalid address
                result = wallet.VerifyMessage(msg, "invalid address", signature);
                Assert.Equal(new MoneroMessageSignatureResult(false, null, null, null), result);
            }
        }

        // Has an address book
        [Fact]
        public void TestAddressBook()
        {
            Assert.True(TEST_NON_RELAYS);

            // initial state
            List<MoneroAddressBookEntry> entries = wallet.GetAddressBookEntries();
            int numEntriesStart = entries.Count;
            foreach (MoneroAddressBookEntry entry in entries) TestAddressBookEntry(entry);

            // test adding standard addresses
            int NUM_ENTRIES = 5;
            string address = TestUtils.GetExternalWalletAddress();
            List<uint> indices = new List<uint>();
            for (int i = 0; i < NUM_ENTRIES; i++)
            {
                indices.Add(wallet.AddAddressBookEntry(address, "hi there!"));
            }
            entries = wallet.GetAddressBookEntries();
            Assert.Equal(numEntriesStart + NUM_ENTRIES, entries.Count);
            foreach (int idx in indices)
            {
                bool found = false;
                foreach (MoneroAddressBookEntry entry in entries)
                {
                    if (idx == entry.GetIndex())
                    {
                        TestAddressBookEntry(entry);
                        Assert.Equal(entry.GetAddress(), address);
                        Assert.Equal(entry.GetDescription(), "hi there!");
                        found = true;
                        break;
                    }
                }
                Assert.True(found, "Index " + idx + " not found in address book indices");
            }

            // edit each address book entry
            foreach (uint idx in indices)
            {
                wallet.EditAddressBookEntry(idx, false, null, true, "hello there!!");
            }
            entries = wallet.GetAddressBookEntries(indices);
            foreach (MoneroAddressBookEntry entry in entries)
            {
                Assert.Equal("hello there!!", entry.GetDescription());
            }

            // delete entries at starting index
            uint deleteIdx = indices[0];
            for (int i = 0; i < indices.Count; i++)
            {
                wallet.DeleteAddressBookEntry(deleteIdx);
            }
            entries = wallet.GetAddressBookEntries();
            Assert.Equal(entries.Count, numEntriesStart);

            // test adding integrated addresses
            indices = new List<uint>();
            string paymentId = "03284e41c342f03"; // payment id less one character
            Dictionary<uint, MoneroIntegratedAddress> integratedAddresses = [];
            Dictionary<uint, string> integratedDescriptions = [];
            for (int i = 0; i < NUM_ENTRIES; i++)
            {
                MoneroIntegratedAddress integratedAddress = wallet.GetIntegratedAddress(null, paymentId + i); // create unique integrated address
                string uuid = GenUtils.GetUUID();
                uint idx = wallet.AddAddressBookEntry(integratedAddress.ToString(), uuid);
                indices.Add(idx);
                integratedAddresses.Add(idx, integratedAddress);
                integratedDescriptions.Add(idx, uuid);
            }
            entries = wallet.GetAddressBookEntries();
            Assert.Equal(entries.Count, numEntriesStart + NUM_ENTRIES);
            foreach (uint idx in indices)
            {
                bool found = false;
                foreach (MoneroAddressBookEntry entry in entries)
                {
                    if (idx == entry.GetIndex())
                    {
                        TestAddressBookEntry(entry);
                        Assert.Equal(integratedDescriptions[idx], entry.GetDescription());
                        Assert.Equal(integratedAddresses[idx].ToString(), entry.GetAddress());
                        Assert.Equal(null, entry.GetPaymentId());
                        found = true;
                        break;
                    }
                }
                Assert.True(found, "Index " + idx + " not found in address book indices");
            }

            // delete entries at starting index
            deleteIdx = indices[0];
            for (int i = 0; i < indices.Count; i++)
            {
                wallet.DeleteAddressBookEntry(deleteIdx);
            }
            entries = wallet.GetAddressBookEntries();
            Assert.Equal(numEntriesStart, entries.Count);
        }

        // Can get and set arbitrary key/value attributes
        [Fact]
        public void TestSetAttributes()
        {
            Assert.True(TEST_NON_RELAYS);

            // set attributes
            Dictionary<string, string> attrs = [];
            for (int i = 0; i < 5; i++)
            {
                string key = "attr" + i;
                string val = GenUtils.GetUUID();
                attrs.Add(key, val);
                wallet.SetAttribute(key, val);
            }

            // test attributes
            foreach (string key in attrs.Keys)
            {
                Assert.Equal(wallet.GetAttribute(key), attrs[key]);
            }

            // get an undefined attribute
            Assert.Null(wallet.GetAttribute("unset_key"));
        }

        // Can convert between a tx config and payment URI
        [Fact]
        public void TestGetPaymentUri()
        {
            Assert.True(TEST_NON_RELAYS);

            // test with address and amount
            MoneroTxConfig config1 = new MoneroTxConfig().SetAddress(wallet.GetAddress(0, 0)).SetAmount(0);
            string uri = wallet.GetPaymentUri(config1);
            MoneroTxConfig config2 = wallet.ParsePaymentUri(uri);
            Assert.Equal(config1, config2);

            // test with subaddress and all fields
            config1.GetDestinations()[0].SetAddress(wallet.GetSubaddress(0, 1).GetAddress());
            config1.GetDestinations()[0].SetAmount(425000000000);
            config1.SetRecipientName("John Doe");
            config1.SetNote("OMZG XMR FTW");
            uri = wallet.GetPaymentUri(config1);
            config2 = wallet.ParsePaymentUri(uri);
            Assert.Equal(config1, config2);

            // test with undefined address
            string address = config1.GetDestinations()[0].GetAddress();
            config1.GetDestinations()[0].SetAddress(null);
            try
            {
                wallet.GetPaymentUri(config1);
                Assert.Fail("Should have thrown RPC exception with invalid parameters");
            }
            catch (MoneroError e)
            {
                Assert.True(e.Message.Contains("Cannot make URI from supplied parameters"));
            }
            config1.GetDestinations()[0].SetAddress(address);

            // test with standalone payment id
            config1.SetPaymentId("03284e41c342f03603284e41c342f03603284e41c342f03603284e41c342f036");
            try
            {
                wallet.GetPaymentUri(config1);
                Assert.Fail("Should have thrown RPC exception with invalid parameters");
            }
            catch (MoneroError e)
            {
                Assert.True(e.Message.Contains("Cannot make URI from supplied parameters"));
            }
        }

        // Can start and stop mining
        [Fact]
        public void TestMining()
        {
            Assert.True(TEST_NON_RELAYS);
            MoneroMiningStatus status = daemon.GetMiningStatus();
            if (status.IsActive() == true) wallet.StopMining();
            wallet.StartMining(2, false, true);
            wallet.StopMining();
        }

        // Can change the wallet password
        [Fact]
        public void TestChangePassword()
        {

            // create random wallet
            MoneroWallet wallet = CreateWallet(new MoneroWalletConfig().SetPassword(TestUtils.WALLET_PASSWORD));
            string path = wallet.GetPath();

            // change password
            string newPassword = "";
            wallet.ChangePassword(TestUtils.WALLET_PASSWORD, newPassword);

            // close wallet without saving
            CloseWallet(wallet);

            // old password does not work (password change is auto saved)
            try
            {
                OpenWallet(new MoneroWalletConfig().SetPath(path).SetPassword(TestUtils.WALLET_PASSWORD));
                Assert.Fail("Should have thrown");
            }
            catch (Exception e)
            {
                Assert.True(e.Message.ToLower().Contains("failed to open wallet") || e.Message.ToLower().Contains("invalid password")); // TODO: different errors from rpc and wallet2
            }

            // open wallet with new password
            wallet = OpenWallet(new MoneroWalletConfig().SetPath(path).SetPassword(newPassword));

            // change password with incorrect password
            try
            {
                wallet.ChangePassword("badpassword", newPassword);
                Assert.Fail("Should have thrown");
            }
            catch (Exception e)
            {
                Assert.Equal("Invalid original password.", e.Message);
            }

            // save and close
            CloseWallet(wallet, true);

            // open wallet
            wallet = OpenWallet(new MoneroWalletConfig().SetPath(path).SetPassword(newPassword));

            // close wallet
            CloseWallet(wallet);
        }

        // Can save and close the wallet in a single call
        [Fact]
        public void TestSaveAndClose()
        {
            Assert.True(TEST_NON_RELAYS);

            // create random wallet
            string password = "";
            MoneroWallet wallet = CreateWallet(new MoneroWalletConfig().SetPassword(password));
            string path = wallet.GetPath();

            // set an attribute
            string uuid = GenUtils.GetUUID();
            wallet.SetAttribute("id", uuid);

            // close the wallet without saving
            CloseWallet(wallet);

            // re-open the wallet and ensure attribute was not saved
            wallet = OpenWallet(path, password);
            Assert.Null(wallet.GetAttribute("id"));

            // set the attribute and close with saving
            wallet.SetAttribute("id", uuid);
            CloseWallet(wallet, true);

            // re-open the wallet and ensure attribute was saved
            wallet = OpenWallet(new MoneroWalletConfig().SetPath(path).SetPassword(password));
            Assert.Equal(uuid, wallet.GetAttribute("id"));
            CloseWallet(wallet);
        }

        #endregion

        #region Test Relays

        #endregion

        #region Reset Tests

        // Can sweep subaddresses
        [Fact]
        public void TestSweepSubaddresses()
        {
            Assert.True(TEST_RESETS);
            TestUtils.WALLET_TX_TRACKER.WaitForWalletTxsToClearPool(wallet);

            const int NUM_SUBADDRESSES_TO_SWEEP = 2;

            // collect subaddresses with balance and unlocked balance
            List<MoneroSubaddress> subaddresses = new List<MoneroSubaddress>();
            List<MoneroSubaddress> subaddressesBalance = new List<MoneroSubaddress>();
            List<MoneroSubaddress> subaddressesUnlocked = new List<MoneroSubaddress>();
            foreach (MoneroAccount account in wallet.GetAccounts(true))
            {
                if (account.GetIndex() == 0) continue;  // skip default account
                foreach (MoneroSubaddress subaddress in account.GetSubaddresses())
                {
                    subaddresses.Add(subaddress);
                    if (((ulong)subaddress.GetBalance()).CompareTo(TestUtils.MAX_FEE) > 0) subaddressesBalance.Add(subaddress);
                    if (((ulong)subaddress.GetUnlockedBalance()).CompareTo(TestUtils.MAX_FEE) > 0) subaddressesUnlocked.Add(subaddress);
                }
            }

            // test requires at least one more subaddresses than the number being swept to verify it does not change
            Assert.True(subaddressesBalance.Count >= NUM_SUBADDRESSES_TO_SWEEP + 1, "Test requires balance in at least " + (NUM_SUBADDRESSES_TO_SWEEP + 1) + " subaddresses from non-default acccount; run send-to-multiple tests");
            Assert.True(subaddressesUnlocked.Count >= NUM_SUBADDRESSES_TO_SWEEP + 1, "Wallet is waiting on unlocked funds");

            // sweep from first unlocked subaddresses
            for (int i = 0; i < NUM_SUBADDRESSES_TO_SWEEP; i++)
            {

                // sweep unlocked account
                MoneroSubaddress unlockedSubaddress = subaddressesUnlocked[i];
                MoneroTxConfig config = new MoneroTxConfig()
                        .SetAddress(wallet.GetPrimaryAddress())
                        .SetAccountIndex(unlockedSubaddress.GetAccountIndex())
                        .SetSubaddressIndices([(uint)unlockedSubaddress.GetIndex()])
                        .SetRelay(true);
                List<MoneroTxWallet> txs = wallet.SweepUnlocked(config);

                // test transactions
                Assert.True(txs.Count > 0);
                foreach (MoneroTxWallet tx in txs)
                {
                    Assert.True(tx.GetTxSet().GetTxs().Contains(tx));
                    TxContext ctx = new TxContext();
                    ctx.Wallet = wallet;
                    ctx.Config = config;
                    ctx.IsSendResponse = true;
                    ctx.IsSweepResponse = true;
                    TestTxWallet(tx, ctx);
                }

                // Assert. unlocked balance is less than max fee
                MoneroSubaddress subaddress = wallet.GetSubaddress(((uint)unlockedSubaddress.GetAccountIndex()), ((uint)unlockedSubaddress.GetIndex()));
                Assert.True(((ulong)subaddress.GetUnlockedBalance()).CompareTo(TestUtils.MAX_FEE) < 0);
            }

            // test subaddresses after sweeping
            List<MoneroSubaddress> subaddressesAfter = new List<MoneroSubaddress>();
            foreach (MoneroAccount account in wallet.GetAccounts(true))
            {
                if (account.GetIndex() == 0) continue;  // skip default account
                foreach (MoneroSubaddress subaddress in account.GetSubaddresses())
                {
                    subaddressesAfter.Add(subaddress);
                }
            }
            Assert.Equal(subaddresses.Count, subaddressesAfter.Count);
            for (int i = 0; i < subaddresses.Count; i++)
            {
                MoneroSubaddress subaddressBefore = subaddresses[i];
                MoneroSubaddress subaddressAfter = subaddressesAfter[i];

                // determine if subaddress was swept
                bool swept = false;
                for (int j = 0; j < NUM_SUBADDRESSES_TO_SWEEP; j++)
                {
                    if (subaddressesUnlocked[j].GetAccountIndex().Equals(subaddressBefore.GetAccountIndex()) && subaddressesUnlocked[j].GetIndex().Equals(subaddressBefore.GetIndex()))
                    {
                        swept = true;
                        break;
                    }
                }

                // Assert. unlocked balance is less than max fee if swept, unchanged otherwise
                if (swept)
                {
                    Assert.True(((ulong)subaddressAfter.GetUnlockedBalance()).CompareTo(TestUtils.MAX_FEE) < 0);
                }
                else
                {
                    Assert.True(((ulong)subaddressBefore.GetUnlockedBalance()).CompareTo(subaddressAfter.GetUnlockedBalance()) == 0);
                }
            }
        }

        // Can sweep accounts
        [Fact]
        public void TestSweepAccounts()
        {
            Assert.True(TEST_RESETS);
            TestUtils.WALLET_TX_TRACKER.WaitForWalletTxsToClearPool(wallet);

            const int NUM_ACCOUNTS_TO_SWEEP = 1;

            // collect accounts with sufficient balance and unlocked balance to cover the fee
            List<MoneroAccount> accounts = wallet.GetAccounts(true);
            List<MoneroAccount> accountsBalance = new List<MoneroAccount>();
            List<MoneroAccount> accountsUnlocked = new List<MoneroAccount>();
            foreach (MoneroAccount account in accounts)
            {
                if (account.GetIndex() == 0) continue;  // skip default account
                if (account.GetBalance().CompareTo(TestUtils.MAX_FEE) > 0) accountsBalance.Add(account);
                if (account.GetUnlockedBalance().CompareTo(TestUtils.MAX_FEE) > 0) accountsUnlocked.Add(account);
            }

            // test requires at least one more accounts than the number being swept to verify it does not change
            Assert.True(accountsBalance.Count >= NUM_ACCOUNTS_TO_SWEEP + 1, "Test requires balance greater than the fee in at least " + (NUM_ACCOUNTS_TO_SWEEP + 1) + " non-default accounts; run send-to-multiple tests");
            Assert.True(accountsUnlocked.Count >= NUM_ACCOUNTS_TO_SWEEP + 1, "Wallet is waiting on unlocked funds");

            // sweep from first unlocked accounts
            for (int i = 0; i < NUM_ACCOUNTS_TO_SWEEP; i++)
            {

                // sweep unlocked account
                MoneroAccount unlockedAccount = accountsUnlocked[i];
                MoneroTxConfig config = new MoneroTxConfig().SetAddress(wallet.GetPrimaryAddress()).SetAccountIndex(unlockedAccount.GetIndex()).SetRelay(true);
                List<MoneroTxWallet> txs = wallet.SweepUnlocked(config);

                // test transactions
                Assert.True(txs.Count > 0);
                foreach (MoneroTxWallet tx in txs)
                {
                    TxContext ctx = new TxContext();
                    ctx.Wallet = wallet;
                    ctx.Config = config;
                    ctx.IsSendResponse = true;
                    ctx.IsSweepResponse = true;
                    TestTxWallet(tx, ctx);
                    Assert.NotNull(tx.GetTxSet());
                    Assert.True(tx.GetTxSet().GetTxs().Contains(tx));
                }

                // Assert. unlocked account balance less than max fee
                MoneroAccount account = wallet.GetAccount((uint)unlockedAccount.GetIndex());
                Assert.True(account.GetUnlockedBalance().CompareTo(TestUtils.MAX_FEE) < 0);
            }

            // test accounts after sweeping
            List<MoneroAccount> accountsAfter = wallet.GetAccounts(true);
            Assert.Equal(accounts.Count, accountsAfter.Count);
            for (int i = 0; i < accounts.Count; i++)
            {
                MoneroAccount accountBefore = accounts[i];
                MoneroAccount accountAfter = accountsAfter[i];

                // determine if account was swept
                bool swept = false;
                for (int j = 0; j < NUM_ACCOUNTS_TO_SWEEP; j++)
                {
                    if (accountsUnlocked[j].GetIndex().Equals(accountBefore.GetIndex()))
                    {
                        swept = true;
                        break;
                    }
                }

                // Assert. unlocked balance is less than max fee if swept, unchanged otherwise
                if (swept)
                {
                    Assert.True(accountAfter.GetUnlockedBalance().CompareTo(TestUtils.MAX_FEE) < 0);
                }
                else
                {
                    Assert.True(accountBefore.GetUnlockedBalance().CompareTo(accountAfter.GetUnlockedBalance()) == 0);
                }
            }
        }

        // Can sweep the whole wallet by accounts
        [Fact(Skip = "Disabled so tests don't sweep the whole wallet")]
        public void TestSweepWalletByAccounts()
        {
            Assert.True(TEST_RESETS);
            TestSweepWallet(null);
        }

        // Can sweep the whole wallet by subaddresses
        [Fact(Skip = "Disabled so tests don't sweep the whole wallet")]
        public void TestSweepWalletBySubaddresses()
        {
            Assert.True(TEST_RESETS);
            TestSweepWallet(true);
        }

        private void TestSweepWallet(bool? sweepEachSubaddress)
        {
            TestUtils.WALLET_TX_TRACKER.WaitForWalletTxsToClearPool(wallet);

            // verify 2 subaddresses with enough unlocked balance to cover the fee
            List<MoneroSubaddress> subaddressesBalance = new List<MoneroSubaddress>();
            List<MoneroSubaddress> subaddressesUnlocked = new List<MoneroSubaddress>();
            foreach (MoneroAccount account in wallet.GetAccounts(true))
            {
                foreach (MoneroSubaddress subaddress in account.GetSubaddresses())
                {
                    if (((ulong)subaddress.GetBalance()).CompareTo(TestUtils.MAX_FEE) > 0) subaddressesBalance.Add(subaddress);
                    if (((ulong)subaddress.GetUnlockedBalance()).CompareTo(TestUtils.MAX_FEE) > 0) subaddressesUnlocked.Add(subaddress);
                }
            }
            Assert.True(subaddressesBalance.Count >= 2, "Test requires multiple accounts with a balance greater than the fee; run send to multiple first");
            Assert.True(subaddressesUnlocked.Count >= 2, "Wallet is waiting on unlocked funds");

            // sweep
            string destination = wallet.GetPrimaryAddress();
            MoneroTxConfig config = new MoneroTxConfig().SetAddress(destination).SetSweepEachSubaddress(sweepEachSubaddress).SetRelay(true);
            MoneroTxConfig copy = config.Clone();
            List<MoneroTxWallet> txs = wallet.SweepUnlocked(config);
            Assert.Equal(copy, config);  // config is unchanged
            foreach (MoneroTxWallet tx in txs)
            {
                Assert.True(tx.GetTxSet().GetTxs().Contains(tx));
                Assert.Null(tx.GetTxSet().GetMultisigTxHex());
                Assert.Null(tx.GetTxSet().GetSignedTxHex());
                Assert.Null(tx.GetTxSet().GetUnsignedTxHex());
            }
            Assert.True(txs.Count > 0);
            foreach (MoneroTxWallet tx in txs)
            {
                config = new MoneroTxConfig()
                        .SetAddress(destination)
                        .SetAccountIndex(tx.GetOutgoingTransfer().GetAccountIndex())
                        .SetSweepEachSubaddress(sweepEachSubaddress)
                        .SetRelay(true);
                TxContext ctx = new TxContext();
                ctx.Wallet = wallet;
                ctx.Config = config;
                ctx.IsSendResponse = true;
                ctx.IsSweepResponse = true;
                TestTxWallet(tx, ctx);
            }

            // all unspent, unlocked outputs must be less than fee
            List<MoneroOutputWallet> spendableOutputs = wallet.GetOutputs(new MoneroOutputQuery().SetIsSpent(false).SetTxQuery(new MoneroTxQuery().SetIsLocked(false)));
            foreach (MoneroOutputWallet spendableOutput in spendableOutputs)
            {
                Assert.True(((ulong)spendableOutput.GetAmount()).CompareTo(TestUtils.MAX_FEE) < 0, "Unspent output should have been swept\n" + spendableOutput.ToString());
            }

            // all subaddress unlocked balances must be less than fee
            subaddressesBalance.Clear();
            subaddressesUnlocked.Clear();
            foreach (MoneroAccount account in wallet.GetAccounts(true))
            {
                foreach (MoneroSubaddress subaddress in account.GetSubaddresses())
                {
                    Assert.True(((ulong)subaddress.GetUnlockedBalance()).CompareTo(TestUtils.MAX_FEE) < 0, "No subaddress should have more unlocked than the fee");
                }
            }
        }

        // Can scan transactions by id
        [Fact]
        public void TestScanTxs()
        {

            // get a few tx hashes
            List<string> txHashes = [];
            List<MoneroTxWallet> txs = wallet.GetTxs();
            if (txs.Count < 3) Assert.Fail("Not enough txs to scan");
            for (int i = 0; i < 3; i++) txHashes.Add(txs[i].GetHash());

            // start wallet without scanning
            MoneroWallet scanWallet = CreateWallet(new MoneroWalletConfig().SetSeed(wallet.GetSeed()).SetRestoreHeight(0));
            scanWallet.StopSyncing(); // TODO: create wallet without daemon connection (offline does not reconnect, default connects to localhost, offline then online causes confirmed txs to disappear)
            Assert.True(scanWallet.IsConnectedToDaemon());

            // scan txs
            scanWallet.ScanTxs(txHashes);

            // TODO: scanning txs causes merge problems reconciling 0 fee, isMinerTx with test txs

            //    // txs are scanned
            //    Assert.Equal(txHashes.Count, scanWallet.GetTxs().Count);
            //    for (int i = 0; i < txHashes.Count; i++) {
            //      Assert.Equal(wallet.GetTx(txHashes[i]), scanWallet.GetTx(txHashes[i]));
            //    }
            //    List<MoneroTxWallet> scannedTxs = scanWallet.GetTxs(txHashes);
            //    Assert.Equal(txHashes.Count, scannedTxs.Count);

            // close wallet
            CloseWallet(scanWallet, false);
        }

        // Can rescan the blockchain
        [Fact(Skip = "Disabled so tests don't delete local cache")]
        public void TestRescanBlockchain()
        {
            Assert.True(TEST_RESETS);
            wallet.RescanBlockchain();
            foreach (MoneroTxWallet tx in wallet.GetTxs())
            {
                TestTxWallet(tx, null);
            }
        }

        #endregion

        #region Notification Tests

        // Can generate notifications sending to different wallet
        [Fact]
        public void TestNotificationsDifferentWallet()
        {
            TestWalletNotifications("TestNotificationsDifferentWallet", false, false, false, false, 0);
        }

        // Can generate notifications sending to different wallet when relayed
        [Fact]
        public void TestNotificationsDifferentWalletWhenRelayed()
        {
            TestWalletNotifications("TestNotificationsDifferentWalletWhenRelayed", false, false, false, true, 3);
        }

        // Can generate notifications sending to different account
        [Fact]
        public void TestNotificationsDifferentAccounts()
        {
            TestWalletNotifications("TestNotificationsDifferentAccounts", true, false, false, false, 0);
        }

        // Can generate notifications sending to same account
        [Fact]
        public void TestNotificationsSameAccount()
        {
            TestWalletNotifications("TestNotificationsSameAccount", true, true, false, false, 0);
        }

        // Can generate notifications sweeping output to different account
        [Fact]
        public void TestNotificationsDifferentAccountSweepOutput()
        {
            TestWalletNotifications("TestNotificationsDifferentAccountSweepOutput", true, false, true, false, 0);
        }

        // Can generate notifications sweeping output to same account when relayed
        [Fact]
        public void TestNotificationsSameAccountSweepOutputWhenRelayed()
        {
            TestWalletNotifications("TestNotificationsSameAccountSweepOutputWhenRelayed", true, true, true, true, 0);
        }

        // Can stop listening
        [Fact]
        public void TestStopListening()
        {
            // create wallet and start background synchronizing
            MoneroWallet wallet = CreateWallet(new MoneroWalletConfig());

            // add listener
            WalletNotificationCollector listener = new WalletNotificationCollector();
            wallet.AddListener(listener);
            try { GenUtils.WaitFor(1000); }
            catch (Exception e) { throw new Exception("An error occurred while sleeping", e); }

            // remove listener and close
            wallet.RemoveListener(listener);
            CloseWallet(wallet);
        }

        // Can be created and receive funds
        [Fact]
        public void TestCreateAndReceive()
        {
            Assert.True(TEST_NOTIFICATIONS);

            // create random wallet
            MoneroWallet receiver = CreateWallet(new MoneroWalletConfig());
            try
            {

                // listen for received outputs
                WalletNotificationCollector myListener = new WalletNotificationCollector();
                receiver.AddListener(myListener);

                // wait for txs to confirm and for sufficient unlocked balance
                TestUtils.WALLET_TX_TRACKER.WaitForWalletTxsToClearPool(wallet);
                TestUtils.WALLET_TX_TRACKER.WaitForUnlockedBalance(wallet, 0, null, TestUtils.MAX_FEE);

                // send funds to the receiver
                MoneroTxWallet sentTx = wallet.CreateTx(new MoneroTxConfig().SetAccountIndex(0).SetAddress(receiver.GetPrimaryAddress()).SetAmount(TestUtils.MAX_FEE).SetRelay(true));

                // wait for funds to confirm
                try { StartMining.Start(); } catch (Exception e) { }
                while ((wallet.GetTx(sentTx.GetHash())).IsConfirmed() != true)
                {
                    if (wallet.GetTx(sentTx.GetHash()).IsFailed() == true) throw new MoneroError("Tx failed in mempool: " + sentTx.GetHash());
                    daemon.WaitForNextBlockHeader();
                }

                // receiver should have notified listeners of received outputs
                try { GenUtils.WaitFor(1000); } catch (ThreadInterruptedException e) { throw new Exception("Thread was interrupted", e); } // zmq notifications received within 1 second
                Assert.False(myListener.GetOutputsReceived().Count == 0);
            }
            finally
            {
                CloseWallet(receiver);
                try { daemon.StopMining(); } catch (Exception e) { }
            }
        }

        // Can freeze and thaw outputs
        [Fact]
        public void TestFreezeOutputs()
        {
            Assert.True(TEST_NON_RELAYS);

            // get an available output
            List<MoneroOutputWallet> outputs = wallet.GetOutputs(new MoneroOutputQuery().SetIsSpent(false).SetIsFrozen(false).SetTxQuery(new MoneroTxQuery().SetIsLocked(false)));
            foreach (MoneroOutputWallet _out in outputs) Assert.False(_out.IsFrozen());
            Assert.True(outputs.Count > 0);
            MoneroOutputWallet output = outputs[0];
            Assert.False(output.GetTx().IsLocked());
            Assert.False(output.IsSpent());
            Assert.False(output.IsFrozen());
            Assert.False(wallet.IsOutputFrozen(output.GetKeyImage().GetHex()));

            // freeze output by key image
            int numFrozenBefore = wallet.GetOutputs(new MoneroOutputQuery().SetIsFrozen(true)).Count;
            wallet.FreezeOutput(output.GetKeyImage().GetHex());
            Assert.True(wallet.IsOutputFrozen(output.GetKeyImage().GetHex()));

            // test querying
            Assert.Equal(numFrozenBefore + 1, wallet.GetOutputs(new MoneroOutputQuery().SetIsFrozen(true)).Count);
            outputs = wallet.GetOutputs(new MoneroOutputQuery().SetKeyImage(new MoneroKeyImage().SetHex(output.GetKeyImage().GetHex())).SetIsFrozen(true));
            Assert.Equal(1, outputs.Count);
            MoneroOutputWallet outputFrozen = outputs[0];
            Assert.True(outputFrozen.IsFrozen());
            Assert.Equal(output.GetKeyImage().GetHex(), outputFrozen.GetKeyImage().GetHex());

            // try to sweep frozen output
            try
            {
                wallet.SweepOutput(new MoneroTxConfig().SetAddress(wallet.GetPrimaryAddress()).SetKeyImage(output.GetKeyImage().GetHex()));
                Assert.Fail("Should have thrown error");
            }
            catch (MoneroError e)
            {
                Assert.Equal("No outputs found", e.Message);
            }

            // try to freeze null key image
            try
            {
                wallet.FreezeOutput(null);
                Assert.Fail("Should have thrown error");
            }
            catch (MoneroError e)
            {
                Assert.Equal("Must specify key image to freeze", e.Message);
            }

            // try to freeze empty key image
            try
            {
                wallet.FreezeOutput("");
                Assert.Fail("Should have thrown error");
            }
            catch (MoneroError e)
            {
                Assert.Equal("Must specify key image to freeze", e.Message);
            }

            // try to freeze bad key image
            try
            {
                wallet.FreezeOutput("123");
                Assert.Fail("Should have thrown error");
            }
            catch (MoneroError e)
            {
                //Assert.Equal("Bad key image", e.Message);
            }

            // thaw output by key image
            wallet.ThawOutput(output.GetKeyImage().GetHex());
            Assert.False(wallet.IsOutputFrozen(output.GetKeyImage().GetHex()));

            // test querying
            Assert.Equal(numFrozenBefore, wallet.GetOutputs(new MoneroOutputQuery().SetIsFrozen(true)).Count);
            outputs = wallet.GetOutputs(new MoneroOutputQuery().SetKeyImage(new MoneroKeyImage().SetHex(output.GetKeyImage().GetHex())).SetIsFrozen(true));
            Assert.Equal(0, outputs.Count);
            outputs = wallet.GetOutputs(new MoneroOutputQuery().SetKeyImage(new MoneroKeyImage().SetHex(output.GetKeyImage().GetHex())).SetIsFrozen(false));
            Assert.Equal(1, outputs.Count);
            MoneroOutputWallet outputThawed = outputs[0];
            Assert.False(outputThawed.IsFrozen());
            Assert.Equal(output.GetKeyImage().GetHex(), outputThawed.GetKeyImage().GetHex());
        }

        // Provides key images of spent outputs
        [Fact]
        public void TestInputKeyImages()
        {
            uint accountIndex = 0;
            uint subaddressIndex = (wallet.GetSubaddresses(0).Count > 1) ? (uint)1 : 0; // TODO: avoid subaddress 0 which is more likely to fail transaction sanity check

            // test unrelayed single transaction
            TestSpendTx(wallet.CreateTx(new MoneroTxConfig().AddDestination(wallet.GetPrimaryAddress(), TestUtils.MAX_FEE).SetAccountIndex(accountIndex)));

            // test unrelayed split transactions
            foreach (MoneroTxWallet tx in wallet.CreateTxs(new MoneroTxConfig().AddDestination(wallet.GetPrimaryAddress(), TestUtils.MAX_FEE).SetAccountIndex(accountIndex)))
            {
                TestSpendTx(tx);
            }

            // test unrelayed sweep dust
            List<string> dustKeyImages = new List<string>();
            foreach (MoneroTxWallet tx in wallet.SweepDust(false))
            {
                TestSpendTx(tx);
                foreach (MoneroOutput input in tx.GetInputs()) dustKeyImages.Add(input.GetKeyImage().GetHex());
            }

            // get available outputs above min amount
            List<MoneroOutputWallet> outputs = wallet.GetOutputs(new MoneroOutputQuery().SetAccountIndex(accountIndex).SetSubaddressIndex(subaddressIndex).SetIsSpent(false).SetIsFrozen(false).SetTxQuery(new MoneroTxQuery().SetIsLocked(false)).SetMinAmount(TestUtils.MAX_FEE));

            // filter dust outputs
            List<MoneroOutputWallet> dustOutputs = new List<MoneroOutputWallet>();
            foreach (MoneroOutputWallet output in outputs)
            {
                if (dustKeyImages.Contains(output.GetKeyImage().GetHex())) dustOutputs.Add(output);
            }
            
            foreach (var dustOutput in dustOutputs) outputs.Remove(dustOutput);

            // test unrelayed sweep output
            TestSpendTx(wallet.SweepOutput(new MoneroTxConfig().SetAddress(wallet.GetPrimaryAddress()).SetKeyImage(outputs[0].GetKeyImage().GetHex())));

            // test unrelayed sweep wallet ensuring all non-dust outputs are spent
            HashSet<string> availableKeyImages = new HashSet<string>();
            foreach (MoneroOutputWallet output in outputs) availableKeyImages.Add(output.GetKeyImage().GetHex());
            HashSet<string> sweptKeyImages = new HashSet<string>();
            List<MoneroTxWallet> txs = wallet.SweepUnlocked(new MoneroTxConfig().SetAccountIndex(accountIndex).SetSubaddressIndex(subaddressIndex).SetAddress(wallet.GetPrimaryAddress()));
            foreach (MoneroTxWallet tx in txs)
            {
                TestSpendTx(tx);
                foreach (MoneroOutput input in tx.GetInputs()) sweptKeyImages.Add(input.GetKeyImage().GetHex());
            }
            Assert.True(sweptKeyImages.Count > 0);

            // max skipped output is less than max fee amount
            MoneroOutputWallet maxSkippedOutput = null;
            foreach (MoneroOutputWallet output in outputs)
            {
                if (!sweptKeyImages.Contains(output.GetKeyImage().GetHex()))
                {
                    if (maxSkippedOutput == null || ((ulong)maxSkippedOutput.GetAmount()).CompareTo(output.GetAmount()) < 0)
                    {
                        maxSkippedOutput = output;
                    }
                }
            }
            Assert.True(maxSkippedOutput == null || ((ulong)maxSkippedOutput.GetAmount()).CompareTo(TestUtils.MAX_FEE) < 0);
        }

        // Can prove unrelayed
        [Fact]
        public void TestProveUnrelayedTxs()
        {
            // create unrelayed tx to verify
            string address1 = TestUtils.GetExternalWalletAddress();
            string address2 = wallet.GetAddress(0, 0);
            string address3 = wallet.GetAddress(1, 0);
            MoneroTxWallet tx = wallet.CreateTx(new MoneroTxConfig()
                    .SetAccountIndex(0)
                    .AddDestination(address1, TestUtils.MAX_FEE)
                    .AddDestination(address2, TestUtils.MAX_FEE * 2)
                    .AddDestination(address3, TestUtils.MAX_FEE * 2));

            // submit tx to daemon but do not relay
            MoneroSubmitTxResult result = daemon.SubmitTxHex(tx.GetFullHex(), true);
            Assert.True(result.IsGood());

            // create random wallet to verify transfers
            MoneroWallet verifyingWallet = CreateWallet(new MoneroWalletConfig());

            // verify transfer 1
            MoneroCheckTx check = verifyingWallet.CheckTxKey(tx.GetHash(), tx.GetKey(), address1);
            Assert.True(check.IsGood());
            Assert.True(check.InTxPool());
            Assert.True(0 == check.GetNumConfirmations());
            Assert.Equal(TestUtils.MAX_FEE, check.GetReceivedAmount());

            // verify transfer 2
            check = verifyingWallet.CheckTxKey(tx.GetHash(), tx.GetKey(), address2);
            Assert.True(check.IsGood());
            Assert.True(check.InTxPool());
            Assert.True(0 == check.GetNumConfirmations());
            Assert.True(check.GetReceivedAmount().CompareTo(TestUtils.MAX_FEE * 2) >= 0); // + change amount

            // verify transfer 3
            check = verifyingWallet.CheckTxKey(tx.GetHash(), tx.GetKey(), address3);
            Assert.True(check.IsGood());
            Assert.True(check.InTxPool());
            Assert.True(0 == check.GetNumConfirmations());
            Assert.Equal(TestUtils.MAX_FEE * 3, check.GetReceivedAmount());

            // cleanup
            daemon.FlushTxPool(tx.GetHash());
            CloseWallet(verifyingWallet);
        }

        // Can get the default fee priority
        [Fact]
        public void TestGetDefaultFeePriority()
        {
            MoneroTxPriority defaultPriority = wallet.GetDefaultFeePriority();
            Assert.True(defaultPriority > 0);
        }

        #region Notification Utils

        private void TestWalletNotifications(string testName, bool sameWallet, bool sameAccount, bool sweepOutput, bool createThenRelay, ulong unlockDelay)
        {
            Assert.True(TEST_NOTIFICATIONS);
            List<string> issues = TestWalletNotificationsAux(sameWallet, sameAccount, sweepOutput, createThenRelay, unlockDelay);
            if (issues.Count == 0) return;
            string msg = testName + "(" + sameWallet + ", " + sameAccount + ", " + sweepOutput + ", " + createThenRelay + ") generated " + issues.Count + " issues:\n" + IssuesToStr(issues);
            Console.WriteLine(msg);
            if (msg.Contains("ERROR:")) Assert.Fail(msg);
        }

        private List<string> TestWalletNotificationsAux(bool sameWallet, bool sameAccount, bool sweepOutput, bool createThenRelay, ulong unlockDelay)
        {
            ulong MAX_POLL_TIME = 5000l; // maximum time granted for wallet to poll

            // collect issues as test runs
            List<string> issues = new List<string>();

            // set sender and receiver
            MoneroWallet sender = wallet;
            MoneroWallet receiver = sameWallet ? sender : CreateWallet(new MoneroWalletConfig());

            // create receiver accounts if necessary
            int numAccounts = receiver.GetAccounts().Count;
            for (int i = 0; i < 4 - numAccounts; i++) receiver.CreateAccount();

            // wait for unlocked funds in source account
            TestUtils.WALLET_TX_TRACKER.WaitForWalletTxsToClearPool(sender);
            TestUtils.WALLET_TX_TRACKER.WaitForUnlockedBalance(sender, 0, null, TestUtils.MAX_FEE * 10);

            // get balances to compare after sending
            ulong senderBalanceBefore = sender.GetBalance();
            ulong senderUnlockedBalanceBefore = sender.GetUnlockedBalance();
            ulong receiverBalanceBefore = receiver.GetBalance();
            ulong receiverUnlockedBalanceBefore = receiver.GetUnlockedBalance();
            ulong lastHeight = daemon.GetHeight();

            // start collecting notifications from sender and receiver
            WalletNotificationCollector senderNotificationCollector = new WalletNotificationCollector();
            WalletNotificationCollector receiverNotificationCollector = new WalletNotificationCollector();
            sender.AddListener(senderNotificationCollector);
            GenUtils.WaitFor(TestUtils.SYNC_PERIOD_IN_MS / 2);
            receiver.AddListener(receiverNotificationCollector);

            // send funds
            TxContext ctx = new TxContext();
            ctx.Wallet = wallet;
            ctx.IsSendResponse = true;
            MoneroTxWallet? senderTx = null;
            List<uint> destinationAccounts = sameAccount ? (sweepOutput ? [ 0 ] : [ 0, 1, 2 ]) : (sweepOutput ? [ 1 ] : [ 1, 2, 3 ]);
            List<MoneroOutputWallet> expectedOutputs = new List<MoneroOutputWallet>();
            if (sweepOutput)
            {
                ctx.IsSweepResponse = true;
                ctx.IsSweepOutputResponse = true;
                List<MoneroOutputWallet> outputs = sender.GetOutputs(new MoneroOutputQuery().SetIsSpent(false).SetTxQuery(new MoneroTxQuery().SetIsLocked(false)).SetAccountIndex(0).SetMinAmount(TestUtils.MAX_FEE * 5));
                if (outputs.Count == 0)
                {
                    issues.Add("ERROR: No outputs available to sweep from account 0");
                    return issues;
                }
                MoneroTxConfig config = new MoneroTxConfig().SetAddress(receiver.GetAddress(destinationAccounts[0], 0)).SetKeyImage(outputs[0].GetKeyImage().GetHex()).SetRelay(!createThenRelay);
                senderTx = sender.SweepOutput(config);
                expectedOutputs.Add(new MoneroOutputWallet().SetAmount(senderTx.GetOutgoingTransfer().GetDestinations()[0].GetAmount()).SetAccountIndex(destinationAccounts[0]).SetSubaddressIndex(0));
                ctx.Config = config;
            }
            else
            {
                MoneroTxConfig config = new MoneroTxConfig().SetAccountIndex(0).SetRelay(!createThenRelay);
                foreach (uint destinationAccount in destinationAccounts)
                {
                    config.AddDestination(receiver.GetAddress(destinationAccount, 0), TestUtils.MAX_FEE); // TODO: send and check random amounts?
                    expectedOutputs.Add(new MoneroOutputWallet().SetAmount(TestUtils.MAX_FEE).SetAccountIndex(destinationAccount).SetSubaddressIndex(0));
                }
                senderTx = sender.CreateTx(config);
                ctx.Config = config;
            }
            if (createThenRelay) sender.RelayTx(senderTx);

            // start timer to measure end of sync period
            ulong startTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // test send tx
            TestTxWallet(senderTx, ctx);

            // test sender after sending
            MoneroOutputQuery outputQuery = new MoneroOutputQuery().SetTxQuery(new MoneroTxQuery().SetHash(senderTx.GetHash())); // query for outputs from sender tx
            if (sameWallet)
            {
                if (senderTx.GetIncomingAmount() == null) issues.Add("WARNING: sender tx incoming amount is null when sent to same wallet");
                else if (senderTx.GetIncomingAmount().Equals(0)) issues.Add("WARNING: sender tx incoming amount is 0 when sent to same wallet");
                else if (((ulong)senderTx.GetIncomingAmount()).CompareTo(senderTx.GetOutgoingAmount() - (senderTx.GetFee())) != 0) issues.Add("WARNING: sender tx incoming amount != outgoing amount - fee when sent to same wallet");
            }
            else
            {
                if (senderTx.GetIncomingAmount() != null) issues.Add("ERROR: tx incoming amount should be null"); // TODO: should be 0? then can remove null checks in this method
            }
            senderTx = sender.GetTxs(new MoneroTxQuery().SetHash(senderTx.GetHash()).SetIncludeOutputs(true))[0];
            if (!sender.GetBalance().Equals(senderBalanceBefore - senderTx.GetFee() - senderTx.GetOutgoingAmount() + senderTx.GetIncomingAmount() == null ? 0 : senderTx.GetIncomingAmount())) issues.Add("ERROR: sender balance after send != balance before - tx fee - outgoing amount + incoming amount (" + sender.GetBalance() + " != " + senderBalanceBefore + " - " + senderTx.GetFee() + " - " + senderTx.GetOutgoingAmount() + " + " + senderTx.GetIncomingAmount() + ")");
            if (((ulong)sender.GetUnlockedBalance()).CompareTo(senderUnlockedBalanceBefore) >= 0) issues.Add("ERROR: sender unlocked balance should have decreased after sending");
            if (senderNotificationCollector.GetBalanceNotifications().Count == 0) issues.Add("ERROR: sender did not notify balance change after sending");
            else
            {
                if (!sender.GetBalance().Equals(senderNotificationCollector.GetBalanceNotifications().Last().Key)) issues.Add("ERROR: sender balance != last notified balance after sending (" + sender.GetBalance() + " != " + senderNotificationCollector.GetBalanceNotifications().Last().Key + ")");
                if (!sender.GetUnlockedBalance().Equals(senderNotificationCollector.GetBalanceNotifications().Last().Value)) issues.Add("ERROR: sender unlocked balance != last notified unlocked balance after sending (" + sender.GetUnlockedBalance() + " != " + senderNotificationCollector.GetBalanceNotifications().Last().Value + ")");
            }
            if (senderNotificationCollector.GetOutputsSpent(outputQuery).Count == 0) issues.Add("ERROR: sender did not announce unconfirmed spent output");

            // test receiver after 2 sync periods
            GenUtils.WaitFor(TestUtils.SYNC_PERIOD_IN_MS - (((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) - startTime));
            startTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // reset timer
            MoneroTxWallet receiverTx = receiver.GetTx(senderTx.GetHash());
            if (!senderTx.GetOutgoingAmount().Equals(receiverTx.GetIncomingAmount()))
            {
                if (sameAccount) issues.Add("WARNING: sender tx outgoing amount != receiver tx incoming amount when sent to same account (" + senderTx.GetOutgoingAmount() + " != " + receiverTx.GetIncomingAmount() + ")");
                else if (sameAccount) issues.Add("ERROR: sender tx outgoing amount != receiver tx incoming amount (" + senderTx.GetOutgoingAmount() + " != " + receiverTx.GetIncomingAmount() + ")");
            }
            if (!receiver.GetBalance().Equals(receiverBalanceBefore + (receiverTx.GetIncomingAmount() == null ? 0 : receiverTx.GetIncomingAmount()) - (receiverTx.GetOutgoingAmount() == null ? 0 : receiverTx.GetOutgoingAmount()) - (sameWallet ? receiverTx.GetFee() : 0)))
            {
                if (sameAccount) issues.Add("WARNING: after sending, receiver balance != balance before + incoming amount - outgoing amount - tx fee when sent to same account (" + receiver.GetBalance() + " != " + receiverBalanceBefore + " + " + receiverTx.GetIncomingAmount() + " - " + receiverTx.GetOutgoingAmount() + " - " + (sameWallet ? receiverTx.GetFee() : 0) + ")");
                else issues.Add("ERROR: after sending, receiver balance != balance before + incoming amount - outgoing amount - tx fee (" + receiver.GetBalance() + " != " + receiverBalanceBefore + " + " + receiverTx.GetIncomingAmount() + " - " + receiverTx.GetOutgoingAmount() + " - " + (sameWallet ? receiverTx.GetFee() : 0) + ")");
            }
            if (!sameWallet && !receiver.GetUnlockedBalance().Equals(receiverUnlockedBalanceBefore)) issues.Add("ERROR: receiver unlocked balance should not have changed after sending");
            if (receiverNotificationCollector.GetBalanceNotifications().Count == 0) issues.Add("ERROR: receiver did not notify balance change when funds received");
            else
            {
                if (!receiver.GetBalance().Equals(receiverNotificationCollector.GetBalanceNotifications().Last().Key)) issues.Add("ERROR: receiver balance != last notified balance after funds received");
                if (!receiver.GetUnlockedBalance().Equals(receiverNotificationCollector.GetBalanceNotifications().Last().Value)) issues.Add("ERROR: receiver unlocked balance != last notified unlocked balance after funds received");
            }
            if (receiverNotificationCollector.GetOutputsReceived(outputQuery).Count == 0) issues.Add("ERROR: receiver did not announce unconfirmed received output");
            else
            {
                foreach (MoneroOutputWallet output in GetMissingOutputs(expectedOutputs, receiverNotificationCollector.GetOutputsReceived(outputQuery), true))
                {
                    issues.Add("ERROR: receiver did not announce received output for amount " + output.GetAmount() + " to subaddress [" + output.GetAccountIndex() + ", " + output.GetSubaddressIndex() + "]");
                }
            }

            // mine until test completes
            StartMining.Start();

            // loop every sync period until unlock tested
            List<Thread> threads = new List<Thread>();
            ulong expectedUnlockTime = lastHeight + unlockDelay;
            ulong? confirmHeight = null;
            while (true)
            {

                // test height notifications
                ulong height = daemon.GetHeight();
                if (height > lastHeight)
                {
                    ulong testStartHeight = lastHeight;
                    lastHeight = height;
                    Thread thread = new Thread(() =>
                    {
                        // Aspetta 2 periodi di sync + poll time per notifiche
                        GenUtils.WaitFor(TestUtils.SYNC_PERIOD_IN_MS * 2 + MAX_POLL_TIME);

                        List<ulong> senderBlockNotifications = senderNotificationCollector.GetBlockNotifications();
                        List<ulong> receiverBlockNotifications = receiverNotificationCollector.GetBlockNotifications();

                        for (ulong i = testStartHeight; i < height; i++)
                        {
                            if (!senderBlockNotifications.Contains(i))
                                issues.Add($"ERROR: sender did not announce block {i}");

                            if (!receiverBlockNotifications.Contains(i))
                                issues.Add($"ERROR: receiver did not announce block {i}");
                        }
                    });
                    threads.Add(thread);
                    thread.Start();
                }

                // check if tx confirmed
                if (confirmHeight == null)
                {

                    // get updated tx
                    MoneroTxWallet tx = receiver.GetTx(senderTx.GetHash());

                    // break if tx fails
                    if (tx.IsFailed() == true)
                    {
                        issues.Add("ERROR: tx failed in tx pool");
                        break;
                    }

                    // test confirm notifications
                    if (tx.IsConfirmed() == true && confirmHeight == null)
                    {
                        confirmHeight = tx.GetHeight();
                        expectedUnlockTime = Math.Max(((ulong)confirmHeight) + NUM_BLOCKS_LOCKED, expectedUnlockTime); // exact unlock time known
                        Thread thread = new Thread(() =>
                        {
                            // Aspetta 2 periodi di sync + poll time per notifiche
                            GenUtils.WaitFor(TestUtils.SYNC_PERIOD_IN_MS * 2 + MAX_POLL_TIME);

                            // Prepara query confermata
                            MoneroOutputQuery confirmedQuery = outputQuery.GetTxQuery()
                                .Clone()
                                .SetIsConfirmed(true)
                                .SetIsLocked(true)
                                .GetOutputQuery();

                            // Controlli sugli output
                            if (senderNotificationCollector.GetOutputsSpent(confirmedQuery).Count == 0)
                                issues.Add("ERROR: sender did not announce confirmed spent output"); // TODO: test amount

                            if (receiverNotificationCollector.GetOutputsReceived(confirmedQuery).Count == 0)
                            {
                                issues.Add("ERROR: receiver did not announce confirmed received output");
                            }
                            else
                            {
                                foreach (MoneroOutputWallet output in GetMissingOutputs(expectedOutputs, receiverNotificationCollector.GetOutputsReceived(confirmedQuery), true))
                                {
                                    issues.Add($"ERROR: receiver did not announce confirmed received output for amount {output.GetAmount()} " +
                                              $"to subaddress [{output.GetAccountIndex()}, {output.GetSubaddressIndex()}]");
                                }
                            }

                            // Se stesso wallet → il net amount speso deve essere uguale alla tx fee
                            if (sameWallet)
                            {
                                ulong netAmount = 0;

                                foreach (MoneroOutputWallet outputSpent in senderNotificationCollector.GetOutputsSpent(confirmedQuery))
                                    netAmount += (ulong)outputSpent.GetAmount();

                                foreach (MoneroOutputWallet outputReceived in senderNotificationCollector.GetOutputsReceived(confirmedQuery))
                                    netAmount -= (ulong)outputReceived.GetAmount();

                                if (((ulong)tx.GetFee()).CompareTo(netAmount) != 0)
                                {
                                    if (sameAccount)
                                    {
                                        issues.Add($"WARNING: net output amount != tx fee when funds sent to same account: {netAmount} vs {tx.GetFee()}");
                                    }
                                    else if (sender is MoneroWalletRpc)
                                    {
                                        issues.Add($"WARNING: net output amount != tx fee when funds sent to same wallet because monero-wallet-rpc does not provide tx inputs: {netAmount} vs {tx.GetFee()}");
                                    }
                                    else
                                    {
                                        issues.Add($"ERROR: net output amount must equal tx fee when funds sent to same wallet: {netAmount} vs {tx.GetFee()}");
                                    }
                                }
                            }
                        });
                        threads.Add(thread);
                        thread.Start();
                    }
                }

                // otherwise test unlock notifications
                else if (height >= expectedUnlockTime)
                {
                    Thread thread = new Thread(() =>
                    {
                        // Aspetta 2 periodi di sync + poll time per notifiche
                        GenUtils.WaitFor(TestUtils.SYNC_PERIOD_IN_MS * 2 + MAX_POLL_TIME);

                        // Prepara query sbloccata
                        MoneroOutputQuery unlockedQuery = outputQuery.GetTxQuery()
                            .Clone()
                            .SetIsLocked(false)
                            .GetOutputQuery();

                        // Controlli sugli output
                        if (senderNotificationCollector.GetOutputsSpent(unlockedQuery).Count == 0)
                            issues.Add("ERROR: sender did not announce unlocked spent output"); // TODO: test amount?

                        foreach (MoneroOutputWallet output in GetMissingOutputs(expectedOutputs, receiverNotificationCollector.GetOutputsReceived(unlockedQuery), true))
                        {
                            issues.Add($"ERROR: receiver did not announce unlocked received output for amount {output.GetAmount()} " +
                                      $"to subaddress [{output.GetAccountIndex()}, {output.GetSubaddressIndex()}]");
                        }

                        // Controllo bilanci del receiver
                        if (!sameWallet && !receiver.GetBalance().Equals(receiver.GetUnlockedBalance()))
                            issues.Add("ERROR: receiver balance != unlocked balance after funds unlocked");

                        // Controllo notifiche balance del sender
                        if (senderNotificationCollector.GetBalanceNotifications().Count == 0)
                        {
                            issues.Add("ERROR: sender did not announce any balance notifications");
                        }
                        else
                        {
                            var lastNotification = senderNotificationCollector.GetBalanceNotifications()[^1]; // ultimo elemento
                            if (!sender.GetBalance().Equals(lastNotification.Key))
                                issues.Add("ERROR: sender balance != last notified balance after funds unlocked");
                            if (!sender.GetUnlockedBalance().Equals(lastNotification.Value))
                                issues.Add("ERROR: sender unlocked balance != last notified unlocked balance after funds unlocked");
                        }

                        // Controllo notifiche balance del receiver
                        if (receiverNotificationCollector.GetBalanceNotifications().Count == 0)
                        {
                            issues.Add("ERROR: receiver did not announce any balance notifications");
                        }
                        else
                        {
                            var lastNotification = receiverNotificationCollector.GetBalanceNotifications()[^1]; // ultimo elemento
                            if (!receiver.GetBalance().Equals(lastNotification.Key))
                                issues.Add("ERROR: receiver balance != last notified balance after funds unlocked");
                            if (!receiver.GetUnlockedBalance().Equals(lastNotification.Value))
                                issues.Add("ERROR: receiver unlocked balance != last notified unlocked balance after funds unlocked");
                        }
                    });
                    threads.Add(thread);
                    thread.Start();
                    break;
                }

                // wait for end of sync period
                GenUtils.WaitFor(TestUtils.SYNC_PERIOD_IN_MS - (((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) - startTime));
                startTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // reset timer
            }

            // wait for test threads
            try
            {
                foreach (Thread thread in threads) thread.Join();
            }
            catch (ThreadInterruptedException e)
            {
                throw new Exception("Thread was interrupted", e);
            }

            // test notified outputs
            foreach (MoneroOutputWallet output in senderNotificationCollector.GetOutputsSpent(outputQuery)) TestNotifiedOutput(output, true, issues);
            foreach (MoneroOutputWallet output in senderNotificationCollector.GetOutputsReceived(outputQuery)) TestNotifiedOutput(output, false, issues);
            foreach (MoneroOutputWallet output in receiverNotificationCollector.GetOutputsSpent(outputQuery)) TestNotifiedOutput(output, true, issues);
            foreach (MoneroOutputWallet output in receiverNotificationCollector.GetOutputsReceived(outputQuery)) TestNotifiedOutput(output, false, issues);

            // clean up
            if (daemon.GetMiningStatus().IsActive() == true) daemon.StopMining();
            sender.RemoveListener(senderNotificationCollector);
            senderNotificationCollector.SetListening(false);
            receiver.RemoveListener(receiverNotificationCollector);
            receiverNotificationCollector.SetListening(false);
            if (sender != receiver) CloseWallet(receiver);
            return issues;
        }

        private static List<MoneroOutputWallet> GetMissingOutputs(List<MoneroOutputWallet> expectedOutputs, List<MoneroOutputWallet> actualOutputs, bool matchSubaddress)
        {
            List<MoneroOutputWallet> missing = [];
            List<MoneroOutputWallet> used = [];
            foreach (MoneroOutputWallet expectedOutput in expectedOutputs)
            {
                bool found = false;
                foreach (MoneroOutputWallet actualOutput in actualOutputs)
                {
                    if (used.Contains(actualOutput)) continue;
                    if (actualOutput.GetAmount().Equals(expectedOutput.GetAmount()) && (!matchSubaddress || (actualOutput.GetAccountIndex() == expectedOutput.GetAccountIndex() && actualOutput.GetSubaddressIndex() == expectedOutput.GetSubaddressIndex())))
                    {
                        used.Add(actualOutput);
                        found = true;
                        break;
                    }
                }
                if (!found) missing.Add(expectedOutput);
            }
            return missing;
        }

        private static string IssuesToStr(List<string> issues)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < issues.Count; i++)
            {
                sb.Append((i + 1) + ": " + issues[i]);
                if (i < issues.Count - 1)
                    sb.Append('\n');
            }
            return sb.ToString();
        }

        private static void TestNotifiedOutput(MoneroOutputWallet output, bool isTxInput, List<string> issues)
        {
            // test tx link
            Assert.NotNull(output.GetTx());
            if (isTxInput) Assert.True(output.GetTx().GetInputs().Contains(output));
            else Assert.True(output.GetTx().GetOutputs().Contains(output));

            // test output values
            TestUtils.TestUnsignedBigInteger(output.GetAmount());
            if (output.GetAccountIndex() != null) Assert.True(output.GetAccountIndex() >= 0);
            else
            {
                if (isTxInput) issues.Add("WARNING: notification of " + GetOutputState(output) + " spent output missing account index"); // TODO (monero-project): account index not provided when output swept by key image.  could retrieve it but slows tx creation significantly
                else issues.Add("ERROR: notification of " + GetOutputState(output) + " received output missing account index");
            }
            if (output.GetSubaddressIndex() != null) Assert.True(output.GetSubaddressIndex() >= 0);
            else
            {
                if (isTxInput) issues.Add("WARNING: notification of " + GetOutputState(output) + " spent output missing subaddress index"); // TODO (monero-project): because inputs are not provided, creating fake input from outgoing transfer, which can be sourced from multiple subaddress indices, whereas an output can only come from one subaddress index; need to provide tx inputs to resolve this
                else issues.Add("ERROR: notification of " + GetOutputState(output) + " received output missing subaddress index");
            }
        }

        private static string GetOutputState(MoneroOutputWallet output)
        {
            if (output.GetTx().IsLocked() == false) return "unlocked";
            if (output.GetTx().IsConfirmed() == true) return "confirmed";
            if (output.GetTx().IsConfirmed() == false) return "unconfirmed";
            throw new Exception("Unknown output state: " + output.ToString());
        }

        private static void TestSpendTx(MoneroTxWallet spendTx)
        {
            Assert.NotNull(spendTx.GetInputs());
            Assert.True(spendTx.GetInputs().Count > 0);
            foreach (MoneroOutput input in spendTx.GetInputs()) Assert.NotNull(input.GetKeyImage().GetHex());
        }

        #endregion

        #endregion

        #region Helpers

        protected void CheckViewOnlyAndOfflineWallets(MoneroWallet viewOnlyWallet, MoneroWallet offlineWallet)
        {
            // wait for txs to confirm and for sufficient unlocked balance
            TestUtils.WALLET_TX_TRACKER.WaitForWalletTxsToClearPool(wallet);
            TestUtils.WALLET_TX_TRACKER.WaitForUnlockedBalance(wallet, 0, null, TestUtils.MAX_FEE * 4);

            // test getting txs, transfers, and outputs from view-only wallet
            Assert.False(viewOnlyWallet.GetTxs().Count == 0);
            Assert.False(viewOnlyWallet.GetTransfers().Count == 0);
            Assert.False(viewOnlyWallet.GetOutputs().Count == 0);

            // collect info from main test wallet
            string primaryAddress = wallet.GetPrimaryAddress();
            string privateViewKey = wallet.GetPrivateViewKey();

            // test and sync view-only wallet
            Assert.Equal(primaryAddress, viewOnlyWallet.GetPrimaryAddress());
            Assert.Equal(privateViewKey, viewOnlyWallet.GetPrivateViewKey());
            Assert.True(viewOnlyWallet.IsViewOnly());
            string errMsg = "Should have failed";
            try
            {
                viewOnlyWallet.GetSeed();
                throw new Exception(errMsg);
            }
            catch (Exception e)
            {
                Assert.NotEqual(errMsg, e.Message);
            }
            try
            {
                viewOnlyWallet.GetSeedLanguage();
                throw new Exception(errMsg);
            }
            catch (Exception e)
            {
                Assert.NotEqual(errMsg, e.Message);
            }
            try
            {
                viewOnlyWallet.GetPrivateSpendKey();
                throw new Exception(errMsg);
            }
            catch (Exception e)
            {
                Assert.NotEqual(errMsg, e.Message);
            }
            Assert.True(viewOnlyWallet.IsConnectedToDaemon());  // TODO: this fails with monero-wallet-rpc and monerod with authentication
            viewOnlyWallet.Sync();
            Assert.True(viewOnlyWallet.GetTxs().Count > 0);

            // export outputs from view-only wallet
            string outputsHex = viewOnlyWallet.ExportOutputs();

            // test offline wallet
            Assert.False(offlineWallet.IsConnectedToDaemon());
            Assert.False(offlineWallet.IsViewOnly());
            if (!(offlineWallet is MoneroWalletRpc)) Assert.Equal(TestUtils.SEED, offlineWallet.GetSeed()); // TODO monero-project: cannot get seed from offline wallet rpc
            Assert.Equal(0, offlineWallet.GetTxs(new MoneroTxQuery().SetInTxPool(false)).Count);

            // import outputs to offline wallet
            int numOutputsImported = offlineWallet.ImportOutputs(outputsHex);
            Assert.True(numOutputsImported > 0, "No outputs imported");

            // export key images from offline wallet
            List<MoneroKeyImage> keyImages = offlineWallet.ExportKeyImages();

            // import key images to view-only wallet
            Assert.True(viewOnlyWallet.IsConnectedToDaemon());
            viewOnlyWallet.ImportKeyImages(keyImages);
            Assert.Equal(wallet.GetBalance(), viewOnlyWallet.GetBalance());

            // create unsigned tx using view-only wallet
            MoneroTxWallet unsignedTx = viewOnlyWallet.CreateTx(new MoneroTxConfig().SetAccountIndex(0).SetAddress(primaryAddress).SetAmount(TestUtils.MAX_FEE* 3));
            Assert.NotNull(unsignedTx.GetTxSet().GetUnsignedTxHex());

            // sign tx using offline wallet
            MoneroTxSet signedTxSet = offlineWallet.SignTxs(unsignedTx.GetTxSet().GetUnsignedTxHex());
            Assert.False(signedTxSet.GetSignedTxHex().Length == 0);
            Assert.Equal(1, signedTxSet.GetTxs().Count);
            Assert.False(signedTxSet.GetTxs()[0].GetHash().Length == 0);

            // parse or "describe" unsigned tx set
            MoneroTxSet describedTxSet = offlineWallet.DescribeUnsignedTxSet(unsignedTx.GetTxSet().GetUnsignedTxHex());
            TestDescribedTxSet(describedTxSet);

            // submit signed tx using view-only wallet
            if (TEST_RELAYS)
            {
                List<string> txHashes = viewOnlyWallet.SubmitTxs(signedTxSet.GetSignedTxHex());
                Assert.Equal(1, txHashes.Count);
                Assert.Equal(64, txHashes[0].Length);
                TestUtils.WALLET_TX_TRACKER.WaitForWalletTxsToClearPool(viewOnlyWallet); // wait for confirmation for other tests
            }
        }

        private List<MoneroTxWallet> GetAndTestTxs(MoneroWallet wallet, MoneroTxQuery? query, TxContext ctx, bool? isExpected)
        {
            MoneroTxQuery copy = null;
            if (query != null) copy = query.Clone();
            List<MoneroTxWallet> txs = wallet.GetTxs(query);
            Assert.NotNull(txs);
            if (isExpected == false) Assert.True(txs.Count == 0);
            if (isExpected == true) Assert.False(txs.Count == 0);
            foreach (MoneroTxWallet tx in txs) TestTxWallet(tx, ctx);
            
            TestGetTxsStructure(txs, query);
            
            if (query != null) Assert.Equal(copy, query);
            return txs;
        }

        private List<MoneroTransfer> GetAndTestTransfers(MoneroWallet wallet, MoneroTransferQuery? query, TxContext ctx, bool? isExpected)
        {
            MoneroTransferQuery copy = null;
            if (query != null) copy = query.Clone();
            List<MoneroTransfer> transfers = wallet.GetTransfers(query);
            if (isExpected == false) Assert.Equal(0, transfers.Count);
            if (isExpected == true) Assert.True(transfers.Count > 0, "Transfers were expected but not found; run send tests?");
            if (ctx == null) ctx = new TxContext();
            ctx.Wallet = wallet;
            foreach (MoneroTransfer transfer in transfers) TestTxWallet(transfer.GetTx(), ctx);
            if (query != null) Assert.Equal(copy, query);
            return transfers;
        }

        private static List<MoneroOutputWallet> GetAndTestOutputs(MoneroWallet wallet, MoneroOutputQuery? query, bool? isExpected)
        {
            MoneroOutputQuery? copy = null;
            if (query != null) copy = query.Clone();
            List<MoneroOutputWallet> outputs = wallet.GetOutputs(query);
            Assert.Equal(copy, query);
            if (isExpected == false) Assert.True(outputs.Count == 0);
            if (isExpected == true) Assert.True(outputs.Count > 0, "Outputs were expected but not found; run send tests");
            foreach (MoneroOutputWallet output in outputs) TestOutputWallet(output);
            if (query != null) Assert.Equal(copy, query);
            return outputs;
        }

        protected void TestInvalidAddressError(MoneroError e)
        {
            Assert.Equal("Invalid address", e.Message);
        }

        protected void TestInvalidTxHashError(MoneroError e)
        {
            Assert.Equal("TX hash has invalid format", e.Message);
        }

        protected void TestInvalidTxKeyError(MoneroError e)
        {
            Assert.Equal("Tx _key has invalid format", e.Message);
        }

        protected void TestInvalidSignatureError(MoneroError e)
        {
            Assert.Equal("Signature size mismatch with additional _tx pubkeys", e.Message);
        }

        protected void TestNoSubaddressError(MoneroError e)
        {
            Assert.Equal("Address must not be a subaddress", e.Message);
        }

        protected void TestSignatureHeaderCheckError(MoneroError e)
        {
            Assert.Equal("Signature header _check error", e.Message);
        }

        private static void TestAccount(MoneroAccount account)
        {
            // test account
            Assert.NotNull(account);
            Assert.True(account.GetIndex() >= 0);
            MoneroUtils.ValidateAddress(account.GetPrimaryAddress(), TestUtils.NETWORK_TYPE);
            TestUtils.TestUnsignedBigInteger(account.GetBalance());
            TestUtils.TestUnsignedBigInteger(account.GetUnlockedBalance());

            // if given, test subaddresses and that their balances add up to account balances
            if (account.GetSubaddresses() != null)
            {
                ulong balance = 0;
                ulong unlockedBalance = 0;
                for (int i = 0; i < account.GetSubaddresses().Count; i++)
                {
                    TestSubaddress(account.GetSubaddresses()[i]);
                    Assert.Equal(account.GetIndex(), account.GetSubaddresses()[i].GetAccountIndex());
                    Assert.Equal(i, (int)account.GetSubaddresses()[i].GetIndex());
                    balance += account.GetSubaddresses()[i].GetBalance() ?? 0;
                    unlockedBalance += account.GetSubaddresses()[i].GetUnlockedBalance() ?? 0;
                }
                Assert.True(account.GetBalance().Equals(balance), "Subaddress balances " + balance + " != account " + account.GetIndex() + " balance " + account.GetBalance());
                Assert.True(account.GetUnlockedBalance().Equals(unlockedBalance), "Subaddress unlocked balances " + unlockedBalance + " != account " + account.GetIndex() + " unlocked balance " + account.GetUnlockedBalance());
            }

            // tag must be undefined or non-empty
            string? tag = account.GetTag();
            Assert.True(tag == null || tag.Length > 0);
        }

        private static void TestSubaddress(MoneroSubaddress subaddress)
        {
            Assert.True(subaddress.GetAccountIndex() >= 0);
            Assert.True(subaddress.GetIndex() >= 0);
            Assert.NotNull(subaddress.GetAddress());
            Assert.True(subaddress.GetLabel() == null || subaddress.GetLabel().Length != 0);
            TestUtils.TestUnsignedBigInteger(subaddress.GetBalance());
            TestUtils.TestUnsignedBigInteger(subaddress.GetUnlockedBalance());
            Assert.True(subaddress.GetNumUnspentOutputs() >= 0);
            Assert.NotNull(subaddress.IsUsed());
            if (subaddress.GetBalance() > 0) Assert.True(subaddress.IsUsed());
            Assert.True(subaddress.GetNumBlocksToUnlock() >= 0);
        }

        protected void TestTxsWallet(List<MoneroTxWallet> txs, TxContext ctx)
        {
            // test each transaction
            Assert.True(txs.Count > 0);
            foreach (MoneroTxWallet tx in txs) TestTxWallet(tx, ctx);

            // test destinations across transactions
            if (ctx.Config != null && ctx.Config.GetDestinations() != null)
            {
                int destinationIdx = 0;
                bool subtractFeeFromDestinations = ctx.Config.GetSubtractFeeFrom() != null && ctx.Config.GetSubtractFeeFrom().Count > 0;
                foreach (MoneroTxWallet tx in txs)
                {
                    // TODO: remove this after >18.3.1 when amounts_by_dest_list is official
                    if (tx.GetOutgoingTransfer().GetDestinations() == null)
                    {
                        Console.WriteLine("Tx missing destinations");
                        return;
                    }

                    ulong amountDiff = 0;
                    foreach (MoneroDestination destination in tx.GetOutgoingTransfer().GetDestinations())
                    {
                        MoneroDestination ctxDestination = ctx.Config.GetDestinations()[destinationIdx];
                        Assert.Equal(ctxDestination.GetAddress(), destination.GetAddress());
                        if (subtractFeeFromDestinations) amountDiff += ctxDestination.GetAmount() - destination.GetAmount() ?? 0;
                        else Assert.Equal(ctxDestination.GetAmount(), destination.GetAmount());
                        destinationIdx++;
                    }
                    if (subtractFeeFromDestinations) Assert.Equal(amountDiff, tx.GetFee());
                }

                Assert.Equal(destinationIdx, ctx.Config.GetDestinations().Count);
            }
        }

        protected void TestTxWallet(MoneroTxWallet tx, TxContext? ctx = null)
        {

            // validate / sanitize inputs
            ctx = new TxContext(ctx);
            ctx.Wallet = null;  // TODO: re-enable
            Assert.NotNull(tx);
            if (ctx.IsSendResponse == null || ctx.Config == null)
            {
                if (ctx.IsSendResponse != null) throw new Exception("if either sendRequest or isSendResponse is defined, they must both be defined");
                if (ctx.Config != null) throw new Exception("if either sendRequest or isSendResponse is defined, they must both be defined");
            }

            // test common field types
            Assert.NotNull(tx.GetHash());
            Assert.NotNull(tx.IsConfirmed());
            Assert.NotNull(tx.IsMinerTx());
            Assert.NotNull(tx.IsFailed());
            Assert.NotNull(tx.IsRelayed());
            Assert.NotNull(tx.InTxPool());
            Assert.NotNull(tx.IsLocked());
            TestUtils.TestUnsignedBigInteger(tx.GetFee());
            if (tx.GetPaymentId() != null) Assert.NotEqual(MoneroTx.DEFAULT_PAYMENT_ID, tx.GetPaymentId()); // default payment id converted to null
            if (tx.GetNote() != null) Assert.True(tx.GetNote().Length > 0);  // empty notes converted to undefined
            Assert.True(tx.GetUnlockTime() >= 0);
            Assert.Null(tx.GetSize());   // TODO monero-wallet-rpc: add tx_size to get_transfers and get_transfer_by_txid
            Assert.Null(tx.GetReceivedTimestamp());  // TODO monero-wallet-rpc: return received timestamp (asked to file issue if wanted)

            // test send _tx
            if (ctx.IsSendResponse == true)
            {
                Assert.True(tx.GetWeight() > 0);
                Assert.NotNull(tx.GetInputs());
                Assert.True(tx.GetInputs().Count > 0);
                foreach (MoneroOutput input in tx.GetInputs()) Assert.True(input.GetTx() == tx);
            }
            else
            {
                Assert.Null(tx.GetWeight());
                Assert.Null(tx.GetInputs());
            }

            // test confirmed
            if (tx.IsConfirmed() == true)
            {
                Assert.NotNull(tx.GetBlock());
                Assert.True(tx.GetBlock().GetTxs().Contains(tx));
                Assert.True(tx.GetBlock().GetHeight() > 0);
                Assert.True(tx.GetBlock().GetTimestamp() > 0);
                Assert.Equal(true, tx.GetRelay());
                Assert.Equal(true, tx.IsRelayed());
                Assert.Equal(false, tx.IsFailed());
                Assert.Equal(false, tx.InTxPool());
                Assert.Equal(false, tx.IsDoubleSpendSeen());
                Assert.True(tx.GetNumConfirmations() > 0);
            }
            else
            {
                Assert.Null(tx.GetBlock());
                Assert.True(tx.GetNumConfirmations() == 0);
            }

            // test in _tx pool
            if (tx.InTxPool() == true)
            {
                Assert.Equal(false, tx.IsConfirmed());
                Assert.Equal(true, tx.GetRelay());
                Assert.Equal(true, tx.IsRelayed());
                Assert.Equal(false, tx.IsDoubleSpendSeen()); // TODO: test double spend attempt
                Assert.Equal(true, tx.IsLocked());

                // these should be initialized unless a response from sending
                if (ctx.IsSendResponse != true)
                {
                    //Assert.True(_tx.GetReceivedTimestamp() > 0);  // TODO: re-enable when received timestamp returned in wallet rpc
                }
            }
            else
            {
                Assert.Null(tx.GetLastRelayedTimestamp());
            }

            // test miner _tx
            if (tx.IsMinerTx() == true)
            {
                Assert.True(tx.GetFee() == 0);
                Assert.True(tx.GetIncomingTransfers().Count > 0);
            }

            // test failed  // TODO: what else to test associated with failed
            if (tx.IsFailed() == true)
            {
                Assert.True(tx.GetOutgoingTransfer() is MoneroTransfer);
                //Assert.True(_tx.GetReceivedTimestamp() > 0);  // TODO: re-enable when received timestamp returned in wallet rpc
            }
            else
            {
                if (tx.IsRelayed() == true) Assert.Equal(tx.IsDoubleSpendSeen(), false);
                else
                {
                    Assert.Equal(false, tx.GetRelay());
                    Assert.Equal(false, tx.IsRelayed());
                    Assert.Null(tx.IsDoubleSpendSeen());
                }
            }
            Assert.Null(tx.GetLastFailedHeight());
            Assert.Null(tx.GetLastFailedHash());

            // received time only for _tx pool or failed txs
            if (tx.GetReceivedTimestamp() != null)
            {
                Assert.True(tx.InTxPool() == true || tx.IsFailed() == true);
            }

            // test relayed _tx
            if (tx.IsRelayed() == true) Assert.Equal(tx.GetRelay(), true);
            if (!tx.GetRelay() == true) Assert.True(!tx.IsRelayed());

            // test outgoing transfer per configuration
            if (ctx.HasOutgoingTransfer == false) Assert.Null(tx.GetOutgoingTransfer());
            if (ctx.HasDestinations == true) Assert.True(tx.GetOutgoingTransfer().GetDestinations().Count > 0);

            // test outgoing transfer
            if (tx.GetOutgoingTransfer() != null)
            {
                Assert.True(tx.IsOutgoing());
                TestTransfer(tx.GetOutgoingTransfer(), ctx);
                if (ctx.IsSweepResponse == true) Assert.Equal(1, tx.GetOutgoingTransfer().GetDestinations().Count);

                // TODO: handle special cases
            }
            else
            {
                Assert.True(tx.GetIncomingTransfers().Count > 0);
                Assert.Null(tx.GetOutgoingAmount());
                Assert.Null(tx.GetOutgoingTransfer());
                Assert.Null(tx.GetRingSize());
                Assert.Null(tx.GetFullHex());
                Assert.Null(tx.GetMetadata());
                Assert.Null(tx.GetKey());
            }

            // test incoming transfers
            if (tx.GetIncomingTransfers() != null)
            {
                Assert.True(tx.IsIncoming());
                Assert.True(tx.GetIncomingTransfers().Count > 0);
                TestUtils.TestUnsignedBigInteger(tx.GetIncomingAmount());
                Assert.Equal(tx.IsFailed(), false);

                // test each transfer and collect transfer sum
                ulong transferSum = 0;
                foreach (MoneroIncomingTransfer transfer in tx.GetIncomingTransfers())
                {
                    TestTransfer(transfer, ctx);
                    transferSum += transfer.GetAmount() ?? 0;
                    if (ctx.Wallet != null) Assert.Equal(ctx.Wallet.GetAddress((uint)transfer.GetAccountIndex(), (uint)transfer.GetSubaddressIndex()), transfer.GetAddress());

                    // TODO special case: transfer amount of 0
                }

                // incoming transfers add up to incoming _tx amount
                Assert.Equal(tx.GetIncomingAmount(), transferSum);
            }
            else
            {
                Assert.NotNull(tx.GetOutgoingTransfer());
                Assert.Null(tx.GetIncomingAmount());
                Assert.Null(tx.GetIncomingTransfers());
            }

            // test _tx results from send or relay
            if (ctx.IsSendResponse == true)
            {

                // test _tx set
                Assert.NotNull(tx.GetTxSet());
                bool found = false;
                foreach (MoneroTxWallet aTx in tx.GetTxSet().GetTxs())
                {
                    if (aTx == tx)
                    {
                        found = true;
                        break;
                    }
                }
                if (ctx.IsCopy == true) Assert.False(found); // copy will not have back reference from _tx set
                else Assert.True(found);

                // test common attributes
                MoneroTxConfig config = ctx.Config;
                Assert.Equal(false, tx.IsConfirmed());
                TestTransfer(tx.GetOutgoingTransfer(), ctx);
                Assert.Equal(MoneroUtils.RING_SIZE, (uint)tx.GetRingSize());
                Assert.True(0 == tx.GetUnlockTime());
                Assert.Null(tx.GetBlock());
                Assert.True(tx.GetKey().Length > 0);
                Assert.NotNull(tx.GetFullHex());
                Assert.True(tx.GetFullHex().Length > 0);
                Assert.NotNull(tx.GetMetadata());
                Assert.Null(tx.GetReceivedTimestamp());
                Assert.True(tx.IsLocked());

                // test locked state
                if (tx.GetUnlockTime() == 0) Assert.Equal(tx.IsConfirmed(), !tx.IsLocked());
                else Assert.Equal(true, tx.IsLocked());
                foreach (MoneroOutputWallet output in tx.GetOutputsWallet())
                {
                    Assert.Equal(tx.IsLocked(), output.IsLocked());
                }

                // test destinations of sent _tx
                if (tx.GetOutgoingTransfer().GetDestinations() == null)
                {
                    Assert.True(config.GetCanSplit());
                    Console.WriteLine("Destinations not returned from split transactions"); // TODO: remove this after >18.3.1 when amounts_by_dest_list official
                }
                else
                {
                    Assert.NotNull(tx.GetOutgoingTransfer().GetDestinations());
                    Assert.True(tx.GetOutgoingTransfer().GetDestinations().Count > 0);
                    bool subtractFeeFromDestinations = ctx.Config.GetSubtractFeeFrom() != null && ctx.Config.GetSubtractFeeFrom().Count > 0;
                    if (ctx.IsSweepResponse == true)
                    {
                        Assert.True(1 == config.GetDestinations().Count);
                        Assert.Null(config.GetDestinations()[0].GetAmount());
                        if (!subtractFeeFromDestinations)
                        {
                            Assert.Equal(tx.GetOutgoingTransfer().GetAmount().ToString(), tx.GetOutgoingTransfer().GetDestinations()[0].GetAmount().ToString());
                        }
                    }
                }


                // test relayed txs
                if (config.GetRelay() == true)
                {
                    Assert.Equal(true, tx.InTxPool());
                    Assert.Equal(true, tx.GetRelay());
                    Assert.Equal(true, tx.IsRelayed());
                    Assert.True(tx.GetLastRelayedTimestamp() > 0);
                    Assert.Equal(false, tx.IsDoubleSpendSeen());
                }

                // test non-relayed txs
                else
                {
                    Assert.Equal(false, tx.InTxPool());
                    Assert.Equal(false, tx.GetRelay());
                    Assert.Equal(false, tx.IsRelayed());
                    Assert.Null(tx.GetLastRelayedTimestamp());
                    Assert.Null(tx.IsDoubleSpendSeen());
                }
            }

            // test _tx result query
            else
            {
                Assert.Null(tx.GetTxSet());  // _tx set only initialized on send responses
                Assert.Null(tx.GetRingSize());
                Assert.Null(tx.GetKey());
                Assert.Null(tx.GetFullHex());
                Assert.Null(tx.GetMetadata());
                Assert.Null(tx.GetLastRelayedTimestamp());
            }

            // test inputs
            if (tx.IsOutgoing() == true && ctx.IsSendResponse == true)
            {
                Assert.NotNull(tx.GetInputs());
                Assert.True(tx.GetInputs().Count > 0);
            }
            if (tx.GetInputs() != null) foreach (MoneroOutputWallet input in tx.GetInputsWallet()) TestInputWallet(input);

            // test outputs
            if (tx.IsIncoming() == true && ctx.IncludeOutputs == true)
            {
                if (tx.IsConfirmed() == true)
                {
                    Assert.NotNull(tx.GetOutputs());
                    Assert.True(tx.GetOutputs().Count > 0);
                }
                else
                {
                    Assert.Null(tx.GetOutputs());
                }
            }
            if (tx.GetOutputs() != null) foreach (MoneroOutputWallet output in tx.GetOutputsWallet()) TestOutputWallet(output);

            // test deep copy
            if (ctx.IsCopy != true) TestTxWalletCopy(tx, ctx);
        }

        private void TestTxWalletCopy(MoneroTxWallet tx, TxContext ctx)
        {
            // copy _tx and Assert. deep equality
            MoneroTxWallet copy = tx.Clone();
            Assert.True(copy is MoneroTxWallet);
            Assert.Equal(copy, tx);

            // test different references
            if (tx.GetOutgoingTransfer() != null)
            {
                Assert.True(tx.GetOutgoingTransfer() != copy.GetOutgoingTransfer());
                Assert.True(tx.GetOutgoingTransfer().GetTx() != copy.GetOutgoingTransfer().GetTx());
                if (tx.GetOutgoingTransfer().GetDestinations() != null)
                {
                    Assert.True(tx.GetOutgoingTransfer().GetDestinations() != copy.GetOutgoingTransfer().GetDestinations());
                    for (int i = 0; i < tx.GetOutgoingTransfer().GetDestinations().Count; i++)
                    {
                        Assert.Equal(copy.GetOutgoingTransfer().GetDestinations()[i], tx.GetOutgoingTransfer().GetDestinations()[i]);
                        Assert.True(tx.GetOutgoingTransfer().GetDestinations()[i] != copy.GetOutgoingTransfer().GetDestinations()[i]);
                    }
                }
            }
            if (tx.GetIncomingTransfers() != null)
            {
                for (int i = 0; i < tx.GetIncomingTransfers().Count; i++)
                {
                    Assert.Equal(copy.GetIncomingTransfers()[i], tx.GetIncomingTransfers()[i]);
                    Assert.True(tx.GetIncomingTransfers()[i] != copy.GetIncomingTransfers()[i]);
                }
            }
            if (tx.GetInputs() != null)
            {
                for (int i = 0; i < tx.GetInputs().Count; i++)
                {
                    Assert.Equal(copy.GetInputs()[i], tx.GetInputs()[i]);
                    Assert.True(tx.GetInputs()[i] != copy.GetInputs()[i]);
                }
            }
            if (tx.GetOutputs() != null)
            {
                for (int i = 0; i < tx.GetOutputs().Count; i++)
                {
                    Assert.Equal(copy.GetOutputs()[i], tx.GetOutputs()[i]);
                    Assert.True(tx.GetOutputs()[i] != copy.GetOutputs()[i]);
                }
            }

            // test copied _tx
            ctx = new TxContext(ctx);
            ctx.IsCopy = true;
            if (tx.GetBlock() != null) copy.SetBlock(tx.GetBlock().Clone().SetTxs([copy])); // copy block for testing
            TestTxWallet(copy, ctx);

            // test merging with copy
            MoneroTxWallet merged = copy.Merge(copy.Clone());
            Assert.Equal(merged.ToString(), tx.ToString());
        }

        private static void TestTransfer(MoneroTransfer transfer, TxContext? ctx)
        {
            if (ctx == null) ctx = new TxContext();
            Assert.NotNull(transfer);
            TestUtils.TestUnsignedBigInteger(transfer.GetAmount());
            if (ctx.IsSweepOutputResponse != true) Assert.True(transfer.GetAccountIndex() >= 0);
            if (transfer.IsIncoming() == true) TestIncomingTransfer((MoneroIncomingTransfer)transfer);
            else TestOutgoingTransfer((MoneroOutgoingTransfer)transfer, ctx);

            // transfer and _tx reference each other
            Assert.NotNull(transfer.GetTx());
            if (!transfer.Equals(transfer.GetTx().GetOutgoingTransfer()))
            {
                Assert.NotNull(transfer.GetTx().GetIncomingTransfers());
                Assert.True(transfer.GetTx().GetIncomingTransfers().Contains(transfer), "Transaction does not reference given transfer");
            }
        }

        private static void TestIncomingTransfer(MoneroIncomingTransfer transfer)
        {
            Assert.True(transfer.IsIncoming());
            Assert.False(transfer.IsOutgoing());
            Assert.NotNull(transfer.GetAddress());
            Assert.True(transfer.GetSubaddressIndex() >= 0);
            Assert.True(transfer.GetNumSuggestedConfirmations() > 0);
        }

        private static void TestOutgoingTransfer(MoneroOutgoingTransfer transfer, TxContext ctx)
        {
            Assert.False(transfer.IsIncoming());
            Assert.True(transfer.IsOutgoing());
            if (ctx.IsSendResponse != true) Assert.NotNull(transfer.GetSubaddressIndices());
            if (transfer.GetSubaddressIndices() != null)
            {
                Assert.True(transfer.GetSubaddressIndices().Count >= 1);
                foreach (uint subaddressIdx in transfer.GetSubaddressIndices()) Assert.True(subaddressIdx >= 0);
            }
            if (transfer.GetAddresses() != null)
            {
                Assert.Equal(transfer.GetSubaddressIndices().Count, transfer.GetAddresses().Count);
                foreach (string address in transfer.GetAddresses()) Assert.NotNull(address);
            }

            // test destinations sum to outgoing amount
            if (transfer.GetDestinations() != null)
            {
                Assert.True(transfer.GetDestinations().Count > 0);
                ulong sum = 0;
                foreach (MoneroDestination destination in transfer.GetDestinations())
                {
                    TestDestination(destination);
                    sum += destination.GetAmount() ?? 0;
                }
                if (!transfer.GetAmount().Equals(sum)) Console.WriteLine(transfer.GetTx().GetTxSet() == null ? transfer.GetTx().ToString() : transfer.GetTx().GetTxSet().ToString());
                Assert.Equal(sum, transfer.GetAmount());
            }
        }

        private static void TestDestination(MoneroDestination destination)
        {
            MoneroUtils.ValidateAddress(destination.GetAddress(), TestUtils.NETWORK_TYPE);
            TestUtils.TestUnsignedBigInteger(destination.GetAmount(), true);
        }

        private static void TestInputWallet(MoneroOutputWallet? input)
        {
            Assert.NotNull(input);
            Assert.NotNull(input.GetKeyImage());
            Assert.NotNull(input.GetKeyImage().GetHex());
            Assert.True(input.GetKeyImage().GetHex().Length > 0);
            Assert.Null(input.GetAmount()); // must get info separately
        }

        private static void TestOutputWallet(MoneroOutputWallet? output)
        {
            Assert.NotNull(output);
            Assert.True(output.GetAccountIndex() >= 0);
            Assert.True(output.GetSubaddressIndex() >= 0);
            Assert.True(output.GetIndex() >= 0);
            Assert.NotNull(output.IsSpent());
            Assert.NotNull(output.IsLocked());
            Assert.NotNull(output.IsFrozen());
            Assert.NotNull(output.GetKeyImage());
            Assert.True(output.GetKeyImage().GetHex().Length > 0);
            TestUtils.TestUnsignedBigInteger(output.GetAmount(), true);

            // output has circular reference to its transaction which has some initialized fields
            MoneroTxWallet tx = output.GetTx();
            Assert.NotNull(tx);
            Assert.True(tx.GetOutputs().Contains(output));
            Assert.NotNull(tx.GetHash());
            Assert.NotNull(tx.IsLocked());
            Assert.Equal(true, tx.IsConfirmed());  // TODO monero-wallet-rpc: possible to get unconfirmed outputs?
            Assert.Equal(true, tx.IsRelayed());
            Assert.Equal(false, tx.IsFailed());
            Assert.True(tx.GetHeight() > 0);

            // test copying
            MoneroOutputWallet copy = output.Clone();
            Assert.True(copy != output);
            Assert.Equal(output.ToString(), copy.ToString());
            Assert.Null(copy.GetTx());  // TODO: should output copy do deep copy of _tx so models are graph instead of tree?  Would need to work out circular references
        }

        private static List<MoneroTxWallet> GetRandomTransactions(MoneroWallet wallet, MoneroTxQuery txQuery, int? minTxs, int? maxTxs)
        {
            List<MoneroTxWallet> txs = wallet.GetTxs(txQuery);
            if (minTxs != null) Assert.True(txs.Count >= minTxs, txs.Count + "/" + minTxs + " transactions found with the query");
            txs.Shuffle();
            if (maxTxs == null) return txs;
            else return txs.GetRange(0, Math.Min((int)maxTxs, txs.Count));
        }

        private static void TestCommonTxSets(List<MoneroTxWallet> txs, bool hasSigned, bool hasUnsigned, bool hasMultisig)
        {
            Assert.True(txs.Count > 0);

            // Assert. that all sets are same reference
            MoneroTxSet? set = null;
            for (int i = 0; i < txs.Count; i++)
            {
                Assert.True(txs[i] is MoneroTxWallet);
                if (i == 0) set = txs[i].GetTxSet();
                else Assert.True(txs[i].GetTxSet() == set);
            }

            // test expected set
            Assert.NotNull(set);
            if (hasSigned)
            {
                Assert.NotNull(set.GetSignedTxHex());
                Assert.True(set.GetSignedTxHex().Length > 0);
            }
            if (hasUnsigned)
            {
                Assert.NotNull(set.GetUnsignedTxHex());
                Assert.True(set.GetUnsignedTxHex().Length > 0);
            }
            if (hasMultisig)
            {
                Assert.NotNull(set.GetMultisigTxHex());
                Assert.True(set.GetMultisigTxHex().Length > 0);
            }
        }

        private static void TestCheckTx(MoneroTxWallet tx, MoneroCheckTx check)
        {
            Assert.NotNull(check.IsGood());
            if (check.IsGood() == true)
            {
                Assert.True(check.GetNumConfirmations() >= 0);
                Assert.NotNull(check.InTxPool());
                TestUtils.TestUnsignedBigInteger(check.GetReceivedAmount());
                if (check.InTxPool()) Assert.True(0 == check.GetNumConfirmations());
                else Assert.True(check.GetNumConfirmations() > 0); // TODO (monero-wall-rpc) this fails (confirmations is 0) for (at least one) transaction that has 1 confirmation on testCheckTxKey()
            }
            else
            {
                Assert.Null(check.InTxPool());
                Assert.Null(check.GetNumConfirmations());
                Assert.Null(check.GetReceivedAmount());
            }
        }

        private static void TestCheckReserve(MoneroCheckReserve check)
        {
            Assert.NotNull(check.IsGood());
            if (check.IsGood() == true)
            {
                TestUtils.TestUnsignedBigInteger(check.GetTotalAmount());
                Assert.True(check.GetTotalAmount() >= 0);
                TestUtils.TestUnsignedBigInteger(check.GetUnconfirmedSpentAmount());
                Assert.True(check.GetUnconfirmedSpentAmount() >= 0);
            }
            else
            {
                Assert.Null(check.GetTotalAmount());
                Assert.Null(check.GetUnconfirmedSpentAmount());
            }
        }

        private static void TestDescribedTxSet(MoneroTxSet describedTxSet)
        {
            Assert.NotNull(describedTxSet.GetTxs());
            Assert.False(describedTxSet.GetTxs().Count == 0);
            Assert.Null(describedTxSet.GetSignedTxHex());
            Assert.Null(describedTxSet.GetUnsignedTxHex());

            // test each transaction
            // TODO: use common _tx wallet test?
            Assert.Null(describedTxSet.GetMultisigTxHex());
            foreach (MoneroTxWallet parsedTx in describedTxSet.GetTxs())
            {
                Assert.True(parsedTx.GetTxSet() == describedTxSet);
                TestUtils.TestUnsignedBigInteger(parsedTx.GetInputSum(), true);
                TestUtils.TestUnsignedBigInteger(parsedTx.GetOutputSum(), true);
                TestUtils.TestUnsignedBigInteger(parsedTx.GetFee());
                TestUtils.TestUnsignedBigInteger(parsedTx.GetChangeAmount());
                if (parsedTx.GetChangeAmount().Equals(0)) Assert.Null(parsedTx.GetChangeAddress());
                else MoneroUtils.ValidateAddress(parsedTx.GetChangeAddress(), TestUtils.NETWORK_TYPE);
                Assert.True(parsedTx.GetRingSize() > 1);
                Assert.True(parsedTx.GetUnlockTime() >= 0);
                Assert.True(parsedTx.GetNumDummyOutputs() >= 0);
                Assert.False(parsedTx.GetExtraHex().Length == 0);
                Assert.True(parsedTx.GetPaymentId() == null || parsedTx.GetPaymentId().Length != 0);
                Assert.True(parsedTx.IsOutgoing());
                Assert.NotNull(parsedTx.GetOutgoingTransfer());
                Assert.NotNull(parsedTx.GetOutgoingTransfer().GetDestinations());
                Assert.False(parsedTx.GetOutgoingTransfer().GetDestinations().Count == 0);
                Assert.Null(parsedTx.IsIncoming());
                foreach (MoneroDestination destination in parsedTx.GetOutgoingTransfer().GetDestinations())
                {
                    TestDestination(destination);
                }
            }
        }

        private static void TestAddressBookEntry(MoneroAddressBookEntry entry)
        {
            Assert.True(entry.GetIndex() >= 0);
            MoneroUtils.ValidateAddress(entry.GetAddress(), TestUtils.NETWORK_TYPE);
            Assert.NotNull(entry.GetDescription());
        }

        private void TestGetTxsStructure(List<MoneroTxWallet> txs, MoneroTxQuery? query)
        {
            if (query == null) query = new MoneroTxQuery();

            // collect unique blocks in order (using set and list instead of TreeSet for direct portability to other languages)
            HashSet<MoneroBlock> seenBlocks = [];
            List<MoneroBlock> blocks = [];
            List<MoneroTxWallet> unconfirmedTxs = [];
            foreach (MoneroTxWallet tx in txs)
            {
                if (tx.GetBlock() == null) unconfirmedTxs.Add(tx);
                else
                {
                    Assert.True(tx.GetBlock().GetTxs().Contains(tx));
                    if (!seenBlocks.Contains(tx.GetBlock()))
                    {
                        seenBlocks.Add(tx.GetBlock());
                        blocks.Add(tx.GetBlock());
                    }
                }
            }

            // _tx hashes must be in order if requested
            if (query.GetHashes() != null)
            {
                Assert.Equal(txs.Count, query.GetHashes().Count);
                for (int i = 0; i < query.GetHashes().Count; i++)
                {
                    Assert.Equal(query.GetHashes()[i], txs[i].GetHash());
                }
            }

            // test that txs and blocks reference each other and blocks are in ascending order unless specific _tx hashes queried
            int index = 0;
            ulong? prevBlockHeight = null;
            foreach (MoneroBlock block in blocks)
            {
                if (prevBlockHeight == null) prevBlockHeight = block.GetHeight();
                else if (query.GetHashes() == null) Assert.True(block.GetHeight() > prevBlockHeight, "Blocks are not in order of heights: " + prevBlockHeight + " vs " + block.GetHeight());
                foreach (MoneroTx tx in block.GetTxs())
                {
                    Assert.True(tx.GetBlock() == block);
                    if (query.GetHashes() == null)
                    {
                        Assert.Equal(txs[index].GetHash(), tx.GetHash()); // verify _tx order is self-consistent with blocks unless txs manually re-ordered by querying by hash
                        Assert.True(txs[index] == tx);
                    }
                    index++;
                }
            }
            Assert.Equal(txs.Count, index + unconfirmedTxs.Count);

            // test that incoming transfers are in order of ascending accounts and subaddresses
            foreach (MoneroTxWallet tx in txs)
            {
                uint? prevAccountIdx = null;
                uint? prevSubaddressIdx = null;
                if (tx.GetIncomingTransfers() == null) continue;
                foreach (MoneroIncomingTransfer transfer in tx.GetIncomingTransfers())
                {
                    if (prevAccountIdx == null) prevAccountIdx = transfer.GetAccountIndex();
                    else
                    {
                        Assert.True(prevAccountIdx <= transfer.GetAccountIndex());
                        if (prevAccountIdx < transfer.GetAccountIndex())
                        {
                            prevSubaddressIdx = null;
                            prevAccountIdx = transfer.GetAccountIndex();
                        }
                        if (prevSubaddressIdx == null) prevSubaddressIdx = transfer.GetSubaddressIndex();
                        else Assert.True(prevSubaddressIdx < transfer.GetSubaddressIndex());
                    }
                }
            }

            // test that outputs are in order of ascending accounts and subaddresses
            foreach (MoneroTxWallet tx in txs)
            {
                uint? prevAccountIdx = null;
                uint? prevSubaddressIdx = null;
                if (tx.GetOutputs() == null) continue;
                foreach (MoneroOutputWallet output in tx.GetOutputsWallet())
                {
                    if (prevAccountIdx == null) prevAccountIdx = output.GetAccountIndex();
                    else
                    {
                        Assert.True(prevAccountIdx <= output.GetAccountIndex());
                        if (prevAccountIdx < output.GetAccountIndex())
                        {
                            prevSubaddressIdx = null;
                            prevAccountIdx = output.GetAccountIndex();
                        }
                        if (prevSubaddressIdx == null) prevSubaddressIdx = output.GetSubaddressIndex();
                        else Assert.True(prevSubaddressIdx <= output.GetSubaddressIndex(), output.GetKeyImage().ToString() + " " + prevSubaddressIdx + " > " + output.GetSubaddressIndex()); // TODO: this does not test that index < other index if subaddresses are equal
                    }
                }
            }

        }

        private static Dictionary<ulong, uint> CountNumInstances(List<ulong> instances)
        {
            Dictionary<ulong, uint> heightCounts = [];
            foreach (ulong instance in instances)
            {
                uint count = heightCounts.GetValueOrDefault(instance, (uint)0);
                heightCounts.Add(instance, count + 1);
            }
            return heightCounts;
        }

        private static HashSet<ulong> GetModes(Dictionary<ulong, uint> counts)
        {
            HashSet<ulong> modes = new HashSet<ulong>();
            uint? maxCount = null;
            foreach (uint count in counts.Values)
            {
                if (maxCount == null || count > maxCount) maxCount = count;
            }
            foreach (var entry in counts)
            {
                if (entry.Value == maxCount) modes.Add(entry.Key);
            }
            return modes;
        }

        #endregion
    }
}

public class TxContext
{
    public MoneroWallet? Wallet;
    public MoneroTxConfig? Config;
    public bool? HasOutgoingTransfer;
    public bool? HasIncomingTransfers;
    public bool? HasDestinations;
    public bool? IsCopy;                 // indicates if a copy is being tested which means back references won't be the same
    public bool? IncludeOutputs;
    public bool? IsSendResponse;
    public bool? IsSweepResponse;
    public bool? IsSweepOutputResponse;  // TODO monero-wallet-rpc: this only necessary because sweep_output does not return account index
    
    public TxContext() { }
    
    public TxContext(TxContext ctx)
    {
        if (ctx == null) return;
        Wallet = ctx.Wallet;
        Config = ctx.Config;
        HasOutgoingTransfer = ctx.HasOutgoingTransfer;
        HasIncomingTransfers = ctx.HasIncomingTransfers;
        HasDestinations = ctx.HasDestinations;
        IsCopy = ctx.IsCopy;
        IncludeOutputs = ctx.IncludeOutputs;
        IsSendResponse = ctx.IsSendResponse;
        IsSweepResponse = ctx.IsSweepResponse;
        IsSweepOutputResponse = ctx.IsSweepOutputResponse;
    }
}