using System;
using System.Threading.Tasks;

namespace SigSharp.Nodes;

public abstract class ReactiveNode : SignalNode
{
    public SignalGroup Group { get; }

    protected ReactiveNode(SignalGroup group, bool isTrackable, bool initiallyDirty, string? name)
        : base(isTrackable, initiallyDirty, name)
    {
        ArgumentNullException.ThrowIfNull(group);
        SignalDisposedException.ThrowIf(group.IsDisposed, group);

        this.Group = group;
        group.AddMember(this);
    }

    protected override ValueTask DisposeAsyncCore()
    {
        this.Group.RemoveMember(this);
        
        return base.DisposeAsyncCore();
    }
}