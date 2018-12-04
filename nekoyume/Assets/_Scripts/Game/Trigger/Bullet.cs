using UnityEngine;

namespace Nekoyume.Game.Trigger
{
    public class Bullet : Damager
    {
        private void Awake()
        {
            var anim = GetComponent<Util.SpriteAnimator>();
            anim.Repeat = true;
        }

        private void FixedUpdate()
        {
            var position = gameObject.transform.position;
            position.x += Time.deltaTime;
            gameObject.transform.position = position;
            if (_hitObjects.Count >= _targetCount)
            {
                var anim = GetComponent<Util.SpriteAnimator>();
                anim.Destroy();
            }
        }
    }
}
