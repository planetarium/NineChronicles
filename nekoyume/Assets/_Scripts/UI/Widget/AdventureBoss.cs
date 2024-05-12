using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
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

        private const float floorHeight = 170;

        public void ChangeFloor(int targetIndex, bool isAnimation = true)
        {
            float targetCenter = targetIndex * floorHeight + (floorHeight / 2);
            float startY = -(targetCenter - (MainCanvas.instance.RectTransform.rect.height/2) - towerCenterAdjuster);

            if(isAnimation)
            {
                towerRect.DoAnchoredMoveY(Math.Min(startY, 0), 0.35f).SetEase(towerMoveEase);
            }
            else
            {
                towerRect.anchoredPosition = new Vector2(towerRect.anchoredPosition.x, Math.Min(startY,0));
            }
        }
    }
}
