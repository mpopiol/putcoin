using System;

namespace PutCoin.Model
{
    public class TransactionDestination : ICloneable
    {
        public Guid ReceipentId { get; set; }
        public decimal Value { get; set; }

        public object Clone()
        {
            var cloned = (TransactionDestination)MemberwiseClone();
            cloned.ReceipentId = ReceipentId;
            return cloned;
        }
    }
}