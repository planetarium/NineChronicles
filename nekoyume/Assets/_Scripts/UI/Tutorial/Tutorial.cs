using UnityEngine;

namespace Nekoyume.UI
{
    public class Tutorial : MonoBehaviour
    {
        [SerializeField] private GuideArrow arrow;
        [SerializeField] private GuideBackground background;
        [SerializeField] private GuideDialog dialog;

        private const int ItemCount = 3;
        private int _finishRef;
        private bool _isPlaying;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                var objectPosition = new Vector2(0, 100);
                Play(new GuideBackgroundData(true, true, objectPosition, PlayEnd),
                    new GuideArrowData(GuideType.Square, objectPosition, false, PlayEnd),
                    new GuideDialogData(objectPosition.y, PlayEnd));
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                var objectPosition = new Vector2(0, -100);
                Play(new GuideBackgroundData(false, true, objectPosition, PlayEnd),
                    new GuideArrowData(GuideType.Square, objectPosition, true, PlayEnd),
                    new GuideDialogData(objectPosition.y, PlayEnd));
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                var objectPosition = new Vector2(100, -100);
                Play(new GuideBackgroundData(false, true, objectPosition, PlayEnd),
                    new GuideArrowData(GuideType.Square, objectPosition, false, PlayEnd),
                    new GuideDialogData(objectPosition.y, PlayEnd));
            }
        }

        public void Play(GuideBackgroundData backgroundData,
            GuideArrowData arrowData,
            GuideDialogData dialogData)
        {
            if (_isPlaying)
            {
                return;
            }

            _finishRef = 0;
            _isPlaying = true;
            background.Play(backgroundData);
            arrow.Play(arrowData);
            dialog.Play(dialogData);
        }

        private void PlayEnd()
        {
            _finishRef += 1;
            if (_finishRef >= ItemCount)
            {
                _isPlaying = false;
                // Stop();
            }
        }

        public void Stop()
        {
            background.Stop();
            arrow.Stop();
            dialog.Stop();
        }
    }
}
