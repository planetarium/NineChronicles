using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class Recipe : MonoBehaviour
    {
        public RectTransform content;
        public List<GameObject> slots;

        #region Mono

        // Start is called before the first frame update
        protected void Awake()
        {
            
        }

        #endregion

        public void Show()
        {
            float estHeight = slots.Count * 90;
            content.sizeDelta = new Vector2(0, (estHeight >= 500) ? estHeight - 500 : 500);
        }

        public void Close()
        {

        }
    }
}