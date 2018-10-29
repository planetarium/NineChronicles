using UnityEngine;

namespace Nekoyume.Game
{
    public class FollowCamera : MonoBehaviour
    {
        public Transform target = null;
        public float followSpeedScale = 0.1f;

        public FollowCamera()
        {
        }

        public void Update()
        {
            if (target != null)
            {
                Vector3 pos =  transform.position;
                pos.x += followSpeedScale * (target.position.x - pos.x);
                transform.position = pos;
            }
        }
    }
}
