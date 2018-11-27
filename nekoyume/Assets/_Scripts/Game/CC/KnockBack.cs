using DG.Tweening;
using UnityEngine;


namespace Nekoyume.Game.CC
{
    public class KnockBack : MonoBehaviour
    {
        public void Set(float power, float duration = 0.5f)
        {
            Vector3 endPosition = transform.TransformPoint(power, 0.0f, 0.0f);
            transform.DOJump(endPosition, 0.02f, 1, duration).onComplete = EndCallback;
        }

        private void EndCallback()
        {
            Destroy(this);
        }
    }
}
