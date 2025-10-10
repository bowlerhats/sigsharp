using System;
using System.Threading;
using System.Threading.Tasks;

namespace SigSharp.Utils;

internal sealed class AsyncManualResetEvent : IDisposable
{
    private volatile TaskCompletionSource<bool> _tcs = new();

    public bool IsSet => _tcs.Task.IsCompleted;

    private bool _disposed;

    public AsyncManualResetEvent(bool isSetInitially)
    {
        if (isSetInitially)
            this.Set();
    }

    public Task<bool> WaitAsync(TimeSpan? timeout = null, CancellationToken? stopToken = null)
    {
        TaskCompletionSource<bool> tcs = _tcs;
        
        if (_disposed || tcs.Task.IsCompleted)
            return Task.FromResult(true);

        if (timeout.HasValue)
        {
            return Task.WhenAny(
                    tcs.Task.ContinueWith(static _ => true, TaskContinuationOptions.ExecuteSynchronously),
                    Task.Delay(timeout.Value).ContinueWith(static _ => false, TaskContinuationOptions.ExecuteSynchronously)
                    )
                .ContinueWith(t => t.Result.Result, TaskContinuationOptions.ExecuteSynchronously);
        }

        if (!stopToken.HasValue)
            return tcs.Task;

        return tcs.Task.WaitAsync(stopToken.Value);
    }

    public bool Wait(TimeSpan? timeout = null, CancellationToken stopToken = default)
    {
        if (_disposed)
            return true;

        if (timeout.HasValue)
        {
            return this.WaitAsync(timeout, stopToken).GetAwaiter().GetResult();
        }
        
        _tcs.Task.Wait(stopToken);

        return true;
    }
    
    public void Set()
    {
        if (!_tcs.TrySetResult(true) && !_tcs.Task.IsCompleted)
        {
            _tcs.SetResult(true);
        }
    }

    public void Reset()
    {
        if (_disposed)
            return;
        
        while (true)
        {
            if (_disposed)
                return;
            
            TaskCompletionSource<bool> tcs = _tcs;

            if (!tcs.Task.IsCompleted)
                return;

            if (Interlocked.CompareExchange(ref _tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
                return;
        }
    }

    public void Dispose()
    {
        var wasDisposed = Interlocked.CompareExchange(ref _disposed, true, false); 
        if (!wasDisposed && _disposed)
        {
            var dts = new TaskCompletionSource<bool>();
            dts.SetResult(true);

            var tcs = Interlocked.Exchange(ref _tcs, dts);
            if (!tcs.Task.IsCompleted)
                tcs.TrySetResult(true);
        }
    }
}