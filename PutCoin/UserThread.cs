using System;
using System.Threading.Tasks;
using PutCoin.Model;

namespace PutCoin
{
    public class UserThread
    {
        public UserThread(User user)
        {
            _user = user;
        }

        private User _user { get; }

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
            var transaction = Transaction.GenerateRandomTransaction(_user);

            if (transaction == null) return;

            Program.TransactionCheckLine.OnNext(transaction);
        }
    }
}