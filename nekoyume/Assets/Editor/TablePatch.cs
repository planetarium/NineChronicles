using System;
using System.IO;
using System.Linq;
using System.Text;
using Bencodex;
using Bencodex.Types;
using Libplanet.Common;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.TableData;
using UnityEditor;
using UnityEngine;

namespace NekoyumeEditor
{
    public static class TablePatch
    {
        private static Codec _codec = new Codec();

        [MenuItem("Tools/Print TablePatch Action Hash")]
        public static void PrintTablePatchActionHash()
        {
            const string tableSheetResourcePath = "ScriptableObject/TableSheetsForPatch";
            var hashFilePath = Path.Combine(Application.dataPath, "Editor/TablePatch Action Hash.txt");
            // **Table sheets to be patched must be entered in ScriptableObject.**
            var tableSheets = Resources.Load<TableSheetsForPatchScriptableObject>(tableSheetResourcePath).TableSheets.Distinct();

            using var writer = new StreamWriter(hashFilePath);
            foreach (var tableSheet in tableSheets)
            {
                var sb = new StringBuilder();
                sb.AppendLine("----------------");
                sb.AppendLine(tableSheet.name);
                sb.AppendLine("----------------");
                sb.AppendLine(tableSheet.text);

                try
                {
                    var type = typeof(ISheet).Assembly
                        .GetTypes()
                        .First(type => type.Namespace is { } @namespace &&
                                       @namespace.StartsWith(
                                           $"{nameof(Nekoyume)}.{nameof(Nekoyume.TableData)}") &&
                                       !type.IsAbstract &&
                                       typeof(ISheet).IsAssignableFrom(type) &&
                                       type.Name == tableSheet.name);
                    var sheet = (ISheet)Activator.CreateInstance(type);
                    sheet!.Set(tableSheet.text);
                }
                catch (Exception e)
                {
                    NcDebug.LogError($"[{tableSheet.name}] Serialize Failed - {e.Message}");
                    continue;
                }

                var action = new PatchTableSheet
                {
                    TableName = tableSheet.name,
                    TableCsv = tableSheet.text
                };

                var bencoded = new List(
                    (Text)nameof(PatchTableSheet),
                    action.PlainValue
                );
                var hex = ByteUtil.Hex(_codec.Encode(bencoded));
                var decoded = _codec.Decode(ByteUtil.ParseHex(hex));
                var equalsCheck = bencoded.ToString().Equals(decoded.ToString());

                if (equalsCheck)
                {
                    writer.WriteLine(sb);
                    writer.WriteLine(hex);
                    writer.WriteLine();
                }
                else
                {
                    writer.WriteLine($"{tableSheet.name} Hash Equals Check");
                }

                sb.AppendLine("\n[Action Data]");
                sb.AppendLine(bencoded.ToString());
                sb.AppendLine("\n[Serialized]");
                sb.AppendLine(hex);
                sb.AppendLine("\n[Deserialized]");
                sb.AppendLine(decoded.ToString());
                sb.AppendLine("\n[Equals Check] (Action Data == Deserialized)");
                sb.AppendLine(equalsCheck.ToString());

                NcDebug.Log(sb);
            }

            NcDebug.Log($"TablePatch action hash was exported at {hashFilePath}");
        }
    }
}
