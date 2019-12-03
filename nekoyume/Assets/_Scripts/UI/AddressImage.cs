using System;
using System.IO;
using Jdenticon;
using Libplanet;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class AddressImage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image accountImage;
        public TextMeshProUGUI accountAddressText;

        private void Awake()
        {
            accountAddressText.transform.parent.gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            accountAddressText.transform.parent.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            accountAddressText.transform.parent.gameObject.SetActive(false);
        }

        public void Set(Address address)
        {
            var image = Identicon.FromValue(address, 64);
            var bgColor = image.Style.BackColor;
            image.Style.BackColor = Jdenticon.Rendering.Color.FromRgba(bgColor.R, bgColor.G, bgColor.B, 0);
            var ms = new MemoryStream();
            image.SaveAsPng(ms);
            var buffer = new byte[ms.Length];
            ms.Read(buffer,0,buffer.Length);
            var t = new Texture2D(8,8);
            if (t.LoadImage(ms.ToArray()))
            {
                var sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero);
                accountImage.overrideSprite = sprite;
                accountImage.SetNativeSize();
                accountAddressText.text = address.ToString();
            }

        }
    }
}
