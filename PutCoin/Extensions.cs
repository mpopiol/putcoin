using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PutCoin
{
    public static class Extensions
    {
        public static string GetTransactionsHash(this string transactionString)
        {
            byte[] hashBytes = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(transactionString));

            return BitConverter.ToString(hashBytes).Replace("-", "");
        }
    }
}