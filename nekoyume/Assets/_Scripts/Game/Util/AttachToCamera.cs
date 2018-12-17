using UnityEngine;


namespace Nekoyume.Game.Util
{
    public class AttachToCamera : MonoBehaviour
    {
        private Camera _mainCamera = null;
        private Vector3 _offset;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _offset = transform.position;
        }

        private void Update()
        {
            if (_mainCamera != null)
            {
                transform.position = _mainCamera.transform.position + _offset;
            }
        }
    }
}
