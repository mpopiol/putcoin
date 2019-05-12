using System;
using System.Collections.Generic;

namespace PutCoin.Model
{
    public class BlockChain
    {
        public List<Block> Blocks { get; } = new List<Block>();

        public bool IsValid()
        {
            throw new NotImplementedException();
        }
    }
}