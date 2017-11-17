using System;
using System.Collections.Generic;

namespace QiDiTu.StateMachineFramework.Collections
{
    public interface IMultiValuedMap<TK, TV>
    {
        bool Put(TK key, TV value);

        ICollection<TV> Remove(TK key);

        bool RemoveMapping(TK key, TV value);

        bool IsEmpty { get; }

        bool ContainsKey(TK key);

        bool ContainsValue(TV value);

        bool ContainsMapping(TK key, TV value);

        void Clear();

        void ForEach(TK key, Action<TV> action);

        void ForEach(Action<TK> action);

        void ForEach(Action<TK, TV> action);

        ISet<TK> KeySetClone();

        ICollection<TV> ValuesClone(TK key);

        ICollection<TV> ValuesClone();

        int KeyCount();

        int ValueCount(TK key);

        int ValuesCount();
    }
}