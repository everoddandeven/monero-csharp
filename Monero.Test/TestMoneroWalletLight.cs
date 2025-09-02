using Monero.Daemon;
using Monero.Test.Utils;
using Monero.Wallet;
using Monero.Wallet.Common;

namespace Monero.Test
{
    public class MoneroWalletLightFixture : MoneroWalletCommonFixture
    {
        public new MoneroWalletLight wallet;
        public MoneroDaemonLws lws;

        public MoneroWalletLightFixture(MoneroWalletLight wallet, MoneroDaemonRpc daemon, MoneroDaemonLws lws) : base(wallet, daemon)
        {
            // Before All
            this.wallet = wallet;
            this.lws = lws;
        }

        public override void Dispose(bool disposing)
        {
            // After All
            if (disposing != true) return;
            base.Dispose(true);

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
    }

    public class TestMoneroWalletLight : TestMoneroWalletCommon, IClassFixture<MoneroWalletLightFixture>
    {
        public TestMoneroWalletLight(MoneroWalletLightFixture walletLightFixture): base(walletLightFixture) {
        
        }

        protected override void CloseWallet(MoneroWallet wallet, bool save)
        {
            throw new NotImplementedException();
        }

        protected override MoneroWallet CreateWallet(MoneroWalletConfig config)
        {
            throw new NotImplementedException();
        }

        protected override List<string> GetSeedLanguages()
        {
            throw new NotImplementedException();
        }

        protected override MoneroWallet GetTestWallet()
        {
            throw new NotImplementedException();
        }

        protected override MoneroWallet OpenWallet(MoneroWalletConfig config)
        {
            throw new NotImplementedException();
        }
    }
}
