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

        public static object GetFieldValue(this Type t, string fieldName, object instance) {
            try {
                return t?.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
            } catch {
                return null;
            }
        }
    }
}
