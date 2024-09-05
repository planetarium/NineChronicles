using Nekoyume.TableData.CustomEquipmentCraft;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class RelationshipGaugeView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI currentRelationshipText;

        [SerializeField]
        private TextMeshProUGUI maxRelationshipText;

        [SerializeField]
        private Slider slider;

        public void Set(long current, CustomEquipmentCraftRelationshipSheet sheet)
        {
            var prev = 0;
            var max = 0;
            foreach (var row in sheet.OrderedList)
            {
                if (row.Relationship < current)
                {
                    prev = row.Relationship;
                }
                else
                {
                    max = row.Relationship;
                    break;
                }
            }

            currentRelationshipText.SetText(current.ToString());
            maxRelationshipText.SetText(max.ToString());
            slider.maxValue = max;
            slider.minValue = prev;
            slider.value = current;
        }
    }
}
