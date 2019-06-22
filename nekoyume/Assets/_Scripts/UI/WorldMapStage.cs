using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class WorldMapStage : MonoBehaviour
    {
        private readonly Color DisabledColor = new Color(255.0f, 255.0f, 255.0f, 200.0f);

        public Button button;
        public Image icon;
        public Text label;

        public Image normalImage;
        public Image selectedImage;
        public Image clearedImage;
        public Image disabledImage;

        public WorldMap Parent { get; set; }
        public int Value { get; set; }

        public void SetImage(Image img)
        {
            normalImage.gameObject.SetActive(false);
            selectedImage.gameObject.SetActive(false);
            clearedImage.gameObject.SetActive(false);
            disabledImage.gameObject.SetActive(false);

            img.gameObject.SetActive(true);
        }

        public void OnClick()
        {
            Parent.SelectedStage = Value;
            Parent.Close();
        }
    }
}