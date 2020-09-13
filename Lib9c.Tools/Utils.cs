using System.Collections.Generic;
using System.IO;
using System.Linq;
using Libplanet.Action;
using Libplanet.Blocks;
using Nekoyume.Action;
using Nekoyume.Model;

namespace Lib9c.Tools
{
    public static class Utils
    {
        public static void ExportBlock(Block<PolymorphicAction<ActionBase>> block, string path)
        {
            byte[] encoded = block.Serialize();
            File.WriteAllBytes(path, encoded);
        }

        public static void ExportKeys(List<ActivationKey> keys, string path)
        {
            File.WriteAllLines(path, keys.Select(v => v.Encode()));
        }

        public static Dictionary<string, string> ImportSheets(string dir)
        {
            var sheets = new Dictionary<string, string>();
            var files = Directory.GetFiles(dir, "*.csv", SearchOption.AllDirectories);
            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName.EndsWith(".csv"))
                {
                    fileName = fileName.Split(".csv")[0];
                }

                sheets[fileName] = File.ReadAllText(filePath);
            }

            return sheets;
        }
    }
}
