using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SigSharp.Nodes;
using SigSharp.Utils;

namespace SigSharp;


public class ComputedSignal<T> : TrackingSignalNode, IReadOnlySignal<T>
{
    public T Value => this.Get();
    public T Untracked => this.GetUntracked();
    
    public bool IsDefault => _comparer.Equals(this.Get(), default);
    public bool IsNull => this.Get() is null;

    public bool IsDefaultUntracked => Signals.Untracked(() => this.IsDefault);
    public bool IsNullUntracked => Signals.Untracked(() => this.IsNull);

    public ComputedSignalOptions Options { get; }

    private readonly SemaphoreSlim _updateLock = new(1);
    private readonly IEqualityComparer<T> _comparer;
    private T _value;
    private SignalObservable<T> _observable;

    private ComputedFunctor<T> _functor;

    internal ComputedSignal(
        SignalGroup group,
        ComputedFunctor<T> functor,
        ComputedSignalOptions opts = null,
        string name = null
        )
        : base(group, true, name)
    {
        _functor = functor;
        
        this.Options = opts ?? ComputedSignalOptions.Defaults;

        _comparer = AsGenericComparer<T>(this.Options.EqualityComparer);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _observable?.Dispose();
            _observable = null;
            
            _updateLock.Dispose();
            
            _functor = default;
        }
        
        base.Dispose(disposing);
    }

    protected virtual ValueTask<T> CalcValue()
    {
        return _functor.Invoke();
    }

    public T GetUntracked()
    {
        return Signals.Untracked(this.Get);
    }

    public T Get()
    {
        this.MarkTracked();
        
        if (this.IsDisposed)
            return default;
        
        if (this.HasTracking && !this.IsDirty)
            return _value;
        
        return this.Update();
    }

    public T Update()
    {
        ValueTask<T> res = this.UpdateAsync();

        if (res.IsCompletedSuccessfully)
        {
            return res.Result;
        }
        
        return res.AsTask().GetAwaiter().GetResult();
    }
    
    public async ValueTask<T> UpdateAsync()
    {
        this.CheckDisposed();
        
        this.MarkTracked();

        var oldValue = _value;

        if (!await _updateLock.WaitAsync(TimeSpan.FromSeconds(2)))
        {
            if (!this.IsDirty)
            {
                this.MarkDirty();
            }

            return _value;
        }

        try
        {

            var tracker = this.StartTrack(false).Readonly();
            try
            {
                _value = await this.CalcValue();
            }
            finally
            {
                this.EndTrack(tracker);
            }
            
            this.MarkPristine();
        
            var isChanged = !_comparer.Equals(_value, oldValue);
            if (isChanged)
            {
                this.Changed();
            
                _observable?.OnNext(_value);
            }
        }
        finally
        {
            _updateLock.Release();
        }

        return _value;
    }

    public IObservable<T> AsObservable()
    {
        this.CheckDisposed();
        
        this.MarkTracked();
        
        _observable ??= new SignalObservable<T>(this.Value);

        return _observable;
    }
    
    public override string ToString()
    {
        var name = this.Name ?? "noname";
        
        if (this.IsDisposed)
            return $"{name} is disposed";

        var state = this.IsDirty ? "Dirty" : "Pristine";
        
        var v = _value;
        
        return v is null ? $"{name}: null {state}" : $"{name}: {v} {state}";
    }
}