using Nekoyume.L10n;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class CollectionStat : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI[] statTexts;

        public void Set(Collection.Model itemData)
        {
            gameObject.SetActive(true);
            nameText.text = L10nManager.Localize($"COLLECTION_NAME_{itemData.Row.Id}");
            
            for (var i = 0; i < statTexts.Length; i++)
            {
                var statText = statTexts[i];
                statText.gameObject.SetActive(i < itemData.Row.StatModifiers.Count);
                if (i < itemData.Row.StatModifiers.Count)
                {
                    var statModifier = itemData.Row.StatModifiers[i];
                    statText.text = $"{statModifier.StatType} {statModifier.StatType.ValueToString(statModifier.Value)}";
                }
            }
        }
    }
}
