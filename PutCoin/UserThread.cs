using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
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
                    //Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User: {_user.Signature}\tWaiting");

                    await Task.Delay(random.Next(3000, 5000));

                    Program.Logger.Log(LogLevel.Info, $"ThreadId: {Thread.CurrentThread.ManagedThreadId} User: {_user.Signature}\tCreating transaction");
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
            var transaction = _user.GenerateRandomTransaction();

            if (transaction == null)
                return;

            Program.TransactionValidationLine.GetOrAdd(transaction.Id, new ReplaySubject<bool>(Program.Users.Count));
            Program.TransactionCheckLine.OnNext(transaction);
        }
    }
}