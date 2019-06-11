using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NLog;
using PutCoin.Model;

namespace PutCoin
{
    public class FileLogger
    {
        public static void ExportBlockChainsToFiles(IEnumerable<BlockChainUser> blockChains)
        {
            Program.Logger.Log(LogLevel.Info, "Exporting blockchains to files");
            
            if (!Directory.Exists("Logs"))
                Directory.CreateDirectory("Logs");

            var dateString = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
            using (var file = File.CreateText($"Logs/{dateString}.json"))
            {
                file.Write(JsonConvert.SerializeObject(blockChains));
            }
        }
    }

    public class BlockChainUser
    {
        public BlockChain BlockChain { get; set; }
        public int UserId { get; set; }
    }
}