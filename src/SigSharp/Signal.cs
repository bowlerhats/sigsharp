using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SigSharp.Nodes;
using SigSharp.Utils;

namespace SigSharp;


public sealed class Signal<T> : SignalNode, IReadOnlySignal<T>, IWritableSignal<T>
{
    public T Value
    {
        get => this.Get();
        set => this.Set(value);
    }

    public T Untracked => this.GetUntracked();

    public bool IsDefault => _comparer.Equals(this.Get(), this.DefaultValue);
    public bool IsNull => this.Get() is null;

    public bool IsDefaultUntracked => Signals.Untracked(() => this.IsDefault);
    public bool IsNullUntracked => Signals.Untracked(() => this.IsNull);

    public T DefaultValue => default!;
    
    public SignalOptions Options { get; }

    private readonly IEqualityComparer<T> _comparer;
    
    private T _value;
    private SignalObservable<T>? _observable;
    private DisposedSignalAccess.DisposedCapture<T> _disposedCapture;

    private Signal(bool isInitialSet, T initialValue, SignalOptions? opts = null, string? name = null)
        : base(true, false, name ?? $"Signal<{typeof(T).Name}>")
    {
        _value = isInitialSet ? initialValue : default!;
        
        this.Options = opts ?? SignalOptions.Defaults;

        this.SetAccessStrategy(this.Options.AccessStrategy);
        
        _comparer = AsGenericComparer<T>(this.Options.EqualityComparer);
        
        if (!isInitialSet)
            _value = this.DefaultValue;
    }
    
    public Signal(T initialValue, SignalOptions? opts = null, string? name = null)
        : this(true, initialValue, opts, name)
    {
    }
    
    public Signal(SignalOptions? opts = null, string? name = null)
        : this(false, default!, opts, name)
    {
    }

    protected override ValueTask DisposeAsyncCore()
    {
        _observable?.Dispose();
        _observable = null;

        _disposedCapture = DisposedSignalAccess.Capture(_value, this.DefaultValue, this, this.Options.DisposedAccessStrategy);
            
        _value = default!;
        
        return base.DisposeAsyncCore();
    }
    
    public override void MarkDirty() { }
    public override void MarkPristine() { }

    public T GetUntracked()
    {
        return Signals.Untracked(this.Get);
    }
    
    public T Get()
    {
        if (this.IsDisposed)
            return DisposedSignalAccess.Access(_disposedCapture, this);
        
        this.MarkTracked();
        this.RequestAccess();
        
        return _value;
    }

    public void SetDefault()
    {
        this.Set(this.DefaultValue);
    }
    
    public void Set(T value)
    {
        this.CheckDisposed();
        
        if (_comparer.Equals(_value, value))
            return;

        if (SignalTracker.IsReadonlyContext)
        {
            throw new SignalReadOnlyContextException();
        }
        
        this.RequestUpdate();
        this.CheckDisposed();
       
        _value = value;
        
        this.Changed();
        
        _observable?.OnNext(_value);
    }

    public IObservable<T> AsObservable()
    {
        this.CheckDisposed();
        
        this.MarkTracked();
        this.RequestAccess();
        
        this.CheckDisposed();
        
        IObservable<T>? res = _observable;
        if (res is null)
        {
            Interlocked.CompareExchange(ref _observable, new SignalObservable<T>(_value), null);
            res = Volatile.Read(ref _observable);
        }
        
        return res;
    }

    public override string ToString()
    {
        if (this.IsDisposed)
            return "disposed";
        
        return $"id: {this.NodeId} | {_value?.ToString() ?? "null"}";
    }
}