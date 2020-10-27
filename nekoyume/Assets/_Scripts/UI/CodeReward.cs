using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nekoyume.UI
{
    public class CodeReward : PopupWidget
    {
        [SerializeField] private CodeRewardEffector effector = null;

        private RedeemCodeState _state;

        private Dictionary<string, List<(ItemBase, int)>> _codeRewards =
            new Dictionary<string, List<(ItemBase, int)>>();

        private const string SEALED_CODES = "SealedCodes";

        [Serializable]
        public class SealedCodes
        {
            public List<string> Codes;

            public SealedCodes(List<string> codes)
            {
                Codes = codes;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _state = new RedeemCodeState(
                (Dictionary) Game.Game.instance.Agent.GetState(Addresses.RedeemCode));
            Show(_state);
        }

        public void Show(RedeemCodeState state)
        {
            _state = state;
            UpdateRewardButton();
            base.Show();
        }

        private void UpdateRewardButton()
        {
            var sealedCodes = GetSealedCodes();
            _codeRewards = sealedCodes.Where(IsExistCode).ToDictionary(code => code, GetItems);
            var button = Find<BottomMenu>().codeRewardButton;
            if (_codeRewards.Any())
            {
                button.Show(OnClickButton, _codeRewards.Count);
            }
            else
            {
                button.Close();
            }
        }

        private void OnClickButton()
        {
            if (!_codeRewards.Any())
            {
                return;
            }

            var reward = _codeRewards.First();
            if (RedeemCode(reward.Key))
            {
                effector.Play(reward.Value);
                UpdateRewardButton();
            }
        }

        private List<(ItemBase, int)> GetItems(string redeemCode)
        {
            var privateKey = new PrivateKey(ByteUtil.ParseHex(redeemCode));
            PublicKey publicKey = privateKey.PublicKey;
            var reward = _state.Map[publicKey];

            TableSheets tableSheets = Game.Game.instance.TableSheets;
            ItemSheet itemSheet = tableSheets.ItemSheet;
            RedeemRewardSheet.Row row =
                tableSheets.RedeemRewardSheet.OrderedList.First(r => r.Id == reward.RewardId);
            var itemRewards = row.Rewards.Where(r => r.Type != RewardType.Gold)
                .Select(r => (ItemFactory.CreateItem(itemSheet[r.ItemId.Value], new Cheat.DebugRandom()), r.Quantity))
                .ToList();

            return itemRewards;
        }

        private bool IsExistCode(string redeemCode)
        {
            var privateKey = new PrivateKey(ByteUtil.ParseHex(redeemCode));
            PublicKey publicKey = privateKey.PublicKey;
            return _state.Map.ContainsKey(publicKey);
        }

        private bool IsUsed(string redeemCode)
        {
            var privateKey = new PrivateKey(ByteUtil.ParseHex(redeemCode));
            PublicKey publicKey = privateKey.PublicKey;

            if (_state.Map.ContainsKey(publicKey))
            {
                return _state.Map[publicKey].UserAddress.HasValue;
            }

            Debug.Log($"Code doesn't exist : {redeemCode}");
            return true;
        }

        private bool RedeemCode(string redeemCode)
        {
            var states = GetSealedCodes();
            var code = states.FirstOrDefault(x => x == redeemCode);
            if (code == null)
            {
                Debug.Log($"Code doesn't exist : {redeemCode}");
                return false;
            }

            states.Remove(code);
            var sealedCodes = new SealedCodes(states);
            var json = JsonUtility.ToJson(sealedCodes);
            PlayerPrefs.SetString(SEALED_CODES, json);
            return true;
        }

        public void AddSealedCode(string redeemCode)
        {
            if (IsUsed(redeemCode))
            {
                Debug.Log($"This code already used : {redeemCode}");
                return;
            }

            var states = GetSealedCodes();
            if (states.Exists(x => x == redeemCode))
            {
                Debug.Log($"Code already exists : {redeemCode}");
                return;
            }

            states.Add(redeemCode);

            var sealedCodes = new SealedCodes(states);
            var json = JsonUtility.ToJson(sealedCodes);
            PlayerPrefs.SetString(SEALED_CODES, json);
        }

        private List<string> GetSealedCodes()
        {
            if (!PlayerPrefs.HasKey(SEALED_CODES))
            {
                var newStates = JsonUtility.ToJson(new SealedCodes(new List<string> { }));
                PlayerPrefs.SetString(SEALED_CODES, newStates);
            }

            var states = PlayerPrefs.GetString(SEALED_CODES);
            var codes = JsonUtility.FromJson<SealedCodes>(states).Codes;
            return codes != null ? codes.ToList() : new List<string>();
        }
    }
}
