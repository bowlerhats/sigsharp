using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SigSharp.Nodes;
using SigSharp.TrackerStores;
using SigSharp.Utils;

namespace SigSharp;


public sealed partial class SignalGroup: SignalNode
{
    private static readonly AsyncLocal<SignalGroup> CurrentBoundGroup = new();

    public static SignalGroup Current => CurrentBoundGroup.Value;
    
    public static SignalGroup CreateBound(SignalGroupOptions opts = null, string name = null)
    {
        var group = new SignalGroup(opts, name);
        
        group.Bind();
        
        return group;
    }

    public static SignalSuspender CreateSuspended(SignalGroupOptions opts = null, string name = null)
    {
        name ??= "Suspender";
        
        var group = new SignalGroup(opts, name);
        
        group.Bind();
    
        return group.Suspend(true);
    }
    
    public SignalGroupOptions Options { get; }

    public IEnumerable<SignalNode> Members => _memberStore.Tracked;
    
    private readonly INamedTrackerStore _namedStore;
    private readonly ITrackerStore _memberStore;

    private readonly ConcurrentQueue<SignalEffect> _queued = new();
    
    private SignalGroup _bindParent;
    
    public SignalGroup(SignalGroupOptions opts = null, string name = null)
        : base(false, name)
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
    
    public SignalGroup(object anchor, SignalGroupOptions opts = null, string name = null)
        :this(opts, name)
    {
        if (anchor is not null)
        {
            this.Options.DisposeLinker?.Invoke(this, anchor);
        }
    }

    public override void Resume()
    {
        base.Resume();
        
        this.ResumeEffects(false);
    }

    public bool TryQueueSuspended(SignalEffect effect)
    {
        if (!this.IsSuspended || _queued.Contains(effect))
            return false;
        
        _queued.Enqueue(effect);

        return true;
    }

    public void Bind()
    {
        _bindParent = Current;
        CurrentBoundGroup.Value = this;
    }

    public void Unbind()
    {
        if (Current == this)
        {
            CurrentBoundGroup.Value = _bindParent;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Unbind();
            
            if (this.IsSuspended && this.Options.AutoResumeSuspendedEffects)
            {
                this.ResumeEffects(true);
            }
            
            foreach (var node in this.Members)
            {
                if (!node.IsDisposed && node.DisposedBySignalGroup)
                {
                    node.Dispose();
                }
            }
            
            _memberStore.Clear();
            _memberStore.Dispose();
            
            _namedStore.Clear();
            _namedStore.Dispose();
            
            RemoveAnchored(this);
        
            UntrackGroup(this);
        }
        
        base.Dispose(disposing);
    }

    public void AddMember(SignalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        
        if (this.IsDisposed)
            return;

        if (node == this)
            return;
        
        _memberStore.Track(node);
    }

    public void RemoveMember(SignalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (this.IsDisposed)
            return;
        
        if (node == this)
            return;
        
        _memberStore.UnTrack(node);
    }

    internal ComputedSignal<T> GetOrCreateComputed<T>(
        ComputedFunctor<T> functor,
        string name,
        int line,
        ComputedSignalOptions opts = null)
    {
        this.CheckDisposed();
        
        var id = new ComputedSignalId(name, line);

        var signal = this.LookupComputed<ComputedSignal<T>>(id);

        if (signal is null)
        {
            signal = new ComputedSignal<T>(this, functor, opts, name);
            this.TrackComputed(signal, id);
        }

        return signal;
    }
    
    internal ComputedSignal<T, TState> GetOrCreateComputed<T, TState>(
        TState state,
        ComputedFunctor<T, TState> functor,
        string name,
        int line,
        ComputedSignalOptions opts = null)
    {
        this.CheckDisposed();
        
        var id = new ComputedSignalId(name, line);

        var signal = this.LookupComputed<ComputedSignal<T, TState>>(id);

        if (signal is null)
        {
            signal = new ComputedSignal<T, TState>(this, state, functor, opts, name);
            this.TrackComputed(signal, id);
        }

        return signal;
    }
    
    internal WeakComputedSignal<T, TState> GetOrCreateWeakComputed<T, TState>(
        TState state,
        ComputedFunctor<T, TState> functor,
        ComputedFunctor<Signal<T>, TState> wrappedFunctor,
        string name,
        int line,
        ComputedSignalOptions opts = null)
        where TState: class
    {
        this.CheckDisposed();
        
        var id = new ComputedSignalId(name, line);

        var signal = this.LookupComputed<WeakComputedSignal<T, TState>>(id);

        if (signal is null)
        {
            signal = new WeakComputedSignal<T, TState>(
                this,
                state,
                functor, 
                wrappedFunctor,
                opts,
                name);
            
            this.TrackComputed(signal, id);
        }

        return signal;
    }

    private T LookupComputed<T>(ComputedSignalId id)
        where T: SignalNode
    {
        return _namedStore.LookupComputed<T>(id);
    }

    private void TrackComputed(SignalNode node, ComputedSignalId id)
    {
        this.AddMember(node);
        
        _namedStore.Track(node, id);
    }

    private void ResumeEffects(bool ignoreMembers)
    {
        HashSet<SignalEffect> scheduled = [];
        
        while (_queued.TryDequeue(out var effect))
        {
            if (!ignoreMembers || !_memberStore.Contains(effect))
            {
                if (scheduled.Add(effect))
                    effect.Schedule();
            }
        }
    }
}