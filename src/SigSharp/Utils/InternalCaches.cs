using System;
using System.Collections.Generic;

namespace SigSharp.Utils;

internal static class InternalCaches
{
    public static readonly Dictionary<Type, string> NameCache = [];
    public static readonly Dictionary<ValueTuple<Type, Type>, string> CollectionNameCache = [];
}