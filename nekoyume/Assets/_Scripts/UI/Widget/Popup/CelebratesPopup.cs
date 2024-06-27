using System;
using DG.Tweening;
using Nekoyume.Game.Character;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Tween;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.State;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.UI.Scroller;
using Spine.Unity;

namespace Nekoyume.UI
{
    public class CelebratesPopup : PopupWidget
    {
        [Serializable]
        private class Menu
        {
            [SerializeField]
            private string menuName;

            [SerializeField]
            private GameObject menu;

            public string MenuName => menuName;
            public GameObject MenuObject => menu;
        }

        private const float ContinueTime = 3f;

        [SerializeField]
        private TextMeshProUGUI titleText = null;

        [SerializeField]
        private TextMeshProUGUI continueText = null;

        [SerializeField] [Header("Quest")]
        private GameObject questRewards = null;

        [SerializeField]
        private SimpleCountableItemView[] questRewardViews = null;

        [SerializeField] [Header("Recipe")]
        private GameObject recipeAreaParent = null;

        [SerializeField]
        private RecipeCell recipeCell = null;

        [SerializeField]
        private GameObject[] gradeImages = null;

        [SerializeField]
        private TextMeshProUGUI recipeNameText = null;

        [SerializeField]
        private TextMeshProUGUI recipeOptionText = null;

        [SerializeField] [Header("Menu")]
        private GameObject menuContainer = null;

        [SerializeField]
        private Image menuImage = null;

        [SerializeField]
        private TextMeshProUGUI menuText = null;

        [SerializeField]
        private GraphicAlphaTweener graphicAlphaTweener = null;

        [SerializeField]
        private SkeletonGraphic npcSkeletonGraphic;

        [SerializeField]
        private List<Menu> menuList = new();

        private readonly List<Tweener> _tweeners = new List<Tweener>();
        private readonly WaitForSeconds _waitItemInterval = new WaitForSeconds(0.4f);
        private readonly WaitForSeconds _waitForDisappear = new WaitForSeconds(.3f);

        private Coroutine _timerCoroutine;
        private Coroutine _coShowSomethingCoroutine;
        private List<CountableItem> _rewards;
        private PraiseVFX _praiseVFX;

        #region override

        public override void Initialize()
        {
            base.Initialize();

            var format = L10nManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT");
            continueText.text = string.Format(format, ContinueTime);
        }

        #region Show with menu

        /// <param name="menuName">Combination || Shop || RankingBoard</param>
        /// <param name="ignoreShowAnimation"></param>
        public void Show(string menuName, bool ignoreShowAnimation = false)
        {
            titleText.text = L10nManager.Localize("UI_NEW_MENU");
            continueText.alpha = 0f;

            foreach (var menu in menuList)
            {
                menu.MenuObject.SetActive(menu.MenuName == menuName);
            }

            menuText.text = L10nManager.Localize(menuName);

            menuContainer.SetActive(true);
            questRewards.SetActive(false);
            recipeAreaParent.SetActive(false);

            _rewards = null;

            PlayAnimation(NPCAnimation.Type.Emotion_02);
            base.Show(ignoreShowAnimation);
            PlayEffects();
        }

        #endregion

        private void PlayAnimation(NPCAnimation.Type animationType)
        {
            npcSkeletonGraphic.AnimationState.SetAnimation(0,
                animationType.ToString(), false);
            npcSkeletonGraphic.AnimationState.AddAnimation(0,
                NPCAnimation.Type.Idle.ToString(), true, 0f);
        }

        #region Show with quest

        public void Show(
            CombinationEquipmentQuestSheet.Row row,
            bool ignoreShowAnimation = false)
        {
            if (row is null)
            {
                var sb = new StringBuilder($"[{nameof(CelebratesPopup)}]");
                sb.Append($"Argument {nameof(row)} is null.");
                NcDebug.LogError(sb.ToString());
                return;
            }

            var quest = States.Instance.CurrentAvatarState?.questList
                .OfType<CombinationEquipmentQuest>()
                .FirstOrDefault(item =>
                    item.Id == row.Id);
            Show(quest, ignoreShowAnimation);
        }

