namespace Lib9c.Tests.TestHelper
{
    using Lib9c.DevExtensions.Action;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume.Model.State;

    public class MakeInitialStateResult
    {
        private readonly IAccountStateDelta _state;
        private readonly CreateTestbed _testbed;
        private readonly AgentState _agentState;
        private readonly AvatarState _avatarState;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly Address _rankingMapAddress;
        private readonly TableSheets _tableSheets;
        private readonly FungibleAssetValue _agentCurrencyGold;
        private readonly FungibleAssetValue _currencyGold;

        public MakeInitialStateResult(
            IAccountStateDelta state,
            CreateTestbed testbed,
            AgentState agentState,
            AvatarState avatarState,
            GoldCurrencyState goldCurrencyState,
            Address rankingMapAddress,
            TableSheets tableSheets,
            FungibleAssetValue currencyGold,
            FungibleAssetValue agentCurrencyGold)
        {
            _state = state;
            _testbed = testbed;
            _agentState = agentState;
            _avatarState = avatarState;
            _goldCurrencyState = goldCurrencyState;
            _rankingMapAddress = rankingMapAddress;
            _tableSheets = tableSheets;
            _currencyGold = currencyGold;
            _agentCurrencyGold = agentCurrencyGold;
        }

        public IAccountStateDelta GetState()
        {
            return _state;
        }

        public CreateTestbed GetTestbed()
        {
            return _testbed;
        }

        public AgentState GetAgentState()
        {
            return _agentState;
        }

        public AvatarState GetAvatarState()
        {
            return _avatarState;
        }

        public GoldCurrencyState GetGoldCurrencyState()
        {
            return _goldCurrencyState;
        }

        public Address GetRankingMapAddress()
        {
            return _rankingMapAddress;
        }

        public TableSheets GetTableSheet()
        {
            return _tableSheets;
        }

        public FungibleAssetValue GetCurrencyGold()
        {
            return _currencyGold;
        }

        public FungibleAssetValue GetAgentCurrencyGold()
        {
            return _agentCurrencyGold;
        }
    }
}
