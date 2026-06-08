using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SigSharp;

public class DictionarySignal<TKey, TValue>
    : CollectionSignal<KeyValuePair<TKey, TValue>, ConcurrentDictionary<TKey, TValue>>,
        IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    public TValue this[TKey key]
    {
        get {
            if (this.IsDisposed)
            {
                return DisposedSignalAccess.Access(this.DisposedCapture, this)[key];
            }
            
            this.MarkTracked(); this.RequestAccess(); return this.BackingCollection[key];
        }
        set => this.Add(key, value);
    }

    public ICollection<TKey> Keys
    {
        get {
            if (this.IsDisposed)
            {
                return DisposedSignalAccess.Access(this.DisposedCapture, this).Keys;
            }
            
            this.MarkTracked(); this.RequestAccess(); return this.BackingCollection.Keys;
        }
    }

    public ICollection<TValue> Values
    {
        get {
            if (this.IsDisposed)
            {
                return DisposedSignalAccess.Access(this.DisposedCapture, this).Values;
            }
            
            this.MarkTracked(); this.RequestAccess(); return this.BackingCollection.Values;
        }
    }

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
    {
        get {
            if (this.IsDisposed)
            {
                return DisposedSignalAccess.Access(this.DisposedCapture, this).Keys;
            }
            
            this.MarkTracked(); this.RequestAccess(); return this.BackingCollection.Keys;
        }
    }

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
    {
        get {
            if (this.IsDisposed)
            {
                return DisposedSignalAccess.Access(this.DisposedCapture, this).Values;
            }
            
            this.MarkTracked(); this.RequestAccess(); return this.BackingCollection.Values;
        }
    }
    
    public IEqualityComparer<TValue> ValueEqualityComparer { get; }

    public DictionarySignal(
        IEnumerable<KeyValuePair<TKey, TValue>> initialValues,
        CollectionSignalOptions? opts = null,
        string? name = null,
        IEqualityComparer<TValue>? valueEqualityComparer = null
        )
        : base(initialValues, opts, name ?? $"DictionarySignal<{typeof(TKey).Name}, {typeof(TValue).Name}>")
    {
        this.ValueEqualityComparer = valueEqualityComparer ?? EqualityComparer<TValue>.Default;
    }

    public DictionarySignal(
        CollectionSignalOptions? opts = null,
        string? name = null,
        IEqualityComparer<TValue>? valueEqualityComparer = null
        )
        : this([], opts, name, valueEqualityComparer)
    {
    }

    protected override ConcurrentDictionary<TKey, TValue> CreateBackingCollection(
        IEnumerable<KeyValuePair<TKey, TValue>> initialValues)
    {
        return new ConcurrentDictionary<TKey, TValue>(initialValues);
    }
    
    public void Add(TKey key, TValue value)
    {
        this.CheckDisposed();
        
        if (this.Options.PrecheckUpdateRequests && this.BackingCollection.TryGetValue(key, out var existing))
        {
            if (this.ValueEqualityComparer.Equals(existing, value))
                return;
            
            this.RequestUpdate();
            this.BackingCollection[key] = value;
            this.Changed();

            return;
        }
        
        this.RequestUpdate();
        
        if (!this.BackingCollection.TryAdd(key, value))
        {
            this.BackingCollection[key] = value;
        }
        
        this.Changed();
    }

    public new void Add(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        
        this.CheckDisposed();
        
        if (this.Options.PrecheckUpdateRequests && pairs is ICollection<KeyValuePair<TKey, TValue>> coll)
        {
            if (coll.Count <= 0)
                return;

            var steady = true;
            foreach (KeyValuePair<TKey, TValue> kvp in coll)
            {
                steady &= this.BackingCollection.ContainsKey(kvp.Key);
                if (!steady)
                    break;
            }

            if (steady)
            {
                foreach (KeyValuePair<TKey, TValue> kvp in coll)
                {
                    steady &= this.BackingCollection.TryGetValue(kvp.Key, out var val)
                        && this.ValueEqualityComparer.Equals(kvp.Value, val);
                    
                    if (!steady)
                        break;
                }
            }

            if (steady)
                return;
        }
        else if (pairs.TryGetNonEnumeratedCount(out var count) && count <= 0)
        {
            return;
        }

        this.RequestUpdate();
        
        var changed = false;
        
        foreach (var (key, value) in pairs)
        {
            if (this.BackingCollection.TryAdd(key, value))
            {
                changed = true;
            }
            else if (!this.ValueEqualityComparer.Equals(this.BackingCollection[key], value))
            {
                this.BackingCollection[key] = value;
                changed = true;
            }
        }

        if (changed)
        {
            this.Changed();
        }
    }
    
    public bool ContainsKey(TKey key)
    {
        if (this.IsDisposed)
        {
            return DisposedSignalAccess
                .Access(this.DisposedCapture, this)
                .ContainsKey(key);
        }
        
        this.MarkTracked();
        this.RequestAccess();
        
        return this.BackingCollection.ContainsKey(key);
    }
    
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (this.IsDisposed)
        {
            return DisposedSignalAccess
                .Access(this.DisposedCapture, this)
                .TryGetValue(key, out value);
        }
        
        this.MarkTracked();
        this.RequestAccess();
        
        return this.BackingCollection.TryGetValue(key, out value);
    }

    public bool Remove(TKey key)
    {
        this.CheckDisposed();
        
        if (this.Options.PrecheckUpdateRequests && !this.BackingCollection.ContainsKey(key))
            return false;
        
        this.RequestUpdate();
        
        var res = this.BackingCollection.Remove(key, out var _);
        if (res)
        {
            this.Changed();
        }

        return res;
    }
    
    bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key)
    {
        if (this.IsDisposed)
        {
            return DisposedSignalAccess
                .Access(this.DisposedCapture, this)
                .ContainsKey(key);
        }
        
        this.MarkTracked();
        this.RequestAccess();
        
        return this.ContainsKey(key);
    }

    bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (this.IsDisposed)
        {
            return DisposedSignalAccess
                .Access(this.DisposedCapture, this)
                .TryGetValue(key, out value);
        }
        
        this.MarkTracked();
        this.RequestAccess();
        
        return this.TryGetValue(key, out value);
    }
}