using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PutCoin.Model
{
    public class User : ICloneable
    {
        public static int CalculatingDifficulty = 5;
        public static int BlockSize = 3;

        public Guid Id { get; set; }
        public string Signature { get; set; }
        public BlockChain BlockChain { get; set; }

        private List<Transaction> pendingTransactions = new List<Transaction>();
        private IEnumerable<Transaction> Transactions => BlockChain.Transactions.Concat(pendingTransactions);

        public object Clone()
        {
            var cloned = (User)MemberwiseClone();
            return cloned;
        }

        public override bool Equals(object obj) => obj is User user && user.Id == Id;

        public void UpdateBlockChain(BlockChain blockChain)
        {
            if (blockChain.IsValid() && blockChain.Blocks.Count > BlockChain.Blocks.Count)
            {
                BlockChain = blockChain;
                pendingTransactions = new List<Transaction>();
            }
        }

        public Block GetNewBlock(Block previousBlock, ICollection<Transaction> transactions)
        {
            var seed = new Random();
            var nonce = seed.Next();

            while (true)
            {
                Console.WriteLine($"Checking nonce: {++nonce}");

                var stringBuilder = new StringBuilder();
                stringBuilder.Append(nonce);

                foreach (var transaction in transactions)
                {
                    stringBuilder.Append(transaction);
                }

                var hash = stringBuilder.ToString().GetHash();

                if (hash.Take(CalculatingDifficulty).All(hashCharacter => hashCharacter == '0'))
                {
                    break;
                }
            }

            return new Block()
            {
                Nonce = nonce.ToString(),
                Transactions = transactions,
                PreviousBlockHash = previousBlock.Hash
            };
        }

        public async Task<bool> CheckTransactionAsync(Transaction transaction)
        {
            var isValid = transaction.IsValidForTransactionHistory(Transactions);

            if (!isValid)
            {
                return false;
            }

            pendingTransactions.Add(transaction);

            if (pendingTransactions.Count == BlockSize)
            {
                await PublishNewBlockAsync();
            }

            return true;
        }

        private Task PublishNewBlockAsync()
        {
            return Task.CompletedTask;
        }

        public bool TryAddNewTransaction(User initiator, Transaction transaction)
        {
            var isValid = transaction.IsValidForTransactionHistory(Transactions);
            var miners = Program.Users.Values.Except(new[] { initiator, this }).ToArray();
            var transactionValidatedCount = 0;
            var responses = miners.Select(async miner => transactionValidatedCount += (await miner.CheckTransactionAsync(transaction) ? 1 : 0)).ToArray();

            Task.WaitAll(responses);

            isValid &= (transactionValidatedCount > miners.Length / 2);

            if (isValid)
            {
                pendingTransactions.Add(transaction);
            }

            return isValid;
        }
    }
}