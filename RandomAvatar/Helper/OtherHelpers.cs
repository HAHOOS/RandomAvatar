using System;
using System.Collections.Generic;

namespace RandomAvatar.Helper
{
    /// <summary>
    /// Helpers for general stuff
    /// </summary>
    public static class OtherHelpers
    {
        /// <summary>
        /// Run an action for each <see cref="KeyValuePair"/> in a given dictionary
        /// </summary>
        /// <typeparam name="TKey">Type of key</typeparam>
        /// <typeparam name="TValue">Type of value</typeparam>
        /// <param name="dictionary">Dictionary to get <see cref="KeyValuePair"/>s from</param>
        /// <param name="action">Action to run on each of them</param>
        public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Action<KeyValuePair<TKey, TValue>> action)
        {
            foreach (var pair in dictionary)
            {
                action?.Invoke(pair);
            }
        }

        /// <summary>
        /// Run an action for each item in a given <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="enumerable"><see cref="IEnumerable{T}"/> that contains the items</param>
        /// <param name="action">Action to run on the items</param>
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action?.Invoke(item);
            }
        }
    }
}