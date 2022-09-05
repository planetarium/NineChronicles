using System.Linq;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class SkillIconHelper
    {
        private static SkillIconScriptableObject _skillIcon;

        private static SkillIconScriptableObject SkillIcon
        {
            get
            {
                if (_skillIcon == null)
                {
                    _skillIcon = Resources.Load<SkillIconScriptableObject>(
                        "ScriptableObject/UI_SkillIcon");
                }

                return _skillIcon;
            }
        }

        public static Sprite GetSkillIcon(int id)
        {
            return SkillIcon.Icons.FirstOrDefault(x => x.id.Equals(id))?.icon;
        }

        public static GameObject GetSkillIconPrefab()
        {
            return SkillIcon.SkillIconPrefab;
        }
    }
}
