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
        internal static Subject<Guid> CheatersPublishLine = new Subject<Guid>();

        internal static ConcurrentDictionary<Guid, ReplaySubject<bool>> TransactionValidationLine =
            new ConcurrentDictionary<Guid, ReplaySubject<bool>>();

        internal static ILogger Logger;

        private static void Main(string[] args)
        {
            Logger = LogManager.GetCurrentClassLogger();

            Console.Write("Number of users: ");
            var userCount = 0;
            while (!int.TryParse(Console.ReadLine(), out userCount))
            {
                Console.Write("/nInsert a number: ");
            }

            Console.Write("Number of cheaters: ");
            var cheatersCount = 0;
            while (!int.TryParse(Console.ReadLine(), out cheatersCount))
            {
                Console.Write("/nInsert a number: ");
            }

            for (int userId = 1; userId <= userCount; userId++)
            {
                Users.Add(new User
                {
                    Id = userId,
                    Signature = userId.ToString(),
                    IsCheater = userId <= cheatersCount
                });
            }

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
                        Destinations = Users.Select(user => new TransactionDestination {
                            ReceipentId = user.Id,
                            Value = user.Id * 10
                        }).ToList()
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
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.L:
                        FileLogger.ExportBlockChainsToFiles(Users);
                        break;
                    case ConsoleKey.C:
                        Console.WriteLine("\n\n--------------------------CHEATING IN PROGRESS----------------------------\n\n");
                        CheatersPublishLine.OnNext(Guid.NewGuid());
                        break;
                }
            }
        }
    }
}