using System;

namespace SigSharp.Nodes;

public abstract class ReactiveNode : SignalNode
{
    public SignalGroup Group { get; }

    protected ReactiveNode(SignalGroup group, bool isTrackable, string name)
        : base(isTrackable, name)
    {
        ArgumentNullException.ThrowIfNull(group);
        ObjectDisposedException.ThrowIf(group.IsDisposed, group);

        this.Group = group;
        group.AddMember(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Group.RemoveMember(this);
        }
        
        base.Dispose(disposing);
    }
}