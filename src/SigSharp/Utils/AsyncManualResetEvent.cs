using System;
using System.Threading;
using System.Threading.Tasks;

namespace SigSharp.Utils;

internal sealed class AsyncManualResetEvent : IDisposable
{
    private volatile TaskCompletionSource<bool> _tcs = new();

    public bool IsSet => _tcs.Task.IsCompleted;

    public AsyncManualResetEvent(bool isSetInitially)
    {
        if (isSetInitially)
            this.Set();
    }

    public Task WaitAsync(CancellationToken? stopToken = null)
    {
        return stopToken.HasValue
            ? _tcs.Task.WaitAsync(stopToken.Value)
            : _tcs.Task;
    }

    public void Wait(CancellationToken stopToken = default)
    {
        _tcs.Task.Wait(stopToken);
    }
    
    public void Set()
    {
        _tcs.TrySetResult(true);
    }

    public void Reset()
    {
        while (true)
        {
            TaskCompletionSource<bool> tcs = _tcs;

            if (!tcs.Task.IsCompleted)
                return;

            if (Interlocked.CompareExchange(ref _tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
                return;
        }
    }

    public void Dispose()
    {
        if (!_tcs.Task.IsCompleted)
            _tcs.TrySetCanceled();
    }
}