using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.VFX;
using Nekoyume.UI;
using UnityEngine;
using Nekoyume.UI.Module;
using Nekoyume.Model.Item;

namespace Nekoyume.Game.Item
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class DropItem : MonoBehaviour
    {
        private const float BeginningAlphaOfFade = 0f;
        private const float DurationToFade = 0.5f;
        private const float DropJumpPower = 1f;
        private const float DurationToDrop = 0.5f;
        private const float DurationToGet = 1f;
        private const int SortOrder = 2000;
        private const float DistanceForInactive = 0.3f;
        private const float MultiplyForLerpSpeed = 3f;

        private static readonly Vector3 DefaultScale = Vector3.one * 0.625f;
        private static readonly float DelayAfterDrop = Mathf.Max(DurationToFade, DurationToDrop) + 0.2f;
        private static readonly Vector3 DropAmount = new Vector3(0.8f, 0f);

        private static UI.Battle _battle;
        private static Vector3 _inventoryPosition = Vector3.zero;

        public DropItemVFX dropItemVfx;
        public ItemBase Item { get; private set; }
        public List<ItemBase> Items { get; private set; }

        private SpriteRenderer _renderer;

        private Tweener _tweenFade;
        private Sequence _sequenceDrop;

        #region Mono

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();

            transform.localScale = DefaultScale;
        }

        private void OnEnable()
        {
            _battle = Widget.Find<UI.Battle>();

            UpdateInventoryPosition();
        }

        private void OnDisable()
        {
            _tweenFade?.Kill(true);
            _tweenFade = null;
            _sequenceDrop?.Kill(true);
            _sequenceDrop = null;
        }

        #endregion

        public IEnumerator CoSet(List<ItemBase> items)
        {
            Items = items;

            yield return StartCoroutine(CoPlay());
        }

        private IEnumerator CoPlay()
        {
            dropItemVfx.Stop();
            var pos = transform.position;
            var color = _renderer.color;
            color.a = BeginningAlphaOfFade;
            _renderer.color = color;
            _renderer.sortingOrder = SortOrder;

            _tweenFade = _renderer.DOFade(1f, DurationToFade);
            _sequenceDrop = transform.DOJump(pos + DropAmount, DropJumpPower, 1, DurationToDrop);

            var scale = transform.localScale;
            transform.DOScale(scale * 1.8f, 1.0f);
            yield return new WaitWhile(_sequenceDrop.IsPlaying);
            dropItemVfx.Play();

            yield return new WaitForSeconds(DelayAfterDrop);
            transform.DOScale(scale, 1.0f);

            while (true)
            {
                UpdateInventoryPosition();
                pos = Vector3.Lerp(transform.position, _inventoryPosition, Time.deltaTime * MultiplyForLerpSpeed);
                transform.position = pos;

                if ((_inventoryPosition - pos).magnitude < DistanceForInactive)
                {
                    break;
                }

                yield return null;
            }

            Widget.Find<BottomMenu>().PlayGetItemAnimation();
            Event.OnGetItem.Invoke(this);
            gameObject.SetActive(false);
        }

        private void UpdateInventoryPosition()
        {
            if (ReferenceEquals(_battle, null))
            {
                _inventoryPosition = new Vector3(-2.99f, -1.84f);
            }
            else
            {
                var bottomMenu = Widget.Find<BottomMenu>();
                if (!bottomMenu)
                {
                    throw new WidgetNotFoundException<BottomMenu>();
                }

                _inventoryPosition = bottomMenu.characterButton.button.transform.position;
                _inventoryPosition.z = transform.position.z;
            }
        }
    }
}
