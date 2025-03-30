using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using SigSharp.Utils;

namespace SigSharp.Nodes;


public abstract class SignalNode : IDisposable
{
    internal static readonly object EmptyObject = new();
    
    public static IEqualityComparer<T> AsGenericComparer<T>(IEqualityComparer comparer)
    {
        return comparer switch
        {
            IEqualityComparer<T> genericComparer => genericComparer,
            _ => EqualityComparer<T>.Default
        };
    }

    public bool IsDisposed { get; private set; }
    
    public virtual bool DisposedBySignalGroup { get; protected set; }
    
    public IEnumerable<SignalNode> ReferencedBy => _referencedBy.Select(static d => d.Key);
    
    public bool IsReferenced => _referencedBy.Any();
    
    public bool IsSuspended { get; private set; }
    public bool WouldBeTracked { get; private set; }
    
    public bool IsTrackable { get; }

    protected readonly ILogger Logger;
    
    private readonly ConditionalWeakTable<SignalNode, object> _referencedBy = [];
    
    protected SignalNode(bool isTrackable)
    {
        Logger = Signals.Options.Logging.CreateLogger(this.GetType());

        this.IsTrackable = isTrackable;
        
        this.MarkTracked();
    }

    ~SignalNode()
    {
        this.Dispose();
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (this.IsDisposed)
            return;
        
        if (disposing)
        {
            this.ReferencedBy.ForEach(d => d.ReferenceDisposed(this));
        
            _referencedBy.Clear();
        }

        this.IsDisposed = true;
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void ReferenceChanged(SignalNode refNode) { }
    protected virtual void ReferenceDisposed(SignalNode refNode) { }

    public void Changed()
    {
        if (this.IsDisposed)
            return;
        
        this.ReferencedBy.ForEach(d => d.ReferenceChanged(this));
    }

    public void MarkTracked()
    {
        if (!this.IsTrackable)
            return;
        
        if (this.IsSuspended)
        {
            this.WouldBeTracked |= SignalTracker.Current is not null;
        }
        else
        {
            SignalTracker.Current?.Track(this);
        }
    }

    public virtual SignalSuspender Suspend(bool disposing = false)
    {
        this.IsSuspended = true;
        return new SignalSuspender(this, disposing);
    }

    public virtual void Resume()
    {
        if (!this.IsSuspended)
            return;
        
        this.IsSuspended = false;
        
        if (this.WouldBeTracked)
        {
            this.MarkTracked();
        }
    }

    public void AddReferencedBy(SignalNode node)
    {
        _referencedBy.AddOrUpdate(node, EmptyObject);
    }

    public void RemoveReferencedBy(SignalNode node)
    {
        _referencedBy.Remove(node);
    }

    protected void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
    }
}