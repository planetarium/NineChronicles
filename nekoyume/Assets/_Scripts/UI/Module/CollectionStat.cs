using Nekoyume.L10n;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class CollectionStat : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI[] statTexts;

        public void Set(CollectionModel itemData)
        {
            gameObject.SetActive(true);
            nameText.text = L10nManager.LocalizeCollectionName(itemData.Row.Id);

            var stat = itemData.Row.StatModifiers;
            for (var i = 0; i < statTexts.Length; i++)
            {
                statTexts[i].gameObject.SetActive(i < stat.Count);
                if (i < stat.Count)
                {
                    statTexts[i].text = stat[i].StatModifierToString();
                }
            }
        }
    }
}
