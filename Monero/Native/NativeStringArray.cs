using System.Runtime.InteropServices;

namespace Monero.Native;

/// <summary>A native <c>const char* const*</c> for an argument of monero-c. monero-c only reads it during the call.</summary>
internal sealed unsafe class NativeStringArray : IDisposable
{
    private readonly IntPtr* _items;
    private readonly int _count;
    private bool _disposed;

    public NativeStringArray(IReadOnlyList<string> strings)
    {
        ArgumentNullException.ThrowIfNull(strings);

        _count = strings.Count;
        _items = (IntPtr*)NativeMemory.AllocZeroed((nuint)Math.Max(_count, 1), (nuint)sizeof(IntPtr));

        try
        {
            for (var i = 0; i < _count; i++)
            {
                _items[i] = Marshal.StringToCoTaskMemUTF8(strings[i] ?? throw new ArgumentException("A string of the array is null", nameof(strings)));
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>The pointer to pass as the array.</summary>
    public IntPtr Pointer => (IntPtr)_items;

    /// <summary>The number of strings.</summary>
    public nuint Count => (nuint)_count;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        for (var i = 0; i < _count; i++)
        {
            if (_items[i] != IntPtr.Zero) Marshal.FreeCoTaskMem(_items[i]);
            _items[i] = IntPtr.Zero;
        }

        NativeMemory.Free(_items);
    }
}
