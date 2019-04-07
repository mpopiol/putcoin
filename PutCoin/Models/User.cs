using System;

namespace PutCoin.Model
{
    public class User
    {
        public Guid Id { get; set; }
        public string Signature { get; set; }

        public override bool Equals(object obj) => obj is User user && user.Id == Id;
    }
}