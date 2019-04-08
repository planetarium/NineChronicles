using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.UI;
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
        private const float DistanceForInactive = 0.3f;
        private const float MultiplyForLerpSpeed = 2f;

        private static readonly Vector3 DefaultScale = Vector3.one * 0.625f;
        private static readonly float DelayAfterDrop = Mathf.Max(DurationToFade, DurationToDrop) + 0.8f;
        private static readonly Vector3 DropAmount = new Vector3(-0.1f, 0f);

        private static Camera _cam = null;
        private static Status _status = null;
        private static Vector3 _inventoryPosition = Vector3.zero;

        public ItemBase Item { get; private set; }
        public List<ItemBase> Items { get; private set; }

        private SpriteRenderer _renderer = null;

        private Tweener _tweenerFade = null;
        private Sequence _sequenceDrop = null;
        private Sequence _sequenceGet = null;

        #region Mono

        private void Awake()
        {
            if (ReferenceEquals(_cam, null))
            {
                _cam = Camera.main;
                if (ReferenceEquals(_cam, null))
                {
                    throw new NotFoundComponentException<Camera>();
                }
            }

            _renderer = GetComponent<SpriteRenderer>();

            transform.localScale = DefaultScale;
        }

        private void OnEnable()
        {
            _status = Widget.Find<Status>();

            UpdateInventoryPosition();
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

        #endregion

        public IEnumerator CoSet(List<ItemBase> items)
        {
            if (ReferenceEquals(_cam, null))
            {
                gameObject.SetActive(false);
                yield break;
            }

            Items = items;

            yield return StartCoroutine(CoPlay());
        }

        private IEnumerator CoPlay()
        {
            var pos = transform.position;
            var color = _renderer.color;
            color.a = BeginningAlphaOfFade;
            _renderer.color = color;
            _renderer.sortingOrder = SortOrder;

            _tweenerFade = _renderer.DOFade(1f, DurationToFade);
            _sequenceDrop = transform.DOJump(pos + DropAmount, DropJumpPower, 1, DurationToDrop);
            yield return new WaitForSeconds(DelayAfterDrop);
            while (true)
            {
                UpdateInventoryPosition();
                pos = Vector3.Lerp(transform.position, _inventoryPosition, Time.deltaTime * MultiplyForLerpSpeed);
                transform.position = pos;

                if ((_inventoryPosition - pos).magnitude < DistanceForInactive)
                {
                    break;
                }
                else
                {
                    yield return null;
                }
            }

            gameObject.SetActive(false);
        }

        private void UpdateInventoryPosition()
        {
            if (ReferenceEquals(_cam, null) ||
                ReferenceEquals(_status, null))
            {
                _inventoryPosition = new Vector3(-2.99f, -1.84f);
            }
            else
            {
                _inventoryPosition = _cam.ScreenToWorldPoint(_status.BtnInventory.transform.position);
                _inventoryPosition.z = transform.position.z;
            }
        }
    }
}
