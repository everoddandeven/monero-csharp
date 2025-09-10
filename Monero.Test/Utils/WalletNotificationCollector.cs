using Monero.Wallet;
using Monero.Wallet.Common;

namespace Monero.Test.Utils;

public class WalletNotificationCollector : MoneroWalletListener
{
    private bool _listening;
    private readonly List<ulong> _blockNotifications = [];
    private readonly List<KeyValuePair<ulong, ulong>> _balanceNotifications = [];
    private readonly List<MoneroOutputWallet> _outputsReceived = [];
    private readonly List<MoneroOutputWallet> _outputsSpent = [];

    public override void OnNewBlock(ulong height)
    {
        Assert.True(_listening);
        if (_blockNotifications.Count > 0)
        {
            var last = _blockNotifications.Last();
            Assert.True(height == last + 1);
        }

        _blockNotifications.Add(height);
    }

    public override void OnBalancesChanged(ulong newBalance, ulong newUnlockedBalance)
    {
        Assert.True(_listening);

        if (_balanceNotifications.Count != 0)
        {
            KeyValuePair<ulong, ulong> lastNotification = _balanceNotifications.Last();
            Assert.True(!newBalance.Equals(lastNotification.Key) || !newUnlockedBalance.Equals(lastNotification.Value)); // test that balances change
        }

        _balanceNotifications.Add(new KeyValuePair<ulong, ulong>(newBalance, newUnlockedBalance));
    }

    public override void OnOutputReceived(MoneroOutputWallet output)
    {
        Assert.True(_listening);
        _outputsReceived.Add(output);
    }

    public override void OnOutputSpent(MoneroOutputWallet output)
    {
        Assert.True(_listening);
        _outputsSpent.Add(output);
    }

    public List<ulong> GetBlockNotifications()
    {
        return _blockNotifications;
    }

    public List<KeyValuePair<ulong, ulong>> GetBalanceNotifications()
    {
        return _balanceNotifications;
    }

    public List<MoneroOutputWallet> GetOutputsReceived(MoneroOutputQuery? query = null)
    {
        List<MoneroOutputWallet> result = [];

        foreach (var output in _outputsReceived)
        {
            if (query == null || query.MeetsCriteria(output))
            {
                result.Add(output);
            }
        }

        return result;
    }

    public List<MoneroOutputWallet> GetOutputsSpent(MoneroOutputQuery? query = null)
    {
        List<MoneroOutputWallet> result = [];

        foreach (var output in _outputsSpent)
        {
            if (query == null || query.MeetsCriteria(output))
            {
                result.Add(output);
            }
        }

        return result;
    }

    public void SetListening(bool listening)
    {
        _listening = listening;
    }

}
