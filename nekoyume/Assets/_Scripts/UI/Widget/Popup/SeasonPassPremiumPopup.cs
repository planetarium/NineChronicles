using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Nekoyume.UI
{
    public class SeasonPassPremiumPopup : PopupWidget
    {
        [SerializeField]
        GameObject[] IsPremiumObj;
        [SerializeField]
        GameObject[] NotPremiumObj;

        [SerializeField]
        GameObject[] IsPremiumPlusObj;
        [SerializeField]
        GameObject[] NotPremiumPlusObj;

        protected override void Awake()
        {
            base.Awake();
            var seasonPassManager = Game.Game.instance.SeasonPassServiceManager;
            seasonPassManager.AvatarInfo.Subscribe((seasonPassInfo) => {
                if (seasonPassInfo == null)
                    return;

                foreach (var item in IsPremiumObj)
                {
                    item.SetActive(seasonPassInfo.IsPremium);
                }
                foreach (var item in NotPremiumObj)
                {
                    item.SetActive(!seasonPassInfo.IsPremium);
                }

                foreach (var item in IsPremiumPlusObj)
                {
                    item.SetActive(seasonPassInfo.IsPremiumPlus);
                }
                foreach (var item in NotPremiumPlusObj)
                {
                    item.SetActive(!seasonPassInfo.IsPremiumPlus);
                }
            }).AddTo(gameObject);
        }
    }
}
