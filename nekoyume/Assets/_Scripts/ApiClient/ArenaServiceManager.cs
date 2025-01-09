using System;
using System.Text;
using Newtonsoft.Json;
using Libplanet.Crypto;
using System.Threading.Tasks;
using System.Collections.Generic;
using Nekoyume.UI.Model;
using System.Linq;
using static ArenaServiceClient;

namespace Nekoyume.ApiClient
{
    public class ArenaServiceManager : IDisposable
    {
        public ArenaServiceClient Client { get; private set; }

        public bool IsInitialized => Client != null;

        public ArenaServiceManager(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                NcDebug.LogError($"ArenaServiceManager Initialized Fail url is Null");
                return;
            }
            Client = new ArenaServiceClient(url);
        }

        public void Dispose()
        {
            Client?.Dispose();
            Client = null;
        }

        public static string CreateJwt(PrivateKey privateKey, string avatarAddress)
        {
            avatarAddress = avatarAddress.StartsWith("0x") ? avatarAddress.Substring(2) : avatarAddress;
            var payload = new
            {
                iss = "user",
                avt_adr = avatarAddress,
                sub = privateKey.PublicKey.ToHex(true),
                role = "User",
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                exp = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds()
            };

            string payloadJson = JsonConvert.SerializeObject(payload);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

            byte[] signature = privateKey.Sign(payloadBytes);

            string signatureBase64 = Convert.ToBase64String(signature);

            string headerJson = JsonConvert.SerializeObject(new { alg = "ES256K", typ = "JWT" });
            string headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson));
            string payloadBase64 = Convert.ToBase64String(payloadBytes);

            return $"Bearer {headerBase64}.{payloadBase64}.{signatureBase64}";
        }

        public async Task<List<ArenaParticipantModel>> GetSeasonsAvailableOpponentsAsync(int seasonId, string avatarAddress)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("ArenaServiceManager is not initialized");
            }

            try
            {
                string jwt = CreateJwt(Game.Game.instance.Agent.PrivateKey, avatarAddress);
                var models = new List<ArenaParticipantModel>();

                await Client.GetSeasonsAvailableopponentsAsync(seasonId, Game.Game.instance.Agent.BlockIndex, jwt, result =>
                {
                    models.AddRange(
                        result.AvailableOpponents.Select(opponent => new ArenaParticipantModel
                        {
                            AvatarAddr = new Address(opponent.AvatarAddress),
                            NameWithHash = opponent.NameWithHash,
                            PortraitId = opponent.PortraitId
                        })
                    );
                }, error =>{
                    NcDebug.LogError($"Failed to get seasons available opponents: {error}");
                });

                return models;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"Failed to get seasons available opponents: {e}");
                throw;
            }
        }

        public async Task<SeasonResponse> GetSeasonByBlockAsync(Int64 blockIndex)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("ArenaServiceManager is not initialized");
            }

            try
            {
                SeasonResponse currentSeason = null;
                // TODO: 아레나 서비스에서 타입변경되면 다시 수정해야함
                await Client.GetSeasonsByblockAsync((int)blockIndex, result =>
                {
                    currentSeason = result;
                }, error =>
                {
                    NcDebug.LogError($"Failed to get current season: {error}");
                });

                return currentSeason;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"Failed to get current season: {e}");
                throw;
            }
        }
    }
}
