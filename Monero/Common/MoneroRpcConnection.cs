using System.Text.Json.Serialization;

namespace Monero.Common;

/// <summary>
/// Models a connection to a daemon or to a wallet RPC server.
/// </summary>
public class MoneroRpcConnection
{
    /// <summary>
    /// Initialize a new RPC connection.
    /// </summary>
    public MoneroRpcConnection()
    {
        // nothing to build
    }

    /// <summary>
    /// Initialize a new RPC connection.
    /// </summary>
    /// <param name="uri">the URI of the server.</param>
    /// <param name="username">the username to authenticate with, or <c>null</c> for none.</param>
    /// <param name="password">the password to authenticate with, or <c>null</c> for none.</param>
    public MoneroRpcConnection(string uri, string? username = null, string? password = null)
    {
        Uri = uri;
        Username = username;
        Password = password;
    }

    /// <summary>
    /// Construct this connection as a copy of the given connection.
    /// </summary>
    /// <param name="connection">the connection to copy.</param>
    public MoneroRpcConnection(MoneroRpcConnection connection)
    {
        Uri = connection.Uri;
        Username = connection.Username;
        Password = connection.Password;
        ProxyUri = connection.ProxyUri;
        ZmqUri = connection.ZmqUri;
        Priority = connection.Priority;
        TimeoutMs = connection.TimeoutMs;
        ResponseTime = connection.ResponseTime;
        SslVerify = connection.SslVerify;
        IsOnline = connection.IsOnline;
        IsAuthenticated = connection.IsAuthenticated;
    }

    /// <summary>
    /// Copy this connection.
    /// </summary>
    /// <returns>a copy of the connection.</returns>
    public virtual MoneroRpcConnection Copy()
    {
        return new MoneroRpcConnection(this);
    }

    /// <summary>
    /// URI of the server.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Uri { get; set; }

    /// <summary>
    /// Username to authenticate with.
    /// </summary>
    [JsonPropertyName("username")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Username { get; set; }

    /// <summary>
    /// Password to authenticate with.
    /// </summary>
    [JsonPropertyName("password")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Password { get; set; }

    /// <summary>
    /// URI of a proxy to connect through, for example the one of Tor.
    /// </summary>
    [JsonPropertyName("proxyUri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProxyUri { get; set; }

    /// <summary>
    /// URI of the ZMQ server.
    /// </summary>
    [JsonPropertyName("zmqUri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ZmqUri { get; set; }

    /// <summary>
    /// Priority of the connection compared to the others, where 1 is the highest, then 2, and so on, and 0 is the lowest.
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Priority { get; set; }

    /// <summary>
    /// Time to wait for a response of the server, in milliseconds.
    /// </summary>
    [JsonPropertyName("timeoutMs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? TimeoutMs { get; set; }

    /// <summary>
    /// Time that the server took to respond to the last check of the connection, in milliseconds.
    /// </summary>
    [JsonPropertyName("responseTime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? ResponseTime { get; set; }

    /// <summary>
    /// Whether the certificate and the host name of an HTTPS server are verified. It is on when it is not set.
    /// </summary>
    [JsonPropertyName("sslVerify")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SslVerify { get; set; }

    /// <summary>
    /// Whether the server answered the last check of the connection.
    /// </summary>
    [JsonPropertyName("isOnline")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsOnline { get; set; }

    /// <summary>
    /// Whether the server accepted the credentials at the last check of the connection.
    /// </summary>
    [JsonPropertyName("isAuthenticated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsAuthenticated { get; set; }
}
