using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

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

    ~SignalObservable()
    {
        this.Dispose();
    }
    
    public void OnNext(T value)
    {
        if (_disposed)
            return;

        _value = value;

        foreach (var (_, observer) in _subscriptions)
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

        if (Interlocked.Exchange(ref _disposed, true))
            return;
        
        var subs = _subscriptions.ToArray();
        
        _subscriptions.Clear();
        _subscriptions = null!;
        
        _value = default!;
        
        foreach (var (subscription, observer) in subs)
        {
            try
            {
                observer.OnCompleted();
            }
            finally
            {
                subscription.Dispose();
            }
        }
        
        GC.SuppressFinalize(this);
    }
    
    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        SignalDisposedException.ThrowIf(_disposed, this);
        
        var sub = new Subscription(this);
        
        _subscriptions.Add(sub, observer);
        
        observer.OnNext(_value);

        return sub;
    }

    private void Disposed(IDisposable subscription)
    {
        if (_disposed)
            return;
        
        _subscriptions.RemoveSafe(subscription);
    }

    private sealed class Subscription : IDisposable
    {
        private SignalObservable<T>? _observable;
        private bool _disposed;

        public Subscription(SignalObservable<T> observable)
        {
            _observable = observable;
        }

        ~Subscription()
        {
            this.Dispose();
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;

            if (Interlocked.Exchange(ref _disposed, true))
                return;

            _observable?.Disposed(this);
            _observable = null;
            
            GC.SuppressFinalize(this);
        }
    }
}