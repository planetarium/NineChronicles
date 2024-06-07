using UnityEngine;

namespace Nekoyume.Game.Character
{
    /// <summary>
    /// SpineColorSetting에서 특정 Key를 가진 색상그룹을 관리하고 싶을 때 사용
    /// </summary>
    public enum SpineColorKey
    {
        None,
        FrostBite,
        Test,
    }

    /// <summary>
    /// Spine Color를 지정하기위한 우선순위를 저장한 enum type
    /// </summary>
    public enum SpineColorPriority
    {
        Always = 0,
        Hit = 5,
        Frostbite = 10,
        Test = 10000,
    }

    // struct을 사용하려 했으나, SimplePQ를 사용하면서 내부적으로 값이 복사되어 class로 변경
    public class SpineColorSetting
    {
        /// <summary>
        /// 텍스처에 곱해지는 컬러, 해당 값이 클수록 밝은 부분이 어두워진다.
        /// </summary>
        public static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        /// <summary>
        /// 어두울수록 더 많이 더해지는 컬러, 해당 값이 클수록 어두운 부분이 밝아진다.
        /// </summary>
        public static readonly int BlackPropertyId = Shader.PropertyToID("_Black");

        private readonly Color         _color;
        private readonly Color         _black;
        private readonly bool          _hasDuration;
        private readonly SpineColorKey _key;

        private float _duration;
        private bool  _isExpired;

        public SpineColorSetting(Color color, bool hasDuration = false, float duration = 0f, SpineColorKey key = SpineColorKey.None)
        {
            _color       = color;
            _black       = Color.black;
            _hasDuration = hasDuration;
            _duration    = duration;
            _key         = key;
            _isExpired   = false;
        }

        public SpineColorSetting(Color color, Color blackColor, bool hasDuration = false, float duration = 0f, SpineColorKey key = SpineColorKey.None)
        {
            _color       = color;
            _black       = blackColor;
            _hasDuration = hasDuration;
            _duration    = duration;
            _key         = key;
            _isExpired   = false;
        }

        public override int GetHashCode()
        {
            var colorHash    = _color.GetHashCode();
            var blackHash    = _black.GetHashCode();
            var durationHash = _hasDuration.GetHashCode();
            var keyHash      = _key.GetHashCode();
            return colorHash ^ blackHash ^ durationHash ^ keyHash;
        }

        public Color Color     => _color;
        public bool  IsExpired => _isExpired;
        public SpineColorKey Key => _key;

        public void UpdateDuration(float deltaTime)
        {
            if (!_hasDuration)
            {
                return;
            }

            _duration -= deltaTime;

            if (_duration <= 0)
            {
                _isExpired = true;
            }
        }

        public void Expire()
        {
            _isExpired = true;
        }

        public void ExpireByKey(SpineColorKey key)
        {
            if (_key == key)
            {
                _isExpired = true;
            }
        }

        public static SpineColorSetting Default => new(Color.white);

        public void SetColor(Character character)
        {
            character.SetSpineColor(_color, ColorPropertyId);
            character.SetSpineColor(_black, BlackPropertyId);
        }
    }
}
