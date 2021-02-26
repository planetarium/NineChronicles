using Nekoyume.EnumType;
using UnityEngine;

namespace Nekoyume.Game.Util
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteRendererPositionConstraintToScreen : PositionConstraintToScreen
    {
        [SerializeField, Tooltip("강제할 스프라이트의 피봇을 설정한다.")]
        private PivotPresetType spritePivot = PivotPresetType.MiddleCenter;

        private SpriteRenderer _spriteRenderer;

        private SpriteRenderer SpriteRenderer => _spriteRenderer
            ? _spriteRenderer
            : _spriteRenderer = GetComponent<SpriteRenderer>();

        protected override Vector3 GetWorldPosition()
        {
            return ActionCamera.instance.GetWorldPosition(
                transform,
                ScreenPivot,
                SpriteRenderer.sprite,
                spritePivot);
        }
    }
}
