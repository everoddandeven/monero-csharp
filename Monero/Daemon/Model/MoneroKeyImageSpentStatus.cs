namespace Monero.Daemon.Model;

/// <summary>
/// Enumerates a key image spent status.
/// </summary>
public enum MoneroKeyImageSpentStatus
{
    /// <summary>
    /// Indicates that key image is not spent on blockchain.
    /// </summary>
    NotSpent = 0,
    /// <summary>
    /// Indicates that key image was spent on blockchain.
    /// </summary>
    Confirmed,
    /// <summary>
    /// Indicates that key image is spent but still on transactions pool.
    /// </summary>
    TxPool
}
