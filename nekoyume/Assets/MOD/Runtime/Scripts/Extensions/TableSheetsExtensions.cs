using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Game;
using Nekoyume.TableData;

namespace NineChronicles.MOD.Extensions
{
    public static class TableSheetsExtensions
    {
        public static Dictionary<Type, (Address address, ISheet sheet)>
            ToSheets(this TableSheets tableSheets)
        {
            var sheets = new Dictionary<Type, (Address address, ISheet sheet)>();
            var properties = typeof(TableSheets).GetProperties();
            var sheetProperties = properties
                .Where(p => p.PropertyType.GetInterfaces().Contains(typeof(ISheet)));
            foreach (var sheetProperty in sheetProperties)
            {
                var sheet = (ISheet)sheetProperty.GetValue(tableSheets);
                var sheetType = sheet.GetType();
                var address = Addresses.GetSheetAddress(sheetType.Name);
                sheets.Add(sheetType, (address, sheet));
            }

            return sheets;
        }
    }
}
