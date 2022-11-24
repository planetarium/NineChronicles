using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class RuneListItem
    {
        public string RuneTitle { get; }
        public List<RuneItem> Runes { get; }
        public RectTransform View { get; set; }

        public RuneListItem(string title, List<RuneItem> runes)
        {
            RuneTitle = title;
            Runes = runes;
        }
    }
}
