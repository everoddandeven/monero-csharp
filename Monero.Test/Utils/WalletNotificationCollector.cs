using Monero.Wallet;
using Monero.Wallet.Common;

namespace Monero.Test.Utils
{
    public class WalletNotificationCollector : MoneroWalletListener
    {
        private bool listening = false;
        private List<ulong> blockNotifications = [];
        private List<KeyValuePair<ulong, ulong>> balanceNotifications = [];
        private List<MoneroOutputWallet> outputsReceived = [];
        private List<MoneroOutputWallet> outputsSpent = [];

        public override void OnNewBlock(ulong height)
        {
            Assert.True(listening);
            if (blockNotifications.Count > 0) 
            {
                var last = blockNotifications.Last();
                Assert.True(height == last + 1);
            }

            blockNotifications.Add(height);
        }

        public override void OnBalancesChanged(ulong newBalance, ulong newUnlockedBalance)
        {
            Assert.True(listening);

            if (balanceNotifications.Count != 0)
            {
                KeyValuePair<ulong, ulong> lastNotification = balanceNotifications.Last();
                Assert.True(!newBalance.Equals(lastNotification.Key) || !newUnlockedBalance.Equals(lastNotification.Value)); // test that balances change
            }

            balanceNotifications.Add(new KeyValuePair<ulong, ulong>(newBalance, newUnlockedBalance));
        }

        public override void OnOutputReceived(MoneroOutputWallet output)
        {
            Assert.True(listening);
            outputsReceived.Add(output);
        }

        public override void OnOutputSpent(MoneroOutputWallet output)
        {
            Assert.True(listening);
            outputsSpent.Add(output);
        }

        public List<ulong> GetBlockNotifications()
        {
            return blockNotifications;
        }

        public List<KeyValuePair<ulong, ulong>> GetBalanceNotifications()
        {
            return balanceNotifications;
        }

        public List<MoneroOutputWallet> GetOutputsReceived(MoneroOutputQuery? query = null)
        {
            List<MoneroOutputWallet> result = [];

            foreach (var output in outputsReceived)
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

            foreach (var output in outputsSpent)
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
            this.listening = listening;
        }

    }
}
