using System;
using System.Collections.Generic;
using System.Linq;

namespace PutCoin.Model
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public IEnumerable<Guid> OriginTransactionIds { get; set; }
        public IEnumerable<TransactionDestination> Destinations { get; set; }
        public string Signature { get; set; }
        public User User { get; set; }
        public bool IsGenesis { get; set; }

        public static Transaction GenerateRandomTransaction(BlockChain blockChain, User initiator)
        {
            var allTransactions = blockChain.Blocks.SelectMany(x => x.Transactions);
            var mineTransactions = allTransactions
                .Where(x => x.Destinations.Select(y => y.Receipent).Contains(initiator))
                .ToArray();

            //I assume that when I use some transaction I have to use it all
            var mineNotUsedTransactions = mineTransactions
                .Where(x => !allTransactions.Any(y => y.OriginTransactionIds.Contains(x.Id) && y.User == initiator))
                .ToArray();

            var potentialReceipents = allTransactions.SelectMany(x => x.Destinations.Select(y => y.Receipent)).Distinct();

            var originTransaction = mineNotUsedTransactions.Shuffle().FirstOrDefault();
            if (originTransaction is default(Transaction))
                return default;

            var valueToSpend = originTransaction.Destinations.Single(x => x.Receipent == initiator).Value;

            var random = new Random();
            var receipents = potentialReceipents.Shuffle().Take(random.Next(1, 2)).ToArray();
            List<TransactionDestination> designations = GetDesignationsForNewTransaction(valueToSpend, random, receipents);

            return new Transaction
            {
                Destinations = designations,
                Id = Guid.NewGuid(),
                OriginTransactionIds = new[] { originTransaction.Id },
                Signature = initiator.Signature,
                User = initiator
            };
        }

        private static List<TransactionDestination> GetDesignationsForNewTransaction(decimal valueToSpend, Random random, User[] receipents)
        {
            var designations = new List<TransactionDestination>();

            for (int i = 0; i < receipents.Count(); i++)
            {
                var designation = new TransactionDestination
                {
                    Receipent = receipents[i],
                };

                if (i == receipents.Count() - 1)
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
    }
}