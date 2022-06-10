using Nekoyume.Model.State;
using Nekoyume.UI.Module;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.TableData.Crystal;

namespace Nekoyume.UI
{
    using TMPro;
    using UniRx;

    public class BuffBonusResultPopup : PopupWidget
    {
        [SerializeField]
        private BonusBuffViewDataScriptableObject bonusBuffViewData;

        [SerializeField]
        private Image selectedBuffBg = null;

        [SerializeField]
        private Image selectedBuffIcon = null;

        [SerializeField]
        private TextMeshProUGUI selectedBuffText = null;

        [SerializeField]
        private List<GameObject> buffViewParents = null;

        public int? SelectedBuffId { get; set; }

        private readonly Dictionary<GameObject, BonusBuffView> buffViewMap
            = new Dictionary<GameObject, BonusBuffView>();

        private readonly Subject<CrystalRandomBuffSheet.Row> OnSelectedSubject
            = new Subject<CrystalRandomBuffSheet.Row>();

        protected override void Awake()
        {
            base.Awake();

            foreach (var viewParent in buffViewParents)
            {
                var view = viewParent.GetComponentInChildren<BonusBuffView>();
                buffViewMap[viewParent] = view;
                view.BonusBuffViewData = bonusBuffViewData;
            }

            OnSelectedSubject.Subscribe(row =>
            {
                foreach (var viewParent in buffViewParents)
                {
                    var inner = viewParent;
                    if (!inner.activeSelf)
                    {
                        continue;
                    }

                    var view = buffViewMap[inner];
                    if (view.UpdateSelected(row))
                    {
                        selectedBuffText.text = view.CurrentSkillName;
                        selectedBuffIcon.sprite = view.CurrentIcon;
                        selectedBuffBg.sprite = view.CurrentGradeData.BgSprite;
                        SelectedBuffId = row.Id;
                    }
                }
            })
            .AddTo(gameObject);
        }

        public void Show(HackAndSlashBuffState state)
        {
            foreach (var viewParent in buffViewParents)
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
                var viewParent = buffViewParents.First(x => !x.activeSelf);
                var view = buffViewMap[viewParent];
                view.SetData(buff, OnSelectedSubject.OnNext);
                viewParent.SetActive(true);
            }

            var selectedBuff = SelectedBuffId.HasValue ?
                buffs.First(x => x.Id == SelectedBuffId) : buffs.First();
            SelectedBuffId = selectedBuff.Id;
            OnSelectedSubject.OnNext(selectedBuff);
            base.Show();
        }
    }
}
