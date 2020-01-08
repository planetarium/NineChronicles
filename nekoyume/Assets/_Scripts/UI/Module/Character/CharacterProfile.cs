using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CharacterProfile : MonoBehaviour
    {
        public bool enemy;
        public Image portrait;
        public Image portraitEnemy;
        public TextMeshProUGUI characterInfo;

        public void Set(int level, string nameWithHash, Sprite sprite)
        {
            characterInfo.text = $"<color=#B38271>Lv.{level}</color> {nameWithHash}";
            portrait.gameObject.SetActive(!enemy);
            portraitEnemy.gameObject.SetActive(enemy);
            if (enemy)
            {
                portraitEnemy.overrideSprite = sprite;
            }
            else
            {
                portrait.overrideSprite = sprite;
            }
        }
    }
}
