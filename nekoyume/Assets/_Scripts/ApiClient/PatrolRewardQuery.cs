using System.Threading.Tasks;
using Nekoyume.UI.Model;

namespace Nekoyume.ApiClient
{
    public static class PatrolRewardQuery
    {
        public static async Task<(AvatarModel, PolicyModel)> InitializeInformation(string avatarAddress, string agentAddress, int level)
        {
            var serviceClient = ApiClients.Instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return (null, null);
            }

            var query =
                $@"query {{
    avatar(avatarAddress: ""{avatarAddress}"", agentAddress: ""{agentAddress}"") {{
        avatarAddress
        agentAddress
        createdAt
        lastClaimedAt
        level
    }}
    policy(level: {level}, free: true) {{
        activate
        minimumLevel
        maxLevel
        minimumRequiredInterval
        rewards {{
            ... on FungibleAssetValueRewardModel {{
                currency
                perInterval
                rewardInterval
            }}
            ... on FungibleItemRewardModel {{
                itemId
                perInterval
                rewardInterval
            }}
        }}
    }}
}}";

            var response = await serviceClient.GetObjectAsync<InitializeResponse>(query);
            if (response is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(InitializeResponse)}");
                return (null, null);
            }

            if (response.Policy is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(PolicyResponse.Policy)}");
            }

            return (response.Avatar ?? await PutAvatar(avatarAddress, agentAddress), response.Policy);
        }

        public static async Task<AvatarModel> LoadAvatarInfo(string avatarAddress, string agentAddress)
        {
            var serviceClient = ApiClients.Instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return null;
            }

            var query = $@"query {{
                avatar(avatarAddress: ""{avatarAddress}"", agentAddress: ""{agentAddress}"") {{
                    avatarAddress
                    agentAddress
                    createdAt
                    lastClaimedAt
                    level
                }}
            }}";

            var response = await serviceClient.GetObjectAsync<AvatarResponse>(query);
            if (response is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(AvatarResponse)}");
                return null;
            }

            return response.Avatar ?? await PutAvatar(avatarAddress, agentAddress);
        }

        public static async Task<PolicyModel> LoadPolicyInfo(int level, bool free = true)
        {
            var serviceClient = ApiClients.Instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return null;
            }

            var query =
                $@"query {{
    policy(level: {level}, free: true) {{
        activate
        minimumLevel
        maxLevel
        minimumRequiredInterval
        rewards {{
            ... on FungibleAssetValueRewardModel {{
                currency
                perInterval
                rewardInterval
            }}
            ... on FungibleItemRewardModel {{
                itemId
                perInterval
                rewardInterval
            }}
        }}
    }}
}}";

            var response = await serviceClient.GetObjectAsync<PolicyResponse>(query);
            if (response is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(PolicyResponse)}");
                return null;
            }

            if (response.Policy is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(PolicyResponse.Policy)}");
            }

            return response.Policy;
        }

        public static async Task<string> ClaimReward(string avatarAddress, string agentAddress)
        {
            var serviceClient = ApiClients.Instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return null;
            }

            var query =
                $@"mutation {{
    claim(avatarAddress: ""{avatarAddress}"", agentAddress: ""{agentAddress}"")
}}";

            var response = await serviceClient.GetObjectAsync<ClaimResponse>(query);
            if (response is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(ClaimResponse)}");
                return null;
            }

            if (response.Claim is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(ClaimResponse.Claim)}");
            }

            return response.Claim;
        }

        private static async Task<AvatarModel> PutAvatar(string avatarAddress, string agentAddress)
        {
            var serviceClient = ApiClients.Instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return null;
            }

            var query =
                $@"mutation {{
    putAvatar(avatarAddress: ""{avatarAddress}"", agentAddress: ""{agentAddress}"") {{
        avatarAddress
        agentAddress
        createdAt
        lastClaimedAt
        level
    }}
}}";

            var response = await serviceClient.GetObjectAsync<PutAvatarResponse>(query);
            if (response is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(PutAvatarResponse)}");
                return null;
            }

            if (response.PutAvatar is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(PutAvatarResponse.PutAvatar)}");
            }

            return response.PutAvatar;
        }
    }
}
