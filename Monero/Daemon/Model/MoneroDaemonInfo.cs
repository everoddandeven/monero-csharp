using System.Text.Json.Serialization;
using Monero.Common;

namespace Monero.Daemon.Model;

/// <summary>
/// Models general information about daemon and the network.
/// </summary>
public class MoneroDaemonInfo : MoneroRpcPaymentInfo
{
    /// <summary>
    /// Daemon version.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }

    /// <summary>
    /// Number of alternative blocks seen by the node.
    /// </summary>
    [JsonPropertyName("numAltBlocks")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? NumAltBlocks { get; set; }
    
    /// <summary>
    /// Maximum allowed adjusted block size based on latest 100000 blocks.
    /// Backward compatibility, same as <c>BlockWeightLimit</c>.
    /// </summary>
    [JsonPropertyName("blockSizeLimit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? BlockSizeLimit { get; set; }
    
    /// <summary>
    /// Median adjusted block size of latest 100000 blocks.
    /// Backward compatibility, same as <c>BlockWeightMedian</c>.
    /// </summary>
    [JsonPropertyName("blockSizeMedian")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? BlockSizeMedian { get; set; }
        
    /// <summary>
    /// Maximum allowed adjusted block size based on latest 100000 blocks.
    /// </summary>
    [JsonPropertyName("blockWeightLimit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? BlockWeightLimit { get; set; }
    
    /// <summary>
    /// Median adjusted block size of latest 100000 blocks.
    /// </summary>
    [JsonPropertyName("blockWeightMedian")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? BlockWeightMedian { get; set; }
    
    /// <summary>
    /// Bootstrap node address used for immediate wallet usability while syncing.
    /// </summary>
    [JsonPropertyName("bootstrapDaemonAddress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BootstrapDaemonAddress { get; set; }
    
    /// <summary>
    /// Least significant 64 bits of the network difficulty, which has 128 bits.
    /// </summary>
    [JsonPropertyName("difficultyLow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? DifficultyLow { get; set; }

    /// <summary>
    /// Most significant 64 bits of the network difficulty.
    /// </summary>
    [JsonPropertyName("difficultyHigh")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? DifficultyHigh { get; set; }

    /// <summary>
    /// The network difficulty, joined from its two halves. Assigning it sets both.
    /// </summary>
    [JsonIgnore]
    public UInt128? Difficulty
    {
        get => MoneroUInt128.Combine(DifficultyLow, DifficultyHigh);
        set => (DifficultyLow, DifficultyHigh) = MoneroUInt128.Split(value);
    }
    
    /// <summary>
    /// Least significant 64 bits of the cumulative difficulty, which has 128 bits.
    /// </summary>
    [JsonPropertyName("cumulativeDifficultyLow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? CumulativeDifficultyLow { get; set; }

    /// <summary>
    /// Most significant 64 bits of the cumulative difficulty.
    /// </summary>
    [JsonPropertyName("cumulativeDifficultyHigh")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? CumulativeDifficultyHigh { get; set; }

    /// <summary>
    /// The cumulative difficulty, joined from its two halves. Assigning it sets both.
    /// </summary>
    [JsonIgnore]
    public UInt128? CumulativeDifficulty
    {
        get => MoneroUInt128.Combine(CumulativeDifficultyLow, CumulativeDifficultyHigh);
        set => (CumulativeDifficultyLow, CumulativeDifficultyHigh) = MoneroUInt128.Split(value);
    }
 
    /// <summary>
    /// Available disk space on the node.
    /// </summary>
    [JsonPropertyName("freeSpace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? FreeSpace { get; set; }
    
    /// <summary>
    /// Number of known disconnected peers.
    /// </summary>
    [JsonPropertyName("numOfflinePeers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumOfflinePeers { get; set; }
 
    /// <summary>
    /// Number of connected peers.
    /// </summary>
    [JsonPropertyName("numOnlinePeers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumOnlinePeers { get; set; }
    
    /// <summary>
    /// Current block height of longest chain known to daemon.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Height { get; set; }
    
    /// <summary>
    /// Current block height of the local chain of the daemon.
    /// </summary>
    [JsonPropertyName("heightWithoutBootstrap")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? HeightWithoutBootstrap { get; set; }
    
    /// <summary>
    /// Indicates if daemon is running on <i>mainnet</i>, <i>testnet</i> or <i>stagenet</i>.
    /// </summary>
    [JsonPropertyName("networkType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MoneroNetworkType? NetworkType { get; set; }
    
    /// <summary>
    /// Indicates if the node is offline.
    /// </summary>
    [JsonPropertyName("isOffline")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsOffline { get; set; }
    
    /// <summary>
    /// Number of peers connected and pulling data from the node.
    /// </summary>
    [JsonPropertyName("numIncomingConnections")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumIncomingConnections { get; set; }
    
    /// <summary>
    /// Number of peers the node is connected and pulling data from.
    /// </summary>
    [JsonPropertyName("numOutgoingConnections")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumOutgoingConnections { get; set; }
    
    /// <summary>
    /// Number of RPC clients connected to the node (including current RPC request).
    /// </summary>
    [JsonPropertyName("numRpcConnections")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumRpcConnections { get; set; }
    
    /// <summary>
    /// Start time of the node, as UNIX timestamp.
    /// </summary>
    [JsonPropertyName("startTimestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? StartTimestamp { get; set; }
    
    /// <summary>
    /// Current time approximated from chain data, as Unix time.
    /// </summary>
    [JsonPropertyName("adjustedTimestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? AdjustedTimestamp { get; set; }
    
    /// <summary>
    /// Current target for next proof of work.
    /// </summary>
    [JsonPropertyName("target")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Target { get; set; }
    
    /// <summary>
    /// The height of the next block to be added to the blockchain.
    /// </summary>
    [JsonPropertyName("targetHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? TargetHeight { get; set; }
    
    /// <summary>
    /// Total number of non-coinbase transactions added to the blockchain.
    /// </summary>
    [JsonPropertyName("numTxs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumTxs { get; set; }
    
    /// <summary>
    /// Current number of transactions still not included in a block.
    /// </summary>
    [JsonPropertyName("numTxsPool")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumTxsPool { get; set; }
    
    /// <summary>
    /// Indicates if a bootstrap node has ever been used since the daemon started.
    /// </summary>
    [JsonPropertyName("wasBootstrapEverUsed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? WasBootstrapEverUsed { get; set; }
    
    /// <summary>
    /// The size of the blockchain database, in bytes.
    /// </summary>
    [JsonPropertyName("databaseSize")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? DatabaseSize { get; set; }
    
    /// <summary>
    /// Indicates if a newer Monero software version is available.
    /// </summary>
    [JsonPropertyName("updateAvailable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UpdateAvailable { get; set; }
    
    /// <summary>
    /// Indicates if the node is currently busy in syncing local blockchain.
    /// </summary>
    [JsonPropertyName("isBusySyncing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsBusySyncing { get; set; }
    
    /// <summary>
    /// Indicates if the node is synchronized.
    /// </summary>
    [JsonPropertyName("isSynchronized")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsSynchronized { get; set; }

    /// <summary>
    /// Indicates if the node restricts its RPC, so a client can't use the calls that are for the owner of the node.
    /// </summary>
    [JsonPropertyName("isRestricted")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsRestricted { get; set; }

    /// <summary>
    /// Indicates if the node runs in regtest mode.
    /// </summary>
    [JsonPropertyName("isRegtest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsRegtest { get; set; }
}
