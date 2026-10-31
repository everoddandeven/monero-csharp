using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Monero.Common;
using Monero.Wallet.Model;

namespace Monero.Native;

/// <summary>
/// The native listener of a wallet. The callbacks run one at a time on a thread of monero-c that the listener owns, with no lock of the wallet
/// held. They only queue what they get, so the code of the program, which can be slow, never holds that thread; a task parses the output JSON
/// and calls <c>onEvent</c> on the thread pool, one event after the other.
/// </summary>
internal sealed class WalletEvents : IDisposable
{
    private readonly Channel<Event> _events = Channel.CreateUnbounded<Event>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Action<Action<MoneroWalletListener>> _onEvent;
    private readonly Task _dispatcher;
    private GCHandle _self;

    public unsafe WalletEvents(Action<Action<MoneroWalletListener>> onEvent)
    {
        _onEvent = onEvent;
        _self = GCHandle.Alloc(this);

        try
        {
            var callbacks = new WalletListenerCallbacks
            {
                StructSize = (uint)sizeof(WalletListenerCallbacks),
                UserData = (void*)GCHandle.ToIntPtr(_self),
                OnSyncProgress = &SyncProgressCallback,
                OnNewBlock = &NewBlockCallback,
                OnBalancesChanged = &BalancesChangedCallback,
                OnOutputReceived = &OutputReceivedCallback,
                OnOutputSpent = &OutputSpentCallback,
            };

            Check.Ok(NativeMethods.monero_wallet_listener_create(in callbacks, out var handle));
            Handle = handle;
        }
        catch
        {
            _self.Free();
            throw;
        }

        _dispatcher = Task.Run(DispatchAsync);
    }

    /// <summary>The native listener, to add to the wallet.</summary>
    public WalletListenerHandle Handle { get; }

    private enum Kind
    {
        SyncProgress,
        NewBlock,
        BalancesChanged,
        OutputReceived,
        OutputSpent,
    }

    private readonly record struct Event(Kind Kind, ulong A, ulong B, ulong C, double Percent, string? Text);

    private static unsafe WalletEvents Of(void* userData) => (WalletEvents)GCHandle.FromIntPtr((IntPtr)userData).Target!;

    // an exception that leaves a callback ends the process, and nothing can be done on a thread of monero-c
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void SyncProgressCallback(void* userData, ulong height, ulong startHeight, ulong endHeight, double percentDone, byte* message)
    {
        try
        {
            Of(userData)._events.Writer.TryWrite(new Event(Kind.SyncProgress, height, startHeight, endHeight, percentDone, Marshal.PtrToStringUTF8((IntPtr)message)));
        }
        catch
        {
            // see above
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void NewBlockCallback(void* userData, ulong height)
    {
        try
        {
            Of(userData)._events.Writer.TryWrite(new Event(Kind.NewBlock, height, 0, 0, 0, null));
        }
        catch
        {
            // see above
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void BalancesChangedCallback(void* userData, ulong newBalance, ulong newUnlockedBalance)
    {
        try
        {
            Of(userData)._events.Writer.TryWrite(new Event(Kind.BalancesChanged, newBalance, newUnlockedBalance, 0, 0, null));
        }
        catch
        {
            // see above
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OutputReceivedCallback(void* userData, byte* json)
    {
        try
        {
            Of(userData)._events.Writer.TryWrite(new Event(Kind.OutputReceived, 0, 0, 0, 0, Marshal.PtrToStringUTF8((IntPtr)json)));
        }
        catch
        {
            // see above
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OutputSpentCallback(void* userData, byte* json)
    {
        try
        {
            Of(userData)._events.Writer.TryWrite(new Event(Kind.OutputSpent, 0, 0, 0, 0, Marshal.PtrToStringUTF8((IntPtr)json)));
        }
        catch
        {
            // see above
        }
    }

    private async Task DispatchAsync()
    {
        await foreach (var e in _events.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                _onEvent(ToCall(e));
            }
            catch
            {
                // a listener that throws doesn't stop the others, or the next events
            }
        }
    }

    private static Action<MoneroWalletListener> ToCall(Event e)
    {
        switch (e.Kind)
        {
            case Kind.SyncProgress:
                return listener => listener.OnSyncProgress(e.A, e.B, e.C, e.Percent, e.Text ?? "");
            case Kind.NewBlock:
                return listener => listener.OnNewBlock(e.A);
            case Kind.BalancesChanged:
                return listener => listener.OnBalancesChanged(e.A, e.B);
            case Kind.OutputReceived:
            {
                var output = MoneroJson.Deserialize<MoneroWalletBlocksResult>(e.Text!).GetOutputs().FirstOrDefault();
                return listener =>
                {
                    if (output != null) listener.OnOutputReceived(output);
                };
            }
            default:
            {
                var output = MoneroJson.Deserialize<MoneroWalletBlocksResult>(e.Text!).GetInputs().FirstOrDefault();
                return listener =>
                {
                    if (output != null) listener.OnOutputSpent(output);
                };
            }
        }
    }

    /// <summary>Wait until the events that callbacks queued so far were dispatched. It is done after the listener was removed from the wallet.</summary>
    public async Task CompleteAsync()
    {
        _events.Writer.TryComplete();
        await _dispatcher.ConfigureAwait(false);
    }

    /// <summary>Free the listener. It is done after the listener was removed from the wallet.</summary>
    public void Dispose()
    {
        Handle.Dispose();
        _events.Writer.TryComplete();
        if (_self.IsAllocated) _self.Free();
    }
}
