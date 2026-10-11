namespace Monero.Wallet.Model;

/// <summary>
/// Enumerates the priorities of the fee of a transaction. The numbers are the ones of monero-cpp and monero-c, and the JSON has them as
/// numbers.
/// </summary>
public enum MoneroTxPriority
{
    /// <summary>
    /// The default priority of the wallet.
    /// </summary>
    Default = 0,

    /// <summary>
    /// The lowest fee, and the slowest confirmation.
    /// </summary>
    Unimportant,

    /// <summary>
    /// A normal fee.
    /// </summary>
    Normal,

    /// <summary>
    /// The highest fee, and the fastest confirmation.
    /// </summary>
    Elevated
}
