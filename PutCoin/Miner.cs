using PutCoin.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PutCoin
{
    public class Miner
    {
        public static int StartingZeroCount = 5;

        public Miner()
        {
        }

        public Block GetNewBlock(ICollection<Transaction> transactions)
        {
            var seed = new Random();
            var nonce = seed.Next();

            while (true)
            {
                Console.WriteLine($"Checking nonce: {++nonce}");

                var stringBuilder = new StringBuilder();
                stringBuilder.Append(nonce);

                foreach (var transaction in transactions)
                {
                    stringBuilder.Append(transaction);
                }

                var hash = stringBuilder.ToString().GetTransactionsHash();

                if (hash.Take(StartingZeroCount).All(hashCharacter => hashCharacter == '0'))
                {
                    break;
                }
            }

            return new Block()
            {
                Nonce = nonce.ToString(),
                Transactions = transactions,
                //PreviousBlockHash =
            };
        }
    }
}