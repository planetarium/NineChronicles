using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.GrandFinale;
using Nekoyume.UI.Module.Arena.Join;
using Nekoyume.ValueControlComponents.Shader;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
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

        public void Set(System.Action onClickJoinArena)
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var roundData = TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            if (roundData is null)
            {
                return;

            }

            arenaJoinButton.OnClickSubject.Subscribe(_ => onClickJoinArena.Invoke()).AddTo(gameObject);

            var grandFinaleRow =
                TableSheets.Instance.GrandFinaleScheduleSheet.GetRowByBlockIndex(blockIndex);
            grandFinaleJoinButton.OnClickSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Widget.Find<ArenaJoin>().Close();
                Widget.Find<ArenaBoard>()
                    .Show(grandFinaleRow, States.Instance.GrandFinaleStates.GrandFinaleParticipants);
            }).AddTo(gameObject);

            SetGrandFinaleInfo(grandFinaleRow, blockIndex);
            SetArenaInfo(roundData, blockIndex);
        }

        private void SetGrandFinaleInfo(
            GrandFinaleScheduleSheet.Row grandFinaleRow,
            long blockIndex)
        {
            var (beginning, end, current) = (grandFinaleRow.StartBlockIndex,
                grandFinaleRow.EndBlockIndex, blockIndex);
            if (current > end)
            {
                grandFinaleProgressFillImage.enabled = false;
                grandFinaleProgressSliderFillText.enabled = false;

                return;
            }

            if (current < beginning)
            {
                grandFinaleProgressFillImage.enabled = false;
                grandFinaleProgressSliderFillText.text = Util.GetBlockToTime(beginning - current);
                grandFinaleProgressSliderFillText.enabled = true;

                return;
            }

            var range = end - beginning;
            var progress = current - beginning;
            var sliderNormalizedValue = (float) progress / range;
            grandFinaleProgressFillImage.fillAmount = sliderNormalizedValue;
            grandFinaleProgressFillImage.enabled = true;
            grandFinaleProgressSliderFillText.text = Util.GetBlockToTime(range - progress);
            grandFinaleProgressSliderFillText.enabled = true;
        }

        private void SetArenaInfo(
            ArenaSheet.RoundData roundData,
            long blockIndex)
        {
            var (beginning, end, current) = roundData.GetSeasonProgress(blockIndex);
            if (current > end)
            {
                arenaProgressFillImage.enabled = false;
                arenaProgressSliderFillText.enabled = false;

                return;
            }

            if (current < beginning)
            {
                arenaProgressFillImage.enabled = false;
                arenaProgressSliderFillText.text = Util.GetBlockToTime(beginning - current);
                arenaProgressSliderFillText.enabled = true;

                return;
            }

            var range = end - beginning;
            var progress = current - beginning;
            var sliderNormalizedValue = (float) progress / range;
            arenaProgressFillImage.fillAmount = sliderNormalizedValue;
            arenaProgressFillImage.enabled = true;
            arenaProgressSliderFillText.text = Util.GetBlockToTime(range - progress);
            arenaProgressSliderFillText.enabled = true;
        }
    }
}
