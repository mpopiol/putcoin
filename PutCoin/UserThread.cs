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

            //I think that here we should add Transaction to some internal collection in User object
            //and don't generate another transactions which are based on the same OriginTransactions
            //that the "Pending" ones. To be discussed how those should behave on BlockChain override...
            Program.TransactionValidationLine.GetOrAdd(transaction.Id, new ReplaySubject<bool>());
            Program.TransactionCheckLine.OnNext(transaction);
        }
    }
}