using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Analytics;

namespace Nekoyume.Manager
{
    public class AnalyticsManager
    {
        private static class Singleton
        {
            internal static readonly AnalyticsManager Value = new AnalyticsManager();

            static Singleton()
            {
            }
        }

        public class EventName
        {
            public const string ClickMainEquipment = "click_main_equipment";
            public const string ClickMainInventory = "click_main_inventory";
            public const string ClickMainShop = "click_main_shop";
            public const string ClickMainCombination = "click_main_combination";
            public const string ClickMainBattle = "click_main_battle";
            public const string ClickBattleBattleEntrance = "click_battle_battle_entrance";
            public const string ClickBattleContinuousBattle = "click_battle_continuous_battle";
            public const string ClickBattleEquipment = "click_battle_equipment";
            public const string ClickBattleInventory = "click_battle_inventory";
            public const string ClickBattleResultToMain = "click_battle_result_to_main";
            public const string ClickBattleResultRetry = "click_battle_result_retry";
            public const string ClickBattleResultNext = "click_battle_result_next";
            public const string ClickCombinationCombination = "click_combination_combination";
            public const string ClickCombinationEditMaterialItem = "click_combination_edit_material_item";
            public const string ClickCombinationRemoveMaterialItem = "click_combination_remove_material_item";

            public const string ActionBattle = "action_battle";
            public const string ActionBattleWin = "action_battle_win";
            public const string ActionBattleLose = "action_battle_lose";
            public const string ActionBattleContinuousCount = "action_battle_continuous_count";
            public const string ActionBattleItemList = "action_battle_item_list";
            public const string ActionCombination = "action_combination";
            public const string ActionCombinationSuccess = "action_combination_success";
            public const string ActionCombinationFail = "action_combination_fail";
            public const string ActionStatusLevelUp = "action_status_level_up";
        }

        private const string StringValue = "Value";

        public static AnalyticsManager Instance => Singleton.Value;

        private readonly Dictionary<string, object> _dictionary = new Dictionary<string, object>();

        private bool _isContinuousBattle;
        private int _continuousBattleCount;
        
        private bool _logEnabled = false;

        private AnalyticsManager()
        {
        }

        public void OnEvent(string eventName)
        {
            SendEvent(eventName);
        }

        public void OnEvent(string eventName, object value)
        {
            _dictionary.Add(StringValue, value);
            SendEvent(eventName, _dictionary);
            _dictionary.Clear();
        }

        public void BattleEntrance(bool continuous)
        {
            if (continuous)
            {
                _isContinuousBattle = true;
                _continuousBattleCount = 1;
                OnEvent(EventName.ClickBattleContinuousBattle);
            }
            else
            {
                _isContinuousBattle = false;
                _continuousBattleCount = 0;
                OnEvent(EventName.ClickBattleBattleEntrance);
            }
        }

        public void Battle(IEnumerable<int> itemIDs)
        {
            OnEvent(EventName.ActionBattle);

            var value = string.Join(",", itemIDs);
            OnEvent(EventName.ActionBattleItemList, value);
        }

        public void BattleLeave()
        {
            if (_isContinuousBattle)
            {
                OnEvent(EventName.ActionBattleContinuousCount, _continuousBattleCount);
            }

            OnEvent(EventName.ClickBattleResultToMain);
        }

        public void BattleContinueAutomatically()
        {
            _continuousBattleCount++;
        }

        private void SendEvent(string name)
        {
#if UNITY_EDITOR
            Log(name);
#else
            Analytics.SendEvent(name, 1);
#endif
        }

        private void SendEvent(string name, IDictionary<string, object> dictionary)
        {
#if UNITY_EDITOR
            Log(name, dictionary);
#else
            Analytics.CustomEvent(name, dictionary);
#endif
        }

        private void Log(string name)
        {
            if (!_logEnabled)
            {
                return;
            }
            
            Debug.LogWarning($"{name} : 1");
        }
        
        private void Log(string name, IDictionary<string, object> dictionary)
        {
            if (!_logEnabled)
            {
                return;
            }
            
            var sb = new StringBuilder();
            sb.AppendLine($"{name} : {{");
            foreach (var item in dictionary)
            {
                sb.AppendLine($"\t\"{item.Key}\" : {item.Value}");
            }

            sb.AppendLine("}");

            Debug.LogWarning(sb.ToString());
        }
    }
}
