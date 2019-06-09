using System;
using System.Collections.Generic;
using System.Linq;

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

        public static Transaction GenerateRandomTransaction(User initiator)
        {
            var allTransactions = initiator.Transactions;
            var mineTransactions = allTransactions
                .Where(x => x.Destinations.Select(y => y.ReceipentId).Contains(initiator.Id))
                .ToArray();

            var mineNotUsedTransactions = mineTransactions
                .Where(x => !allTransactions.Any(y => y.OriginTransactionIds != null && y.OriginTransactionIds.Contains(x.Id) && y.UserId == initiator.Id))
                .ToArray();

            var originTransaction = mineNotUsedTransactions.Shuffle().FirstOrDefault();
            if (originTransaction is default(Transaction))
                return null;

            var valueToSpend = originTransaction.Destinations.Single(x => x.ReceipentId == initiator.Id).Value;

            var random = new Random();
            
            var receipents = Program.Users.Values
                .Where(x => x.Id != initiator.Id)
                .Shuffle()
                .Take(random.Next(1, 2))
                .Select(x => x.Id).ToArray();
            
            List<TransactionDestination> designations = GetDesignationsForNewTransaction(valueToSpend, receipents);

            return new Transaction
            {
                Destinations = designations,
                Id = Guid.NewGuid(),
                OriginTransactionIds = new[] { originTransaction.Id },
                Signature = initiator.Signature,
                UserId = initiator.Id
            };
        }

        private static List<TransactionDestination> GetDesignationsForNewTransaction(decimal valueToSpend, int[] receipentIds)
        {
            var designations = new List<TransactionDestination>();
            var random = new Random();

            for (int i = 0; i < receipentIds.Count(); i++)
            {
                var designation = new TransactionDestination
                {
                    ReceipentId = receipentIds[i],
                };

                if (i == receipentIds.Count() - 1)
                {
                    designation.Value = valueToSpend;
                }
                else
                {
                    designation.Value = valueToSpend / random.Next(1, 10);
                    valueToSpend -= designation.Value;
                }
                designations.Add(designation);
            }

            return designations;
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

        public object Clone()
        {
            var cloned = (Transaction)MemberwiseClone();
            cloned.Destinations = Destinations.Select(x => (TransactionDestination)x.Clone()).ToArray();
            cloned.OriginTransactionIds = OriginTransactionIds?.ToArray();
            cloned.UserId = UserId;
            return cloned;
        }
    }
}