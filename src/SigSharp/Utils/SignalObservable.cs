using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SigSharp.Utils;

internal sealed class SignalObservable<T> : IObservable<T>, IDisposable
{
    private ConditionalWeakTable<IDisposable, IObserver<T>> _subscriptions = [];
    private bool _disposed;

    private T _value;

    public SignalObservable(T initialValue)
    {
        _value = initialValue;
    }
    
    public void OnNext(T value)
    {
        if (_disposed)
            return;

        _value = value;

        foreach ((_, IObserver<T> observer) in _subscriptions)
        {
            try
            {
                observer.OnNext(value);
            }
            catch (Exception)
            {
                //ignore
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;

        KeyValuePair<IDisposable, IObserver<T>>[] subs = _subscriptions.ToArray();
        
        _subscriptions.Clear();
        _subscriptions = null;
        
        _value = default;
        
        foreach ((var subscription, IObserver<T> observer) in subs)
        {
            observer.OnCompleted();
            subscription.Dispose();
        }
    }
    
    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        var sub = new Subscription(this);
        
        _subscriptions.Add(sub, observer);
        
        observer.OnNext(_value);

        return sub;
    }

    private void Disposed(IDisposable subscription)
    {
        if (_disposed)
            return;
        
        _subscriptions.Remove(subscription);
    }

    private sealed class Subscription : IDisposable
    {
        private SignalObservable<T> _observable;
        private bool _disposed;

        public Subscription(SignalObservable<T> observable)
        {
            _observable = observable;
        }

        ~Subscription()
        {
            if (!_disposed)
                return;
            
            this.Dispose();
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
            
            _disposed = true;
            
            _observable?.Disposed(this);
            _observable = null;
            
            GC.SuppressFinalize(this);
        }
    }
}