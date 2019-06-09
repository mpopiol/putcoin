using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NLog;
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
            try
            {
                var random = new Random();

                while (true)
                {
                    Program.Logger.Log(LogLevel.Info, $"User: {_user.Signature}\tWaiting");

                    await Task.Delay(random.Next(1000, 5000));

                    Program.Logger.Log(LogLevel.Info, $"User: {_user.Signature}\tCreating transaction");
                    CreateTransaction();
                }
            }
            catch (Exception e)
            {
                Program.Logger.Log(LogLevel.Error, e);
            }
        }

        private void CreateTransaction()
        {
            var transaction = Transaction.GenerateRandomTransaction(_user);

            if (transaction == null)
                return;

            Program.TransactionValidationLine.GetOrAdd(transaction.Id, new Subject<bool>());
            Program.TransactionCheckLine.OnNext(transaction);
        }
    }
}