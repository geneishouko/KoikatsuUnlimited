﻿using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoikatsuUnlimited.Shared
{
    internal class WeakKeyDictionary<TKey, TValue> where TValue : class
    {
        private readonly Dictionary<HashedWeakReference, TValue> Items;

        public WeakKeyDictionary()
        {
            Items = new Dictionary<HashedWeakReference, TValue>();
        }

        public int Count
        {
            get { return Items.Count; }
        }

        public void Set(TKey key, TValue value)
        {
            Items[new HashedWeakReference(key)] = value;
            // Naive O(n) key prune
            var deadKeys = Items.Keys.Where(reference => !reference.IsAlive).ToList();
            foreach (HashedWeakReference reference in deadKeys)
            {
                Items.Remove(reference);
            }
        }

        public TValue Get(TKey key)
        {
            TValue value;
            if (!Items.TryGetValue(new HashedWeakReference(key), out value))
            {
                return null;
            }
            else
            {
                return value;
            }
        }
    }
}
