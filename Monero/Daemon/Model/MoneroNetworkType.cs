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
    StageNet
}
