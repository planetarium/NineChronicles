using System;
using Assets.SimpleLocalization;
using UnityEngine;

namespace Nekoyume.TableData
{
    [Serializable]
    public class SkillSheet : Sheet<int, SkillSheet.Row>
    {
        [Serializable]
        public struct Row : ISheetRow<int>
        {
            public int Id { get; private set; }
            public string Name { get; private set; }

            public int Key => Id;
            
            public void Set(string[] fields)
            {
                Id = int.TryParse(fields[0], out var id) ? id : 0;
                Name = fields[1];
            }
        }
    }

    public static class SkillSheetRowExtension
    {
        public static string GetLocalizedName(this SkillSheet.Row row)
        {
            return LocalizationManager.Localize($"SKILL_NAME_{row.Id}");
        }
        
        public static Sprite GetIcon(this SkillSheet.Row row)
        {
            var path = $"UI/Icons/Skill/{row.Id}";
            var sprite = Resources.Load<Sprite>(path);
            if (sprite)
            {
                return sprite;
            }
            
            path = $"UI/Icons/Skill/{100000}";
            sprite = Resources.Load<Sprite>(path);

            return sprite; 
        }
    }
}
