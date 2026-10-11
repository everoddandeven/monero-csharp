namespace Monero.Daemon.Model;

/// <summary>
/// Models an entry of the backlog of the transaction pool. monero-cpp has no field in it, and monero-c writes each entry as an empty JSON
/// object, so the number of the entries is all that it tells.
/// </summary>
public class MoneroTxBacklogEntry
{
    
}
