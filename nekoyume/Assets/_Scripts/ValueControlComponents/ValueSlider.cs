using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.ValueControlComponents
{
    [ExecuteInEditMode]
    public class ValueSlider : MonoBehaviour
    {
        [SerializeField]
        private float _min;

        [SerializeField]
        private float _max;

        [SerializeField, Range(0f, 1f)]
        private float _normalizedValue;

        public float NormalizedValue
        {
            get => _normalizedValue;
            set => _normalizedValue = math.min(1f, math.max(0f, value));
        }

#if UNITY_EDITOR
        [ReadOnly]
        public float _value;
#else
        private float _value;
#endif

        public float Value => _value;

        protected virtual void Update()
        {
            var vector = _max - _min;
            var delta = vector * _normalizedValue;
            _value = _min + delta;
        }
    }
}
