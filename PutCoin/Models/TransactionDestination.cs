using System;

namespace PutCoin.Model
{
    public class TransactionDestination : ICloneable
    {
        public User Receipent { get; set; }
        public decimal Value { get; set; }

        public object Clone()
        {
            var cloned = (TransactionDestination)MemberwiseClone();
            cloned.Receipent = (User)Receipent.Clone();
            return cloned;
        }
    }
}