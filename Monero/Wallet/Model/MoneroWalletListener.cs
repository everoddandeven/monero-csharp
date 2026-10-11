namespace Monero.Wallet.Model;

/// <summary>
/// Receives the notifications of a wallet. Every method does nothing, so a listener overrides the ones it needs.
/// </summary>
public class MoneroWalletListener
{
    /// <summary>
    /// Called with the progress of a sync.
    /// </summary>
    /// <param name="height">the height of the block that was synced.</param>
    /// <param name="startHeight">the height that the sync started from.</param>
    /// <param name="endHeight">the height that the sync goes to.</param>
    /// <param name="percentDone">the progress of the sync, from 0 to 1.</param>
    /// <param name="message">a description of the progress.</param>
    public virtual void OnSyncProgress(ulong height, ulong startHeight, ulong endHeight, double percentDone, string message)
    {
        // nothing to do
    }

    /// <summary>
    /// Called when the wallet processes a new block.
    /// </summary>
    /// <param name="height">the height of the block.</param>
    public virtual void OnNewBlock(ulong height)
    {
        // nothing to do
    }

    /// <summary>
    /// Called when the balances of the wallet change.
    /// </summary>
    /// <param name="newBalance">the balance, in atomic units.</param>
    /// <param name="newUnlockedBalance">the balance that can be spent now, in atomic units.</param>
    public virtual void OnBalancesChanged(ulong newBalance, ulong newUnlockedBalance)
    {
        // nothing to do
    }

    /// <summary>
    /// Called when the wallet receives an output. The output is in its transaction, which is in its block.
    /// </summary>
    /// <param name="output">the output.</param>
    public virtual void OnOutputReceived(MoneroOutputWallet output)
    {
        // nothing to do
    }

    /// <summary>
    /// Called when the wallet spends an output. The output is in its transaction, which is in its block.
    /// </summary>
    /// <param name="output">the output.</param>
    public virtual void OnOutputSpent(MoneroOutputWallet output)
    {
        // nothing to do
    }
}
