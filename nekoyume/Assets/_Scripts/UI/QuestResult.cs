using Assets.SimpleLocalization;
using DG.Tweening;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Tween;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class QuestResult : Widget
    {
        [SerializeField]
        private TextMeshProUGUI questCompletedText = null;

        [SerializeField]
        private TextMeshProUGUI continueText = null;

        [SerializeField]
        private RectTransform npcPosition = null;

        [SerializeField]
        private SimpleCountableItemView[] itemViews = null;

        [SerializeField]
        private Blur blur = null;

        [SerializeField]
        private DOTweenTextAlpha _textAlphaTweener = null;

        private readonly List<Tweener> _tweeners = new List<Tweener>();
        private readonly WaitForSeconds _waitOneSec = new WaitForSeconds(1f);
        private readonly WaitForSeconds _waitForDisappear = new WaitForSeconds(.3f);

        private NPC _npc = null;
        private Coroutine _timerCoroutine = null;
        private List<CountableItem> _rewards = null;

        private const float ContinueTime = 10f;
        private const int NPCId = 300001;

        #region override

        protected override void Awake()
        {
            base.Awake();
            blur.onClick = DisappearNPC;
        }

        protected override void Update()
        {
            base.Update();

            // UI에 플레이어 고정.
            _npc.transform.position = npcPosition.position;
        }

        public void Show(Nekoyume.Model.Quest.Quest quest, bool ignoreShowAnimation = false)
        {
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

        private void Show(Nekoyume.Model.Quest.Quest quest, List<CountableItem> rewards,
            bool ignoreShowAnimation = false)
        {
            foreach (var view in itemViews)
                view.Hide();

            questCompletedText.text = LocalizationManager.Localize("UI_QUEST_COMPLETED");
            continueText.alpha = 0f;

            _rewards = rewards;

            var go = Game.Game.instance.Stage.npcFactory.Create(
                NPCId,
                npcPosition.position,
                LayerType.UI,
                100);
            _npc = go.GetComponent<NPC>();
            _npc.SpineController.Appear(.3f);
            _npc.PlayAnimation(NPCAnimation.Type.Appear_01);

            base.Show(ignoreShowAnimation);
            MakeNotification(quest);
            UpdateLocalState(quest);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (!(_timerCoroutine is null))
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            base.Close(ignoreCloseAnimation);
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            StartCoroutine(CoShowRewards(_rewards));
            base.OnCompleteOfShowAnimationInternal();
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            _npc.gameObject.SetActive(false);
            foreach (var tweener in _tweeners)
            {
                tweener.Kill();
            }

            _tweeners.Clear();
            _rewards.Clear();
            base.OnCompleteOfCloseAnimationInternal();
        }

        #endregion

        private static void MakeNotification(Nekoyume.Model.Quest.Quest quest)
        {
            if (quest is null)
            {
                return;
            }

            var format = LocalizationManager.Localize("NOTIFICATION_QUEST_REQUEST_REWARD");
            var msg = string.Format(format, quest.GetContent());
            Notification.Push(MailType.System, msg);
        }

        private static void UpdateLocalState(Nekoyume.Model.Quest.Quest quest)
        {
            if (quest is null)
            {
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var rewardMap = quest.Reward.ItemMap;
            foreach (var reward in rewardMap)
            {
                var materialRow = Game.Game.instance.TableSheets.MaterialItemSheet
                    .First(pair => pair.Key == reward.Key);

                LocalStateModifier.AddItem(
                    avatarAddress,
                    materialRow.Value.ItemId,
                    reward.Value,
                    false);
            }

            LocalStateModifier.RemoveReceivableQuest(avatarAddress, quest.Id);
        }

        private IEnumerator CoShowRewards(IReadOnlyList<CountableItem> rewards)
        {
            for (var i = 0; i < itemViews.Length; ++i)
            {
                var itemView = itemViews[i];

                if (i < rewards.Count)
                {
                    itemView.SetData(rewards[i]);
                    itemView.Show();
                    var rectTransform = itemView.GetComponent<RectTransform>();
                    var originalScale = rectTransform.localScale;
                    rectTransform.localScale = Vector3.zero;
                    var tweener = rectTransform
                        .DOScale(originalScale, 1f)
                        .SetEase(Ease.OutElastic);
                    tweener.onKill = () => rectTransform.localScale = originalScale;
                    _tweeners.Add(tweener);
                    yield return _waitOneSec;
                }
                else
                {
                    itemView.Hide();
                }
            }

            _textAlphaTweener.PlayReverse();
            yield return _waitForDisappear;
            StartContinueTimer();
        }

        private void StartContinueTimer()
        {
            _timerCoroutine = StartCoroutine(CoContinueTimer(ContinueTime));
        }

        private IEnumerator CoContinueTimer(float timer)
        {
            blur.button.interactable = true;
            var format = LocalizationManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT");
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

            DisappearNPC();
        }

        private void DisappearNPC()
        {
            blur.button.interactable = false;
            _textAlphaTweener.Play();
            _npc.SpineController.Disappear(.3f);
            _npc.PlayAnimation(NPCAnimation.Type.Disappear_01);
            Close();
        }
    }
}
