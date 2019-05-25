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
                var cameraPosition = _mainCamera.transform.position;
                cameraPosition.z = 0;
                transform.position = cameraPosition + _offset;
            }
        }
    }
}
