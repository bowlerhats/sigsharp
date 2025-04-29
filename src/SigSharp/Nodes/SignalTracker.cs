using System;
using System.Collections.Generic;
using SigSharp.Utils;

namespace SigSharp.Nodes;

public sealed partial class SignalTracker
{
    internal IReadOnlyCollection<SignalNode> Tracked => _touchedNodes;
    internal IReadOnlyCollection<SignalNode> Changed => _changedNodes;

    internal bool IsReadonly => _isReadonly || (_parent?.IsReadonly ?? false);
    internal bool CanAcceptForwarded => _recursive || (_parent?.CanAcceptForwarded ?? false);

    private readonly ConcurrentHashSet<SignalNode> _touchedNodes = [];
    private readonly ConcurrentHashSet<SignalNode> _changedNodes = [];

    private SignalTracker? _parent;
    private bool _isReadonly;

    private bool _isTracking = true;
    private bool _isChangeTracking;

    private bool _forwardEnabled = true;
    private bool _recursive;

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

    internal SignalTracker DisableForwarding(bool disabled = true)
    {
        _forwardEnabled = !disabled;

        return this;
    }

    internal void Track(SignalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_isTracking)
        {
            _touchedNodes.Add(node);
        }

        this.TrackForward(node);
    }

    internal void TrackForward(SignalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_recursive && _isTracking)
        {
            _touchedNodes.Add(node);
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

            _changedNodes.Add(node);
        }

        this.TrackForwardChanged(node);
    }

    internal void TrackForwardChanged(SignalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_recursive && _isChangeTracking)
        {
            _changedNodes.Add(node);
        }

        if (!_forwardEnabled)
            return;

        if (_parent?.CanAcceptForwarded ?? false)
            _parent.TrackForwardChanged(node);
    }

    private SignalTracker Reset()
    {
        _touchedNodes.Clear();
        _changedNodes.Clear();

        _parent = null;

        _isReadonly = false;
        _isTracking = true;
        _recursive = false;
        _isChangeTracking = false;
        _forwardEnabled = true;

        return this;
    }

    private SignalTracker Init(SignalTracker? parent)
    {
        this.Reset();

        _parent = parent;

        return this;
    }
}