using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Tween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class WorldBossRewardScreen : ScreenWidget
    {
        private const float ContinueTime = 3f;

        [Serializable]
        private class Items
        {
            public GameObject Object;
            public Image Icon;
            public TextMeshProUGUI Count;
        }

        [SerializeField]
        private GraphicAlphaTweener graphicAlphaTweener;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private TextMeshProUGUI continueText;

        [SerializeField]
        private TextMeshProUGUI crystalCountText;

        [SerializeField]
        private List<Items> runes;

        private RaiderState _cachedRaiderState;
        private PraiseVFX _praiseVFX;
        private Coroutine _coCloseCoroutine;
        private int _cachedBossId;
        private int timer;

        private System.Action _closeCallback;

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = () => { Close(true); };
        }

        public void CachingInformation(RaiderState raiderState, int bossId)
        {
            _cachedRaiderState = raiderState;
            _cachedBossId = bossId;
        }

        public void Show(IRandom random)
        {
            base.Show();
            titleText.text = L10nManager.Localize("UI_BOSS_BATTLE_GRADE_REWARDS");
            UpdateRewardItems(GetRewards(random));
            Find<WorldBossDetail>().GotRewards();

            graphicAlphaTweener.Play();
            PlayEffects();
            if (_coCloseCoroutine != null)
            {
                StopCoroutine(_coCloseCoroutine);
            }

            _coCloseCoroutine = StartCoroutine(CoClose());
        }

        public void Show(WorldBossRewards rewards, System.Action closeCallback)
        {
            base.Show();
            titleText.text = L10nManager.Localize("UI_BOSS_KILL_REWARDS");
            UpdateRewardItems(rewards);
            graphicAlphaTweener.Play();
            PlayEffects();
            _closeCallback = closeCallback;
            if (_coCloseCoroutine != null)
            {
                StopCoroutine(_coCloseCoroutine);
            }

            _coCloseCoroutine = StartCoroutine(CoClose());
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            StopEffects();
            if (_coCloseCoroutine != null)
            {
                StopCoroutine(_coCloseCoroutine);
            }

            _closeCallback?.Invoke();
            _closeCallback = null;
            base.Close(ignoreCloseAnimation);
        }

        private IEnumerator CoClose()
        {
            timer = 3;
            while (timer >= 0)
            {
                continueText.text = L10nManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT", timer);
                yield return new WaitForSecondsRealtime(1f);
                timer--;
            }

            Close();
        }

        private WorldBossRewards GetRewards(IRandom random)
        {
            var runeWeightSheet = Game.Game.instance.TableSheets.RuneWeightSheet;
            var rewardSheet = Game.Game.instance.TableSheets.WorldBossRankRewardSheet;
            var runeSheet = Game.Game.instance.TableSheets.RuneSheet;
            var materialItemSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            var characterSheet = Game.Game.instance.TableSheets.WorldBossCharacterSheet;
            var rank = WorldBossHelper.CalculateRank(
                characterSheet[_cachedBossId],
                _cachedRaiderState.HighScore);

            var totalRewards = new WorldBossRewards();
            for (var i = _cachedRaiderState.LatestRewardRank; i < rank; i++)
            {
                var (assets, materials) = WorldBossHelper.CalculateReward(
                    i + 1,
                    _cachedBossId,
                    runeWeightSheet,
                    rewardSheet,
                    runeSheet,
                    materialItemSheet,
                    random
                );
                totalRewards.Assets.AddRange(assets);
                foreach (var pair in materials)
                {
                    totalRewards.Materials.TryAdd(pair.Key, 0);
                    totalRewards.Materials[pair.Key] += pair.Value;
                }
            }

            return totalRewards;
        }

        private void UpdateRewardItems(WorldBossRewards rewards)
        {
            var crystalReward = rewards.Assets
                .Where(x => x.Currency.Ticker == "CRYSTAL")
                .Sum(x => MathematicsExtensions.ConvertToInt32(x.GetQuantityString()));
            crystalCountText.text = $"{crystalReward:#,0}";

            foreach (var rune in runes)
            {
                rune.Object.SetActive(false);
            }

            var totalRuneRewards = new Dictionary<string, int>();
            foreach (var runeReward in rewards.Assets.Where(x => x.Currency.Ticker != "CRYSTAL"))
            {
                var key = runeReward.Currency.Ticker;
                var count = MathematicsExtensions.ConvertToInt32(runeReward.GetQuantityString());
                totalRuneRewards.TryAdd(key, 0);
                totalRuneRewards[key] += count;
            }

            var index = 0;
            foreach (var (ticker, count) in totalRuneRewards)
            {
                runes[index].Object.SetActive(true);
                runes[index].Count.text = $"{count:#,0}";
                if (RuneFrontHelper.TryGetRuneStoneIcon(ticker, out var icon))
                {
                    runes[index].Icon.sprite = icon;
                }

                index++;
            }

            foreach (var (material, count) in rewards.Materials)
            {
                runes[index].Object.SetActive(true);
                runes[index].Count.text = $"{count:#,0}";
                runes[index].Icon.sprite = SpriteHelper.GetItemIcon(material.Id);

                index++;
            }
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
    }
}
