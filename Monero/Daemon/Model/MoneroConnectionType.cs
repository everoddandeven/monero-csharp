namespace Monero.Daemon.Model;

/// <summary>
/// Enumerates the types of address of a connection of the daemon. The numbers are the ones of monero-cpp and monero-c, and the JSON has
/// them as numbers.
/// </summary>
public enum MoneroConnectionType
{
    /// <summary>
    /// The type of the address is not known.
    /// </summary>
    Invalid = 0,

    /// <summary>
    /// An IPv4 address.
    /// </summary>
    Ipv4,

    /// <summary>
    /// An IPv6 address.
    /// </summary>
    Ipv6,

    /// <summary>
    /// A Tor onion address.
    /// </summary>
    Tor,

    /// <summary>
    /// An I2P address.
    /// </summary>
    I2P
}
