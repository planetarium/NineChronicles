using Codice.Utils;
using DG.Tweening;
using System;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class AdventureBoss : Widget
    {
        [SerializeField]
        private RectTransform towerRect;
        [SerializeField]
        private float towerCenterAdjuster = 52;
        [SerializeField]
        private Ease towerMoveEase = Ease.OutCirc;

        private const float _floorHeight = 170;

        public void ChangeFloor(int targetIndex, bool isStartPointRefresh = true, bool isAnimation = true)
        {
            float targetCenter = targetIndex * _floorHeight + (_floorHeight / 2);
            float startY = -(targetCenter - (MainCanvas.instance.RectTransform.rect.height/2) - towerCenterAdjuster);

            if(isAnimation)
            {
                if (isStartPointRefresh)
                {
                    towerRect.anchoredPosition = new Vector2(towerRect.anchoredPosition.x, 0);
                }
                towerRect.DoAnchoredMoveY(Math.Min(startY, 0), 0.35f).SetEase(towerMoveEase);
            }
            else
            {
                towerRect.anchoredPosition = new Vector2(towerRect.anchoredPosition.x, Math.Min(startY,0));
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            ChangeFloor(Game.Game.instance.AdventureBossData.ExploreInfo.Value.Floor);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }
    }
}
