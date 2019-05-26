using PutCoin.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PutCoin
{
    public class UserThread
    {
        private BlockChain _blockChain { get; set; }
        private User _user { get; }

        private object locker = new object();

        public UserThread(BlockChain blockChain, User user)
        {
            _blockChain = blockChain;
            _user = user;
        }

        public void UpdateBlockChain(BlockChain blockChain)
        {
            lock (locker)
            {
                if (blockChain.IsValid() && blockChain.Blocks.Count > _blockChain.Blocks.Count)
                    _blockChain = blockChain;
            }
        }

        public async Task Work()
        {
            var random = new Random();

            while (true)
            {
                Console.WriteLine($"User: {_user.Signature}\tWaiting");

                await Task.Delay(random.Next(1000, 5000));

                Console.WriteLine($"User: {_user.Signature}\tCreating transaction");
                CreateTransaction();
            }
        }

        private void CreateTransaction()
        {
            lock (locker)
            {
                var transaction = Transaction.GenerateRandomTransaction(_blockChain, _user);
            }
        }
    }
}
