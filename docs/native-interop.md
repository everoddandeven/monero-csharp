# Native interop and async design

How the wallet and daemon classes of monero-csharp call monero-c, and how they expose async methods without leaking memory or wasting threads. It is for the people who write the native layer and the classes on top of it: `MoneroWalletFull`, `MoneroWalletRpc`, `MoneroDaemonRpc`, and later `MoneroWalletLws`.

The code here was compiled with the .NET 8 SDK and run against a build of monero-c with the ABI 0.1.0, on an in-memory wallet and on a regtest node. The repository targets .NET 10, and nothing used here is newer than .NET 8. What was checked is listed in [What was checked](#what-was-checked), and a code block that was not run is marked as a sketch.

## What to decide first

- **The classes are async, for all three implementations.** Full and Rpc call blocking native code, so an async method of theirs moves the call to another thread. It doesn't save the thread, it frees the caller. Lws talks to a server and can be async all the way down. An async interface fits the three, and a sync one would make Lws block for nothing.
- **Nothing changes in monero-c.** Its network code is synchronous inside wallet2, so an async C function would only move the thread from .NET to C++, and it would double the ABI.
- **One thread for each handle, not for each call.** A wallet handle doesn't allow two calls at once, so the calls of a wallet are in a queue anyway, and a dedicated thread with a queue is the honest form of it.

## The ABI in the rules that matter

monero-c is documented in `monero_c.h` and in the [package README](https://github.com/libmonero/monero-c/blob/main/packaging/dotnet-core/README.md). These are the parts that the native layer depends on.

- Most functions return a `monero_result`: 0 is `MONERO_OK`, -1 is `MONERO_ERROR`. The data comes back in `out_*` parameters. On an error the pointer outputs are NULL.
- `monero_last_error()` has the message of the last failed call **on the calling thread**. A good call doesn't clear it. The pointer belongs to monero-c: copy the text, and never free it.
- A string or a buffer in an `out_*` parameter is allocated by monero-c, and it is released with `monero_utils_free()`. Nothing else frees it: not `Marshal.FreeHGlobal()`, not `Marshal.FreeCoTaskMem()`.
- A call can succeed and leave an output NULL, when there is no result (a block that the daemon doesn't have). Check the pointer.
- Strings are UTF-8. A C `bool` is one byte (`UnmanagedType.U1`). A `size_t` is a `nuint`.
- An optional parameter is a pointer, and NULL means none.
- A handle (`monero_wallet*`, `monero_daemon*`, `monero_rpc_connection*`, a listener) is released with its own `*_free()`. To keep a wallet that is a file, call `monero_wallet_close(wallet, true)` before.
- The library is `monero_c`, from the package `Monero.Native`: `LibraryImport("monero_c")`. The declarations that exist in the repository today use `"monero-c"`, and they return a `string` or read an error with `monero_api_error()`. That is the shape of another API. In monero-c a function like `monero_utils_get_integrated_address` returns a `monero_result` and writes to `char** out_json`. Rewrite those declarations, don't copy their style.
- Compare `monero_utils_get_abi_version()` with the version that the code was written for, when the library loads. While the major version is 0, a different minor version can break callers.

## The layers

```
MoneroWalletFull / MoneroWalletRpc / MoneroDaemonRpc     public, async, models
        │
 NativeWorker (one thread and a queue for each handle)   internal
        │
 SafeHandles, NativeString, Check, events                 internal helpers
        │
 Native (the LibraryImport declarations)                  internal, no logic
```

A layer only calls the one below. The public classes never hold an `IntPtr`, and `Native` has no logic.

## Memory, one case at a time

### The string that a function returns

This is the leak that is easy to write. Don't declare the output as `string` or return a `string` from a `LibraryImport`: the marshaller frees it with `FreeCoTaskMem()`, which is not the allocator of the library on Windows, and it leaves the doubt on the other systems. Declare it as `IntPtr`, and release it in a `finally`:

```csharp
[LibraryImport("monero_c")]
public static partial int monero_wallet_get_primary_address(WalletHandle wallet, out IntPtr address);

// the text of an out parameter: it is read from the native memory, and the memory is freed when the scope ends
internal readonly unsafe ref struct NativeString
{
    private readonly IntPtr _ptr;
    public NativeString(IntPtr ptr) => _ptr = ptr;
    public bool IsNull => _ptr == IntPtr.Zero;
    public ReadOnlySpan<byte> Utf8 => _ptr == IntPtr.Zero ? default : MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)_ptr);
    public override string ToString() => _ptr == IntPtr.Zero ? "" : Marshal.PtrToStringUTF8(_ptr)!;
    public void Dispose() { if (_ptr != IntPtr.Zero) Native.monero_utils_free(_ptr); }
}

static string Address(WalletHandle wallet)
{
    Check.Ok(Native.monero_wallet_get_primary_address(wallet, out var ptr));
    var text = new NativeString(ptr);
    try { return text.ToString(); }
    finally { text.Dispose(); }
}
```

- The `try`/`finally` begins right after the call that wrote the pointer, with nothing that can throw in between. `Check.Ok()` comes before it, because on an error the pointer is NULL and there is nothing to free.
- A byte buffer (`uint8_t**` with a `size_t*`) is freed in the same way, with `monero_utils_free()`.
- **A `ref struct` can't be a local of an `async` method** (the compiler says CS4012). Do the native work in a method or a lambda that is not `async`, as above. That is also where the worker runs it.

### A big JSON without a string in between

A sketch: `BlocksResult` and `MoneroJson` are the models and the source-generated context that are still to be written.

The results of `get_txs()` or `get_blocks_by_range()` can be megabytes. Parse the UTF-8 of the native memory, and skip the copy to a UTF-16 `string`:

```csharp
static BlocksResult Txs(WalletHandle wallet, string? queryJson)
{
    Check.Ok(Native.monero_wallet_get_txs(wallet, queryJson, out var ptr));
    var json = new NativeString(ptr);
    try { return JsonSerializer.Deserialize(json.Utf8, MoneroJson.Default.BlocksResult)!; }   // a source-generated context
    finally { json.Dispose(); }
}
```

The memory is freed only after the deserializer has returned, so nothing that the models keep points into it. `System.Text.Json` copies what it keeps.

### What goes in

- A `string` parameter with `StringMarshalling.Utf8` is fine for inputs: the generated code allocates the native copy for the call and frees it after. The leak is only in the outputs.
- A JSON argument is `JsonSerializer.Serialize(config, MoneroJson.Default.X)` as a string. Skip the nulls (`JsonIgnoreCondition.WhenWritingNull`): monero-cpp reads a JSON `null` as the text "null", and monero-c drops the nulls only in the config of `monero_wallet_create()`.
- An array of strings (`const char* const*`) needs a native array of native strings. Build it in a helper that is `IDisposable`: `NativeMemory.Alloc()` for the array and `Marshal.StringToCoTaskMemUTF8()` for each string, and free every pointer in `Dispose()` (`NativeMemory.Free()` and `Marshal.FreeCoTaskMem()`). This memory is yours, not monero-c's, which only reads it during the call.
- An optional number is a `ref ulong`, and `ref Unsafe.NullRef<ulong>()` is the NULL.
- Limits that make a call fail early: a JSON argument can't nest deeper than 64 levels, and a mnemonic can't be longer than 4096 bytes.

### The handles

A handle is a `SafeHandle`. It frees the native object once, when it is disposed or finalized, and never while a call uses it.

```csharp
internal sealed class WalletHandle : SafeHandle
{
    public WalletHandle() : base(IntPtr.Zero, ownsHandle: true) { }
    public override bool IsInvalid => handle == IntPtr.Zero;
    protected override bool ReleaseHandle() { Native.monero_wallet_free(handle); return true; }
}

[LibraryImport("monero_c", StringMarshalling = StringMarshalling.Utf8)]
public static partial int monero_wallet_create(string configJson, out WalletHandle wallet);
```

- Take the handle as an `out WalletHandle` in the declaration, and not as an `IntPtr`: the pointer is wrapped in the same step that it is received, with no gap where an exception leaks it.
- A call that uses the handle holds a reference for its whole length. `Dispose()` of the handle then waits for the call to end before it frees the native wallet, even if another thread calls it in the middle:

```csharp
internal static class HandleUse
{
    public static T With<T>(SafeHandle handle, Func<T> call)
    {
        bool added = false;
        handle.DangerousAddRef(ref added);
        try { return call(); }
        finally { if (added) handle.DangerousRelease(); }
    }
}
```

- `ReleaseHandle()` does not save a wallet. If the wallet is a file, the class closes it with `monero_wallet_close(wallet, true)` first. A wallet that is only finalized is freed with no save.

### The listener

A listener has a native handle and a `GCHandle` for the object that receives its events, and the two are released in an order. The callbacks are static methods with `[UnmanagedCallersOnly]`, and the `GCHandle` goes to them as `user_data`. A delegate and its lifetime are not needed.

```csharp
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WalletListenerCallbacks
{
    public void* UserData;
    public delegate* unmanaged[Cdecl]<void*, ulong, ulong, ulong, double, byte*, void> OnSyncProgress;
    public delegate* unmanaged[Cdecl]<void*, ulong, void> OnNewBlock;
    public delegate* unmanaged[Cdecl]<void*, ulong, ulong, void> OnBalancesChanged;
    public delegate* unmanaged[Cdecl]<void*, byte*, void> OnOutputReceived;
    public delegate* unmanaged[Cdecl]<void*, byte*, void> OnOutputSpent;
}
```

- The order of the fields is the one of `monero_wallet_listener_callbacks` in the header, and a field that is NULL is a notification that you skip.
- A callback runs on a thread of monero-cpp, **while the sync holds the lock of the wallet**. It does the least possible: it writes the event to a `Channel<T>` and returns. If it called a method of the same wallet that needs the lock (`get_balance`, `get_txs`, `get_outputs`, `get_accounts`), the call would never return.
- An exception that leaves a callback ends the process, so the body of every callback is in a `try`/`catch`.
- One native listener for each wallet, for its whole life: you add it once, you never remove it, and the listeners of the users are a list in C#. This is the rule of the README of monero-c, and it removes the races of adding and removing native listeners.

The full class is in [Events](#events).

### Cancellation and exceptions

The call frees what it allocates **inside the delegate that the worker runs**, and never relies on the caller to consume the result. A caller that cancels, or that throws, then leaves nothing behind: the call that was already running finishes and frees its text, and the result is dropped.

### The checklist

1. Every `out IntPtr` of a string or a buffer has a `try`/`finally` with `monero_utils_free()`.
2. No `string` or `char*` returned by `LibraryImport`. Handles are `out SafeHandle`.
3. The pointer of `monero_last_error()` is copied, and it is never freed.
4. The native work is in a method that is not `async`.
5. A call that uses a handle holds `DangerousAddRef()`.
6. A cancelled task still frees its text.
7. The order of the close: the shutdown request, the close, the listener, the wallet. See [Closing](#closing).
8. A test measures the native heap, see [Testing for leaks](#testing-for-leaks).

## Errors

```csharp
internal static class Check
{
    // reads the message right after the call that failed, on the same thread
    public static void Ok(int result)
    {
        if (result == 0) return;
        throw new MoneroError(Marshal.PtrToStringUTF8(Native.monero_last_error()) ?? "monero-c error");
    }
}
```

The error is per thread, and a continuation of an `await` can run on another one, so the call and `Check.Ok()` are in the same non-async delegate. On the worker thread of a handle this is natural, and it is one more reason for it.

## The async model

### The worker

A wallet doesn't allow two calls at once, except `monero_wallet_request_shutdown()`. A daemon handle serializes its calls inside monero-cpp. So each handle has **one thread with a queue**, and the async methods put their call in it:

```csharp
internal sealed class NativeWorker : IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;
    private volatile bool _closed;

    public NativeWorker(string name)
    {
        // 8 MB, as the main thread of Linux: the default of Windows is 1 MB, and monero-cpp parses JSON by recursion
        _thread = new Thread(() => { foreach (var work in _queue.GetConsumingEnumerable()) work(); }, 8 * 1024 * 1024) { IsBackground = true, Name = name };
        _thread.Start();
    }

    public Task<T> RunAsync<T>(Func<T> call, CancellationToken ct = default)
    {
        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (ct.IsCancellationRequested) { done.SetCanceled(ct); return done.Task; }
        if (_closed) { done.SetException(new ObjectDisposedException(_thread.Name)); return done.Task; }
        var registration = ct.CanBeCanceled ? ct.Register(() => done.TrySetCanceled(ct)) : default;
        try
        {
            _queue.Add(() =>
            {
                registration.Dispose();
                if (done.Task.IsCompleted) return;   // canceled while it waited: the native call is skipped
                try { done.TrySetResult(call()); }   // the call frees what it allocates, so a canceled caller leaks nothing
                catch (Exception e) { done.TrySetException(e); }
            });
        }
        catch (InvalidOperationException) { done.TrySetException(new ObjectDisposedException(_thread.Name)); }
        return done.Task;
    }

    public void Dispose() { _closed = true; _queue.CompleteAdding(); }
}
```

- A dedicated thread blocks on its queue, so the queue is a `BlockingCollection`. A `Channel<T>` is for the consumers that are async, and it is used for the events.
- `RunContinuationsAsynchronously` makes the code that awaits resume on the thread pool, not on the worker. The worker never runs the code of the user, so a slow continuation can't hold the next call, and `.GetAwaiter().GetResult()` on a method of the wallet can't deadlock it.
- The cost is **one thread for each live wallet or daemon**, whatever the number of calls. The .NET thread pool is not used by the native calls, so many wallets can't starve it.
- Every method of a class is a line (a sketch):

```csharp
public Task<string> GetPrimaryAddressAsync(CancellationToken ct = default) =>
    _worker.RunAsync(() => HandleUse.With(_handle, () => Address(_handle)), ct);
```

### What cancellation can do

- A call that waits in the queue is canceled at once and never reaches the native code.
- A call that runs can't be stopped: the token completes the task as canceled, and the native call finishes and frees its memory. A sync that is already running is the case that hurts: the only call that may run during it is `monero_wallet_request_shutdown()`, which makes the wallet unusable after it. Don't wire a `CancellationToken` of a normal sync to it. If you need to cancel a sync and keep the wallet, ask for a call in monero-c that does that, and don't build it in C#.

### The daemon

The daemon class has the same worker, which matches the native lock. The one call that must not be in the queue is `monero_daemon_wait_for_next_block_header()`: it blocks its thread, and the other calls of the handle run meanwhile. `WaitForNextBlockHeaderAsync` is built on the listener of the daemon, like the next section.

Several daemons or wallets can share a `monero_rpc_connection` handle: monero-cpp locks it. Each of them has its own worker.

### Waiting and syncing

- `WaitForNextBlockAsync()`: a `TaskCompletionSource` that the event of a new block completes, with the listener removed from the C# list when it is done. It holds no thread. Don't call `monero_wallet_wait_for_next_block()`, which can't be stopped and misses a wake-up.
- A sync with progress, `SyncAsync(IProgress<MoneroSyncProgress>?, ct)`: the call runs on the worker, and the progress comes from the events of the wallet. It keeps the queue busy for the whole sync, and the other calls of the wallet wait behind it, which is also what the lock of monero-cpp does.
- An app with a UI uses `StartSyncing(period)` and the events: the sync runs on a thread of monero-cpp, and the worker stays free for the calls of the user.

## Events

One native listener for each wallet, and a `Channel<T>` between it and the code of the users:

```csharp
internal abstract record WalletEvent;
internal sealed record NewBlock(ulong Height) : WalletEvent;
internal sealed record OutputReceived(string Json) : WalletEvent;
internal sealed record OutputSpent(string Json) : WalletEvent;
internal sealed record BalancesChanged(ulong Balance, ulong Unlocked) : WalletEvent;

internal sealed unsafe class WalletEvents : IDisposable
{
    private readonly Channel<WalletEvent> _events = Channel.CreateUnbounded<WalletEvent>(new() { SingleReader = true });
    private GCHandle _self;
    public ListenerHandle Handle { get; }
    public ChannelReader<WalletEvent> Reader => _events.Reader;

    public WalletEvents()
    {
        _self = GCHandle.Alloc(this);
        var callbacks = new WalletListenerCallbacks
        {
            UserData = (void*)GCHandle.ToIntPtr(_self),
            OnNewBlock = &NewBlockCallback,
            OnBalancesChanged = &BalancesCallback,
            OnOutputReceived = &ReceivedCallback,
            OnOutputSpent = &SpentCallback,
        };
        Check.Ok(Native.monero_wallet_listener_create(in callbacks, out var handle));
        Handle = handle;
    }

    private static WalletEvents Of(void* userData) => (WalletEvents)GCHandle.FromIntPtr((IntPtr)userData).Target!;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void NewBlockCallback(void* userData, ulong height) { try { Of(userData)._events.Writer.TryWrite(new NewBlock(height)); } catch { } }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void BalancesCallback(void* userData, ulong balance, ulong unlocked) { try { Of(userData)._events.Writer.TryWrite(new BalancesChanged(balance, unlocked)); } catch { } }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ReceivedCallback(void* userData, byte* json) { try { Of(userData)._events.Writer.TryWrite(new OutputReceived(Marshal.PtrToStringUTF8((IntPtr)json)!)); } catch { } }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void SpentCallback(void* userData, byte* json) { try { Of(userData)._events.Writer.TryWrite(new OutputSpent(Marshal.PtrToStringUTF8((IntPtr)json)!)); } catch { } }

    // after monero_wallet_close() has returned, which waits for the callbacks in flight, and before the wallet is freed
    public void Dispose()
    {
        Handle.Dispose();
        _events.Writer.TryComplete();
        if (_self.IsAllocated) _self.Free();
    }
}
```

- The JSON of `on_output_received` and `on_output_spent` is the output in its tx and block, `{"blocks":[...]}`: the parser of the results reads it. A received output is in the `outputs` of the tx, and a spent one in its `inputs`. The text of a callback is valid only during the call, so it is copied right there, as above.
- A task reads the `Channel` and calls the listeners of the users on the thread pool, one event after the other. A user can then call a method of the wallet from a listener: the call goes to the queue of the worker, and no lock of monero-cpp is held by the thread that runs the listener.
- The same shape serves the daemon, with `monero_daemon_listener_callbacks`, which has one callback, `on_block_header`.

## Closing

The order matters, and it is the one that the README of monero-c gives. The method is a sketch of the class that will hold the pieces, and each step of it was run in the console project:

```csharp
public async ValueTask DisposeAsync()
{
    if (Interlocked.Exchange(ref _closing, 1) == 1) return;
    HandleUse.With(_handle, () => { Check.Ok(Native.monero_wallet_request_shutdown(_handle)); return 0; });   // the one call that may run now
    await _worker.RunAsync(() => HandleUse.With(_handle, () => { Check.Ok(Native.monero_wallet_close(_handle, _saveOnClose)); return 0; }));
    _events.Dispose();     // after the close, which waited for the callbacks, and before the wallet is freed
    _worker.Dispose();
    _handle.Dispose();     // monero_wallet_free(), after the last call that holds the handle
}
```

1. `monero_wallet_request_shutdown()` from the thread of the caller makes the calls that are in flight return soon, so the close doesn't wait for a sync that lasts minutes. It is only for a full wallet: a keys wallet and an RPC wallet answer "request_shutdown() not supported", and for those the line is skipped.
2. `monero_wallet_close()` on the worker, after the calls that were in the queue, saves the wallet if it is a file and waits for the callbacks in flight.
3. The listener is freed, then its `GCHandle`.
4. The wallet is freed by the `SafeHandle`.

Without `DisposeAsync()` the finalizer of the `SafeHandle` still frees the wallet, and doesn't save it.

## The results

The shapes are in [`docs/json-shapes.md`](https://github.com/libmonero/monero-c/blob/main/docs/json-shapes.md) of monero-c. For the code of the classes:

- `get_txs()`, `get_transfers()` and `get_outputs()` of the wallet, and `get_tx()`, `get_txs()` and `get_tx_pool()` of the daemon, return `{"blocks":[...]}`. A tx is in its block, and a transfer or an output in its tx. Deserialize it to a type with `Blocks`, and **give the models their parents back** after: set `tx.Block` and `transfer.Tx` while you walk the tree, so that the user reads `tx.Block.Height` as in monero-java.
- A tx that no block has yet (in the pool) is in a block with no `height`.
- The calls that make txs return `{"txSets":[...]}`. A view-only wallet gives the `unsignedTxHex` of the set, which `SignTxsAsync()` of a wallet with the keys takes.
- The txs come in the order of the blocks. **A query by hashes doesn't return them in the order of the hashes**: sort them (a sketch).

```csharp
var position = hashes.Select((hash, i) => (hash, i)).ToDictionary(x => x.hash, x => x.i);
txs.Sort((a, b) => position[a.Hash!].CompareTo(position[b.Hash!]));
```

- The lists that have no parent (accounts, subaddresses, address book, key images) are plain arrays.
- The amounts are `ulong`, and a JSON number that is more than 2^53 is exact for `System.Text.Json`.
- A NULL output is `null` (a block that the daemon doesn't have), and the method returns `null` and not an error.

## MoneroWalletLws

The interface is async, so Lws implements it with real I/O, and no worker for the network. What it can ask of monero-c is the cryptography (keys, addresses, key images, the signing of a tx), and that is CPU work with no wait: it goes in `Task.Run()`, or on a worker if it uses a handle. Creating a tx from outputs and decoys that the server gave needs a call in monero-c that doesn't exist yet: a creation that doesn't depend on a synced wallet2. That is the design to open with monero-c when Lws starts, and it is not an async question.

## Testing for leaks

On Linux, `mallinfo2()` of glibc gives the bytes that the native heap holds. A test calls a function many times and checks that the heap doesn't grow. Without the `monero_utils_free()` it does, and it is visible:

```csharp
[StructLayout(LayoutKind.Sequential)]
struct MallInfo2 { public nuint arena, ordblks, smblks, hblks, hblkhd, usmblks, fsmblks, uordblks, fordblks, keepcost; }

[LibraryImport("libc")] static partial MallInfo2 mallinfo2();

static long Heap() => (long)mallinfo2().uordblks;

for (int i = 0; i < 200; i++) await worker.RunAsync(() => Address(wallet));      // warm up
long before = Heap();
for (int i = 0; i < 20000; i++) await worker.RunAsync(() => Address(wallet));
long growth = Heap() - before;
Assert.True(growth < 256 * 1024, $"the native heap grew {growth} bytes");
```

Measured with a wallet in memory: 14 to 28 KB after 20000 calls with the free, and 2188 KB without it, so a threshold of 256 KB tells them apart with room. Do the same for each kind of result: the big JSON, a buffer, a call that fails, a call that is canceled. For the whole library, an ASan build of monero-c (`LD_PRELOAD` of libasan, `ASAN_OPTIONS=detect_leaks=1`) reports the leaks of the native side. The runtime of .NET has its own that you suppress.

The tests that need a node use the regtest `docker-compose.yml` of monero-c (`tests/c/integration`). Two things that were seen there:

- A wallet that has just asked the daemon for its height didn't see the blocks mined a few seconds later: `sync` returned 0 blocks at once, and fetched 5 after a wait of 35 seconds. It was seen once and its cause is not investigated. In a test, mine the blocks before you create the wallet that connects, or wait.
- The wallet RPC server of that file stops when a test calls `monero_wallet_rpc_stop()`, and the wallets that a test makes stay in its volume: `docker compose down -v` before the next run.

## What was checked

A console project with the pieces of this document, on .NET 8, against the library of monero-c at ABI 0.1.0:

- `out WalletHandle` of a `LibraryImport`, a wallet created from a config JSON, the address read and freed with `NativeString`, and the error text of a bad query read on the worker thread.
- The leak probe above: 28.5 KB for 20000 calls with the free, 2187.9 KB without it.
- A queued call that is canceled doesn't reach the native code, and the call that was running when it was canceled finishes.
- A handle disposed during a call: the call still gets its 95 characters, the handle is closed at once, and the native wallet is freed after the call.
- The order of the close: the shutdown request, the close on the worker, the listener, then the wallet.
- The listener with `[UnmanagedCallersOnly]` function pointers and a `GCHandle`, on a regtest node: 5 blocks mined to the wallet, a sync that returned 5 blocks, and the events read from the `Channel` as 5 new blocks and 5 outputs received. No exception, and the close and the frees in the order above.

Not checked: .NET 10, Windows and macOS, a keys or RPC wallet through this layer, and many wallets at once.
