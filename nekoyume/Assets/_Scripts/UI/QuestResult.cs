using Assets.SimpleLocalization;
using DG.Tweening;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Tween;
using System.Collections;
using System.Collections.Generic;
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

        private NPC _npc = null;
        private Coroutine _timerCoroutine = null;
        private WaitForSeconds _waitForDisappear = new WaitForSeconds(.3f);
        private List<Tweener> _tweeners = new List<Tweener>();

        private const float ContinueTime = 10f;
        private const int NPCId = 300001;

        protected override void Awake()
        {
            base.Awake();
            blur.onClick = DisappearNPC;
        }

        #region override

        public void Show(List<CountableItem> rewards, bool ignoreShowAnimation = false)
        {
            questCompletedText.text = LocalizationManager.Localize("UI_QUEST_COMPLETED");
            for (int i = 0; i < itemViews.Length; ++i)
            {
                var itemView = itemViews[i];
                if (i < rewards.Count)
                {
                    itemView.SetData(rewards[i]);
                    itemView.Show();
                    var rectTransform = itemView.iconImage.rectTransform;
                    var originalScale = rectTransform.localScale;
                    rectTransform.localScale = Vector3.zero;
                    var tweener = rectTransform
                                    .DOScale(originalScale, 1f)
                                    .SetEase(Ease.OutElastic);
                    tweener.onKill = () => rectTransform.localScale = originalScale;
                    _tweeners.Add(tweener);
                }
                else
                {
                    itemView.Hide();
                }
            }

            var go = Game.Game.instance.Stage.npcFactory.Create(
                NPCId,
                npcPosition.position,
                LayerType.UI,
                100);
            _npc = go.GetComponent<NPC>();
            _npc.SpineController.Appear(.3f);
            _npc.PlayAnimation(NPCAnimation.Type.Appear_01);

            base.Show(ignoreShowAnimation);
            StartContinueTimer();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;

            base.Close(ignoreCloseAnimation);
            _npc.gameObject.SetActive(false);
            foreach (var tweener in _tweeners)
            {
                tweener.Kill();
            }
            _tweeners.Clear();
        }

        #endregion

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
