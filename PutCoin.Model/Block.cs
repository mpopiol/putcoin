using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PutCoin.Model
{
    public class Block
    {
        public string PreviousBlockHash { get; set; }
        public IEnumerable<Transaction> Transactions { get; set; }
        public string Nonce { get; set; }

        public string Hash
        {
            get
            {
                var stringToHash = $"{PreviousBlockHash}_{String.Join(";", Transactions.Select(x => x.Id.ToString()))}_{Nonce}";
                var byteArray = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(stringToHash));
                var builder = new StringBuilder();
                var chars = byteArray.Select(x => x.ToString("X2")).ToArray();
                for (int i = 0; i < chars.Length; i++)
                    builder.Append(chars[i]);

                return builder.ToString();
            }
        }
    }
}