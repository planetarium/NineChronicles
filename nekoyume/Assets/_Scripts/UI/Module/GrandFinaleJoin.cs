using System;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.GrandFinale;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class GrandFinaleJoin : MonoBehaviour
    {
        [SerializeField]
        private ConditionalButton arenaJoinButton;

        [SerializeField]
        private ConditionalButton grandFinaleJoinButton;

        [SerializeField]
        private Image arenaProgressFillImage;

        [SerializeField]
        private TextMeshProUGUI arenaProgressSliderFillText;

        [SerializeField]
        private Image grandFinaleProgressFillImage;

        [SerializeField]
        private TextMeshProUGUI grandFinaleProgressSliderFillText;

        private GrandFinaleScheduleSheet.Row _grandFinaleScheduleRow;

        public void Set(System.Action onClickJoinArena)
        {
            arenaJoinButton.OnClickSubject
                .Subscribe(_ => onClickJoinArena.Invoke())
                .AddTo(gameObject);
            grandFinaleJoinButton.OnClickSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Widget.Find<ArenaJoin>().Close();
                Widget.Find<ArenaBoard>()
                    .Show(_grandFinaleScheduleRow,
                        States.Instance.GrandFinaleStates.GrandFinaleParticipants);
            }).AddTo(gameObject);
            UpdateInformation();
        }

        public void UpdateInformation()
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            ArenaSheet.RoundData currentRoundData;
            try
            {
                currentRoundData =
                    TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            }
            catch (Exception)
            {
                return;
            }

            if (currentRoundData is null)
            {
                return;
            }

            _grandFinaleScheduleRow =
                TableSheets.Instance.GrandFinaleScheduleSheet?.GetRowByBlockIndex(blockIndex);
            if (_grandFinaleScheduleRow is null)
            {
                return;
            }

            SetScheduleUI(
                (_grandFinaleScheduleRow.StartBlockIndex, _grandFinaleScheduleRow.EndBlockIndex, blockIndex),
                grandFinaleProgressFillImage,
                grandFinaleProgressSliderFillText);
            SetScheduleUI(
                currentRoundData.GetSeasonProgress(blockIndex),
                arenaProgressFillImage,
                arenaProgressSliderFillText);
        }

        private void SetScheduleUI(
            (long beginning, long end, long current) tuple,
            Image sliderImage,
            TextMeshProUGUI text)
        {
            var (beginning, end, current) = tuple;
            if (current > end)
            {
                sliderImage.enabled = false;
                text.enabled = false;

                return;
            }

            if (current < beginning)
            {
                arenaProgressFillImage.enabled = false;
                text.text = (beginning - current).BlockRangeToTimeSpanString();
                text.enabled = true;

                return;
            }

            var range = end - beginning;
            var progress = current - beginning;
            var sliderNormalizedValue = (float) progress / range;
            sliderImage.fillAmount = sliderNormalizedValue;
            sliderImage.enabled = true;
            text.text = (range - progress).BlockRangeToTimeSpanString();
            text.enabled = true;
        }
    }
}
