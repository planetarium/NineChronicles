using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.ShaderComponents
{
    [ExecuteInEditMode]
    public class ShaderPropertyController : MonoBehaviour
    {
        [SerializeField]
        private Graphic _graphic;

        [SerializeField]
        private string _propertyName;

        [SerializeField]
        private float _value;

        private void Update()
        {
            if (_graphic is null ||
                string.IsNullOrEmpty(_propertyName))
            {
                return;
            }

            _graphic.material.SetFloat(_propertyName, _value);
        }
    }
}
