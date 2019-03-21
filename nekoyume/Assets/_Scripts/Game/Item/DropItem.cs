using System;
using DG.Tweening;
using System.Collections;
using UnityEngine;


namespace Nekoyume.Game.Item
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class DropItem : MonoBehaviour
    {
        private const float BeginningAlphaOfFade = 0f;
        private const float DurationToFade = 0.5f;
        private const float DropJumpPower = 0.6f;
        private const float DurationToDrop = 0.7f;
        private const float DurationToGet = 1f;
        private const int SortOrder = 2000;

        private static readonly float DelayAfterDrop = Mathf.Max(DurationToFade, DurationToDrop) + 0.8f;
        private static readonly Vector3 DropAmount = new Vector3(-0.1f, 0f);
        private static readonly Vector3 GetPosition = new Vector3(-2.99f, -1.84f);

        private static Camera _cam = null;

        public ItemBase Item { get; private set; }

        private SpriteRenderer _renderer = null;
        
        private Tweener _tweenerFade = null;
        private Sequence _sequenceDrop = null;
        private Sequence _sequenceGet = null;

        // Mono

        private void Awake()
        {
            if (ReferenceEquals(_cam, null))
            {
                _cam = Camera.main;
            }

            _renderer = GetComponent<SpriteRenderer>();

            transform.localScale = Vector3.one * 0.625f;
        }

        private void OnDisable()
        {
            _tweenerFade?.Kill();
            _tweenerFade = null;
            _sequenceDrop?.Kill();
            _sequenceDrop = null;
            _sequenceGet?.Kill();
            _sequenceGet = null;
        }

        // ~Mono

        public void Set(ItemBase item)
        {
            if (ReferenceEquals(_cam, null))
            {
                gameObject.SetActive(false);
                return;
            }

            Item = item;

            StartCoroutine(CoPlay());
        }

        private IEnumerator CoPlay()
        {
            var pos = transform.position;
            var endPos = _cam.transform.TransformPoint(GetPosition);
            var color = _renderer.color;
            color.a = BeginningAlphaOfFade;
            _renderer.color = color;
            _renderer.sortingOrder = SortOrder;

            _tweenerFade = _renderer.DOFade(1f, DurationToFade);
            _sequenceDrop = transform.DOJump(pos + DropAmount, DropJumpPower, 1, DurationToDrop);
            yield return new WaitForSeconds(DelayAfterDrop);
            _sequenceGet = transform.DOJump(endPos, 1f, 1, DurationToGet);
            yield return new WaitForSeconds(DurationToGet);

            gameObject.SetActive(false);
        }
    }
}