using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Common;
using Libplanet.Crypto;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CodeRewardPopup : PopupWidget
    {
        [SerializeField]
        private CodeRewardEffector effector;

        [SerializeField]
        private Button button;

        [SerializeField]
        private TextMeshProUGUI count;

        private readonly Dictionary<string, List<(ItemBase, int)>> _codeRewards =
            new Dictionary<string, List<(ItemBase, int)>>();

        protected override void Awake()
        {
            base.Awake();
            button.onClick.AddListener(OnClickButton);
        }

        public void Show(string sealedCode, RedeemCodeState state)
        {
            _codeRewards.Add(sealedCode, GetItems(state, sealedCode));
            UpdateButton(_codeRewards.Count);
            base.Show();
        }

        private void OnClickButton()
        {
            if (!_codeRewards.Any())
            {
                return;
            }

            var reward = _codeRewards.First();
            effector.gameObject.SetActive(true);
            effector.Play(reward.Value);
            _codeRewards.Remove(reward.Key);
            UpdateButton(_codeRewards.Count);
        }

        private void UpdateButton(int value)
        {
            count.text = value.ToString();
            button.gameObject.SetActive(value > 0);
        }

        private static List<(ItemBase, int)> GetItems(RedeemCodeState redeemCodeState, string redeemCode)
        {
            PublicKey publicKey;
            try
            {
                var privateKey = new PrivateKey(ByteUtil.ParseHex(redeemCode));
                publicKey = privateKey.PublicKey;
            }
            catch (FormatException e)
            {
                NcDebug.LogError($"{e.Message} {redeemCode}");
                return new List<(ItemBase, int)>();
            }

            var reward = redeemCodeState.Map[publicKey];
            var tableSheets = Game.Game.instance.TableSheets;
            var itemSheet = tableSheets.ItemSheet;
            var row = tableSheets.RedeemRewardSheet.OrderedList.First(r => r.Id == reward.RewardId);
            var itemRewards = row.Rewards
                .Where(r => r.Type == RewardType.Item && r.ItemId.HasValue)
                .Select(r => (
                    ItemFactory.CreateItem(itemSheet[r.ItemId.Value], new Cheat.DebugRandom()),
                    r.Quantity))
                .ToList();

            return itemRewards;
        }
    }
}
