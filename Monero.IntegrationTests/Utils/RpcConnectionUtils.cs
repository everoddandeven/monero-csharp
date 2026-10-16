namespace Monero.IntegrationTests.Utils;

/// <summary>
/// Test utils for RPC connections.
/// </summary>
public static class RpcConnectionUtils
{
    /// <summary>
    /// Default network error message.
    /// </summary>
    public const string NetworkErrorMsg = "Network error: no response from offline_server_uri/json_rpc (timeout: 180000ms)";
}
