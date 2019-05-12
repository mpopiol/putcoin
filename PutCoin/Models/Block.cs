using System;
using System.Collections.Generic;
using System.Linq;

namespace PutCoin.Model
{
    public class Block : ICloneable
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

        public object Clone()
        {
            var cloned = (Block)MemberwiseClone();
            cloned.Transactions = Transactions.Select(x => (Transaction)x.Clone()).ToArray();
            return cloned;
        }
    }
}