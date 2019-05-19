using System;
using System.Collections.Generic;
using System.Linq;

namespace PutCoin.Model
{
    public class BlockChain : ICloneable
    {
        public List<Block> Blocks { get; set; } = new List<Block>();
        public IEnumerable<Transaction> Transactions => Blocks.SelectMany(x => x.Transactions);

        public object Clone()
        {
            var cloned = (BlockChain)MemberwiseClone();
            cloned.Blocks = Blocks.Select(x => (Block)x.Clone()).ToList();
            return cloned;
        }

        public bool IsValid()
        {
            return !(AreThereMoreThanOneTransactionsWithTheSameOrigin()
                || IsThereAnyTransactionWithInvalidOrigin()
                || IsThereTransactionWithDifferentValueSpentThanAvailable()
            );
        }

        private bool AreThereMoreThanOneTransactionsWithTheSameOrigin()
        {
            foreach (var transaction in Transactions)
            {
                foreach (var user in transaction.Destinations.Select(x => x.Receipent))
                {
                    if (Transactions.Count(x => x.OriginTransactionIds.Contains(transaction.Id) && x.User == user) > 1)
                        return true;
                }
            }

            return false;
        }

        private bool IsThereAnyTransactionWithInvalidOrigin()
        {
            var transactionsToProcess = Transactions.Where(x => !x.IsGenesis);
            foreach (var transaction in transactionsToProcess)
            {
                var originTransaction = Transactions.Where(x => transaction.OriginTransactionIds.Contains(x.Id));
                if (originTransaction.Count() != transaction.OriginTransactionIds.Count())
                    return true;

                if (originTransaction.Any(x => !x.Destinations.Select(y => y.Receipent).Contains(transaction.User)))
                    return true;
            }

            return false;
        }

        private bool IsThereTransactionWithDifferentValueSpentThanAvailable()
        {
            var transactionsToProcess = Transactions.Where(x => !x.IsGenesis);
            foreach (var transaction in transactionsToProcess)
            {
                var moneySpent = transaction.Destinations.Sum(x => x.Value);
                var moneyAvailable = Transactions.Where(x => transaction.OriginTransactionIds.Contains(x.Id))
                    .SelectMany(x => x.Destinations)
                    .Where(x => x.Receipent == transaction.User)
                    .Sum(x => x.Value);

                if (moneySpent != moneyAvailable)
                    return true;
            }

            return false;
        }
    }
}