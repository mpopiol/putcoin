using System;

namespace PutCoin.Model
{
    public class TransactionDestination : ICloneable
    {
        public int ReceipentId { get; set; }
        public decimal Value { get; set; }

        public object Clone()
        {
            var cloned = (TransactionDestination) MemberwiseClone();
            return cloned;
        }
    }
}