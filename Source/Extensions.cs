using System;
using System.Collections.Generic;
using System.Reflection;

namespace Merthsoft.AutoOnAutoOff {
    public static class Extensions {
        public static void ForEach<T>(this IEnumerable<T> set, Action<T> action) {
            foreach (var t in set) {
                action.Invoke(t);
            }
        }

        public static T GetFieldValue<T>(this Type t, string fieldName, object instance) where T : class
        {
            try {
                return t?.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance) as T;
            } catch {
                return null;
            }
        }

        public static T GetFieldValue<T>(this object instance, string fieldName) where T : class
            => instance?.GetType()?.GetFieldValue<T>(fieldName, instance);

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> valueGenerator)
            => dict.TryGetValue(key, out var value)
                ? value
                : dict[key] = valueGenerator(key);
    }
}
