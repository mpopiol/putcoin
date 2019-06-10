using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;

namespace PutCoin.Model
{
    public class Transaction : ICloneable
    {
        public Guid Id { get; set; }
        public IEnumerable<Guid> OriginTransactionIds { get; set; }
        public IEnumerable<TransactionDestination> Destinations { get; set; }
        public string Signature { get; set; }
        public int UserId { get; set; }
        public bool IsGenesis { get; set; }

        public object Clone()
        {
            var cloned = (Transaction) MemberwiseClone();
            cloned.Destinations = Destinations.Select(x => (TransactionDestination) x.Clone()).ToArray();
            cloned.OriginTransactionIds = OriginTransactionIds?.ToArray();
            cloned.UserId = UserId;
            return cloned;
        }

        public bool IsValidForTransactionHistory(IEnumerable<Transaction> transactions)
        {
            var moneySpent = Destinations.Sum(x => x.Value);
            var moneyAvailable = transactions.Where(x => OriginTransactionIds.Contains(x.Id))
                .SelectMany(x => x.Destinations)
                .Where(x => x.ReceipentId == UserId)
                .Sum(x => x.Value);

            return moneySpent <= moneyAvailable;
        }

        public override bool Equals(object obj)
        {
            return obj is Transaction tr && tr.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}