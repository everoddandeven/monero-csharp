using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Monero.Daemon.Model;

namespace Monero.Native;

/// <summary>The native listener of a daemon. Callbacks only queue the header JSON; a task parses it and calls <c>onHeader</c> on the thread pool.</summary>
internal sealed class DaemonEvents : IDisposable
{
    private readonly Channel<string> _events = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Action<MoneroBlockHeader> _onHeader;
    private GCHandle _self;

    public unsafe DaemonEvents(Action<MoneroBlockHeader> onHeader)
    {
        _onHeader = onHeader;
        _self = GCHandle.Alloc(this);

        try
        {
            var callbacks = new DaemonListenerCallbacks
            {
                UserData = (void*)GCHandle.ToIntPtr(_self),
                OnBlockHeader = &BlockHeaderCallback,
            };

            Check.Ok(NativeMethods.monero_daemon_listener_create(in callbacks, out var handle));
            Handle = handle;
        }
        catch
        {
            _self.Free();
            throw;
        }

        _ = Task.Run(DispatchAsync);
    }

    /// <summary>The native listener, to add to the daemon.</summary>
    public DaemonListenerHandle Handle { get; }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void BlockHeaderCallback(void* userData, byte* json)
    {
        // an exception that leaves a callback ends the process
        try
        {
            var events = (DaemonEvents)GCHandle.FromIntPtr((IntPtr)userData).Target!;
            events._events.Writer.TryWrite(Marshal.PtrToStringUTF8((IntPtr)json)!);
        }
        catch
        {
            // nothing can be done on a thread of monero-cpp
        }
    }

    private async Task DispatchAsync()
    {
        await foreach (var json in _events.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                _onHeader(Common.MoneroJson.Deserialize<MoneroBlockHeader>(json));
            }
            catch
            {
                // a listener that throws doesn't stop the others, or the next headers
            }
        }
    }

    /// <summary>Free the listener. It is done after the listener was removed from the daemon.</summary>
    public void Dispose()
    {
        Handle.Dispose();
        _events.Writer.TryComplete();
        if (_self.IsAllocated) _self.Free();
    }
}
