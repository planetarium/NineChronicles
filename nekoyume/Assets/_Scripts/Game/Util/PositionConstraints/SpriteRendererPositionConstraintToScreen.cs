using System.Collections;
using Nekoyume.EnumType;
using UnityEngine;

namespace Nekoyume.Game.Util
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteRendererPositionConstraintToScreen : MonoBehaviour
    {
        [SerializeField]
        private bool constraintOnce = false;

        [SerializeField]
        private bool constraintX = false;

        [SerializeField]
        private bool constraintY = false;

        [SerializeField]
        private bool constraintZ = false;

        [SerializeField]
        private PivotPresetType screenPivot = PivotPresetType.MiddleCenter;

        [SerializeField]
        private PivotPresetType spritePivot = PivotPresetType.MiddleCenter;

        private Transform _transform;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _transform = transform;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            Constraint();
        }

        private void OnEnable()
        {
            if (constraintOnce)
            {
                return;
            }

            StartCoroutine(CoUpdate());
        }

        private IEnumerator CoUpdate()
        {
            while (enabled)
            {
                Constraint();

                yield return null;
            }
        }

        private void Constraint()
        {
            var position = _transform.position;
            var position2 =
                ActionCamera.instance.GetWorldPosition(
                    transform,
                    screenPivot,
                    _spriteRenderer.sprite,
                    spritePivot);

            if (constraintX)
            {
                position.x = position2.x;
            }

            if (constraintY)
            {
                position.y = position2.y;
            }

            if (constraintZ)
            {
                position.z = position2.z;
            }

            _transform.position = position;
        }
    }
}
