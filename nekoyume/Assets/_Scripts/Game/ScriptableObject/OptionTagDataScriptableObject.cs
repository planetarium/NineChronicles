using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.ScriptableObject;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_OptionTagData", menuName = "Scriptable Object/Option Tag Data",
        order = int.MaxValue)]
    public class OptionTagDataScriptableObject : ScriptableObject
    {
        [field: SerializeField]
        public Sprite StatOptionSprite { get; set; }

        [field: SerializeField]
        public Sprite SkillOptionSprite { get; set; }

        [SerializeField]
        private int fallbackGrade;

        [SerializeField]
        private List<OptionTagData> datas;

        public OptionTagData GetOptionTagData(int grade)
        {
            OptionTagData data = null;
            data = datas.FirstOrDefault(x => x.Grade == grade);
            if (data is null)
            {
                data = datas.FirstOrDefault(x => x.Grade == fallbackGrade);
            }

            return data;
        }
    }
}
