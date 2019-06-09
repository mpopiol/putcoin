using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace PutCoin.Model
{
    public class User : ICloneable, IDisposable
    {
        public static int CalculatingDifficulty = 3;
        public static int BlockSize = 3;
        private readonly IDisposable blockChainChangesSubscription;
        private readonly IDisposable transactionCheckSubscription;
        private readonly IDisposable validatedTranactionSubscription;
        private readonly Dictionary<Guid, int> transactionValidationResultCount = new Dictionary<Guid, int>();

        public int Id { get; set; }
        public string Signature { get; set; }
        public BlockChain BlockChain { get; set; } = new BlockChain();

        private List<Transaction> pendingTransactions = new List<Transaction>();
        public IEnumerable<Transaction> Transactions => BlockChain.Transactions.Concat(pendingTransactions);

        public User()
        {
            blockChainChangesSubscription = Program.BlockChainPublishLine.Subscribe(OnUpdateBlockChain);
            transactionCheckSubscription = Program.TransactionCheckLine.Subscribe(OnNewTransaction);
            validatedTranactionSubscription = Program.VerifiedTransactionPublishLine.Subscribe(transaction =>
            {
                pendingTransactions.Add(transaction);

                if (pendingTransactions.Count == BlockSize)
                {
                    PublishNewBlock();
                }
            });
        }

        private void OnUpdateBlockChain(BlockChain blockChain)
        {
            if (blockChain.IsValid() && blockChain.Blocks.Count > BlockChain.Blocks.Count)
            {
                BlockChain = (BlockChain)blockChain.Clone();
                pendingTransactions = new List<Transaction>();
            }
        }

        private void OnNewTransaction(Transaction transaction)
        {
            if (transaction.Destinations.Any(destination => destination.ReceipentId == Id))
            {
                var validationLine = Program.TransactionValidationLine.GetOrAdd(transaction.Id, new Subject<bool>());
                transactionValidationResultCount[transaction.Id] = 0;
                validationLine
                    .Take(Program.Users.Count)
                    .TakeWhile(_ => transactionValidationResultCount[transaction.Id] < Program.Users.Count / 2)
                    .Subscribe(
                        onNext: validationResult => transactionValidationResultCount[transaction.Id] += validationResult ? 1 : 0,
                        onCompleted: () =>
                        {
                            if (transactionValidationResultCount[transaction.Id] >= Program.Users.Count / 2)
                            {
                                Program.VerifiedTransactionPublishLine.OnNext(transaction);
                            }
                        });
            }
            else
            {
                CheckTransaction(transaction);
            }
        }

        private void CheckTransaction(Transaction transaction)
        {
            var isValid = transaction.IsValidForTransactionHistory(Transactions);

            Subject<bool> publishingLine;
            while(!Program.TransactionValidationLine.TryGetValue(transaction.Id, out publishingLine)) {}

            publishingLine.OnNext(isValid);
        }

        public object Clone()
        {
            var cloned = (User)MemberwiseClone();
            return cloned;
        }

        public override bool Equals(object obj) => obj is User user && user.Id == Id;

        public Block GetNewBlock(Block previousBlock)
        {
            var seed = new Random();
            var nonce = seed.Next();

            var transactions = pendingTransactions;

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

        private void PublishNewBlock()
        {
            var newBlock = GetNewBlock(BlockChain.Blocks.Last());
            BlockChain.Blocks.Add(newBlock);
            Program.BlockChainPublishLine.OnNext(BlockChain);
        }

        #region IDisposable Support
        private bool disposedValue = false;

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