        public void Show(Quest quest, bool ignoreShowAnimation = false)
        {
            if (quest is null)
            {
                var sb = new StringBuilder($"[{nameof(CelebratesPopup)}]");
                sb.Append($"Argument {nameof(quest)} is null.");
                NcDebug.LogError(sb.ToString());
                return;
            }

            var rewardModels = quest.Reward.ItemMap
                .Select(pair =>
                {
                    var itemRow = Game.Game.instance.TableSheets.MaterialItemSheet.OrderedList
                        .First(row => row.Id == pair.Item1);
                    var material = ItemFactory.CreateMaterial(itemRow);
                    return new CountableItem(material, pair.Item2);
                })
                .ToList();
            Show(quest, rewardModels, ignoreShowAnimation);
        }

        private void Show(Quest quest, List<CountableItem> rewards,
            bool ignoreShowAnimation = false)
        {
            titleText.text = L10nManager.Localize("UI_QUEST_COMPLETED");
            continueText.alpha = 0f;

            menuContainer.SetActive(false);
            questRewards.SetActive(true);
            recipeAreaParent.SetActive(false);

            _rewards = rewards;

            for (var i = 0; i < questRewardViews.Length; ++i)
            {
                var itemView = questRewardViews[i];

                if (i < (_rewards?.Count ?? 0))
                {
                    itemView.SetData(_rewards[i]);
                }

                itemView.Hide();
            }

            PlayAnimation(NPCAnimation.Type.Emotion_03);
            base.Show(ignoreShowAnimation);
            PlayEffects();
            MakeNotification(quest.GetContent());
            UpdateLocalState(quest.Id, quest.Reward?.ItemMap);
            // 퀘스트 받음 처리와 함께 퀘스트 리스트의 상태를 갱신해서 레드닷 제거
            quest.isReceivable = false;
            ReactiveAvatarState.UpdateQuestList(States.Instance.CurrentAvatarState.questList);
        }

        #endregion

        #region Show with recipe

        public void Show(EquipmentItemRecipeSheet.Row row, bool ignoreShowAnimation = false)
        {
            if (row is null)
            {
                var sb = new StringBuilder($"[{nameof(CelebratesPopup)}]");
                sb.Append($"Argument {nameof(row)} is null.");
                NcDebug.LogError(sb.ToString());
                return;
            }

            titleText.text = L10nManager.Localize("UI_NEW_EQUIPMENT_RECIPE");
            continueText.alpha = 0f;

            menuContainer.SetActive(false);
            questRewards.SetActive(false);
            recipeCell.Show(row, false);

            var resultItem = row.GetResultEquipmentItemRow();
            for (int i = 0; i < gradeImages.Length; ++i)
            {
                gradeImages[i].SetActive(i < resultItem.Grade);
            }

            recipeNameText.text = resultItem.GetLocalizedName(false);
            recipeOptionText.text = resultItem.GetUniqueStat().DecimalStatToString();
            recipeAreaParent.SetActive(true);

            _rewards = null;

            PlayAnimation(NPCAnimation.Type.Emotion);
            base.Show(ignoreShowAnimation);
            PlayEffects();
        }

        #endregion

        public override void Close(bool ignoreCloseAnimation = false)
        {
            graphicAlphaTweener.Play();
            StopEffects();

            if (!(_timerCoroutine is null))
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            base.Close(ignoreCloseAnimation);
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            if (!(_coShowSomethingCoroutine is null))
            {
                StopCoroutine(_coShowSomethingCoroutine);
                _coShowSomethingCoroutine = null;
            }

            if (menuContainer.activeSelf)
            {
                _coShowSomethingCoroutine = StartCoroutine(CoShowMenu());
            }

            if (questRewards.activeSelf)
            {
                _coShowSomethingCoroutine = StartCoroutine(CoShowQuestRewards(_rewards));
            }

            if (recipeCell.gameObject.activeSelf)
            {
                _coShowSomethingCoroutine = StartCoroutine(CoShowEquipmentRecipe());
            }

            base.OnCompleteOfShowAnimationInternal();
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            foreach (var tweener in _tweeners)
            {
                tweener.Kill();
            }

            _tweeners.Clear();
            _rewards = null;
            base.OnCompleteOfCloseAnimationInternal();
        }

