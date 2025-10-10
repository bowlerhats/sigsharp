using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using SigSharp.Nodes;
using SigSharp.Utils;

namespace SigSharp;

public class ComputedSignal<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
    )]T> : TrackingSignalNode, IReadOnlySignal<T>
{
    public T Value => this.Get();
    public T Untracked => this.GetUntracked();
    
    public bool IsDefault => _comparer.Equals(this.Get(), this.DefaultValue);
    public bool IsNull => this.Get() is null;

    public bool IsDefaultUntracked => Signals.Untracked(() => this.IsDefault);
    public bool IsNullUntracked => Signals.Untracked(() => this.IsNull);

    public T? DefaultValue { get; }

    public ComputedSignalOptions Options { get; }

    protected virtual bool IsAsyncFunctor => _functor.IsValueTask;

    private readonly SemaphoreSlim _updateLock = new(1);
    private readonly IEqualityComparer<T> _comparer;
    private T _value;
    private SignalObservable<T>? _observable;
    private DisposedSignalAccess.DisposedCapture<T> _disposedCapture;

    private ComputedFunctor<T> _functor;

    internal ComputedSignal(
        SignalGroup group,
        ComputedFunctor<T> functor,
        ComputedSignalOptions? opts = null,
        string? name = null
        )
        : base(group, true, true, name)
    {
        _functor = functor;
        
        this.Options = opts ?? ComputedSignalOptions.Defaults;

        this.SetAccessStrategy(this.Options.AccessStrategy);

        _comparer = AsGenericComparer<T>(this.Options.EqualityComparer);

        this.DefaultValue = this.Options.DefaultValueProvider.GetDefaultValue<T>(default);

        _value = this.DefaultValue!;
    }

    protected override ValueTask DisposeAsyncCore()
    {
        _observable?.Dispose();
        _observable = null;
            
        _updateLock.Dispose();
            
        _functor = default;

        _disposedCapture = DisposedSignalAccess.Capture(_value, this.DefaultValue!, this, this.Options.DisposedAccessStrategy);
        
        _value = default!;
        
        return base.DisposeAsyncCore();
    }

    protected virtual T CalcValueSyncOnly()
    {
        return _functor.InvokeSyncOnly();
    }
    
    protected virtual ValueTask<T> CalcValueAsync()
    {
        return _functor.InvokeAsync();
    }

    public T GetUntracked()
    {
        return Signals.Untracked(this.Get);
    }

    public T Get()
    {
        if (this.IsDisposed)
            return DisposedSignalAccess.Access(_disposedCapture, this);

        if (this.IsDisposing || this.Group.IsDisposing)
            return this.DefaultValue!;
        
        this.MarkTracked();
        this.RequestAccess();
        
        if (!this.IsDirty && this.HasTracking)
            return _value;
        
        return this.Update();
    }

    public T Update()
    {
        this.CheckDisposed();

        var update = this.UpdateAsync();

        return update.IsCompletedSuccessfully
            ? update.Result
            : update.AsTask().GetAwaiter().GetResult();
    }

    private T GetDisposingValue()
    {
        return this.Options.DisposedAccessStrategy switch
            {
                DisposedSignalAccess.Strategy.DefaultValue or DisposedSignalAccess.Strategy.DefaultScalar
                    => this.DefaultValue!,
                _ => _value
            };
    }

    public async ValueTask<T> UpdateAsync()
    {
        using var activity = this.StartActivity();
        
        this.CheckDisposed();
        
        if (this.IsDisposed)
            return DisposedSignalAccess.Access(_disposedCapture, this);
                
        if (this.IsDisposing || this.Group.IsDisposing)
            return this.GetDisposingValue();
        
        this.RequestUpdate();
        
        var oldValue = _value;
        
        var wasWaiting = false;
        
        // allow brief synchronous wait to try to avoid Task overhead if possible
        // ReSharper disable once MethodHasAsyncOverload
        if (!_updateLock.Wait(TimeSpan.FromMilliseconds(5)))
        {
            wasWaiting = true;
            
            while (!await _updateLock.WaitAsync(TimeSpan.FromSeconds(1)))
            {
                if (this.IsDisposed)
                    return DisposedSignalAccess.Access(_disposedCapture, this);
                
                if (this.IsDisposing || this.Group.IsDisposing)
                    return this.GetDisposingValue();
                
                if (!this.IsDirty)
                    return _value;

                if (this.IsCycliclyReferenced())
                {
                    this.MarkDirty();

                    return _value;
                }
            }
        }

        try
        {
            if (this.IsDisposing || this.Group.IsDisposing)
            {
                return DisposedSignalAccess.Access(_disposedCapture, this);
            }
            
            if (!wasWaiting || this.IsDirty)
            {
                const int maxRecalc = 50;
                var recalc = 0;
                do
                {
                    this.IsShadowDirty = false;
                    
                    var tracker = this.StartTrack(false).Readonly();
                    try
                    {
                        _value = this.IsAsyncFunctor
                            ? await this.CalcValueAsync()
                            : this.CalcValueSyncOnly();

                        _value ??= this.DefaultValue ?? default!;
                    }
                    catch (SignalDeadlockedException)
                    {
                        if (this.IsDisposing || this.Group.IsDisposing)
                        {
                            return DisposedSignalAccess.Access(_disposedCapture, this);
                        }

                        this.IsShadowDirty = true;

                        if (!tracker.IsRoot || tracker.IsInEffect())
                            throw;
                    }
                    catch (SignalPreemptedException)
                    {
                        tracker.DisableTracking();
                        
                        if (this.IsDisposing || this.Group.IsDisposing)
                        {
                            return DisposedSignalAccess.Access(_disposedCapture, this);
                        }
                        
                        this.IsShadowDirty = true;

                        if (!tracker.IsRoot)
                            throw;
                    }
                    finally
                    {
                        this.EndTrack(tracker);
                    }
                    
                } while (this.IsShadowDirty && ++recalc < maxRecalc);
            }

            if (this.IsShadowDirty)
            {
                this.MarkDirty();
            }
            else
            {
                this.MarkPristine();
            }

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
        var name = this.Name ?? "noname";

        if (this.IsDisposed)
            return $"{name} is disposed";

        var state = this.IsDirty ? "Dirty" : "Pristine";

        var v = this.IsDisposing ? this.GetDisposingValue() : _value;

        return v is null
            ? $"{name}({this.NodeId}): null {state} in {this.Group.Name ?? "UnknownGroup"}"
            : $"{name}({this.NodeId}): {v} {state} in {this.Group.Name ?? "UnknownGroup"}";
    }
}