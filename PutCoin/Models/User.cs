using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using NLog;

namespace PutCoin.Model
{
    public class User : ICloneable, IDisposable
    {
        public static int CalculatingDifficulty = 3;
        public static int BlockSize = 3;
        private readonly IDisposable blockChainChangesSubscription;
        private readonly IDisposable transactionCheckSubscription;
        private readonly Dictionary<Guid, int> transactionValidationResultCount = new Dictionary<Guid, int>();
        private readonly IDisposable validatedTranactionSubscription;

        private List<Transaction> pendingTransactions = new List<Transaction>();

        public User()
        {
            blockChainChangesSubscription = Program.BlockChainPublishLine.Subscribe(OnUpdateBlockChain);
            transactionCheckSubscription = Program.TransactionCheckLine.Subscribe(OnNewTransaction);
            validatedTranactionSubscription = Program.VerifiedTransactionPublishLine.Subscribe(transaction =>
            {
                pendingTransactions.Add(transaction);

                if (pendingTransactions.Count == BlockSize) PublishNewBlock();
            });
        }

        public int Id { get; set; }
        public string Signature { get; set; }
        public BlockChain BlockChain { get; set; } = new BlockChain();
        public IEnumerable<Transaction> Transactions => BlockChain.Transactions.Concat(pendingTransactions);

        public object Clone()
        {
            var cloned = (User) MemberwiseClone();
            return cloned;
        }

        private void OnUpdateBlockChain(BlockChain blockChain)
        {
            if (blockChain.IsValid() && blockChain.Blocks.Count > BlockChain.Blocks.Count)
            {
                BlockChain = (BlockChain) blockChain.Clone();
                pendingTransactions = new List<Transaction>();
            }
        }

        private void OnNewTransaction(Transaction transaction)
        {
            Program.Logger.Log(LogLevel.Info, $"User {Id} OnNewTransaction - Destinations: {String.Join('-', transaction.Destinations.Select(x => x.ReceipentId))}");

            if (transaction.Destinations.Any(destination => destination.ReceipentId == Id))
            {
                Program.Logger.Log(LogLevel.Info, $"User {Id} trying to create queue for transaction {transaction.Id}");

                var validationLine = Program.TransactionValidationLine.GetOrAdd(transaction.Id, new Subject<bool>());

                transactionValidationResultCount[transaction.Id] = 0;
                validationLine
                    .Take(Program.Users.Count)
                    .TakeWhile(_ => transactionValidationResultCount[transaction.Id] < (Program.Users.Count - transaction.Destinations.Count()) / 2)
                    .Subscribe(
                        validationResult =>
                        {
                            Program.Logger.Log(LogLevel.Info, $"User {Id} ValidationResult: {validationResult}");

                            transactionValidationResultCount[transaction.Id] += validationResult ? 1 : 0;
                        },
                        () =>
                        {
                            if (transactionValidationResultCount[transaction.Id] >= Program.Users.Count / 2)
                                Program.VerifiedTransactionPublishLine.OnNext(transaction);

                            Program.Logger.Log(LogLevel.Info, $"User {Id} OnCompleted: {(transactionValidationResultCount[transaction.Id] - transaction.Destinations.Count()) >= Program.Users.Count / 2}");
                        });
            }
            else
            {
                CheckTransaction(transaction);
                Program.Logger.Log(LogLevel.Info, $"User {Id} Checked transaction");
            }
        }

        private void CheckTransaction(Transaction transaction)
        {
            var isValid = transaction.IsValidForTransactionHistory(Transactions);

            Program.TransactionValidationLine.TryGetValue(transaction.Id, out var publishingLine);

            if (publishingLine == null)
            {
            }

            publishingLine.OnNext(isValid);
        }

        public override bool Equals(object obj)
        {
            return obj is User user && user.Id == Id;
        }

        public Block GetNewBlock(Block previousBlock)
        {
            var seed = new Random();
            var nonce = seed.Next();

            var transactions = pendingTransactions;

            Program.Logger.Log(LogLevel.Info, $"User {Id} started validating Block");
            
            while (true)
            {
                Console.WriteLine($"Checking nonce: {++nonce}");

                var stringBuilder = new StringBuilder();
                stringBuilder.Append(nonce);

                foreach (var transaction in transactions) stringBuilder.Append(transaction);

                var hash = stringBuilder.ToString().GetHash();

                if (hash.Take(CalculatingDifficulty).All(hashCharacter => hashCharacter == '0')) break;
            }
            
            Program.Logger.Log(LogLevel.Info, $"User {Id} finished validating Block");

            return new Block
            {
                Nonce = nonce.ToString(),
                Transactions = transactions,
                PreviousBlockHash = previousBlock.Hash
            };
        }

        private void PublishNewBlock()
        {
            var newBlock = GetNewBlock(BlockChain.Blocks.Last());
            BlockChain.Blocks.Add(newBlock);
            Program.BlockChainPublishLine.OnNext(BlockChain);
        }

        #region IDisposable Support

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    blockChainChangesSubscription.Dispose();
                    transactionCheckSubscription.Dispose();
                    validatedTranactionSubscription.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}