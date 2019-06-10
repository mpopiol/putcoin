using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using NLog;

namespace PutCoin.Model
{
    public class User : ICloneable, IDisposable
    {
        public static int CalculatingDifficulty = 4;
        public static int BlockSize = 3;
        private readonly IDisposable blockChainChangesSubscription;
        private readonly IDisposable transactionCheckSubscription;
        private readonly Dictionary<string, int> transactionValidationResultCount = new Dictionary<string, int>();
        private readonly IDisposable validatedTransactionSubscription;

        private List<Transaction> pendingTransactions = new List<Transaction>();
        
        public List<Transaction> minePendingTransactions = new List<Transaction>();

        public User()
        {
            blockChainChangesSubscription = Program.BlockChainPublishLine
                .ObserveOn(NewThreadScheduler.Default)
                .Subscribe(OnUpdateBlockChain);
            transactionCheckSubscription = Program.TransactionCheckLine
                .ObserveOn(NewThreadScheduler.Default)
                .Subscribe(OnNewTransaction);
            validatedTransactionSubscription = Program.VerifiedTransactionPublishLine
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(transaction =>
                {
                    pendingTransactions.Add(transaction);

                    Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User {Id} Pending trans: {pendingTransactions.Count}");

                    if (pendingTransactions.Count >= BlockSize)
                    {
                        PublishNewBlock();
                    }
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
            if (blockChain.IsValid && blockChain.Blocks.Count > BlockChain.Blocks.Count)
            {
                BlockChain = (BlockChain) blockChain.Clone();
                pendingTransactions = pendingTransactions.Except(BlockChain.Transactions).ToList();
            }
        }

        private void OnNewTransaction(Transaction transaction)
        {
            Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User {Id} OnNewTransaction - Destinations: {String.Join(", ", transaction.Destinations.Select(x => x.ReceipentId))}");
            var resultKey = transaction.Id.ToString();

            if (transaction.Destinations.Any(destination => destination.ReceipentId == Id) && !transactionValidationResultCount.ContainsKey(resultKey))
            {
                if (transaction.Destinations.Select(x => x.ReceipentId).OrderBy(x => x).First() != Id)
                    return;
                
                transactionValidationResultCount[resultKey] = 0;
                Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User {Id} trying to create queue for transaction {transaction.Id}");

                var validationLine = Program.TransactionValidationLine.GetOrAdd(transaction.Id, new ReplaySubject<bool>(Program.Users.Count));
                var minimumAcceptance = (Program.Users.Count - transaction.Destinations.Count()) / 2;
                
                validationLine
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .TakeWhile(_ => transactionValidationResultCount[resultKey] < minimumAcceptance)
                    //.Take(Program.Users.Count)
                    .Subscribe(
                        validationResult =>
                        {
                            Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} T: {transaction.Id} User {Id} ValidationResult: {validationResult}");

                            transactionValidationResultCount[resultKey] += validationResult ? 1 : 0;
                        },
                        () =>
                        {
                            var positiveResults = transactionValidationResultCount[resultKey];
                            if (positiveResults >= minimumAcceptance)
                                Program.VerifiedTransactionPublishLine.OnNext(transaction);

                            Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} T: {transaction.Id} User {Id} OnCompleted: {transactionValidationResultCount[resultKey] > minimumAcceptance}");
                        });
            }
            else
            {
                CheckTransaction(transaction);
                Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} T: {transaction.Id} User {Id} Checked transaction");
            }
        }

        private void CheckTransaction(Transaction transaction)
        {
            Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User {Id} Checking transaction");
            var isValid = transaction.IsValidForTransactionHistory(Transactions);

            Program.TransactionValidationLine.TryGetValue(transaction.Id, out var publishingLine);

            if (publishingLine == null)
            {
                return;
            }

            publishingLine.OnNext(isValid);
        }

        public override bool Equals(object obj)
        {
            return obj is User user && user.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public Block GetNewBlock(Block previousBlock)
        {
            var seed = new Random();
            var nonce = seed.Next();

            var transactions = pendingTransactions.ToArray();

            Program.Logger.Log(LogLevel.Info, $"\t\tThreadId: {Thread.CurrentThread.ManagedThreadId} User {Id} started validating Block");
            
            while (transactions.All(x => pendingTransactions.Contains(x)))
            {
                var potentialBlock = new Block
                {
                    Nonce = (++nonce).ToString(),
                    Transactions = transactions,
                    PreviousBlockHash = previousBlock.Hash
                };

                if (potentialBlock.Hash.Take(CalculatingDifficulty).All(hashCharacter => hashCharacter == '0'))
                {
                    Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User {Id} finished validating Block");

                    return potentialBlock;
                }
            }

            return null;
        }

        private void PublishNewBlock()
        {
            var newBlock = GetNewBlock(BlockChain.Blocks.Last());

            if (newBlock is null)
                return;
            
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
                    validatedTransactionSubscription.Dispose();
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