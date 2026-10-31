using System.Runtime.InteropServices;

namespace Monero.Native;

/// <summary>
/// A string or buffer allocated by monero-c. It is released with <c>monero_utils_free()</c>, or with <c>monero_utils_free_secret()</c> when it
/// holds a seed or a private key, which overwrites it first.
/// </summary>
internal readonly unsafe ref struct NativeString
{
    private readonly IntPtr _ptr;
    private readonly bool _secret;

    public NativeString(IntPtr ptr, bool secret = false)
    {
        _ptr = ptr;
        _secret = secret;
    }

    /// <summary>Whether the call had no result, for example a block that the daemon doesn't have.</summary>
    public bool IsNull => _ptr == IntPtr.Zero;

    /// <summary>The text as UTF-8, read from the native memory.</summary>
    public ReadOnlySpan<byte> Utf8 => _ptr == IntPtr.Zero ? default : MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)_ptr);

    public override string ToString() => _ptr == IntPtr.Zero ? "" : Marshal.PtrToStringUTF8(_ptr)!;

    public void Dispose()
    {
        if (_ptr == IntPtr.Zero) return;

        if (_secret) NativeMethods.monero_utils_free_secret(_ptr);
        else NativeMethods.monero_utils_free(_ptr);
    }
}
