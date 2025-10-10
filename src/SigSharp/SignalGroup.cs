using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SigSharp.Nodes;
using SigSharp.TrackerStores;
using SigSharp.Utils;

namespace SigSharp;


public sealed partial class SignalGroup: SignalNode
{
    private static readonly AsyncLocal<SignalGroup?> CurrentBoundGroup = new();

    public static SignalGroup? Current => CurrentBoundGroup.Value;

    internal static SignalSuspender CreateGlobalSuspended(SignalGroupOptions? opts = null, string? name = null)
    {
        lock (GlobalSuspenderLock)
        {
            name ??= "GlobalSuspender";

            var group = new SignalGroup(opts, name);

            var sus = group.Suspend(true);
            
            var current = GlobalSuspender;
            group._bindParent = current;
            GlobalSuspender = group;

            return sus;
        }
    }
    
    internal static SignalSuspender CreateSuspended(SignalGroupOptions? opts = null, string? name = null)
    {
        name ??= "Suspender";
        
        var group = new SignalGroup(opts, name);

        var sus = group.Suspend(true);
        
        group.Bind();

        return sus;
    }
    
    public SignalGroupOptions Options { get; }

    internal ITrackerStore MemberStore => _memberStore;

    private readonly INamedTrackerStore _namedStore;
    private readonly ITrackerStore _memberStore;
    private readonly Lock _trackLock = new();

    private readonly ConcurrentQueue<SignalEffect> _queued = new();
    
    private SignalGroup? _bindParent;
    
    public SignalGroup(SignalGroupOptions? opts = null, string? name = null)
        : base(false, false, name)
    {
        this.Options = opts ?? SignalGroupOptions.Defaults;

        if (this.Options.WeakTrack)
        {
            _namedStore = new WeakNamedTrackerStore();
            _memberStore = new WeakTrackerStore();
        }
        else
        {
            _namedStore = new ConcurrentNamedTrackerStore();
            _memberStore = new ConcurrentTrackerStore();
        }

        TrackGroup(this);
    }

    public SignalGroup(object anchor, SignalGroupOptions? opts = null, string? name = null)
        : this(opts, name)
    {
        this.Options.DisposeLinker?.Invoke(this, anchor);
    }
    
    public override void MarkDirty() { }
    public override void MarkPristine() { }

    public override void Resume()
    {
        base.Resume();
        
        this.ResumeEffects(false);
    }

    public async ValueTask<bool> WaitIdleAsync(TimeSpan? waitBetweenChecks = null, CancellationToken stopToken = default)
    {
        if (this.IsDisposing || this.IsSuspended || !_memberStore.HasAny)
            return false;
        
        HashSet<SignalEffect> effects = [];
        
        var wasWorking = false;
        bool isWorking;
        do
        {
            isWorking = false;

            _memberStore.Collect(effects);

            if (!effects.Any())
                break;
            
            foreach (var effect in effects)
            {
                if (effect.IsDisposing || effect.IsSuspended)
                    continue;

                if (await effect.WaitIdleAsync(stopToken: stopToken))
                {
                    isWorking = true;
                    wasWorking = true;
                }
            }

            if (waitBetweenChecks.HasValue)
                await Task.Delay(waitBetweenChecks.Value, stopToken);
            
        } while (isWorking);

        return wasWorking;
    }

    public bool TryQueueSuspended(SignalEffect effect)
    {
        if (!this.IsSuspended)
            return false;

        if (!_queued.Contains(effect))
            _queued.Enqueue(effect);

        return true;
    }

    internal void Bind()
    {
        if (Current == this)
        {
            
        }
        _bindParent = Current;
        CurrentBoundGroup.Value = this;
    }

    internal void Unbind()
    {
        if (GlobalSuspender == this)
        {
            GlobalSuspender = _bindParent;
        }
        
        if (Current == this)
        {
            CurrentBoundGroup.Value = _bindParent;
        }
    }

    public bool HasBound(SignalGroup group)
    {
        return _bindParent == group || (_bindParent?.HasBound(group) ?? false);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        this.Unbind();
            
        if (this.IsSuspended && this.Options.AutoResumeSuspendedEffects)
        {
            this.ResumeEffects(true);
        }
            
        await _memberStore.WithEachAsync(static async node =>
            {
                if (node is { IsDisposing: false, DisposedBySignalGroup: true })
                {
                    if (node is SignalEffect effect)
                    {
                        effect.StopAutoRun();
                        
                        await effect.WaitIdleAsync();
                    }
                    
                    await node.DisposeAsync();
                }
            });

        _memberStore.Clear();
        _memberStore.Dispose();

        lock (_trackLock)
        {
            _namedStore.Clear();
            _namedStore.Dispose();
        }

        RemoveAnchored(this);
        
        UntrackGroup(this);
        
        await base.DisposeAsyncCore();
    }

