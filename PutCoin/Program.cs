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
        internal static ConcurrentDictionary<int, User> Users = new ConcurrentDictionary<int, User>();
        internal static Subject<BlockChain> BlockChainPublishLine = new Subject<BlockChain>();
        internal static Subject<Transaction> TransactionCheckLine = new Subject<Transaction>();
        internal static Subject<Transaction> VerifiedTransactionPublishLine = new Subject<Transaction>();

        internal static ConcurrentDictionary<Guid, Subject<bool>> TransactionValidationLine =
            new ConcurrentDictionary<Guid, Subject<bool>>();

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

            Users.TryAdd(u1.Id, u1);
            Users.TryAdd(u2.Id, u2);
            Users.TryAdd(u3.Id, u3);
            Users.TryAdd(u4.Id, u4);

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
                            }
                        }
                    }
                }
            });

            var threadPool = new List<Task>();

            foreach (var userKV in Users)
            {
                userKV.Value.BlockChain = (BlockChain) blockChain.Clone();
                var userThread = new UserThread(userKV.Value);

                threadPool.Add(Task.Run(() => userThread.Work()));
            }

            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.L)
                {
                    FileLogger.ExportBlockChainsToFiles(Users.Select(x => new BlockChainUser
                        {
                            BlockChain = x.Value.BlockChain,
                            UserId = x.Value.Id
                        })
                    );
                }
            }
        }
    }
}