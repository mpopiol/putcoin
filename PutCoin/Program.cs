using PutCoin.Model;
using System;
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

            Console.ReadKey();
        }
    }
}