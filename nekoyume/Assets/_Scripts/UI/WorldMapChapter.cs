using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class WorldMapChapter : MonoBehaviour
    {
        public int colCount = 5;
        public Vector2 spacing = new Vector2();
        public Vector2 rand = new Vector2();

        [ContextMenu ("Reposition")]
        private void Reposition()
        {
            var rect = GetComponent<RectTransform>();
            int x = 1;
            int y = 1;
            for (int childIndex = 0; childIndex < transform.childCount; ++childIndex)
            {
                if (childIndex > 0 && childIndex % colCount == 0)
                {
                    x = 1;
                    y++;
                }
                var child = transform.GetChild(childIndex);
                var randPosition = new Vector2(spacing.x * x, -spacing.y * y);
                randPosition.y += Random.Range(rand.x, rand.y);
                child.transform.localPosition = randPosition;
                x++;
            }
        }
    }
}
