using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;

namespace PutCoin
{
    public static class Extensions
    {
        public static string GetHash(this string stringToHash)
        {
            var hashBytes = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(stringToHash));

            return BitConverter.ToString(hashBytes).Replace("-", "");
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.OrderBy(x => Guid.NewGuid());
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> enumerable, T element)
            => enumerable.Concat(element == null ? Enumerable.Empty<T>() : new T[] {element});
    }
}