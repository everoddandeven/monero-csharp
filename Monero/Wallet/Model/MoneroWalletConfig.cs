using System.Text.Json.Serialization;
using Monero.Common;
using Monero.Daemon.Model;

namespace Monero.Wallet.Model;

/// <summary>
/// Configures a wallet to create. The JSON is the one that monero-c reads: a wallet is restored from its seed if the config has one, from its
/// keys if it has the address or the private keys, and it has a new random seed otherwise.
/// </summary>
public class MoneroWalletConfig
{
    /// <summary>
    /// Initialize a new wallet config.
    /// </summary>
    public MoneroWalletConfig()
    {
        // nothing to build
    }

    /// <summary>
    /// Construct this config as a deep copy of the given config.
    /// </summary>
    /// <param name="config">the config to copy.</param>
    public MoneroWalletConfig(MoneroWalletConfig config)
    {
        Path = config.Path;
        Password = config.Password;
        NetworkType = config.NetworkType;
        Server = config.Server?.Copy();
        IsTrustedDaemon = config.IsTrustedDaemon;
        Seed = config.Seed;
        SeedOffset = config.SeedOffset;
        PrimaryAddress = config.PrimaryAddress;
        PrivateViewKey = config.PrivateViewKey;
        PrivateSpendKey = config.PrivateSpendKey;
        RestoreHeight = config.RestoreHeight;
        Language = config.Language;
        SaveCurrent = config.SaveCurrent;
        AccountLookahead = config.AccountLookahead;
        SubaddressLookahead = config.SubaddressLookahead;
        IsMultisig = config.IsMultisig;
        Regtest = config.Regtest;
    }

    /// <summary>
    /// Copy this config.
    /// </summary>
    /// <returns>a deep copy of the config.</returns>
    public virtual MoneroWalletConfig Copy()
    {
        return new MoneroWalletConfig(this);
    }

    /// <summary>
    /// Path of the wallet file, which is written with its keys file. The wallet stays in memory without it.
    /// </summary>
    [JsonPropertyName("path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Path { get; set; }

    /// <summary>
    /// Password that encrypts the wallet files.
    /// </summary>
    [JsonPropertyName("password")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Password { get; set; }

    /// <summary>
    /// Network of the wallet. It is the only field that monero-c needs.
    /// </summary>
    [JsonPropertyName("networkType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroNetworkType? NetworkType { get; set; }

    /// <summary>
    /// Daemon that the wallet connects to when it is created.
    /// </summary>
    [JsonPropertyName("server")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroRpcConnection? Server { get; set; }

    /// <summary>
    /// Whether the daemon is trusted. When it is not set, a daemon on a local address is trusted and the others are not.
    /// </summary>
    [JsonPropertyName("isTrustedDaemon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsTrustedDaemon { get; set; }

    /// <summary>
    /// Mnemonic seed to restore the wallet from. For a multisig wallet, it is its multisig data in hexadecimal format.
    /// </summary>
    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Seed { get; set; }

    /// <summary>
    /// Offset of the mnemonic seed.
    /// </summary>
    [JsonPropertyName("seedOffset")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SeedOffset { get; set; }

    /// <summary>
    /// Primary address to restore the wallet from.
    /// </summary>
    [JsonPropertyName("primaryAddress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrimaryAddress { get; set; }

    /// <summary>
    /// Private view key to restore the wallet from.
    /// </summary>
    [JsonPropertyName("privateViewKey")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrivateViewKey { get; set; }

    /// <summary>
    /// Private spend key to restore the wallet from. Without it, the wallet is view-only.
    /// </summary>
    [JsonPropertyName("privateSpendKey")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrivateSpendKey { get; set; }

    /// <summary>
    /// Height of the first block to scan. It is 0 when it is not set.
    /// </summary>
    [JsonPropertyName("restoreHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? RestoreHeight { get; set; }

    /// <summary>
    /// Language of the seed. It is English when it is not set.
    /// </summary>
    [JsonPropertyName("language")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; }

    /// <summary>
    /// Whether to save the current wallet when the config is applied.
    /// </summary>
    [JsonPropertyName("saveCurrent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SaveCurrent { get; set; }

    /// <summary>
    /// Number of accounts that the wallet scans. It goes with the lookahead of the subaddresses, or none does.
    /// </summary>
    [JsonPropertyName("accountLookahead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? AccountLookahead { get; set; }

    /// <summary>
    /// Number of subaddresses of each account that the wallet scans. It goes with the lookahead of the accounts, or none does.
    /// </summary>
    [JsonPropertyName("subaddressLookahead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? SubaddressLookahead { get; set; }

    /// <summary>
    /// Whether the wallet is restored as a multisig wallet.
    /// </summary>
    [JsonPropertyName("isMultisig")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsMultisig { get; set; }

    /// <summary>
    /// The way that monero-cpp has for a regtest wallet, which is a flag of the main network. monero-c takes <see cref="MoneroNetworkType.Regtest"/>
    /// as the network type instead.
    /// </summary>
    [JsonPropertyName("regtest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Regtest { get; set; }
}
