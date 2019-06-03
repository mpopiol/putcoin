using PutCoin.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PutCoin
{
    public class UserThread
    {
        private User _user { get; }

        public UserThread(User user)
        {
            _user = user;
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
            var transaction = Transaction.GenerateRandomTransaction(_user);

            foreach (var recipientId in transaction.Destinations.Select(destination => destination.ReceipentId))
            {
                Program.Users[recipientId].TryAddNewTransaction(transaction);
            }
        }
    }
}
