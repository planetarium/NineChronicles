using UnityEngine;
using EnhancedUI.EnhancedScroller;
using UniRx;
using System.Collections.Generic;

namespace Nekoyume.UI.Scroller
{
    public class MailScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
    {
        public EnhancedScroller scroller;
        public MailCellView cellViewPrefab;
        public readonly Subject<MailCellView> onClickCellView = new Subject<MailCellView>();

        private IReadOnlyList<Nekoyume.Model.Mail.Mail> _data;
        private float _cellViewHeight = 40f;

        #region Mono

        private void Awake()
        {
            scroller.Delegate = this;
            _cellViewHeight = cellViewPrefab.GetComponent<RectTransform>().rect.height;
        }

        #endregion

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cellView = scroller.GetCellView(cellViewPrefab) as MailCellView;
            if (cellView is null)
            {
                throw new FailedToInstantiateGameObjectException(cellViewPrefab.name);
            }

            cellView.name = $"Cell {dataIndex}";
            cellView.SetData(_data[dataIndex]);
            return cellView;
        }

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            return _cellViewHeight;
        }

        public int GetNumberOfCells(EnhancedScroller scroller)
        {
            return _data.Count;
        }

        public void SetData(List<Nekoyume.Model.Mail.Mail> mailBox)
        {
            _data = mailBox;
            scroller.ReloadData();
        }
    }
}
