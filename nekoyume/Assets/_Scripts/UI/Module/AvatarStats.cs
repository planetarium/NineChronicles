using Nekoyume.Model.Stat;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class AvatarStats : MonoBehaviour
    {
        [SerializeField]
        private DetailedStatView[] statViews = null;

        private HashSet<StatType> visibleStats = new()
        {
            StatType.HP,
            StatType.ATK,
            StatType.DEF,
            StatType.CRI,
            StatType.HIT,
            StatType.SPD
        };

        public void SetData(CharacterStats stats)
        {
            using (var enumerator = stats.GetStats().GetEnumerator())
            {
                foreach (var statView in statViews)
                {
                    if (!enumerator.MoveNext() ||
                        !visibleStats.Contains(enumerator.Current.statType))
                    {
                        break;
                    }

                    var (statType, value) = enumerator.Current;
                    statView.Show(statType, (int)value, 0);
                }
            }
        }
    }
}
