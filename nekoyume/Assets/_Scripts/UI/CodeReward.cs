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

        private Dictionary<string, List<(ItemBase, int)>> _codeRewards =
            new Dictionary<string, List<(ItemBase, int)>>();

        private const string SEALED_CODES = "SealedCodes";

        private RedeemCodeState _state = null;

        private RedeemCodeState State
        {
            get
            {
                if (_state == null)
                {
                    _state = new RedeemCodeState(
                        (Dictionary) Game.Game.instance.Agent.GetState(Addresses.RedeemCode));
                }

                return _state;
            }
            set => _state = value;
        }

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
            UpdateRewardButton();
        }

        public void Show(RedeemCodeState state)
        {
            State = state;
            UpdateRewardButton();
            base.Show();
        }

        private void UpdateRewardButton()
        {
            _codeRewards = GetSealedCodes()
                .Where(sealedCode => !string.IsNullOrEmpty(sealedCode))
                .ToDictionary(sealedCode => sealedCode,
                    sealedCode => GetItems(State, sealedCode));

            if (_codeRewards.Any())
            {
                Find<CodeRewardButton>().Show(OnClickButton, _codeRewards.Count);
            }
            else
            {
                Find<CodeRewardButton>().Close();
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
                Debug.LogError($"{e.Message} {redeemCode}");
                return new List<(ItemBase, int)>();
            }

            var reward = redeemCodeState.Map[publicKey];
            var tableSheets = Game.Game.instance.TableSheets;
            var itemSheet = tableSheets.ItemSheet;
            var row = tableSheets.RedeemRewardSheet.OrderedList.First(r => r.Id == reward.RewardId);
            var itemRewards = row.Rewards
                .Where(r => r.Type == RewardType.Item && r.ItemId.HasValue)
                .Select(r => (ItemFactory.CreateItem(itemSheet[r.ItemId.Value], new Cheat.DebugRandom()), r.Quantity))
                .ToList();

            return itemRewards;
        }

        private bool RedeemCode(string redeemCode)
        {
            var codes = GetSealedCodes();
            var code = codes.FirstOrDefault(x => x == redeemCode);
            if (code == null)
            {
                Debug.Log($"Code doesn't exist : {redeemCode}");
                return false;
            }

            codes.Remove(code);
            var sealedCodes = new SealedCodes(codes);
            var json = JsonUtility.ToJson(sealedCodes);
            PlayerPrefs.SetString(SEALED_CODES, json);
            return true;
        }

        public void AddSealedCode(string redeemCode)
        {
            var codes = GetSealedCodes();
            if (codes.Exists(x => x == redeemCode))
            {
                Debug.Log($"Code already exists : {redeemCode}");
                return;
            }

            codes.Add(redeemCode);

            var sealedCodes = new SealedCodes(codes);
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
