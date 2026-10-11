namespace Monero.Daemon.Model;

/// <summary>
/// Monero network type parser.
/// </summary>
internal static class MoneroNetworkTypeParser
{
    /// <summary>
    /// Parse Monero network type.
    /// </summary>
    /// <param name="networkType">network type in integer format: 0 is mainnet, 1 is testnet and 2 is stagenet.</param>
    /// <returns>parsed network type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">the number is not a network type.</exception>
    public static MoneroNetworkType Parse(int networkType)
    {
        if (Enum.IsDefined(typeof(MoneroNetworkType), networkType))
        {
            return (MoneroNetworkType)networkType;
        }

        throw new ArgumentOutOfRangeException(nameof(networkType), networkType, "Unknown network type");
    }

    /// <summary>
    /// Parse Monero network type, as the daemon RPC names it.
    /// </summary>
    /// <param name="networkType">network type in text format: <c>mainnet</c>, <c>testnet</c> or <c>stagenet</c>, in any case.</param>
    /// <returns>parsed network type.</returns>
    /// <exception cref="ArgumentException">the text is not a network type.</exception>
    public static MoneroNetworkType Parse(string networkType)
    {
        return networkType.Trim().ToLowerInvariant() switch
        {
            "mainnet" => MoneroNetworkType.MainNet,
            "testnet" => MoneroNetworkType.TestNet,
            "stagenet" => MoneroNetworkType.StageNet,
            _ => throw new ArgumentException($"Unknown network type: {networkType}", nameof(networkType))
        };
    }
}
