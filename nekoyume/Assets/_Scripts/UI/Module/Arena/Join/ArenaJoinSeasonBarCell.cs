using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module.Arena.Join
{
    [Serializable]
    public class ArenaJoinSeasonBarItemData
    {
        public bool visible;
    }

    public class ArenaJoinSeasonBarScrollContext
    {
        public int selectedIndex = -1;
    }

    public class ArenaJoinSeasonBarCell :
        FancyCell<ArenaJoinSeasonBarItemData, ArenaJoinSeasonBarScrollContext>
    {
        private static class AnimatorHash
        {
            public static readonly int Scroll = Animator.StringToHash("Scroll");
        }

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private Image _image;

        [SerializeField]
        private TextMeshProUGUI _text;

        private ArenaJoinSeasonBarItemData _currentData;

        [SerializeField]
        private float _currentPosition;

        private void OnEnable() => UpdatePosition(_currentPosition);

        public override void UpdateContent(ArenaJoinSeasonBarItemData itemData)
        {
            _currentData = itemData;
            _image.enabled = _currentData.visible;
        }

        public override void UpdatePosition(float position)
        {
            _currentPosition = position;
            PlayAnimation(_animator, _currentPosition);
        }

        private static void PlayAnimation(Animator animator, float normalizedTime)
        {
            if (animator.isActiveAndEnabled)
            {
                animator.Play(AnimatorHash.Scroll, -1, normalizedTime);
            }

            animator.speed = 0;
        }
    }
}
