using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.ValueControlComponents.Shader
{
    [ExecuteInEditMode]
    public class ShaderPropertySlider : ValueSlider
    {
        [SerializeField]
        private Graphic _graphic;

        [SerializeField]
        private string _propertyName;

        protected override void Update()
        {
            base.Update();

            if (_graphic is null ||
                string.IsNullOrEmpty(_propertyName))
            {
                return;
            }

            _graphic.material.SetFloat(_propertyName, Value);
        }
    }
}
