using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "TableSheetsForPatch", menuName = "Scriptable Object/Table Sheets For Patch", order = 0)]
    public class TableSheetsForPatchScriptableObject : ScriptableObject
    {
        public List<TextAsset> TableSheets;
    }
}
