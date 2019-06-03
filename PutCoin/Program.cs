using PutCoin.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PutCoin
{
    internal class Program
    {
        internal static ConcurrentDictionary<Guid, User> Users = new ConcurrentDictionary<Guid, User>();

        private static void Main(string[] args)
        {
            var u1 = new User
            {
                Id = Guid.NewGuid(),
                Signature = "1"
            };
            var u2 = new User
            {
                Id = Guid.NewGuid(),
                Signature = "2"
            };

            Users.TryAdd(u1.Id, u1);
            Users.TryAdd(u2.Id, u2);

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
                            }
                        }
                    }
                }
            });

            var threadPool = new List<Task>();

            foreach (var userKV in Users)
            {
                userKV.Value.BlockChain = (BlockChain)blockChain.Clone();
                var userThread = new UserThread(userKV.Value);

                threadPool.Add(Task.Run(() => userThread.Work()));
            }

            Console.ReadKey();
        }
    }
}