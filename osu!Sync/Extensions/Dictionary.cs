using System.Collections.Generic;

namespace osuSync.Extensions {

    public static class DictionaryExtensions {

        // Source: https://stackoverflow.com/a/294139
        public static T Merge<T, TKey, TValue>(this T me, IDictionary<TKey, TValue> other, bool overrideExisting = true) where T : IDictionary<TKey, TValue>, new() {
            T result = new T();
            foreach(KeyValuePair<TKey, TValue> pair in other) {
                result[pair.Key] = pair.Value;
            }

            return result;
        }
    }
}
