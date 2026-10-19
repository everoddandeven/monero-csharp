using Monero.Wallet;
using Monero.Wallet.Model;

namespace Monero.IntegrationTests.Utils;

/// <summary>
/// Utilities of the wallet tests: funding wallets with the coins of the mining wallet.
/// </summary>
public static class WalletTestUtils
{
    /// <summary>Atomic units in 1 XMR.</summary>
    public const ulong AtomicUnitsPerXmr = 1_000_000_000_000UL;

    /// <summary>
    /// Get an address of a wallet that is not a test wallet.
    /// </summary>
    public static string GetExternalWalletAddress() => "87a1Yf47UqyQFCrMqqtxfvhJN9se3PgbmU7KUFWqhSu5aih6YsZYoxfjgyxAM1DztNNSdoYTZYn9xa3vHeJjoZqdAybnLzN";

    /// <summary>
    /// Select a subaddress of a wallet with a minimum unlocked balance.
    /// </summary>
    /// <param name="wallet">the wallet to select the subaddress from.</param>
    /// <param name="minBalance">the minimum unlocked balance of the subaddress.</param>
    /// <param name="skipPrimary">skips the primary address of the wallet.</param>
    /// <returns>the subaddress, or <c>null</c> if the wallet has none with that balance.</returns>
    public static async Task<MoneroSubaddress?> SelectSubaddressWithMinBalance(MoneroWallet wallet, ulong minBalance, bool skipPrimary = true)
    {
        foreach (var account in await wallet.GetAccountsAsync(true))
        {
            foreach (var subaddress in account.Subaddresses ?? [])
            {
                if (skipPrimary && account.Index == 0 && subaddress.Index == 0) continue;
                if (subaddress.UnlockedBalance >= minBalance) return subaddress;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if a wallet has the funds that the tests need.
    /// </summary>
    public static async Task<bool> IsWalletFunded(MoneroWallet wallet, ulong amountPerAddress, int numAccounts, int numSubaddresses)
    {
        var amountRequired = amountPerAddress * (ulong)(numSubaddresses + 1) * (ulong)numAccounts;
        var requiredSubaddresses = numAccounts * (numSubaddresses + 1);

        await wallet.SyncAsync();
        if (await wallet.GetBalanceAsync() < amountRequired) return false;

        var accounts = await wallet.GetAccountsAsync(true);
        if (accounts.Count < numAccounts) return false;

        var funded = accounts.SelectMany(account => account.Subaddresses ?? []).Count(subaddress => subaddress.UnlockedBalance >= amountPerAddress);

        return funded >= requiredSubaddresses;
    }

    /// <summary>
    /// Build a transaction that funds every subaddress of the accounts up to an amount, creating the accounts and the subaddresses that are missing.
    /// </summary>
    public static async Task<MoneroTxConfig> BuildTxConfig(MoneroWallet wallet, int numAccounts, int numSubaddresses, ulong amountPerAddress)
    {
        var config = new MoneroTxConfig { AccountIndex = 0, Relay = true, CanSplit = true };

        while ((await wallet.GetAccountsAsync()).Count < numAccounts) await wallet.CreateAccountAsync();

        for (uint accountIdx = 0; accountIdx < numAccounts; accountIdx++)
        {
            var account = await wallet.GetAccountAsync(accountIdx, true);
            var count = account.Subaddresses?.Count ?? 0;
            while (count++ < numSubaddresses + 1) await wallet.CreateSubaddressAsync(accountIdx);

            var indices = Enumerable.Range(0, numSubaddresses + 1).Select(i => (uint)i).ToList();
            foreach (var subaddress in await wallet.GetSubaddressesAsync(accountIdx, indices))
            {
                // skip the subaddress that is funded already
                if (subaddress.UnlockedBalance >= amountPerAddress) continue;

                config.AddDestination(subaddress.Address!, amountPerAddress);
            }
        }

        return config;
    }

    /// <summary>
    /// Fund a wallet with the coins of the mining wallet.
    /// </summary>
    /// <param name="wallet">the wallet to fund.</param>
    /// <param name="xmrAmountPerAddress">the amount of XMR for each address.</param>
    /// <param name="numAccounts">the number of accounts to fund.</param>
    /// <param name="numSubaddresses">the number of subaddresses to fund in each account, besides the primary address.</param>
    /// <returns>the funding transactions, none if the wallet had its funds.</returns>
    public static async Task<List<MoneroTxWallet>> FundWallet(MoneroWallet wallet, ulong xmrAmountPerAddress = 10, int numAccounts = 3, int numSubaddresses = 5)
    {
        var amountPerAddress = xmrAmountPerAddress * AtomicUnitsPerXmr;
        if (await IsWalletFunded(wallet, amountPerAddress, numAccounts, numSubaddresses)) return [];

        var amountRequired = amountPerAddress * (ulong)(numSubaddresses + 1) * (ulong)numAccounts;
        var config = await BuildTxConfig(wallet, numAccounts, numSubaddresses, amountPerAddress);

        var miningWallet = await TestUtils.GetMiningWalletRpc();
        var balance = await miningWallet.GetUnlockedBalanceAsync();
        Assert.That(balance, Is.GreaterThan(amountRequired), "The mining wallet doesn't have enough unlocked balance");

        var txs = await miningWallet.CreateTxsAsync(config);
        foreach (var tx in txs)
        {
            Assert.That(tx.IsFailed, Is.Not.True, "Cannot fund wallet: tx failed");
        }

        await wallet.SaveAsync();

        return txs;
    }
}
