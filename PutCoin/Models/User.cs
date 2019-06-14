using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NLog;

namespace PutCoin.Model
{
    public class User : ICloneable, IDisposable
    {
        public static int CalculatingDifficulty = 4;
        private readonly IDisposable cheaterSubscription;
        private readonly IDisposable blockChainChangesSubscription;
        private readonly IDisposable transactionCheckSubscription;
        private readonly Dictionary<string, int> transactionValidationResultCount = new Dictionary<string, int>();
        private readonly IDisposable validatedTransactionSubscription;

        private volatile BlockVerificationStatusType BlockVerificationStatus = BlockVerificationStatusType.NoVerification;

        private List<Transaction> pendingTransactions = new List<Transaction>();
        private List<Transaction> generatedTransactions = new List<Transaction>();
        public List<Transaction> rejectedTransactions = new List<Transaction>();

        private List<Guid> autoAcceptedTransactionIds = new List<Guid>();

        public bool IsCheater { get; set; }

        public User()
        {
            cheaterSubscription = Program.CheatersPublishLine
                .ObserveOn(NewThreadScheduler.Default)
                .Subscribe(OnNewCheat);
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
                    generatedTransactions.Remove(transaction);

                    if (!BlockChain.IsValid(transaction) && !IsCheater)
                    {
                        Program.Logger.Log(LogLevel.Info, $"User {Id} Skipping INVALID transaction which came from VALIDATED transactions queue!");
                        
                        if (!rejectedTransactions.Contains(transaction))
                            rejectedTransactions.Add(transaction);
                        
                        return;
                    }
                        
                    if (!BlockChain.Transactions.Contains(transaction))
                        pendingTransactions.Add(transaction);

                    Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User {Id} Pending trans: {pendingTransactions.Count}");

                    if (BlockVerificationStatus == BlockVerificationStatusType.NoVerification && pendingTransactions.Skip(1).Any())
                    {
                        PublishNewBlock();
                    }
                });
        }

        private void OnNewCheat(Guid transactionId)
        {
            if (!IsCheater)
            {
                return;
            }

            autoAcceptedTransactionIds.Add(transactionId);

            if (Id > 1)
            {
                return;
            }

            PublishFakeTransaction(transactionId);
        }

        private void PublishFakeTransaction(Guid transactionId)
        {
            var transaction = GenerateFakeTransaction(transactionId);

            if (transaction == null)
                return;

            Program.TransactionValidationLine.GetOrAdd(transaction.Id, new ReplaySubject<bool>(Program.Users.Count));
            Program.TransactionCheckLine.OnNext(transaction);
        }

        private Transaction GenerateFakeTransaction(Guid transactionId)
        {
            var cheaterIds = Program.Users.Where(user => user.IsCheater).Select(user => user.Id).ToArray();

            var victimsValidTransactions = BlockChain.Transactions
                .Where(x => x.Destinations.Select(y => y.ReceipentId).Any(recipientId => !cheaterIds.Contains(recipientId)))
                .ToArray();

            var allMadeTransactions = Transactions.Concat(pendingTransactions).ToArray();
            var victimsNotUsedTransactions = victimsValidTransactions
                .Where(x => !allMadeTransactions.Any(y =>
                    y.OriginTransactionIds != null && y.OriginTransactionIds.Contains(x.Id) &&
                    y.UserId == Id))
                .ToArray();

            var originTransaction = victimsNotUsedTransactions.Shuffle().FirstOrDefault();
            if (originTransaction is default(Transaction))
            {
                Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User: {Id} tried to cheat, but victims have no money");
                return null;
            }

            var originSource = originTransaction.Destinations.First(x => !cheaterIds.Contains(x.ReceipentId));

            var valueToSpend = originSource.Value;
            var destinations = GetDestinationsForNewTransaction(valueToSpend, cheaterIds);

            var newTransaction = new Transaction
            {
                Destinations = destinations,
                Id = transactionId,
                OriginTransactionIds = new[] { originTransaction.Id },
                Signature = "free_money",
                UserId = originSource.ReceipentId,
                CreatedBy = Id
            };
            generatedTransactions.Add(newTransaction);

            return newTransaction;
        }

        public int Id { get; set; }
        public string Signature { get; set; }
        public BlockChain BlockChain { get; set; } = new BlockChain();
        
        [JsonIgnore]
        public IEnumerable<Transaction> Transactions => BlockChain.Transactions.Concat(pendingTransactions);

        public object Clone()
        {
            var cloned = (User) MemberwiseClone();
            return cloned;
        }

        private void OnUpdateBlockChain(BlockChain blockChain)
        {
            var lastBlockTransactionSignatureIsHack = blockChain.Blocks.Last().Transactions.First().Signature == "free_money";
            if (blockChain.IsValid() && blockChain.Blocks.Count > BlockChain.Blocks.Count)
            {
                BlockChain = (BlockChain) blockChain.Clone();
                pendingTransactions = pendingTransactions.Except(BlockChain.Transactions).ToList();

                Program.Logger.Log(LogLevel.Info, $"USER {Id} -.-.-.-.-.-. BLOCKS {BlockChain.Blocks.Count}");
                Program.Logger.Log(LogLevel.Info, $"USER {Id} -.-.-.-.-.-. Valid? {BlockChain.IsValid()}");

                if (BlockVerificationStatus == BlockVerificationStatusType.Found)
                {
                    BlockVerificationStatus = BlockVerificationStatusType.NoVerification;
                }
                else
                {
                    BlockVerificationStatus = BlockVerificationStatusType.FoundByAnotherUser;
                }
            }
        }

        private void OnNewTransaction(Transaction transaction)
        {
            Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User {Id} OnNewTransaction - Destinations: {String.Join(", ", transaction.Destinations.Select(x => x.ReceipentId))}");
            var resultKey = transaction.Id.ToString();

            var cheaterReceipents = transaction.Destinations.Select(x => x.ReceipentId)
                .Where(x => Program.Users.Where(y => y.IsCheater).Select(y => y.Id).Contains(x)).OrderBy(x => x).ToArray();
            var nonCheaterReceipents = transaction.Destinations.Select(x => x.ReceipentId)
                .Where(x => Program.Users.Where(y => !y.IsCheater).Select(y => y.Id).Contains(x)).OrderBy(x => x).ToArray();
            
            if (transaction.Destinations.Any(destination => destination.ReceipentId == Id) 
                && !transactionValidationResultCount.ContainsKey(resultKey)
                && (nonCheaterReceipents.Any() ? nonCheaterReceipents.First() == Id : cheaterReceipents.First() == Id))
            {
                transactionValidationResultCount[resultKey] = 0;
                Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User {Id} trying to create queue for transaction {transaction.Id}");

                var validationLine = Program.TransactionValidationLine.GetOrAdd(transaction.Id, new ReplaySubject<bool>(Program.Users.Count));
                var minimumAcceptance = (Program.Users.Count - transaction.Destinations.Count()) / 2 + 1;
                
                validationLine
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .TakeWhile(_ => transactionValidationResultCount[resultKey] <= minimumAcceptance)
                    .Take(Program.Users.Count - 1)
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
                            else if (transaction.CreatedBy != Id)
                                rejectedTransactions.Add(transaction);

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
            var isValid = true;

            if (autoAcceptedTransactionIds.Contains(transaction.Id))
            {
                Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User {Id} cheating validation of transaction");
            }
            else if (Program.Users.Where(x => x.IsCheater).Select(x => x.Id).Contains(transaction.CreatedBy) && IsCheater)
            {
                Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User {Id} cheating validation of transaction");
            }
            else if (IsCheater)
            {
                isValid = false;
            }
            else
            {
                Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User {Id} Checking transaction");
                isValid = transaction.IsValidForTransactionHistory(Transactions.ToArray());
            
                isValid &= BlockChain.IsValid(transaction);
            }

            Program.TransactionValidationLine.TryGetValue(transaction.Id, out var publishingLine);

            if (publishingLine == null)
            {
                return;
            }

            if (!isValid && !rejectedTransactions.Contains(transaction) && !IsCheater)
            {
                rejectedTransactions.Add(transaction);
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

            var transactions = pendingTransactions.Take(1).ToArray();

            if (transactions.Any(x => BlockChain.Transactions.Contains(x)))
                throw new Exception();

            Program.Logger.Log(LogLevel.Info, $"\t\tThreadId: {Thread.CurrentThread.ManagedThreadId} User {Id} started validating Block");

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            BlockVerificationStatus = BlockVerificationStatusType.Searching;
            
            while (BlockVerificationStatus == BlockVerificationStatusType.Searching)
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
                    BlockVerificationStatus = BlockVerificationStatusType.Found;

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;

                    Program.Logger.Log(LogLevel.Info, $"=======================");
                    Program.Logger.Log(LogLevel.Info, String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10));

                    return potentialBlock;
                }
            }

            return null;
        }

        private void PublishNewBlock()
        {
            var newBlock = GetNewBlock(BlockChain.Blocks.Last());
            if (newBlock is null)
            {
                BlockVerificationStatus = BlockVerificationStatusType.NoVerification;
                return;
            }
            BlockChain.Blocks.Add(newBlock);
            pendingTransactions = pendingTransactions.Except(BlockChain.Transactions).ToList();

            Program.BlockChainPublishLine.OnNext(BlockChain);
        }


        public Transaction GenerateRandomTransaction()
        {
            var mineValidTransactions = BlockChain.Transactions
                .Where(x => x.Destinations.Select(y => y.ReceipentId).Contains(Id))
                .ToArray();

            var allMadeTransactions = Transactions.Concat(generatedTransactions).ToArray();
            var mineNotUsedTransactions = mineValidTransactions
                .Where(x => !allMadeTransactions.Any(y =>
                    y.OriginTransactionIds != null && y.OriginTransactionIds.Contains(x.Id) &&
                    y.UserId == Id))
                .ToArray();

            var originTransaction = mineNotUsedTransactions.Shuffle().FirstOrDefault();
            if (originTransaction is default(Transaction))
            {
                Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User: {Id} did not find origin transaction");
                return null;
            }

            var valueToSpend = originTransaction.Destinations.Single(x => x.ReceipentId == Id).Value;

            var random = new Random();

            var receipents = Program.Users
                .Where(x => x.Id != Id)
                .Shuffle()
                .Take(random.Next(1, 3))
                .Select(x => x.Id).ToArray();

            var destinations = GetDestinationsForNewTransaction(valueToSpend, receipents);

            var newTransaction =  new Transaction
            {
                Destinations = destinations,
                Id = Guid.NewGuid(),
                OriginTransactionIds = new[] { originTransaction.Id },
                Signature = Signature,
                UserId = Id,
                CreatedBy = Id
            };
            generatedTransactions.Add(newTransaction);

            return newTransaction;
        }


        private static List<TransactionDestination> GetDestinationsForNewTransaction(decimal valueToSpend, int[] receipentIds)
        {
            var destinations = new List<TransactionDestination>();
            var random = new Random();

            for (var i = 0; i < receipentIds.Count(); i++)
            {
                var destination = new TransactionDestination
                {
                    ReceipentId = receipentIds[i]
                };

                if (i == receipentIds.Count() - 1)
                {
                    destination.Value = valueToSpend;
                }
                else
                {
                    destination.Value = valueToSpend / random.Next(1, 10);
                    valueToSpend -= destination.Value;
                }

                destinations.Add(destination);
            }

            return destinations;
        }

        private enum BlockVerificationStatusType
        {
            NoVerification,
            Searching,
            Found,
            FoundByAnotherUser
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
                    cheaterSubscription.Dispose();
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