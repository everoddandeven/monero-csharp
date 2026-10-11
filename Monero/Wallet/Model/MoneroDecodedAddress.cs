using System.Text.Json.Serialization;
using Monero.Daemon.Model;

namespace Monero.Wallet.Model;

/// <summary>
/// Models an address that was decoded: its type, and the network that it is for.
/// </summary>
public class MoneroDecodedAddress
{
    /// <summary>
    /// Initialize a new decoded address.
    /// </summary>
    public MoneroDecodedAddress()
    {
        // nothing to build
    }

    /// <summary>
    /// Initialize a new decoded address.
    /// </summary>
    /// <param name="address">the address.</param>
    /// <param name="addressType">the type of the address.</param>
    /// <param name="networkType">the network that the address is for.</param>
    public MoneroDecodedAddress(string address, MoneroAddressType addressType, MoneroNetworkType networkType)
    {
        Address = address;
        AddressType = addressType;
        NetworkType = networkType;
    }

    /// <summary>
    /// The address.
    /// </summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }

    /// <summary>
    /// Type of the address.
    /// </summary>
    [JsonPropertyName("addressType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroAddressType? AddressType { get; set; }

    /// <summary>
    /// Network that the address is for.
    /// </summary>
    [JsonPropertyName("networkType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroNetworkType? NetworkType { get; set; }
}
