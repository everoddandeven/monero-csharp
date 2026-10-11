namespace Monero.Daemon.Model;

/// <summary>
/// Enumerates daemon networks. The numbers are the ones of monero-cpp and monero-c, and the JSON has them as numbers.
/// </summary>
public enum MoneroNetworkType
{
    /// <summary>
    /// The main network, where Monero has value.
    /// </summary>
    MainNet = 0,

    /// <summary>
    /// The test network.
    /// </summary>
    TestNet,

    /// <summary>
    /// The stage network, a copy of the main network with no value, to try software before it is used on the main one.
    /// </summary>
    StageNet,

    /// <summary>
    /// A private test chain on which the blocks are mined on demand. The ABI of monero-c gives it a type of its own, 3, for the wallets, while
    /// monero-cpp has it as a flag of the main network, and a daemon reports it as <see cref="MainNet"/> with its <c>isRegtest</c> set.
    /// </summary>
    Regtest
}
