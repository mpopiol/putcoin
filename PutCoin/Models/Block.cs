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
                return stringToHash.GetHash();
            }
        }
    }
}