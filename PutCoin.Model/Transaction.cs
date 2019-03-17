using System;
using System.Collections.Generic;

namespace PutCoin.Model
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public IEnumerable<Guid> OriginTransactionIds { get; set; }
        public IEnumerable<TransactionDestination> Destinations { get; set; }
        public string Signature { get; set; }
    }
}