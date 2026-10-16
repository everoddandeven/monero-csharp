using System.Runtime.InteropServices;
using System.Text.Json;
using Monero.Common;

namespace Monero.Native;

/// <summary>Checks the results of the functions of monero-c and reads what they return. The last error is per thread: call and check run in the same non-async method.</summary>
internal static class Check
{
    private static readonly object AbiLock = new();
    private static bool _abiChecked;

    /// <summary>The version of the C ABI that this code was written for. While the major version is 0, a different minor version can break the callers.</summary>
    public const uint AbiMajor = 0;

    /// <inheritdoc cref="AbiMajor"/>
    public const uint AbiMinor = 1;

    /// <summary>Throws if the result of a call is an error, with the message that monero-c recorded for this thread.</summary>
    public static void Ok(int result)
    {
        if (result == 0) return;

        var message = Marshal.PtrToStringUTF8(NativeMethods.monero_last_error());
        throw new MoneroError(string.IsNullOrEmpty(message) ? "monero-c error" : message);
    }

    /// <summary>Compares the version of the ABI of the loaded library with the one that this code was written for. It is done once.</summary>
    public static void EnsureLibrary()
    {
        if (_abiChecked) return;

        lock (AbiLock)
        {
            if (_abiChecked) return;

            try
            {
                Ok(NativeMethods.monero_utils_get_abi_version(out var major, out var minor, out var patch));

                if (major != AbiMajor || minor != AbiMinor)
                {
                    throw new MoneroError($"The version of the ABI of monero-c is {major}.{minor}.{patch}, and {AbiMajor}.{AbiMinor}.x is needed");
                }
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
                throw new MoneroError("The native library monero_c is not found, or it is too old. Reference the package Monero.Native", e);
            }

            _abiChecked = true;
        }
    }

    /// <summary>Reads the JSON that a call returned in a native string as a model, and frees the native string.</summary>
    public static T Json<T>(int result, IntPtr ptr)
    {
        Ok(result);

        var json = new NativeString(ptr);
        try
        {
            return MoneroJson.Deserialize<T>(json.Utf8);
        }
        finally
        {
            json.Dispose();
        }
    }

    /// <summary>Reads the JSON that a call returned in a native string as a model, when the call can have no result.</summary>
    public static T? JsonOrNull<T>(int result, IntPtr ptr) where T : class
    {
        Ok(result);

        var json = new NativeString(ptr);
        try
        {
            return json.IsNull ? null : MoneroJson.Deserialize<T>(json.Utf8);
        }
        finally
        {
            json.Dispose();
        }
    }

    /// <summary>Reads the text that a call returned in a native string, and frees the native string.</summary>
    public static string? Text(int result, IntPtr ptr)
    {
        Ok(result);

        var text = new NativeString(ptr);
        try
        {
            return text.IsNull ? null : text.ToString();
        }
        finally
        {
            text.Dispose();
        }
    }

    /// <summary>Reads a JSON array of strings that a call returned.</summary>
    public static List<string> Strings(int result, IntPtr ptr) => Json<List<string>>(result, ptr);

    /// <summary>Reads a native array of values that a call returned, and frees it.</summary>
    public static unsafe List<T> Array<T>(int result, IntPtr ptr, nuint count) where T : unmanaged
    {
        Ok(result);

        var list = new List<T>((int)count);
        try
        {
            var items = (T*)ptr;
            for (nuint i = 0; i < count; i++) list.Add(items[i]);
        }
        finally
        {
            if (ptr != IntPtr.Zero) NativeMethods.monero_utils_free(ptr);
        }

        return list;
    }

    /// <summary>The JSON of a model for an argument of monero-c.</summary>
    public static string ToJson<T>(T model) => MoneroJson.Serialize(model);

    /// <summary>Whether an exception is a failure to read a model.</summary>
    public static bool IsJsonError(Exception e) => e is JsonException;
}
