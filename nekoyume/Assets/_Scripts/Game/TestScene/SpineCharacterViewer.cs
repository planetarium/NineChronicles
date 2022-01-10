using Nekoyume.Game.Character;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.TestScene
{
    public class SpineCharacterViewer : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField resourceIdField;

        [SerializeField]
        private Button loadButton;

        [SerializeField]
        private Enemy enemy;

        [SerializeField]
        private NPC npc;

        [SerializeField]
        private Player player;

        private void Awake()
        {
            loadButton.onClick.AddListener(Show);
        }

        private void Show()
        {
            var text = resourceIdField.text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (IsMonster(text))
            {
                if (!int.TryParse(text, out var id))
                {
                    return;
                }

                CreateMonster(id);
            }
            else if (IsNPC(text))
            {

            }
            else if (IsPlayer(text))
            {

            }
            else if (IsFullCostume(text))
            {

            }
            else
            {
                return;
            }

        }

        private void CreateMonster(int id)
        {
            enemy.UpdateSpineResource(id);

        }

        #region Check Type

        private bool IsPlayer(string prefabName)
        {
            return prefabName.StartsWith("1");
        }

        private bool IsMonster(string prefabName)
        {
            return prefabName.StartsWith("2");
        }

        private bool IsNPC(string prefabName)
        {
            return prefabName.StartsWith("3") ||
                prefabName.StartsWith("dialog_");
        }

        private bool IsFullCostume(string prefabName)
        {
            return prefabName.StartsWith("4");
        }

        #endregion
    }
}
