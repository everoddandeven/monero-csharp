using System.Runtime.InteropServices;

namespace Monero.Native;

/// <summary>A string or buffer allocated by monero-c. It is released with <c>monero_utils_free()</c>.</summary>
internal readonly unsafe ref struct NativeString
{
    private readonly IntPtr _ptr;

    public NativeString(IntPtr ptr)
    {
        _ptr = ptr;
    }

    /// <summary>Whether the call had no result, for example a block that the daemon doesn't have.</summary>
    public bool IsNull => _ptr == IntPtr.Zero;

    /// <summary>The text as UTF-8, read from the native memory.</summary>
    public ReadOnlySpan<byte> Utf8 => _ptr == IntPtr.Zero ? default : MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)_ptr);

    public override string ToString() => _ptr == IntPtr.Zero ? "" : Marshal.PtrToStringUTF8(_ptr)!;

    public void Dispose()
    {
        if (_ptr != IntPtr.Zero) NativeMethods.monero_utils_free(_ptr);
    }
}
