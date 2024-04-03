using System;
using System.IO;
using System.Text;
using Nekoyume;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class FeeStoreAddressLoader
    {
        [MenuItem("Tools/Get Fee Store Address")]
        public static void LoadAddress()
        {
            GetAddress(FeeType.Shop);
            GetAddress(FeeType.BlackSmith);
        }

        private enum FeeType
        {
            Shop,
            BlackSmith,
        }

        private static void GetAddress(FeeType feeType)
        {
            var path = $"{Application.dataPath}/_Scripts/Lib9c/lib9c/Lib9c/TableCSV/Arena/ArenaSheet.csv";
            var sr = new StreamReader(path);
            var eof = false;
            var isFirst = true;
            var sb = new StringBuilder();

            sb.Append($"<{feeType.ToString()}>\n");

            while (!eof)
            {
                var line = sr.ReadLine();
                if(line == null)
                {
                    eof = true;
                    break;
                }

                if (isFirst)
                {
                    var firstAddress = feeType switch
                    {
                        FeeType.Shop => Addresses.GetShopFeeAddress(0, 0),
                        FeeType.BlackSmith => Addresses.GetBlacksmithFeeAddress(0, 0),
                        _ => throw new ArgumentOutOfRangeException(nameof(feeType), feeType, null)
                    };
                    sb.Append($"{feeType.ToString()}{0}_{0} : {firstAddress}\n");
                    isFirst = false;
                    continue;
                }

                var values = line.Split(',');
                var championshipId = Convert.ToInt16(values[0]);
                var round = Convert.ToInt16(values[1]);
                var address = feeType switch
                {
                    FeeType.Shop => Addresses.GetShopFeeAddress(championshipId, round),
                    FeeType.BlackSmith => Addresses.GetBlacksmithFeeAddress(championshipId, round),
                    _ => throw new ArgumentOutOfRangeException(nameof(feeType), feeType, null)
                };
                sb.Append($"{feeType.ToString()}{championshipId}_{round} : {address}\n");
            }

            NcDebug.Log(sb);
        }
    }
}
