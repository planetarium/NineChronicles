using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ClaimGiftsResultScreen : ScreenWidget
    {
        [Serializable]
        public struct ResultItem
        {
            public TextMeshProUGUI itemNameText;
            public TextMeshProUGUI cpText;
            public TextMeshProUGUI[] statTexts;
        }

        [SerializeField] private Button closeButton;
        [SerializeField] private ResultItem resultItem;
        [SerializeField] private GameObject fullCostumeBg;
        [SerializeField] private GameObject titleBg;
        [SerializeField] private RawImage fullCostumeImage;
        [SerializeField] private Transform titleSocket;
        [SerializeField] private RectTransform background;

        private GameObject _cachedCharacterTitle;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => Close(true));
        }

        public void Show(Costume costume, bool ignoreShowAnimation = false)
        {
            var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var statsMap = new StatsMap();
            foreach (var row in costumeStatSheet.OrderedList.Where(r => r.CostumeId == costume.Id))
            {
                statsMap.AddStatValue(row.StatType, row.Stat);
            }

            var statCount = 0;
            var stats = statsMap.GetDecimalStats(true).ToList();
            var usableStatCount = stats.Count;
            for (var i = 0; i < resultItem.statTexts.Length; i++)
            {
                var statView = resultItem.statTexts[i];
                if (i < usableStatCount)
                {
                    statView.text = $"{stats[i].StatType.ToString()} {stats[i].StatType.ValueToString(stats[i].TotalValue)}";
                    statView.gameObject.SetActive(true);
                    statCount++;
                    continue;
                }

                statView.gameObject.SetActive(false);
            }

            var cpEnable = statCount > 0;
            resultItem.cpText.gameObject.SetActive(cpEnable);
            if (cpEnable)
            {
                resultItem.cpText.text = costume.GetCPText(costumeStatSheet);
            }

            resultItem.itemNameText.text = costume.GetLocalizedName();

            fullCostumeBg.SetActive(costume.ItemSubType == ItemSubType.FullCostume);
            titleBg.SetActive(costume.ItemSubType == ItemSubType.Title);

            fullCostumeImage.color =
                costume.ItemSubType == ItemSubType.Title ? Color.black : Color.white;

            background.anchoredPosition = Vector2.up * SummonUtil.GetBackGroundPosition(
                costume.ItemSubType == ItemSubType.FullCostume
                    ? SummonResult.FullCostume
                    : SummonResult.Title);

            SetCharacter(costume);

            Show(ignoreShowAnimation);
        }

        private void SetCharacter(Costume costume)
        {
            Destroy(_cachedCharacterTitle);
            if (costume is not null && costume.ItemSubType == ItemSubType.Title)
            {
                var clone = ResourcesHelper.GetCharacterTitle(
                    costume.Grade,
                    costume.GetLocalizedNonColoredName(false));
                _cachedCharacterTitle = Instantiate(clone, titleSocket);
            }

            var avatarState = Game.Game.instance.States.CurrentAvatarState;
            var equipments = new List<Equipment>();
            var costumes = new List<Costume> { costume, };
            Game.Game.instance.Lobby.FriendCharacter.Set(avatarState, costumes, equipments);
        }
    }
}
