using Nekoyume.Model.State;
using Nekoyume.UI.Module;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace Nekoyume.UI
{
    using UniRx;

    public class BuffBonusResultPopup : PopupWidget
    {
        [SerializeField]
        private List<GameObject> buffViews = new List<GameObject>();

        private List<IDisposable> _disposablesOnDisabled = new List<IDisposable>();

        protected override void OnDisable()
        {
            base.OnDisable();
            _disposablesOnDisabled.DisposeAllAndClear();
        }

        public void Show(HackAndSlashBuffState state)
        {
            foreach (var viewParent in buffViews)
            {
                viewParent.SetActive(false);
            }

            var buffs = state.BuffIds
                .Select(buffId =>
                {
                    var randomBuffSheet = Game.Game.instance.TableSheets.CrystalRandomBuffSheet;
                    if (!randomBuffSheet.TryGetValue(buffId, out var bonusBuffRow))
                    {
                        return null;
                    }
                    return bonusBuffRow;
                })
                .OrderBy(x => x.Rank)
                .ThenBy(x => x.Id);

            foreach (var buff in buffs)
            {
                var viewParent = buffViews.First(x => !x.activeSelf);
                var view = viewParent.GetComponentInChildren<BonusBuffView>();
                view.SetData(buff);
                view.OnSelectedSubject
                    .Subscribe(row =>
                    {
                        foreach (var viewParent in buffViews)
                        {
                            var view = viewParent.GetComponentInChildren<BonusBuffView>();
                            view.UpdateSelected(row);
                        }
                    })
                    .AddTo(_disposablesOnDisabled);

                viewParent.SetActive(true);
            }

            base.Show();
        }
    }
}
