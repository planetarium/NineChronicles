using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class WorldClearPopup : PopupWidget
    {
        [SerializeField] private TextMeshProUGUI worldIdText;
        [SerializeField] private TextMeshProUGUI worldNameText;
        [SerializeField] private Button closeButton;

        public bool Displaying => gameObject.activeSelf;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });
        }

        public void Show(int worldId, string worldName, bool ignoreShowAnimation = false)
        {
            worldIdText.text = IntToRoman(worldId);
            worldNameText.text = worldName;

            base.Show(ignoreShowAnimation);
        }

        private static string IntToRoman(int num)
        {
            var romanLetters = new[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            var numbers = new[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };

            var result = string.Empty;
            int i = 0;
            while (num > 0)
            {
                if (num >= numbers[i])
                {
                    num -= numbers[i];
                    result += romanLetters[i];
                }
                else
                {
                    i++;
                }
            }

            return result;
        }
    }
}
