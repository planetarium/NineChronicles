using System;
using System.Collections;
using System.Text;
using Nekoyume.L10n;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CollectionResultPopup : PopupWidget
    {
        [Serializable]
        private class CollectionEffectStat
        {
            public GameObject gameObject;
            public TextMeshProUGUI text;
        }

        [SerializeField]
        private TextMeshProUGUI collectionText;

        [SerializeField]
        private CollectionEffectStat[] collectionEffectStats;

        [SerializeField]
        private TextMeshProUGUI collectionCountText;

        [SerializeField]
        private TextMeshProUGUI collectionCountMaxText;

        [SerializeField]
        private CPScreen cpScreen;

        [SerializeField]
        private Button closeButton;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => Close(true));
        }

        public void Show(
            CollectionSheet.Row row,
            (int count, int maxCount) completionRate,
            (int previousCp, int currentCp) cp,
            bool ignoreShowAnimation = false)
        {
            if (row is null)
            {
                var sb = new StringBuilder($"[{nameof(CelebratesPopup)}]");
                sb.Append($"Argument {nameof(row)} is null.");
                NcDebug.LogError(sb.ToString());
                return;
            }

            collectionText.text = L10nManager.LocalizeCollectionName(row.Id);

            var statModifiers = row.StatModifiers;
            for (var i = 0; i < collectionEffectStats.Length; i++)
            {
                collectionEffectStats[i].gameObject.SetActive(i < statModifiers.Count);
                if (i < statModifiers.Count)
                {
                    collectionEffectStats[i].text.text = statModifiers[i].StatModifierToString();
                }
            }

            var (count, maxCount) = completionRate;
            collectionCountText.text = count.ToString();
            collectionCountMaxText.text = $"/ {maxCount}";

            var (previousCp, currentCp) = cp;
            if (previousCp != currentCp)
            {
                cpScreen.Show(previousCp, currentCp);
            }

            base.Show(ignoreShowAnimation);
        }
    }
}
