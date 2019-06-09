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
            var cloned = (BlockChain) MemberwiseClone();
            cloned.Blocks = Blocks.Select(x => (Block) x.Clone()).ToList();
            return cloned;
        }

        public bool IsValid()
        {
            return !(AreThereMoreThanOneTransactionsWithTheSameOrigin()
                     || IsThereAnyTransactionWithInvalidOrigin()
                     || IsThereTransactionWithDifferentValueSpentThanAvailable()
                     || IsThereBlockWithInvalidHash()
                );
        }

        private bool IsThereBlockWithInvalidHash()
        {
            return Blocks.Any(x =>
                x.Hash.Take(User.CalculatingDifficulty) != Enumerable.Repeat('0', User.CalculatingDifficulty));
        }

        private bool AreThereMoreThanOneTransactionsWithTheSameOrigin()
        {
            foreach (var transaction in Transactions)
            foreach (var userId in transaction.Destinations.Select(x => x.ReceipentId))
                if (Transactions.Where(trans => trans.OriginTransactionIds != null).Count(x =>
                        x.OriginTransactionIds.Contains(transaction.Id) && x.UserId == userId) > 1)
                    return true;

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

                if (originTransaction.Any(x => !x.Destinations.Select(y => y.ReceipentId).Contains(transaction.UserId)))
                    return true;
            }

            return false;
        }

        private bool IsThereTransactionWithDifferentValueSpentThanAvailable()
        {
            var transactionsToProcess = Transactions.Where(x => !x.IsGenesis);
            foreach (var transaction in transactionsToProcess)
                if (!transaction.IsValidForTransactionHistory(Transactions))
                    return true;

            return false;
        }
    }
}