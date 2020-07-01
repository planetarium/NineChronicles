using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class HelpPopup : PopupWidget
    {
        private static HelpPopup _instanceCache;

        private static HelpPopup Instance => _instanceCache
            ? _instanceCache
            : _instanceCache = Find<HelpPopup>();

        [SerializeField]
        private Button nextButton = null;

        [SerializeField]
        private Button previousButton = null;

        [SerializeField]
        private Button gotItButton = null;

        private int _id;
        private int _pageIndex;

        public static void Help(int id)
        {
            if (Instance.IsActive())
            {
                Instance.Close(true);
            }

            Instance.SetId(id);
            Instance.Show();
        }

        protected override void Awake()
        {
            base.Awake();

            nextButton.OnClickAsObservable().Subscribe(Next).AddTo(gameObject);
            previousButton.OnClickAsObservable().Subscribe(Previous).AddTo(gameObject);
            gotItButton.OnClickAsObservable().Subscribe(GotIt).AddTo(gameObject);
        }

        private void SetId(int id)
        {
            if (id == _id)
            {
                return;
            }

            _id = id;
            SetPage(0);
        }

        private void SetPage(int pageIndex)
        {
            _pageIndex = pageIndex;
        }

        private void Next(Unit unit)
        {
            SetPage(_pageIndex + 1);
        }

        private void Previous(Unit unit)
        {
            SetPage(_pageIndex - 1);
        }

        private void GotIt(Unit unit)
        {
            Close();
        }
    }
}
