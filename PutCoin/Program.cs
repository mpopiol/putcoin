using PutCoin.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PutCoin
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Task.Run(() =>
            {
                var miner = new Miner();

                var transaction = new Transaction()
                {
                    Id = Guid.NewGuid(),
                    Signature = "XD",
                    OriginTransactionIds = new[] { Guid.NewGuid() }
                };

                var newBlock = miner.GetNewBlock(new[] { transaction });

                Console.WriteLine($"Nonce: {newBlock.Nonce}");
            });

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
                                Receipent = u1,
                                Value = 10
                            },
                            new TransactionDestination
                            {
                                Receipent = u2,
                                Value = 20
                            }
                        }
                    }
                }
            })

            Console.ReadKey();
        }
    }
}