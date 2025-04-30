using System;
using System.Collections.Generic;
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

    public bool IsDefault => _comparer.Equals(this.Get(), default);
    public bool IsNull => this.Get() is null;

    public bool IsDefaultUntracked => Signals.Untracked(() => this.IsDefault);
    public bool IsNullUntracked => Signals.Untracked(() => this.IsNull);
    
    public SignalOptions Options { get; }

    private readonly IEqualityComparer<T> _comparer;
    
    private T _value;
    private SignalObservable<T>? _observable;
    private DisposedSignalAccess.DisposedCapture<T> _disposedCapture;

    public Signal(T initialValue, SignalOptions? opts = null, string? name = null)
        : base(true, name)
    {
        _value = initialValue;
        
        this.Options = opts ?? SignalOptions.Defaults;
        
        _comparer = AsGenericComparer<T>(this.Options.EqualityComparer);
    }
    
    public Signal(SignalOptions? opts = null)
        : this(default!, opts)
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _observable?.Dispose();
            _observable = null;

            _disposedCapture = DisposedSignalAccess.Capture(_value, this, this.Options.DisposedAccessStrategy);
            
            _value = default!;
        }

        base.Dispose(disposing);
    }

    public T GetUntracked()
    {
        return Signals.Untracked(this.Get);
    }
    
    public T Get()
    {
        if (this.IsDisposed)
            return DisposedSignalAccess.Access(_disposedCapture, this);
        
        this.MarkTracked();

        return _value;
    }

    public void SetDefault()
    {
        this.Set(default!);
    }
    
    public void Set(T value)
    {
        this.CheckDisposed();
        
        if (_comparer.Equals(_value, value))
            return;

        if (SignalTracker.IsReadonlyContext)
        {
            throw new SignalException("Attempted to set signal in read-only context");
        }
        
        _value = value;
        
        this.Changed();
        
        SignalTracker.Current?.TrackChanged(this);
        
        _observable?.OnNext(_value);
    }

    public IObservable<T> AsObservable()
    {
        this.CheckDisposed();
        
        this.MarkTracked();
        
        _observable ??= new SignalObservable<T>(_value);

        return _observable;
    }
    
    public override string ToString()
    {
        if (this.IsDisposed)
            return "disposed";
        
        return _value?.ToString() ?? "null";
    }
}