using Nekoyume.EnumType;
using Nekoyume.Game.Battle;
using UnityEngine;

namespace Nekoyume.UI
{
    public class HudContainer : HudWidget
    {
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private float maxHeightOnBattleRender = 215;
        [SerializeField] private float maxHeightOnAdventureBoss = 180;

        public void UpdatePosition(Camera canvasCamera, GameObject target, Vector3 offset = new())
        {
            var targetPosition = target.transform.position + offset;
            var targetPos = targetPosition.ToCanvasPosition(canvasCamera, MainCanvas.instance.Canvas);
            if (BattleRenderer.Instance.IsOnBattle)
            {
                var height = Game.Game.instance.Stage.StageType == StageType.AdventureBoss ? maxHeightOnAdventureBoss : maxHeightOnBattleRender;
                targetPos.y = Mathf.Min(targetPos.y, height);
            }

            RectTransform.anchoredPosition = targetPos;
            RectTransform.localScale = new Vector3(1, 1);
        }

        public void UpdateAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
        }
    }
}
