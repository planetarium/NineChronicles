using Nekoyume.Game.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class SkillIcon : MonoBehaviour
    {
        [SerializeField]
        private Image skillIcon;

        [SerializeField]
        private TouchHandler touchHandler;

        public void Set(Sprite icon)
        {
            skillIcon.sprite = icon;
        }
    }
}
