using UnityEngine;


namespace Nekoyume.Game
{
    public class ActionCamera : MonoBehaviour
    {
        public Transform target = null;
        public float followSpeedScale = 0.08f;
        public float targetRatioX = 0.3f;
        public int pixelPerUnit = 160;

        public void Update()
        {
            if (target != null)
            {
                float offsetX = (Screen.width * 0.5f - Screen.width * targetRatioX) / pixelPerUnit;
                Vector3 pos =  transform.position;
                pos.x += followSpeedScale * (target.position.x + offsetX - pos.x);
                transform.position = pos;
            }
        }
    }
}
