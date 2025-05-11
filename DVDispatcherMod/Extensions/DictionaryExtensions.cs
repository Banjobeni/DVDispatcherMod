using System.Collections.Generic;

namespace DVDispatcherMod.Extensions {
    public static class DictionaryExtensions {
        public static TValue TryGetOrNull<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : class {
            if (dictionary.TryGetValue(key, out TValue value)) {
                return value;
            }

            return null;
        }
    }
}