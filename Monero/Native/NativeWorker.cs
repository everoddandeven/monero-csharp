using System.Collections.Concurrent;

namespace Monero.Native;

/// <summary>One thread with a queue per handle: its calls run one after the other, and continuations resume on the thread pool.</summary>
internal sealed class NativeWorker : IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;
    private volatile bool _closed;

    public NativeWorker(string name)
    {
        // 8 MB, as the main thread of Linux: the default of Windows is 1 MB, and monero-cpp parses JSON by recursion
        _thread = new Thread(Loop, 8 * 1024 * 1024) { IsBackground = true, Name = name };
        _thread.Start();
    }

    private void Loop()
    {
        foreach (var work in _queue.GetConsumingEnumerable()) work();
    }

    /// <summary>Run a call on the thread of the handle. A queued call is canceled at once; a running one finishes, but the task is canceled.</summary>
    public Task<T> RunAsync<T>(Func<T> call, CancellationToken ct = default)
    {
        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (ct.IsCancellationRequested)
        {
            done.SetCanceled(ct);
            return done.Task;
        }

        if (_closed)
        {
            done.SetException(new ObjectDisposedException(_thread.Name));
            return done.Task;
        }

        var registration = ct.CanBeCanceled ? ct.Register(() => done.TrySetCanceled(ct)) : default;

        try
        {
            _queue.Add(() =>
            {
                registration.Dispose();
                if (done.Task.IsCompleted) return;

                try
                {
                    done.TrySetResult(call());
                }
                catch (Exception e)
                {
                    done.TrySetException(e);
                }
            });
        }
        catch (InvalidOperationException)
        {
            registration.Dispose();
            done.TrySetException(new ObjectDisposedException(_thread.Name));
        }

        return done.Task;
    }

    /// <summary>Run a call that returns nothing on the thread of the handle.</summary>
    public Task RunAsync(Action call, CancellationToken ct = default)
    {
        return RunAsync(() =>
        {
            call();
            return 0;
        }, ct);
    }

    public void Dispose()
    {
        _closed = true;
        _queue.CompleteAdding();
    }
}
