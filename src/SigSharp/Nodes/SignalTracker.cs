using System;
using SigSharp.Utils;

namespace SigSharp.Nodes;

internal sealed partial class SignalTracker
{
    internal ConcurrentHashSet<SignalNode> Tracked { get; } = [];
    internal ConcurrentHashSet<SignalNode> Changed { get; } = [];
    internal ConcurrentHashSet<SignalEffect> Effects { get; } = [];

    internal bool IsReadonly => _isReadonly || (_parent?.IsReadonly ?? false);
    internal bool CanAcceptForwarded => _recursive || (_parent?.CanAcceptForwarded ?? false);

    internal bool AcceptEffects => _collectEffects || (_parent?.AcceptEffects ?? false);

    private SignalTracker? _parent;
    private bool _isReadonly;

    private bool _isTracking = true;
    private bool _isChangeTracking;

    private bool _forwardEnabled = true;
    private bool _recursive;
    private bool _collectEffects;

    internal SignalTracker Readonly(bool @readonly = true)
    {
        _isReadonly = @readonly;

        return this;
    }

    internal SignalTracker DisableTracking()
    {
        _isTracking = false;
        _isChangeTracking = false;

        return this;
    }

    internal SignalTracker EnableTracking(bool enabled = true)
    {
        _isTracking = enabled;

        return this;
    }

    internal SignalTracker EnableChangeTracking(bool enabled = true)
    {
        _isChangeTracking = enabled;

        return this;
    }

    internal SignalTracker Recursive(bool recursive = true)
    {
        _recursive = recursive;

        return this;
    }

    internal SignalTracker CollectEffects(bool collectEffects = true)
    {
        _collectEffects = collectEffects;
    
        return this;
    }

    internal SignalTracker DisableForwarding(bool disabled = true)
    {
        _forwardEnabled = !disabled;

        return this;
    }

    internal void PostEffect(SignalEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (_collectEffects)
        {
            this.Effects.Add(effect);
        }

        if (_parent?.AcceptEffects ?? false)
        {
            _parent?.PostEffect(effect);
        }
    }

    internal void Track(SignalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_isTracking)
        {
            this.Tracked.Add(node);
        }

        this.TrackForward(node);
    }

    internal void TrackForward(SignalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_recursive && _isTracking)
        {
            this.Tracked.Add(node);
        }

        if (!_forwardEnabled)
            return;

        if (_parent?.CanAcceptForwarded ?? false)
            _parent.TrackForward(node);
    }

    internal void TrackChanged(SignalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_isChangeTracking)
        {
            if (_isReadonly)
                throw new SignalReadOnlyContextException();

            this.Changed.Add(node);
        }

        this.TrackForwardChanged(node);
    }

    internal void TrackForwardChanged(SignalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_recursive && _isChangeTracking)
        {
            this.Changed.Add(node);
        }

        if (!_forwardEnabled)
            return;

        if (_parent?.CanAcceptForwarded ?? false)
            _parent.TrackForwardChanged(node);
    }

    private SignalTracker Reset()
    {
        this.Tracked.Clear();
        this.Changed.Clear();
        this.Effects.Clear();

        _parent = null;

        _isReadonly = false;
        _isTracking = true;
        _recursive = false;
        _isChangeTracking = false;
        _forwardEnabled = true;
        _collectEffects = false;

        return this;
    }

    private SignalTracker Init(SignalTracker? parent)
    {
        this.Reset();

        _parent = parent;

        return this;
    }
}