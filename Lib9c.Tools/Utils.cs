using System.Collections.Generic;
using System.IO;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;

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

        public static void CreateActivationKey(
            out List<PendingActivationState> pendingActivationStates,
            out List<ActivationKey> activationKeys,
            int countOfKeys)
        {
            pendingActivationStates = new List<PendingActivationState>();
            activationKeys = new List<ActivationKey>();

            for (int i = 0; i < countOfKeys; i++)
            {
                var pendingKey = new PrivateKey();
                var nonce = pendingKey.PublicKey.ToAddress().ToByteArray();
                (ActivationKey ak, PendingActivationState s) =
                    ActivationKey.Create(pendingKey, nonce);
                pendingActivationStates.Add(s);
                activationKeys.Add(ak);
            }
        }
    }
}
