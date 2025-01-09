using System;
using System.Text;
using Newtonsoft.Json;
using Libplanet.Crypto;
using System.Threading.Tasks;
using System.Collections.Generic;
using Nekoyume.UI.Model;
using System.Linq;
using static ArenaServiceClient;
using Cysharp.Threading.Tasks;

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
                throw new InvalidOperationException("[ArenaServiceManager] Called before initialization");
            }

            try
            {
                string jwt = CreateJwt(Game.Game.instance.Agent.PrivateKey, avatarAddress);
                var models = new List<ArenaParticipantModel>();
                await UniTask.SwitchToMainThread();
                await Client.GetSeasonsAvailableopponentsAsync(seasonId, Game.Game.instance.Agent.BlockIndex, jwt,
                    on200OK: result =>
                    {
                        models.AddRange(
                            result.AvailableOpponents.Select(opponent => new ArenaParticipantModel
                            {
                                AvatarAddr = new Address(opponent.AvatarAddress),
                                NameWithHash = opponent.NameWithHash,
                                PortraitId = opponent.PortraitId,
                                Cp = (int)opponent.Cp,
                                Level = opponent.Level,
                                Score = opponent.Score,
                            })
                    );
                    }, onError: error =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Failed to get available opponents | " +
                            $"SeasonId: {seasonId} | " +
                            $"AvatarAddress: {avatarAddress ?? "null"} | " +
                            $"Error: {error}");
                    });

                return models;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[ArenaServiceManager] Failed to get available opponents | " +
                    $"SeasonId: {seasonId} | " +
                    $"AvatarAddress: {avatarAddress ?? "null"} | " +
                    $"Error: {e.Message}");
                throw;
            }
        }

        public async Task<ArenaParticipantModel> GetSeasonsLeaderboardParticipantAsync(int seasonId, string avatarAddress)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("[ArenaServiceManager] Called before initialization");
            }

            try
            {
                ArenaParticipantModel model = null;
                await UniTask.SwitchToMainThread();
                await Client.GetSeasonsLeaderboardParticipantsAsync(seasonId, avatarAddress,
                    on200OK: result =>
                    {
                        model = new ArenaParticipantModel
                        {
                            AvatarAddr = new Address(result.AvatarAddress),
                            NameWithHash = result.NameWithHash,
                            PortraitId = result.PortraitId,
                            Cp = (int)result.Cp,
                            Level = result.Level,
                            Score = result.Score,
                            Rank = result.Rank
                        };
                    }, onError: error =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Failed to get leaderboard participant | " +
                            $"SeasonId: {seasonId} | " +
                            $"AvatarAddress: {avatarAddress ?? "null"} | " +
                            $"Error: {error}");
                    });

                return model;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[ArenaServiceManager] Failed to get leaderboard participant | " +
                    $"SeasonId: {seasonId} | " +
                    $"AvatarAddress: {avatarAddress ?? "null"} | " +
                    $"Error: {e.Message}");
                throw;
            }
        }

        public async Task<SeasonResponse> GetSeasonByBlockAsync(Int64 blockIndex)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("[ArenaServiceManager] Called before initialization");
            }

            try
            {
                SeasonResponse currentSeason = null;
                // TODO: 아레나 서비스에서 타입변경되면 다시 수정해야함
                await UniTask.SwitchToMainThread();
                await Client.GetSeasonsByblockAsync((int)blockIndex,
                    on200OK: result =>
                    {
                        currentSeason = result;
                    }, onError: error =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Failed to get season by block | " +
                            $"BlockIndex: {blockIndex} | " +
                            $"Error: {error}");
                    });

                return currentSeason;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[ArenaServiceManager] Failed to get season by block | " +
                    $"BlockIndex: {blockIndex} | " +
                    $"Error: {e.Message}");
                throw;
            }
        }

        public async Task<SeasonResponse> PostSeasonsParticipantsAsync(int seasonId, string avatarAddress, string nameWithHash, int portraitId, Int64 cp, int level)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("[ArenaServiceManager] Called before initialization");
            }

            try
            {
                await UniTask.SwitchToMainThread();
                var request = new ParticipateRequest
                {
                    NameWithHash = nameWithHash,
                    PortraitId = portraitId,
                    Cp = cp,
                    Level = level
                };

                SeasonResponse seasonResponse = null;
                string jwt = CreateJwt(Game.Game.instance.Agent.PrivateKey, avatarAddress);
                await Client.PostSeasonsParticipantsAsync(seasonId, jwt, request,
                    on201Created: result =>
                    {
                        seasonResponse = result;
                    },
                    onError: error =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Failed to post seasons participants | " +
                            $"SeasonId: {seasonId} | " +
                            $"AvatarAddress: {avatarAddress ?? "null"} | " +
                            $"Error: {error}");
                    });

                return seasonResponse;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[ArenaServiceManager] Failed to post seasons participants | " +
                    $"SeasonId: {seasonId} | " +
                    $"AvatarAddress: {avatarAddress ?? "null"} | " +
                    $"Error: {e.Message}");
                throw;
            }
        }
    }
}
