using UnityEngine;
using EnhancedUI.EnhancedScroller;
using UniRx;
using Nekoyume.Game.Mail;

namespace Nekoyume.UI.Scroller
{
    public class MailScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
    {
        public EnhancedScroller scroller;
        public MailCellView cellViewPrefab;
        public readonly Subject<MailCellView> onClickCellView = new Subject<MailCellView>();

        private MailBox _mailBox;
        private float _cellViewHeight = 40f;

        #region Mono

        private void Awake()
        {
            this.ComponentFieldsNotNullTest();

            scroller.Delegate = this;
            _cellViewHeight = cellViewPrefab.GetComponent<RectTransform>().rect.height;
        }

        #endregion

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cellView = scroller.GetCellView(cellViewPrefab) as MailCellView;
            if(cellView is null)
            {
                throw new FailedToInstantiateGameObjectException(cellViewPrefab.name);
            }
            
            cellView.name = $"Cell {dataIndex}";
            cellView.SetData(_mailBox[dataIndex]);
            if (cellView.onClickDisposable is null)
            {
                cellView.onClickDisposable = cellView.onClickButton
                    .Subscribe(_ =>
                    {
                        onClickCellView.OnNext(cellView);
                        cellView.onClickDisposable.Dispose();
                        cellView.onClickDisposable = null;
                    }).AddTo(cellView.gameObject);
            }
            return cellView;
        }

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            return _cellViewHeight;
        }

        public int GetNumberOfCells(EnhancedScroller scroller)
        {
            return _mailBox.Count;
        }

        public void SetData(MailBox mailBox)
        {
            _mailBox = mailBox;
            scroller.ReloadData();
        }
    }
}