        #endregion

        private static void MakeNotification(string questContent)
        {
            var format = L10nManager.Localize("NOTIFICATION_QUEST_REQUEST_REWARD");
            var msg = string.IsNullOrEmpty(questContent)
                ? string.Empty
                : string.Format(format, questContent);
            NotificationSystem.Push(MailType.System, msg,
                NotificationCell.NotificationType.Information);
        }

        private static void UpdateLocalState(int questId, IEnumerable<Tuple<int, int>> rewards)
        {
            if (rewards is null)
            {
                var sb = new StringBuilder($"[{nameof(CelebratesPopup)}]");
                sb.Append($"Argument {nameof(rewards)} is null.");
                NcDebug.LogError(sb.ToString());
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            foreach (var reward in rewards)
            {
                var materialRow = Game.Game.instance.TableSheets.MaterialItemSheet
                    .First(pair => pair.Key == reward.Item1);

                LocalLayerModifier.AddItem(
                    avatarAddress,
                    materialRow.Value.ItemId,
                    reward.Item2,
                    false);
            }

            LocalLayerModifier.RemoveReceivableQuest(avatarAddress, questId, false);
        }

        private void PlayEffects()
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);

            if (_praiseVFX)
            {
                _praiseVFX.Stop();
            }

            var position = ActionCamera.instance.transform.position;
            _praiseVFX = VFXController.instance.CreateAndChaseCam<PraiseVFX>(position);
            _praiseVFX.Play();
        }

        private void StopEffects()
        {
            AudioController.instance.StopSfx(AudioController.SfxCode.RewardItem);

            if (_praiseVFX)
            {
                _praiseVFX.Stop();
                _praiseVFX = null;
            }
        }

        private IEnumerator CoShowMenu()
        {
            // 메뉴 연출을 재생합니다.

            graphicAlphaTweener.PlayReverse();
            yield return _waitForDisappear;
            StartContinueTimer();
            _coShowSomethingCoroutine = null;
        }

        private IEnumerator CoShowQuestRewards(IReadOnlyList<CountableItem> rewards)
        {
            for (var i = 0; i < questRewardViews.Length; ++i)
            {
                var itemView = questRewardViews[i];

                if (i < (rewards?.Count ?? 0))
                {
                    itemView.Show();
                    var rectTransform = itemView.GetComponent<RectTransform>();
                    var originalScale = rectTransform.localScale;
                    rectTransform.localScale = Vector3.zero;
                    var tweener = rectTransform
                        .DOScale(originalScale, 1f)
                        .SetEase(Ease.OutElastic);
                    tweener.onKill = () => rectTransform.localScale = originalScale;
                    _tweeners.Add(tweener);
                    yield return _waitItemInterval;
                }
            }

            graphicAlphaTweener.PlayReverse();
            yield return _waitForDisappear;
            StartContinueTimer();
            _coShowSomethingCoroutine = null;
        }

        private IEnumerator CoShowEquipmentRecipe()
        {
            // 장비 레시피 연출을 재생합니다.

            graphicAlphaTweener.PlayReverse();
            yield return _waitForDisappear;
            StartContinueTimer();
            _coShowSomethingCoroutine = null;
        }

        private void StartContinueTimer()
        {
            if (!(_timerCoroutine is null))
            {
                StopCoroutine(_timerCoroutine);
            }

            _timerCoroutine = StartCoroutine(CoContinueTimer(ContinueTime));
        }

        private IEnumerator CoContinueTimer(float timer)
        {
            var format = L10nManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT");
            continueText.alpha = 1f;

            var prevFlooredTime = Mathf.Round(timer);
            while (timer >= .3f)
            {
                // 텍스트 업데이트 횟수를 줄이기 위해 소숫점을 내림해
                // 정수부만 체크 후 텍스트 업데이트 여부를 결정합니다.
                var flooredTime = Mathf.Floor(timer);
                if (flooredTime < prevFlooredTime)
                {
                    prevFlooredTime = flooredTime;
                    continueText.text = string.Format(format, flooredTime);
                }

                timer -= Time.deltaTime;
                yield return null;
            }

            Close();
            _timerCoroutine = null;
        }
    }
}
