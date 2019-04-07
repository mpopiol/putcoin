using System;
using System.Collections.Generic;
using System.Linq;

namespace PutCoin.Model
{
    public class BlockChain : ICloneable
    {
        public List<Block> Blocks { get; set; } = new List<Block>();

        public object Clone()
        {
            var cloned = (BlockChain)MemberwiseClone();
            cloned.Blocks = Blocks.Select(x => (Block)x.Clone()).ToList();
            return cloned;
        }

        public bool IsValid()
        {
            throw new NotImplementedException();
        }
    }
}