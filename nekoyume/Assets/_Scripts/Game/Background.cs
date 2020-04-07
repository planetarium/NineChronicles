using UnityEngine;


namespace Nekoyume.Game
{
    public class Background : MonoBehaviour
    {
        // FIXME: parallaxSize를 자동으로 구해주는 로직이 SpriteAtlas를 사용하는 케이스를 커버해주지 못해고 있습니다.
        // 스프라이트의 사이즈가 이를 포함하는 텍스쳐의 사이즈와 다르기 때문입니다.
        // 지금은 임시로 스프라이트를 포함하는 트랜스폼의 스케일이 기본값이라는 가정하에 렉트를 사용하도록 수정합니다.
        [SerializeField]
        private bool autoParallaxSize = true;

        [SerializeField]
        private float parallaxSize = 0.0f;

        [SerializeField]
        private float parallaxSpeed = 0.0f;

        private Transform _cameraTransform;
        private Transform[] _images;
        private float _lastCameraX;
        private int _leftIndex;
        private int _rightIndex;

        private void Awake ()
        {
            _cameraTransform = ActionCamera.instance.transform;

            _images = new Transform[transform.childCount];
            for (var i = 0; i < transform.childCount; ++i)
            {
                _images[i] = transform.GetChild(i);
                if (i == 0 &&
                    autoParallaxSize)
                {
                    var sprite = _images[i].GetComponent<SpriteRenderer>()?.sprite;
                    if (!(sprite is null))
                    {
                        parallaxSize = sprite.rect.width / sprite.pixelsPerUnit;
                    }
                }
                _images[i].position = new Vector3(parallaxSize * i, _images[i].position.y, _images[i].position.z);
            }

            _lastCameraX = _cameraTransform.position.x;
            _leftIndex = 0;
            _rightIndex = _images.Length - 1;
        }

        private void Update()
        {
            var camPosX = _cameraTransform.position.x;

            if (!Mathf.Approximately(parallaxSpeed, 0.0f))
            {
                var deltaX = camPosX - _lastCameraX;
                transform.position += Vector3.right * (deltaX * parallaxSpeed * 3f);
                _lastCameraX = camPosX;
            }

            if (_images.Length <= 1)
            {
                return;
            }

            if (camPosX < _images[_leftIndex].transform.position.x)
            {
                MoveLeft();
            }

            if (camPosX > _images[_rightIndex].transform.position.x)
            {
                MoveRight();
            }
        }

        private void MoveLeft()
        {
            var position = Vector3.right * (_images[_leftIndex].position.x - parallaxSize);
            position.y = _images[_rightIndex].position.y;
            _images[_rightIndex].position = position;
            _leftIndex = _rightIndex;
            _rightIndex--;
            if (_rightIndex < 0)
            {
                _rightIndex = _images.Length - 1;
            }
        }

        private void MoveRight()
        {
            var position = Vector3.right * (_images[_rightIndex].position.x + parallaxSize);
            position.y = _images[_leftIndex].position.y;
            _images[_leftIndex].position = position;
            _rightIndex = _leftIndex;
            _leftIndex++;
            if (_leftIndex == _images.Length)
            {
                _leftIndex = 0;
            }
        }
    }
}
