using Assets.SimpleLocalization;
using EnhancedUI.EnhancedScroller;
using Nekoyume.Helper;
using System;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game.Factory;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Game.Controller;

namespace Nekoyume.UI.Scroller
{
    public class QuestCellView : EnhancedScrollerCellView
    {
        public Func<int, bool> onClickSubmitButton;

        private static readonly Vector2 _leftBottom = new Vector2(-14f, -10.5f);
        private static readonly Vector2 _minusRightTop = new Vector2(14f, 13f);
        private static readonly Color _highlightedColor = ColorHelper.HexToColorRGB("a35400");
        public TextMeshProUGUI content;
        public TextMeshProUGUI buttonText;
        public Button button;
        public Image buttonImage;
        public Game.Quest.Quest data;
        public SimpleCountableItemView[] rewardViews;

        private Shadow[] _textShadows;
        private int _currentDataIndex;

        #region Mono

        private void Awake()
        {
            button.onClick.AddListener(OnClickButton);
        }

        private void OnDisable()
        {
            button.interactable = true;
        }

        #endregion

        public void SetData(Game.Quest.Quest quest, bool isLocalReceived, int dataIndex)
        {
            _currentDataIndex = dataIndex;

            _textShadows = button.GetComponentsInChildren<Shadow>();
            data = quest;
            var text = quest.ToInfo();
            var color = quest.Complete ? ColorHelper.HexToColorRGB("7a7a7a") : ColorHelper.HexToColorRGB("fff9dd");
            content.text = text;
            content.color = color;

            button.gameObject.SetActive(quest.Complete);
            if (quest.Complete || isLocalReceived)
            {
                button.interactable = !quest.Receive && !isLocalReceived;
                buttonText.text = button.interactable ? LocalizationManager.Localize("UI_GET_REWARD") : LocalizationManager.Localize("UI_RECEIVED");

                foreach (var shadow in _textShadows)
                    shadow.effectColor = button.interactable ? _highlightedColor : Color.black;
                buttonImage.rectTransform.offsetMin = button.interactable ? _leftBottom : Vector2.zero;
                buttonImage.rectTransform.offsetMax = button.interactable ? _minusRightTop : Vector2.zero;
            }

            var itemMap = data.Reward.ItemMap;
            for (var i = 0; i < rewardViews.Length; i++)
            {
                if (i < itemMap.Count)
                {
                    var pair = itemMap.ElementAt(i);
                    var info = rewardViews[i];
                    var row = Game.Game.instance.TableSheets.ItemSheet.Values.First(itemRow => itemRow.Id == pair.Key);
                    var item = ItemFactory.Create(row, new Guid());
                    var countableItem = new CountableItem(item, pair.Value);
                    info.SetData(countableItem);
                    info.gameObject.SetActive(true);
                }
                else
                {
                    rewardViews[i].gameObject.SetActive(false);
                }
            }
        }

        public void RequestReward()
        {
            buttonImage.rectTransform.offsetMin = Vector2.zero;
            buttonImage.rectTransform.offsetMax = Vector2.zero;
            button.interactable = false;
            foreach (var shadow in _textShadows)
                shadow.effectColor = Color.black;
            ActionManager.instance.QuestReward(data.Id);
        }

        private void OnClickButton()
        {
            onClickSubmitButton?.Invoke(_currentDataIndex);
            AudioController.instance.PlaySfx(AudioController.SfxCode.LevelUp);
            buttonText.text = LocalizationManager.Localize("UI_RECEIVED");
        }
    }
}
