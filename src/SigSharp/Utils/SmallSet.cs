using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

// ReSharper disable ForCanBeConvertedToForeach

namespace SigSharp.Utils;

internal sealed class SmallSet<T> : ICollection<T>, IReadOnlyCollection<T>
    where T: notnull
{
    private const int DefaultSize = 32;
    
    bool ICollection<T>.IsReadOnly => false;
    
    public int Count { get; private set; }

    public bool IsEmpty => this.Count <= 0;

    private readonly Slot[] _slots;

    private readonly IEqualityComparer<T> _comparer;
    
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
    
    private bool HasCapacity => this.Count <= _slots.Length;

    private long _version;
    private long _clearCount;
    
    public SmallSet()
        : this(DefaultSize)
    {
    }
    
    public SmallSet(int size)
    {
        _slots = new Slot[size];
        _comparer = EqualityComparer<T>.Default;
    }

    public SmallSet(IEnumerable<T> collection)
        : this(collection, EqualityComparer<T>.Default)
    {
    }
    
    public SmallSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        : this(comparer)
    {
        var slots = collection.Select(d => new Slot(true, d, ++_version)).ToArray();
        if (slots.Length > _slots.Length)
        {
            _slots = slots;
        }
        else
        {
            slots.CopyTo(_slots, 0);
        }
    }
    
    public SmallSet(IEqualityComparer<T> comparer)
    {
        _slots = new Slot[DefaultSize];
        _comparer = comparer;
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }
    
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(this);
    }
    
    void ICollection<T>.Add(T item)
    {
        if (!this.Add(item))
            throw new IndexOutOfRangeException("Collection is full");
    }

    public bool Add(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!this.HasCapacity)
                return false;
            
            var occupied = 0;
            var freeIndex = -1;
            for (var i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                if (slot.IsOccupied)
                {
                    occupied++;
                    if (_comparer.Equals(slot.Item, item))
                        return true;
                }
                else if (freeIndex < 0)
                {
                    freeIndex = i;
                }
                
                if (freeIndex >= 0 && occupied >= this.Count)
                    break;
            }

            if (freeIndex >= 0)
            {
                _slots[freeIndex].IsOccupied = true;
                _slots[freeIndex].Item = item;
                _slots[freeIndex].Version = ++_version;
                
                this.Count++;

                return true;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return false;
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            if (this.IsEmpty)
                return;
            
            for (var i = 0; i < _slots.Length; i++)
            {
                _slots[i].Reset();
            }

            this.Count = 0;
            _version = 0;
            _clearCount++;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool Contains(T item)
    {
        _lock.EnterReadLock();
        try
        {
            if (this.IsEmpty)
                return false;
            
            var occupied = 0;
            for (var i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                if (!slot.IsOccupied)
                    continue;
                
                if (slot.Item is not null && _comparer.Equals(item, slot.Item))
                    return true;
                    
                if (++occupied >= this.Count)
                    break;
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _lock.EnterReadLock();
        try
        {
            if (this.IsEmpty)
                return;
            
            var occupied = 0;
            for (var i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                if (!slot.IsOccupied)
                    continue;

                if (slot.Item is not null)
                {
                    array[arrayIndex++] = slot.Item;
                }

                if (++occupied >= this.Count)
                    break;
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    public bool Remove(T item)
    {
        var wasRemoved = false;
        
        _lock.EnterWriteLock();
        try
        {
            if (this.IsEmpty)
                return false;
            
            var occupied = 0;
            for (var i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                if (!slot.IsOccupied)
                    continue;

                if (slot.Item is not null && _comparer.Equals(item, slot.Item))
                {
                    _slots[i].Reset();
                    this.Count--;
                    occupied--;
                    
                    wasRemoved = true;
                }

                if (++occupied >= this.Count)
                    break;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return wasRemoved;
    }

    public record struct Slot(
        bool IsOccupied,
        T? Item,
        long Version
    )
    {
        public void Reset()
        {
            this.IsOccupied = false;
            this.Item = default;
        }
    }

    public struct Enumerator : IEnumerator<T>
    {
        public T Current { get; private set; } = default!;
        object? IEnumerator.Current => this.Current;

        private Slot[] Slots => _set._slots;
        
        private int _pos = -1;
        private long _fromVersion;
        private long _toVersion;
        private bool _endReached;
        private readonly long _clear;
        private readonly SmallSet<T> _set;

        public Enumerator(SmallSet<T> set)
        {
            _set = set;
            _clear = set._clearCount;
            this.Reset();
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            return this.MoveNext(true);
        }

        private bool MoveNext(bool locked)
        {
            if (_endReached)
                return false;
            
            var fromStart = _pos == -1;
            
            if (locked)
                _set._lock.EnterReadLock();
            
            try
            {
                if (_clear != _set._clearCount)
                {
                    _endReached = true;

                    return false;
                }

                while (++_pos < this.Slots.Length)
                {
                    var slot = this.Slots[_pos];

                    if (!slot.IsOccupied || slot.Item is null || slot.Version < _fromVersion)
                        continue;

                    this.Current = slot.Item!;

                    return true;
                }
                
                if (!fromStart && _toVersion < _set._version)
                {
                    _fromVersion = _toVersion + 1;
                    _toVersion = _set._version;
                    _pos = -1;

                    if (this.MoveNext(false))
                        return true;
                }
            
                _endReached = true;
            }
            finally
            {
                if (locked)
                    _set._lock.ExitReadLock();
            }

            return false;
        }
        

        public void Reset()
        {
            _pos = -1;
            _endReached = _set.IsEmpty;
            _fromVersion = 0;
            _toVersion = _set._version;
        }
    }
}