using System;
using System.Collections;
using System.Collections.Generic;

namespace SigSharp.Utils;

internal static class ObjectWalker
{
    public static ObjectWalker<T> Walk<T>(
        T entryItem,
        Func<T, T?> nextFunc,
        Func<T?, bool>? stopFunc = null
        )
        where T : class
    {
        stopFunc ??= static item => item is null;

        return new ObjectWalker<T>(entryItem, nextFunc, stopFunc);
    }
}

internal ref struct ObjectWalker<T> : IEnumerator<T>
    where T : class
{
    
    
    public T Current { get; private set; }

    object? IEnumerator.Current => this.Current;
    
    private T _start;
    private T? _next;
    private Func<T, T?> _nextFunc;
    private Func<T?, bool> _stopFunc;
    private bool _endReached;

    public ObjectWalker<T> GetEnumerator()
    {
        return this;
    }

    public ObjectWalker(T start, Func<T, T?> nextFunc, Func<T?, bool> stopFunc)
    {
        _start = start;
        _next = start;
        _nextFunc = nextFunc;
        _stopFunc = stopFunc;
        
        this.Current = start;
    }

    public bool MoveNext()
    {
        if (_endReached || _next is null)
            return false;

        if (_next is null || _stopFunc(_next))
        {
            _endReached = true;

            return false;
        }

        this.Current = _next;

        _next = _nextFunc(_next);
        
        return true;
    }
    
    public void Reset()
    {
        this.Current = _start;
        _next = _start;
        _endReached = false;
    }

    public void Dispose()
    {
        this.Current = null!;
        _nextFunc = null!;
        _stopFunc = null!;
        _start = null!;
        _next = null!;
    }
}