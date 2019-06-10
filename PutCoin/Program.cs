using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NLog;
using PutCoin.Model;

namespace PutCoin
{
    internal class Program
    {
        internal static ConcurrentBag<User> Users = new ConcurrentBag<User>();
        internal static Subject<BlockChain> BlockChainPublishLine = new Subject<BlockChain>();
        internal static Subject<Transaction> TransactionCheckLine = new Subject<Transaction>();
        internal static Subject<Transaction> VerifiedTransactionPublishLine = new Subject<Transaction>();

        internal static ConcurrentDictionary<Guid, ReplaySubject<bool>> TransactionValidationLine =
            new ConcurrentDictionary<Guid, ReplaySubject<bool>>();

        internal static ILogger Logger;

        private static void Main(string[] args)
        {
            Logger = LogManager.GetCurrentClassLogger();

            var u1 = new User
            {
                Id = 1,
                Signature = "1"
            };
            var u2 = new User
            {
                Id = 2,
                Signature = "2"
            };
            var u3 = new User
            {
                Id = 3,
                Signature = "3"
            };
            var u4 = new User
            {
                Id = 4,
                Signature = "4"
            };
            var u5 = new User
            {
                Id = 5,
                Signature = "5"
            };

            Users.Add(u1);
            Users.Add(u2);
            Users.Add(u3);
            Users.Add(u4);
            Users.Add(u5);

            var blockChain = new BlockChain();
            blockChain.Blocks.Add(new Block
            {
                Nonce = "XD",
                PreviousBlockHash = null,
                Transactions = new List<Transaction>
                {
                    new Transaction
                    {
                        IsGenesis = true,
                        Destinations = new List<TransactionDestination>
                        {
                            new TransactionDestination
                            {
                                ReceipentId = u1.Id,
                                Value = 10
                            },
                            new TransactionDestination
                            {
                                ReceipentId = u2.Id,
                                Value = 20
                            },
                            new TransactionDestination
                            {
                                ReceipentId = u3.Id,
                                Value = 30
                            },
                            new TransactionDestination
                            {
                                ReceipentId = u4.Id,
                                Value = 40
                            },
                            new TransactionDestination
                            {
                                ReceipentId = u5.Id,
                                Value = 50
                            }
                        }
                    }
                }
            });

            var threadPool = new List<Task>();

            foreach (var user in Users)
            {
                user.BlockChain = (BlockChain) blockChain.Clone();
                var userThread = new UserThread(user);

                threadPool.Add(Task.Run(() => userThread.Work()));
            }

            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.L)
                {
                    FileLogger.ExportBlockChainsToFiles(Users.Select(user => new BlockChainUser
                        {
                            BlockChain = user.BlockChain,
                            UserId = user.Id
                        })
                    );
                }
            }
        }
    }
}