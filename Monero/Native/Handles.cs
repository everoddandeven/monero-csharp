using System.Runtime.InteropServices;

namespace Monero.Native;

/// <summary>A <c>monero_daemon*</c>. It is freed once, when it is disposed or finalized, and never while a call uses it.</summary>
internal sealed class DaemonHandle : SafeHandle
{
    public DaemonHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        NativeMethods.monero_daemon_free(handle);
        return true;
    }
}

/// <summary>A <c>monero_daemon_listener*</c>.</summary>
internal sealed class DaemonListenerHandle : SafeHandle
{
    public DaemonListenerHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        NativeMethods.monero_daemon_listener_free(handle);
        return true;
    }
}

/// <summary>A <c>monero_rpc_connection*</c>.</summary>
internal sealed class RpcConnectionHandle : SafeHandle
{
    public RpcConnectionHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        NativeMethods.monero_rpc_connection_free(handle);
        return true;
    }
}
