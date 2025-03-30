using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SigSharp.Nodes;
using SigSharp.Utils;

namespace SigSharp;

public partial class SignalGroup
{
    private static readonly ConditionalWeakTable<object, SignalGroup> AnchoredGroups = new();
    private static readonly ConditionalWeakTable<SignalGroup, object> AllGroups = new();

    public static SignalGroup Of<TAnchor>(TAnchor anchor, SignalGroupOptions opts = null)
        where TAnchor: class
    {
        if (anchor is SignalGroup signalGroup)
            return signalGroup;

        if (anchor is ReactiveNode reactiveNode)
            return reactiveNode.Group;

        if (anchor is SignalNode)
            throw new SignalException("Signal nodes cannot be anchors");
        
        if (AnchoredGroups.TryGetValue(anchor, out var anchoredGroup))
        {
            return anchoredGroup;
        }
        
        if (opts is null)
            return null;

        anchoredGroup = new SignalGroup(anchor, opts);
        
        AnchoredGroups.Add(anchor, anchoredGroup);

        return anchoredGroup;
    }
    
    public static IEnumerable<SignalGroup> GetAllGroups()
    {
        return AllGroups.Select(d => d.Key);
    }

    internal static void RemoveAnchored(SignalGroup signalGroup)
    {
        AnchoredGroups
            .Where(kvp => kvp.Value == signalGroup)
            .ForEach(d => AnchoredGroups.Remove(d));
    }

    internal static void TrackGroup(SignalGroup signalGroup)
    {
        AllGroups.TryAdd(signalGroup, EmptyObject);
    }

    internal static void UntrackGroup(SignalGroup signalGroup)
    {
        AllGroups.Remove(signalGroup);
    }
}