using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace QiDiTu.StateMachineFramework.Collections
{
    public class HashSetValuedHashMap<TK, TV> : IMultiValuedMap<TK, TV>
    {
        private readonly Dictionary<TK, ISet<TV>> dictionary = new Dictionary<TK, ISet<TV>>();

        private readonly object lockObj = new object();

        public bool Put(TK key, TV value)
        {
            lock (lockObj)
            {
                if (!dictionary.ContainsKey(key))
                {
                    dictionary.Add(key, new HashSet<TV>());
                }
                return dictionary[key]?.Add(value) ?? false;
            }
        }

        public ICollection<TV> Remove(TK key)
        {
            lock (lockObj)
            {
                if (!dictionary.ContainsKey(key))
                {
                    return new HashSet<TV>();
                }
                ICollection<TV> collection = dictionary[key];
                return dictionary.Remove(key) ? collection : new HashSet<TV>();
            }
        }

        [SuppressMessage("ReSharper", "SuggestVarOrType_Elsewhere")]
        public bool RemoveMapping(TK key, TV value)
        {
            lock (lockObj)
            {
                if (!dictionary.ContainsKey(key))
                {
                    return false;
                }
                ISet<TV> values = dictionary[key];
                if (values == null)
                {
                    dictionary.Remove(key);
                    return false;
                }
                bool result = values.Remove(value);
                if (values.Count == 0)
                {
                    dictionary.Remove(key);
                }
                return result;
            }
        }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public bool IsEmpty => dictionary.Count == 0;

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public bool ContainsKey(TK key)
        {
            return dictionary.ContainsKey(key);
        }

        public bool ContainsValue(TV value)
        {
            return ValuesClone().Contains(value);
        }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public bool ContainsMapping(TK key, TV value)
        {
            try
            {
                return dictionary[key]?.Contains(value) ?? false;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        public void Clear()
        {
            lock (lockObj)
            {
                dictionary.Clear();
            }
        }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public void ForEach(TK key, Action<TV> action)
        {
            if (!dictionary.ContainsKey(key))
            {
                return;
            }
            lock (lockObj)
            {
                if (!dictionary.ContainsKey(key))
                {
                    return;
                }
                foreach (TV v in dictionary[key])
                {
                    action.Invoke(v);
                }
            }
        }

        public void ForEach(Action<TK> action)
        {
            lock (lockObj)
            {
                foreach (TK key in dictionary.Keys)
                {
                    action.Invoke(key);
                }
            }
        }

        public void ForEach(Action<TK, TV> action)
        {
            lock (lockObj)
            {
                foreach (TK key in dictionary.Keys)
                {
                    foreach (TV v in dictionary[key])
                    {
                        action.Invoke(key, v);
                    }
                }
            }
        }

        public ISet<TK> KeySetClone()
        {
            lock (lockObj)
            {
                return new HashSet<TK>(dictionary.Keys);
            }
        }

        public ICollection<TV> ValuesClone(TK key)
        {
            lock (lockObj)
            {
                return new HashSet<TV>(dictionary[key]);
            }
        }

        public ICollection<TV> ValuesClone()
        {
            lock (lockObj)
            {
                return dictionary.Values.SelectMany(set => set).ToList();
            }
        }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public int KeyCount()
        {
            return dictionary.Count;
        }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public int ValueCount(TK key)
        {
            if (!dictionary.ContainsKey(key))
            {
                throw new KeyNotFoundException();
            }
            lock (lockObj)
            {
                if (!dictionary.ContainsKey(key))
                {
                    throw new KeyNotFoundException();
                }
                return dictionary[key].Count;
            }
        }

        public int ValuesCount()
        {
            return ValuesClone().Count;
        }
    }
}