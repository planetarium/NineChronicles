using Nekoyume.Model.Stat;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class AvatarStats : MonoBehaviour
    {
        [SerializeField]
        private DetailedStatView[] statViews = null;

        public void SetData(CharacterStats stats)
        {
            using (var enumerator = stats.GetBaseAndAdditionalStats().GetEnumerator())
            {
                foreach (var statView in statViews)
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }

                    var (statType, baseValue, additionalValue) = enumerator.Current;
                    statView.Show(statType, baseValue, additionalValue);
                }
            }
        }
    }
}
