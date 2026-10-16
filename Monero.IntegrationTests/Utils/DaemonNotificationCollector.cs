using Monero.Daemon;
using Monero.Daemon.Model;

namespace Monero.IntegrationTests.Utils;

/// <summary>
/// Collects block headers from a daemon.
/// </summary>
public class DaemonNotificationCollector : MoneroDaemonListener
{
    private readonly object _lock = new();
    private readonly HashSet<string> _blockHashes = [];
    private readonly List<MoneroBlockHeader> _blockHeaders = [];
    private readonly List<Exception> _errors = [];

    /// <summary>
    /// Initialize a new daemon notification collector.
    /// </summary>
    /// <param name="daemon">daemon instance to collect block headers from.</param>
    /// <param name="autoRemove">if <c>true</c>, the listener removes itself from the daemon after the first block notification.</param>
    public DaemonNotificationCollector(MoneroDaemon daemon, bool autoRemove = false)
    {
        Daemon = daemon;
        AutoRemove = autoRemove;
    }

    /// <summary>Daemon instance.</summary>
    public MoneroDaemon Daemon { get; }

    /// <summary>Specifies if the listener should remove itself after a block notification.</summary>
    public bool AutoRemove { get; }

    /// <summary>Last block height collected by the listener.</summary>
    public ulong? LastBlockHeight { get; private set; }

    /// <summary>Number of block headers collected so far.</summary>
    public int NumBlockHeaders
    {
        get
        {
            lock (_lock) return _blockHeaders.Count;
        }
    }

    /// <summary>Number of block hashes collected so far.</summary>
    public int NumBlockHashes
    {
        get
        {
            lock (_lock) return _blockHashes.Count;
        }
    }

    /// <summary>Errors while collecting notifications.</summary>
    public List<Exception> Errors
    {
        get
        {
            lock (_lock) return [.. _errors];
        }
    }

    /// <inheritdoc />
    public override void OnBlockHeader(MoneroBlockHeader header)
    {
        base.OnBlockHeader(header);

        try
        {
            lock (_lock)
            {
                Assert.That(header.Hash, Is.Not.Null);
                Assert.That(_blockHashes.Add(header.Hash!), Is.True, $"Duplicate block header found: {header.Hash}");
                _blockHeaders.Add(header);

                Assert.That(header.Height, Is.Not.Null);
                if (LastBlockHeight != null && header.Height != LastBlockHeight + 1)
                {
                    TestContext.Progress.WriteLine($"Daemon listener missed blocks range {LastBlockHeight + 1}-{header.Height - 1}");
                }

                LastBlockHeight = header.Height;
            }

            if (AutoRemove) Daemon.RemoveListener(this);
        }
        catch (Exception e)
        {
            lock (_lock) _errors.Add(e);
        }
    }
}
