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
        private TextMeshProUGUI resourceWarningText;

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
            resourceWarningText.gameObject.SetActive(false);
        }

        private void Show()
        {
            enemy.gameObject.SetActive(false);
            npc.gameObject.SetActive(false);
            player.gameObject.SetActive(false);
            resourceWarningText.gameObject.SetActive(false);

            var text = resourceIdField.text;
            if (string.IsNullOrEmpty(text))
            {
                resourceWarningText.text = "Resource ID is empty.";
                resourceWarningText.gameObject.SetActive(true);
                return;
            }

            try
            {
                if (IsMonster(text))
                {
                    ShowMonster(text);
                }
                else if (IsNPC(text))
                {
                    ShowNPC(text);
                }
                else if (IsPlayer(text))
                {
                    ShowPlayer(text);
                }
                else if (IsFullCostume(text))
                {
                    ShowFullCostume(text);
                }
                else
                {
                    resourceWarningText.text = "Prefab name is not vaild.";
                    resourceWarningText.gameObject.SetActive(true);
                    return;
                }
            }
            catch (FailedToLoadResourceException<GameObject> e)
            {
                resourceWarningText.text = e.Message;
                resourceWarningText.gameObject.SetActive(true);
            }
        }

        private void ShowMonster(string id)
        {
            enemy.gameObject.SetActive(true);
            enemy.ChangeSpineResource(id);
        }

        private void ShowNPC(string id)
        {
            npc.gameObject.SetActive(true);
            npc.ChangeSpineResource(id);
        }

        private void ShowPlayer(string id)
        {
            player.gameObject.SetActive(true);
            player.ChangeSpineResource(id, false);
        }

        private void ShowFullCostume(string id)
        {
            player.gameObject.SetActive(true);
            player.ChangeSpineResource(id, true);
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
