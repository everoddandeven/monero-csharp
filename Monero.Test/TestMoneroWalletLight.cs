using Monero.Daemon;
using Monero.Wallet;
using Monero.Wallet.Common;

namespace Monero.Test;

public class MoneroWalletLightFixture : MoneroWalletCommonFixture
{
    public new MoneroWalletLight _wallet;
    public MoneroDaemonLws _lws;

    public MoneroWalletLightFixture(MoneroWalletLight wallet, MoneroDaemonRpc daemon, MoneroDaemonLws lws) : base(wallet, daemon)
    {
        // Before All
        _wallet = wallet;
        _lws = lws;
    }
}

public class TestMoneroWalletLight : TestMoneroWalletCommon, IClassFixture<MoneroWalletLightFixture>
{
    public TestMoneroWalletLight(MoneroWalletLightFixture walletLightFixture) : base(walletLightFixture)
    {

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
