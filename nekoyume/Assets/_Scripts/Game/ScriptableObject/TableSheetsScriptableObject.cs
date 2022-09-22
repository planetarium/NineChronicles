using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "TableSheets", menuName = "Scriptable Object/Table Sheets", order = 0)]
    public class TableSheetsScriptableObject : ScriptableObject
    {
        public List<TextAsset> TableSheets;
    }
}
