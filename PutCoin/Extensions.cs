using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PutCoin
{
    public static class Extensions
    {
        public static string GetHash(this string stringToHash)
        {
            byte[] hashBytes = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(stringToHash));

            return BitConverter.ToString(hashBytes).Replace("-", "");
        }
    }
}