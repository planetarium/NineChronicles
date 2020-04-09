using System.Collections;
using Nekoyume.EnumType;
using UnityEngine;

namespace Nekoyume.Game.Util
{
    public class PositionConstraintToScreen : MonoBehaviour
    {
        [SerializeField, Tooltip("Awake 단계에서 한 번만 강제할 것인지 설정한다.")]
        private bool constraintOnce = false;

        [SerializeField, Tooltip("X축을 강제할 것인지 설정한다.")]
        private bool constraintX = false;

        [SerializeField, Tooltip("Y축을 강제할 것인지 설정한다.")]
        private bool constraintY = false;

        [SerializeField, Tooltip("Z축을 강제할 것인지 설정한다.")]
        private bool constraintZ = false;

        [SerializeField, Tooltip("강제할 화면의 피봇을 설정한다.")]
        private PivotPresetType screenPivot = PivotPresetType.MiddleCenter;

        private Transform _transform;

        protected PivotPresetType ScreenPivot => screenPivot;

        private Transform Transform => _transform
            ? _transform
            : _transform = GetComponent<Transform>();

        private void Awake()
        {
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

        public void Constraint()
        {
            var position = Transform.position;
            var position2 = GetWorldPosition();

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

            Transform.position = position;
        }

        protected virtual Vector3 GetWorldPosition()
        {
            return ActionCamera.instance.GetWorldPosition(
                transform,
                screenPivot);
        }
    }
}
