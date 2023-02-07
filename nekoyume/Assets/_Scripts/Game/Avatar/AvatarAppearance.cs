using Nekoyume.Game.Character;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Avatar
{
    public class AvatarAppearance : MonoBehaviour
    {
        [SerializeField]
        private BoxCollider boxCollider;

        [SerializeField]
        private Animator animator;

        private CharacterAnimator _animator;
        private HudContainer _hudContainer;
        private GameObject _cachedCharacterTitle;

        public AvatarSpineController SpineController { get; private set; }
        public BoxCollider BoxCollider => boxCollider;


        public void Set()
        {
        }
    }
}
