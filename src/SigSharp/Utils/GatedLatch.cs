using System;
using System.Collections.Generic;
using System.Threading;
using SigSharp.Nodes;

namespace SigSharp.Utils;

internal sealed class GatedLatch<T>: IDisposable
    where T : notnull
{
    public bool IsGateClosed => !_gate.IsSet;
    public SignalTracker? ClosedBy => _closedBy;
    
    private readonly ManualResetEventSlim _gate = new(true);
    private readonly ManualResetEventSlim _latch = new(true);

    private SpinLock _lock = new(false);

    private readonly HashSet<T> _latches = new(32);
    
    private bool _disposed;
    private SignalTracker? _closedBy;
    
    public void Dispose()
    {
        var wasDisposed = Interlocked.CompareExchange(ref _disposed, true, false); 
        if (!wasDisposed && _disposed)
        {
            _gate.Dispose();
            _latch.Dispose();
            _latches.Clear();
        }
    }

    public bool HasLatch(T item)
    {
        this.CheckDisposed();
        
        var lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);

            return _latches.Contains(item);
        }
        finally
        {
            if (lockTaken)
                _lock.Exit(false);
        }
    }
    
    public bool Acquire(T item, TimeSpan timeout)
    {
        this.CheckDisposed();
        
        if (!_gate.Wait(timeout))
            return false;
        
        var lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);

            if (_latches.Add(item))
            {
                if (_latch.IsSet)
                    _latch.Reset();
            }
        }
        finally
        {
            if (lockTaken)
                _lock.Exit(false);
        }
        
        return true;
    }

    public void Release(T item)
    {
        this.CheckDisposed();
        
        var lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);

            _latches.Remove(item);
            
            if (_latches.Count == 0)
            {
                if (!_latch.IsSet)
                    _latch.Set();
            }
        }
        finally
        {
            if (lockTaken)
                _lock.Exit(false);
        }
    }

    public void CloseGate(SignalTracker closer)
    {
        this.CheckDisposed();

        Interlocked.CompareExchange(ref _closedBy, closer, null);
        if (_closedBy == closer)
        {
            _gate.Reset();
        }
    }

    public void ReleaseGate(SignalTracker expectedCloser)
    {
        this.CheckDisposed();

        Interlocked.CompareExchange(ref _closedBy, null, expectedCloser);
        if (_closedBy is null)
        {
            _gate.Set();
        }
        else
        {
            throw new SignalException("Unexpected gate release");
        }
    }
    
    public bool Wait(TimeSpan timeout)
    {
        this.CheckDisposed();
        
        return _latch.Wait(timeout);
    }

    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}