using DG.Tweening;
using Nekoyume.EnumType;
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
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CelebratesPopup : Widget
    {
        private const float ContinueTime = 3f;
        private const int NPCId = 300004;

        [SerializeField]
        private TextMeshProUGUI titleText = null;

        [SerializeField]
        private TextMeshProUGUI continueText = null;

        [SerializeField]
        private RectTransform npcPosition = null;

        [SerializeField]
        private GameObject questRewards = null;

        [SerializeField]
        private SimpleCountableItemView[] questRewardViews = null;

        [SerializeField]
        private RecipeCellView recipeCellView = null;

        [SerializeField]
        private GameObject menuContainer = null;

        [SerializeField]
        private Image menuImage = null;

        [SerializeField]
        private TextMeshProUGUI menuText = null;

        [SerializeField]
        private Blur blur = null;

        [SerializeField]
        private DOTweenTextAlpha textAlphaTweener = null;

        private readonly List<Tweener> _tweeners = new List<Tweener>();
        private readonly WaitForSeconds _waitItemInterval = new WaitForSeconds(0.4f);
        private readonly WaitForSeconds _waitForDisappear = new WaitForSeconds(.3f);

        private NPC _npc;
        private Coroutine _timerCoroutine;
        private Coroutine _coShowSomethingCoroutine;
        private List<CountableItem> _rewards;
        private PraiseVFX _praiseVFX;

        public override WidgetType WidgetType => WidgetType.Tooltip;

        #region override

        protected override void Awake()
        {
            base.Awake();
            blur.onClick = () => Close();
        }

        public override void Initialize()
        {
            base.Initialize();

            var format = L10nManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT");
            continueText.text = string.Format(format, ContinueTime);
        }

        protected override void Update()
        {
            base.Update();

            // UI에 플레이어 고정.
            if (_npc)
            {
                _npc.transform.position = npcPosition.position;
            }
        }

        #region Show with menu

        /// <param name="menuName">Combination || Shop || RankingBoard</param>
        /// <param name="ignoreShowAnimation"></param>
        public void Show(string menuName, bool ignoreShowAnimation = false)
        {
            titleText.text = L10nManager.Localize("UI_NEW_MENU");
            continueText.alpha = 0f;

            menuImage.overrideSprite = SpriteHelper.GetMenuIllustration(menuName);
            menuImage.SetNativeSize();

            switch (menuName)
            {
                default:
                    menuText.text = string.Empty;
                    break;
                case nameof(Combination):
                    menuText.text = L10nManager.Localize("UI_COMBINATION");
                    break;
                case nameof(RankingBoard):
                    menuText.text = L10nManager.Localize("UI_RANKING");
                    break;
                case nameof(Shop):
                    menuText.text = L10nManager.Localize("UI_SHOP");
                    break;
                case nameof(MimisbrunnrPreparation):
                    menuText.text = L10nManager.Localize("UI_MIMISBRUNNR");
                    break;
            }

            menuContainer.SetActive(true);
            questRewards.SetActive(false);
            recipeCellView.Hide();

            _rewards = null;

            AppearNPC(ignoreShowAnimation, NPCAnimation.Type.Emotion_02);
            base.Show(ignoreShowAnimation);
            PlayEffects();
        }

        #endregion

        #region Show with quest

        public void Show(
            CombinationEquipmentQuestSheet.Row row,
            bool ignoreShowAnimation = false)
        {
            if (row is null)
            {
                var sb = new StringBuilder($"[{nameof(CelebratesPopup)}]");
                sb.Append($"Argument {nameof(row)} is null.");
                Debug.LogError(sb.ToString());
                return;
            }

            var quest = States.Instance.CurrentAvatarState?.questList
                .OfType<CombinationEquipmentQuest>()
                .FirstOrDefault(item =>
                    item.Id == row.Id);
            Show(quest, ignoreShowAnimation);
        }

        public void Show(Nekoyume.Model.Quest.Quest quest, bool ignoreShowAnimation = false)
        {
            if (quest is null)
            {
                var sb = new StringBuilder($"[{nameof(CelebratesPopup)}]");
                sb.Append($"Argument {nameof(quest)} is null.");
                Debug.LogError(sb.ToString());
                return;
            }

            var rewardModels = quest.Reward.ItemMap
                .Select(pair =>
                {
                    var itemRow = Game.Game.instance.TableSheets.MaterialItemSheet.OrderedList
                        .First(row => row.Id == pair.Key);
                    var material = ItemFactory.CreateMaterial(itemRow);
                    return new CountableItem(material, pair.Value);
                })
                .ToList();
            Show(quest, rewardModels, ignoreShowAnimation);
        }

        private void Show(
            Nekoyume.Model.Quest.Quest quest,
            List<CountableItem> rewards,
            bool ignoreShowAnimation = false)
        {
            titleText.text = L10nManager.Localize("UI_QUEST_COMPLETED");
            continueText.alpha = 0f;

            menuContainer.SetActive(false);
            questRewards.SetActive(true);
            recipeCellView.Hide();

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

            AppearNPC(ignoreShowAnimation, NPCAnimation.Type.Emotion_03);
            base.Show(ignoreShowAnimation);
            PlayEffects();
            MakeNotification(quest.GetContent());
            UpdateLocalState(quest.Id, quest.Reward?.ItemMap);
        }

        #endregion

        #region Show with recipe

        public void Show(
            EquipmentItemRecipeSheet.Row row,
            bool ignoreShowAnimation = false)
        {
            if (row is null)
            {
                var sb = new StringBuilder($"[{nameof(CelebratesPopup)}]");
                sb.Append($"Argument {nameof(row)} is null.");
                Debug.LogError(sb.ToString());
                return;
            }

            titleText.text = L10nManager.Localize("UI_NEW_EQUIPMENT_RECIPE");
            continueText.alpha = 0f;

            recipeCellView.Set(row);

            menuContainer.SetActive(false);
            questRewards.SetActive(false);
            recipeCellView.Show();

            _rewards = null;

            AppearNPC(ignoreShowAnimation, NPCAnimation.Type.Emotion_01);
            base.Show(ignoreShowAnimation);
            PlayEffects();
        }

        #endregion

        public override void Close(bool ignoreCloseAnimation = false)
        {
            blur.button.interactable = false;
            textAlphaTweener.Play();
            DisappearNPC(ignoreCloseAnimation);
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

            if (recipeCellView.gameObject.activeSelf)
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
            Notification.Push(MailType.System, msg);
        }

        private static void UpdateLocalState(int questId, Dictionary<int, int> rewards)
        {
            if (rewards is null)
            {
                var sb = new StringBuilder($"[{nameof(CelebratesPopup)}]");
                sb.Append($"Argument {nameof(rewards)} is null.");
                Debug.LogError(sb.ToString());
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            foreach (var reward in rewards)
            {
                var materialRow = Game.Game.instance.TableSheets.MaterialItemSheet
                    .First(pair => pair.Key == reward.Key);

                LocalLayerModifier.AddItem(
                    avatarAddress,
                    materialRow.Value.ItemId,
                    reward.Value,
                    false);
            }

            LocalLayerModifier.RemoveReceivableQuest(avatarAddress, questId);
        }

        private void AppearNPC(bool ignoreShowAnimation, NPCAnimation.Type animationType)
        {
            if (_npc is null)
            {
                var go = Game.Game.instance.Stage.npcFactory.Create(
                    NPCId,
                    npcPosition.position,
                    LayerType.UI,
                    100);
                _npc = go.GetComponent<NPC>();
            }

            _npc.SpineController.Appear(ignoreShowAnimation ? 0f : .3f);
            _npc.PlayAnimation(animationType);
        }

        private void DisappearNPC(bool ignoreCloseAnimation)
        {
            if (!_npc)
            {
                return;
            }

            _npc.SpineController.Disappear(
                ignoreCloseAnimation ? 0f : .3f,
                true,
                () =>
                {
                    _npc.gameObject.SetActive(false);
                        _npc = null;
                });
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

            textAlphaTweener.PlayReverse();
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

            textAlphaTweener.PlayReverse();
            yield return _waitForDisappear;
            StartContinueTimer();
            _coShowSomethingCoroutine = null;
        }

        private IEnumerator CoShowEquipmentRecipe()
        {
            // 장비 레시피 연출을 재생합니다.

            textAlphaTweener.PlayReverse();
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
            blur.button.interactable = true;
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
