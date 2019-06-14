using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PutCoin.Model
{
    public class BlockChain : ICloneable
    {
        public List<Block> Blocks { get; set; } = new List<Block>();

        [JsonIgnore]
        public IEnumerable<Transaction> Transactions => Blocks.SelectMany(x => x.Transactions);

        public object Clone()
        {
            var cloned = (BlockChain) MemberwiseClone();
            cloned.Blocks = Blocks.Select(x => (Block) x.Clone()).ToList();
            return cloned;
        }

        public bool IsValid(Transaction additionalTransaction = null) => 
                !AreThereMoreThanOneTransactionsWithTheSameOrigin(additionalTransaction) &&
                !IsThereAnyTransactionWithInvalidOrigin(additionalTransaction) &&
                !IsThereTransactionWithDifferentValueSpentThanAvailable(additionalTransaction) &&
                !IsThereBlockWithInvalidHash() &&
                Transactions.Concat(additionalTransaction).All(x => x.Signature?.Equals(x.UserId.ToString()) ?? true);

        private bool IsThereBlockWithInvalidHash()
        {
            return Blocks.Any(x => x.PreviousBlockHash != null &&
                x.Hash.Take(User.CalculatingDifficulty).Any(character => character != '0'));
        }

        private bool AreThereMoreThanOneTransactionsWithTheSameOrigin(Transaction additionalTransaction)
        {
            var transactions = Transactions.Concat(additionalTransaction).ToArray();
            
            foreach (var transaction in transactions)
            foreach (var userId in transaction.Destinations.Select(x => x.ReceipentId))
                {
                    var transactionWithTheSameOrigin = transactions.Where(trans => trans.OriginTransactionIds != null).Where(x =>
                        x.OriginTransactionIds.Contains(transaction.Id) && x.UserId == userId).ToArray();
                    if (transactionWithTheSameOrigin.Length > 1)
                        return true;
                }
                

            return false;
        }

        private bool IsThereAnyTransactionWithInvalidOrigin(Transaction additionalTransaction)
        {
            var transactions = Transactions.Concat(additionalTransaction).ToArray();
            var transactionsToProcess = transactions.Where(x => !x.IsGenesis).ToArray();
            foreach (var transaction in transactionsToProcess)
            {
                var originTransaction = transactions.Where(x => transaction.OriginTransactionIds.Contains(x.Id)).ToArray();
                if (originTransaction.Count() != transaction.OriginTransactionIds.Count())
                    return true;

                if (originTransaction.Any(x => !x.Destinations.Select(y => y.ReceipentId).Contains(transaction.UserId)))
                    return true;
            }

            return false;
        }

        private bool IsThereTransactionWithDifferentValueSpentThanAvailable(Transaction additionalTransaction)
        {
            var transactions = Transactions.Concat(additionalTransaction).ToArray();
            
            var transactionsToProcess = transactions.Where(x => !x.IsGenesis);
            foreach (var transaction in transactionsToProcess)
                if (!transaction.IsValidForTransactionHistory(transactions))
                    return true;

            return false;
        }
    }
}