    public bool AddMember(SignalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        
        if (this.IsDisposing || node == this)
            return false;
        
        return _memberStore.Track(node);
    }

    public void RemoveMember(SignalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (this.IsDisposed || node == this)
            return;
        
        _memberStore.UnTrack(node);
    }

    internal ComputedSignal<T>? GetOrCreateComputed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T>(
        ComputedFunctor<T> functor,
        string name,
        int line,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null
        )
    {
        if (this.IsDisposing)
            return null;
        
        var id = new ComputedSignalId(name, line);

        var signal = this.LookupComputed<ComputedSignal<T>>(id);

        if (signal is not null)
            return signal;
        
        this.CheckDisposed();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;
        
        signal = new ComputedSignal<T>(this, functor, opts, name);
        
        var tracked = this.TrackComputed(signal, id);
        if (tracked != signal)
        {
            signal.Dispose();
            signal = tracked;
        }

        return signal;
    }
    
    internal ComputedSignal<T, TState>? GetOrCreateComputed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TState>(
        TState state,
        ComputedFunctor<T, TState> functor,
        string name,
        int line,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null
        )
    {
        if (this.IsDisposing)
            return null;
        
        var id = new ComputedSignalId(name, line);

        var signal = this.LookupComputed<ComputedSignal<T, TState>>(id);

        if (signal is not null)
            return signal;
        
        this.CheckDisposed();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;
        
        signal = new ComputedSignal<T, TState>(this, state, functor, opts, name);
        
        var tracked = this.TrackComputed(signal, id);
        if (tracked != signal)
        {
            signal.Dispose();
            signal = tracked;
        }

        return signal;
    }
    
    internal WeakComputedSignal<T, TState>? GetOrCreateWeakComputed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TState>(
        TState state,
        ComputedFunctor<T, TState> functor,
        ComputedFunctor<Signal<T>, TState> wrappedFunctor,
        string name,
        int line,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null
        )
        where TState: class
    {
        if (this.IsDisposing)
            return null;
        
        var id = new ComputedSignalId(name, line);

        var signal = this.LookupComputed<WeakComputedSignal<T, TState>>(id);

        if (signal is not null)
            return signal;
        
        this.CheckDisposed();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;
        
        signal = new WeakComputedSignal<T, TState>(
            this,
            state,
            functor,
            wrappedFunctor,
            opts,
            name);

        var tracked = this.TrackComputed(signal, id);
        if (tracked != signal)
        {
            signal.Dispose();
            signal = tracked;
        }

        return signal;
    }

    private T? LookupComputed<T>(ComputedSignalId id)
        where T: SignalNode
    {
        lock (_trackLock)
        {
            return _namedStore.LookupComputed<T>(id);
        }
    }

    private TNode TrackComputed<TNode>(TNode node, ComputedSignalId id)
        where TNode : SignalNode
    {
        lock (_trackLock)
        {
            var existing = this.LookupComputed<TNode>(id);

            if (existing is not null)
                return existing;

            if (!this.AddMember(node) && !_memberStore.Contains(node))
            {
                throw new SignalException("Failed to track computed signal node");
            }
            
            if (!_namedStore.Track(node, id))
            {
                this.RemoveMember(node);
                
                throw new SignalException("Failed to track named computed signal node");
            }
        }

        return node;
    }

    private void ResumeEffects(bool ignoreMembers)
    {
        var bound = Current;
        if (bound is not null && (bound == this || bound.HasBound(this)))
            bound = _bindParent;

        while (_bindParent is not null && !_bindParent.IsSuspended)
        {
            _bindParent = _bindParent._bindParent;
        }

        bound ??= GlobalSuspender;
        
        HashSet<SignalEffect> scheduled = [];
        
        while (_queued.TryDequeue(out var effect))
        {
            if (effect.IsDisposing)
                continue;
            
            if (!ignoreMembers || !_memberStore.Contains(effect))
            {
                if (scheduled.Add(effect))
                {
                    if (bound is not null && bound != this && bound.TryQueueSuspended(effect))
                    {
                        continue;
                    }
                    
                    effect.Schedule(true);
                }
            }
        }
    }
}