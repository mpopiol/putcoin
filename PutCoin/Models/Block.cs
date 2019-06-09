using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                var stringBuilder = new StringBuilder();
                stringBuilder.Append(Nonce);

                foreach (var transaction in Transactions)
                {
                    stringBuilder.Append(transaction);
                }

                return stringBuilder.ToString().GetHash();
            }
        }

        public object Clone()
        {
            var cloned = (Block) MemberwiseClone();
            cloned.Transactions = Transactions.Select(x => (Transaction) x.Clone()).ToArray();
            return cloned;
        }
    }
}