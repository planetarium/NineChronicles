using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.State;
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
        private const string OffSeasonString = "off-season";

        [SerializeField]
        private ArenaJoinSeasonInfo arenaJoinSeasonInfo;

        [SerializeField]
        private ConditionalButton arenaJoinButton;

        [SerializeField]
        private ConditionalButton grandFinaleJoinButton;

        [SerializeField]
        private ShaderPropertySlider seasonProgressSlider;

        [SerializeField]
        private Image seasonProgressFillImage;

        [SerializeField]
        private TextMeshProUGUI seasonProgressSliderFillText;

        public void Set(System.Action onClickJoinArena)
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var roundData = TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            arenaJoinSeasonInfo.SetData(
                OffSeasonString,
                roundData,
                ArenaJoinSeasonInfo.RewardType.Food,
                null
            );
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

            var (beginning, end, current) = (grandFinaleRow.StartBlockIndex,
                grandFinaleRow.EndBlockIndex, blockIndex);
            if (current > end)
            {
                seasonProgressFillImage.enabled = false;
                seasonProgressSliderFillText.enabled = false;

                return;
            }

            if (current < beginning)
            {
                seasonProgressFillImage.enabled = false;
                seasonProgressSliderFillText.text = Util.GetBlockToTime(beginning - current);
                seasonProgressSliderFillText.enabled = true;

                return;
            }

            var range = end - beginning;
            var progress = current - beginning;
            var sliderNormalizedValue = (float)progress / range;
            seasonProgressSlider.NormalizedValue = sliderNormalizedValue;
            seasonProgressFillImage.enabled = true;
            seasonProgressSliderFillText.text = Util.GetBlockToTime(range - progress);
            seasonProgressSliderFillText.enabled = true;
        }
    }
}
