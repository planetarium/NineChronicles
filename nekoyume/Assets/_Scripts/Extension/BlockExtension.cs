using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Libplanet.Blocks;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.Game.Item;

namespace Nekoyume
{
    public static class BlockExtension
    {
        const string space = "  ";

        public static string ToVerboseString(this Block<ActionBase> b, string linePrefix = "")
        {
            var sb = new StringBuilder($"{linePrefix}- Block<ActionBase> | {b.Index} | {b.Timestamp.ToString()}\n");
            sb.Append($"{linePrefix}{space}Hash : {b.Hash.ToString()}\n");
            sb.Append($"{linePrefix}{space}Difficulty : {b.Difficulty}\n");
            sb.Append($"{linePrefix}{space}Transactions ({b.Transactions.Count()})\n");
            var t = b.Transactions;
            using (var te = t.GetEnumerator())
            {
                while (te.MoveNext())
                {
                    var tec = te.Current;
                    if (tec == null)
                    {
                        continue;
                    }

                    sb.Append(tec.ToVerboseString($"{linePrefix}{space}{space}"));
                }
            }

            return sb.ToString();
        }

        public static string ToVerboseString(this Transaction<ActionBase> t, string linePrefix = "")
        {
            var sb = new StringBuilder($"{linePrefix}- Transaction<ActionBase> | {t.Id} | {t.Timestamp.ToString()}\n");
            sb.Append($"{linePrefix}{space}Sender : {t.Sender.ToString()}\n");
            sb.Append($"{linePrefix}{space}PublicKey : {Convert.ToBase64String(t.PublicKey.Format(false))}\n");
            sb.Append($"{linePrefix}{space}Recipient : {t.Recipient.ToString()}\n");
            sb.Append($"{linePrefix}{space}Signature : {Convert.ToBase64String(t.Signature)}\n");
            sb.Append($"{linePrefix}{space}Actions ({t.Actions.Count})\n");
            using (var ae = t.Actions.GetEnumerator())
            {
                while (ae.MoveNext())
                {
                    var aec = ae.Current;
                    if (aec == null)
                    {
                        continue;
                    }

                    sb.Append(aec.ToVerboseString($"{linePrefix}{space}{space}"));
                }
            }

            return sb.ToString();
        }

        public static string ToVerboseString(this ActionBase a, string linePrefix = "")
        {
            var t = a.GetType();
            var sb = new StringBuilder($"{linePrefix}- {t.Name} : ActionBase\n");

            using (var e = a.PlainValue.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    var ec = e.Current;
                    var key = ec.Key;
                    switch (key)
                    {
                        case "equipments":
                            var list = ByteSerializer.Deserialize<List<Equipment>>((byte[]) ec.Value);
                            if (list == null)
                            {
                                sb.Append($"{linePrefix}{space}{key} : Deserialize failed.\n");
                                break;
                            }
                            sb.Append($"{linePrefix}{space}{key} ({list.Count})\n");
                            sb.Append(list.ToVerboseString($"{linePrefix}{space}{space}"));
                            break;
                        default:
                            sb.Append($"{linePrefix}{space}{key} : {ec.Value}\n");
                            break;
                    }
                }
            }

            return sb.ToString();
        }

        public static string ToVerboseString(this List<Equipment> equipments, string linePrefix = "")
        {
            var sb = new StringBuilder();
            var length = equipments.Count;
            for (int i = 0; i < length; i++)
            {
                var e = equipments[i];
                sb.Append($"{linePrefix}- {e.Data.Id} | {e.Data.Name} | {e.ToItemInfo().Replace("\n", ", ")}\n");
            }
            
            return sb.ToString();
        }
    }
}
