namespace Monero.Daemon.Model;

/// <summary>
/// Receives notifications as a daemon is updated.
/// </summary>
public class MoneroDaemonListener
{
    /// <summary>
    /// Last notified block header.
    /// </summary>
    public MoneroBlockHeader? LastHeader { get; private set; }

    /// <summary>
    /// Called when a new block is added to the chain.
    /// </summary>
    /// <param name="header">Is the header of the block added to the chain.</param>
    public virtual void OnBlockHeader(MoneroBlockHeader header)
    {
        LastHeader = header;
    }
}