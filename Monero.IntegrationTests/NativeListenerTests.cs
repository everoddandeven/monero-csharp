using Monero.Common;
using Monero.Daemon;
using Monero.Daemon.Model;
using Monero.Wallet;
using Monero.Wallet.Model;

namespace Monero.IntegrationTests;

/// <summary>
/// Adding a listener creates a native listener of monero-c, and monero-c refuses a struct of callbacks that doesn't say its size. The tests
/// need the native library and no node, since monero-c reaches the server when a call needs it.
/// </summary>
[TestFixture]
public class NativeListenerTests
{
    private const string Unreachable = "http://127.0.0.1:1";

    [Test]
    public void AddingADaemonListenerCreatesTheNativeListener()
    {
        using var daemon = new MoneroDaemonRpc(Unreachable);
        var listener = new MoneroDaemonListener();

        Assert.DoesNotThrow(() => daemon.AddListener(listener));
        Assert.That(daemon.GetListeners(), Has.Count.EqualTo(1));

        Assert.DoesNotThrow(() => daemon.RemoveListener(listener));
        Assert.That(daemon.GetListeners(), Is.Empty);
    }

    [Test]
    public void AddingAWalletListenerCreatesTheNativeListener()
    {
        using var wallet = new MoneroWalletRpc(Unreachable);
        var listener = new MoneroWalletListener();

        Assert.DoesNotThrow(() => wallet.AddListener(listener));
        Assert.That(wallet.GetListeners(), Has.Count.EqualTo(1));

        Assert.DoesNotThrow(() => wallet.RemoveListener(listener));
        Assert.That(wallet.GetListeners(), Is.Empty);
    }

    // the kind of the failure is the one that monero-c recorded for the call: a range that is too long is checked before the node is asked,
    // and a node that does not answer is a network failure, which monero-cpp reports with its general exception
    [Test]
    public async Task AFailureHasTheKindThatMoneroCRecorded()
    {
        using var daemon = new MoneroDaemonRpc(Unreachable, timeoutMs: 500);

        var invalid = Assert.ThrowsAsync<MoneroError>(async () => await daemon.GetBlocksByRangeAsync(0, 200000));
        Assert.That(invalid!.Code, Is.EqualTo(MoneroErrorCode.InvalidArgument));
        Assert.That(invalid.Message, Does.Contain("range is longer"));

        var network = Assert.ThrowsAsync<MoneroError>(async () => await daemon.GetHeightAsync());
        Assert.That(network!.Code, Is.EqualTo(MoneroErrorCode.Generic));
        Assert.That(network.Message, Is.Not.Empty);

        await Task.CompletedTask;
    }
}
