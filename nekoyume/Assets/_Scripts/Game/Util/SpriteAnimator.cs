using UnityEngine;


namespace Nekoyume.Game.Util
{
    public class SpriteAnimator : MonoBehaviour
    {
        static private int DEFAULT_LAYER = 15;

        private SpriteRenderer _renderer;
        private Sprite[] _sprites = null;
        private float _fps = 24;
        private int _currentFrame = 0;
        private float _updateTime = 0.0f;
        private bool _playing = false;
        public bool Repeat = false;

        private void Awake()
        {
            _renderer = gameObject.GetComponent<SpriteRenderer>();
            _renderer.sortingOrder = DEFAULT_LAYER;
        }
        
        private void Update()
        {
            if (_playing)
            {
                _updateTime += Time.deltaTime;
                float t = 1.0f / _fps;
                if (_updateTime >= t)
                {
                    _updateTime -= t;
                    NextFrame();
                }
            }
        }

        public void Play(string name)
        {
            _updateTime = 0.0f;
            _playing = true;
            _sprites = Resources.LoadAll<Sprite>(string.Format("images/{0}", name));
            SetFrame(0);
        }

        private void NextFrame()
        {
            SetFrame(_currentFrame + 1);
        }

        private void SetFrame(int frame)
        {
            if (_sprites.Length > frame)
            {
                _currentFrame = frame;
            }
            else
            {
                _currentFrame = 0;
                if (!Repeat)
                {
                    Destroy();
                }
            }
            _renderer.sprite = _sprites[_currentFrame];
        }

        public void Destroy()
        {
            _renderer.sprite = null;
            _playing = false;
            gameObject.SetActive(false);
        }
    }
}
