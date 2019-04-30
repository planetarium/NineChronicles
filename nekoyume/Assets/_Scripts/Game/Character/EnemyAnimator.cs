using System.Linq;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class EnemyAnimator : MecanimCharacterAnimator
    {
        public EnemyAnimator(CharacterBase root) : base(root)
        {
        }
        
        public override Vector3 GetHUDPosition()
        {
            var spriteRenderer = root.GetComponentsInChildren<Renderer>()
                .OrderByDescending(r => r.transform.position.y)
                .First();
            var y = spriteRenderer.bounds.max.y - root.transform.position.y;
            var body = root.GetComponentsInChildren<Transform>().First(g => g.name == "body");
            var bodyRenderer = body.GetComponent<Renderer>();
            var x = bodyRenderer.bounds.min.x - root.transform.position.x + bodyRenderer.bounds.size.x / 2;
            return new Vector3(x, y, 0.0f);

        }
    }
}
