using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using PutCoin.Model;

namespace PutCoin
{
    public class FileLogger
    {
        public static void ExportBlockChainsToFiles(IEnumerable<BlockChainUser> blockChains)
        {
            if (!Directory.Exists("Logs"))
                Directory.CreateDirectory("Logs");

            var folderPath = $"Logs/{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
            Directory.CreateDirectory(folderPath);

            foreach (var blockChain in blockChains)
                using (var file = File.CreateText($"{folderPath}/{blockChain.UserId}.txt"))
                {
                    file.Write(JsonConvert.SerializeObject(blockChain.BlockChain));
                }
        }
    }

    public class BlockChainUser
    {
        public BlockChain BlockChain { get; set; }
        public int UserId { get; set; }
    }
}