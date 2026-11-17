using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>Pure set of zones a body currently overlaps. No duplicates; safe against double exit.</summary>
    public class ZoneSet
    {
        /// <summary>Cached liveness check: a live, enabled Unity component.</summary>
        public static readonly Func<object, bool> IsAliveUnityObject =
            o => o is Behaviour b && b != null && b.isActiveAndEnabled;

        readonly List<object> items = new List<object>();

        public int Count => items.Count;
        public object this[int i] => items[i];

        public void Enter(object item)
        {
            if (item != null && !items.Contains(item)) items.Add(item);
        }

        public void Exit(object item) => items.Remove(item);

        /// <summary>Drop items for which isAlive returns false. Does not allocate.</summary>
        public void Prune(Func<object, bool> isAlive)
        {
            for (int i = items.Count - 1; i >= 0; i--)
                if (!isAlive(items[i])) items.RemoveAt(i);
        }
    }
}
