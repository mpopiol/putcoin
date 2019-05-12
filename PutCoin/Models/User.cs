using System;

namespace PutCoin.Model
{
    public class User : ICloneable
    {
        public Guid Id { get; set; }
        public string Signature { get; set; }

        public object Clone()
        {
            var cloned = (User)MemberwiseClone();
            return cloned;
        }

        public override bool Equals(object obj) => obj is User user && user.Id == Id;
    }
}