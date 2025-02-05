using System;
using System.Text;
using Newtonsoft.Json;
using Libplanet.Crypto;
using System.Threading.Tasks;
using System.Collections.Generic;
using Nekoyume.UI.Model;
using System.Linq;
using GeneratedApiNamespace.ArenaServiceClient;
using Cysharp.Threading.Tasks;
using Libplanet.Types.Assets;
using System.Globalization;
using Nekoyume;

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

        public static string CreateCurrentJwt()
        {
            return CreateJwt(Game.Game.instance.Agent.PrivateKey, Game.Game.instance.States.CurrentAvatarState.address.ToHex());
        }

        public async Task<List<ArenaParticipantModel>> GetAvailableopponentsAsync(string avatarAddress)
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
                await Client.GetAvailableopponentsAsync(jwt,
                    on200: result =>
                    {
                        models.AddRange(
                            result.Select(opponent => new ArenaParticipantModel
                            {
                                AvatarAddr = new Address(opponent.AvatarAddress),
                                NameWithHash = opponent.NameWithHash,
                                PortraitId = opponent.PortraitId,
                                Cp = (int)opponent.Cp,
                                Level = opponent.Level,
                                Score = opponent.Score,
                            })
                    );
                    },
                    onError: error =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Failed to get available opponents | " +
                            $"AvatarAddress: {avatarAddress ?? "null"} | " +
                            $"Error: {error}");
                    });

                return models;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[ArenaServiceManager] Failed to get available opponents | " +
                    $"AvatarAddress: {avatarAddress ?? "null"} | " +
                    $"Error: {e.Message}");
                throw e;
            }
        }

        public async Task<int> PostTicketsRefreshPurchaseAsync(string txId, FungibleAssetValue amount, string avatarAddress)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("[ArenaServiceManager] Called before initialization");
            }

            try
            {
                await UniTask.SwitchToMainThread();

                decimal calculatedAmount = decimal.Parse(amount.GetQuantityString(), CultureInfo.InvariantCulture);

                var request = new PurchaseTicketRequest
                {
                    TicketCount = 1,
                    PurchasePrice = calculatedAmount,
                    TxId = txId,
                };

                string jwt = CreateJwt(Game.Game.instance.Agent.PrivateKey, avatarAddress);
                int response = -1;

                await Client.PostTicketsRefreshPurchaseAsync(jwt, request,
                    on201: result =>
                    {
                        response = result;
                    },
                    on423: _ =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Locked | " +
                            $"TxId: {txId} | " +
                            $"AvatarAddress: {avatarAddress ?? "null"}");
                        throw new ArenaServiceException("UI_ARENA_SERVICE_LOCKED");
                    },
                    onError: error =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Failed to post available opponents | " +
                            $"TxId: {txId} | " +
                            $"AvatarAddress: {avatarAddress ?? "null"} | " +
                            $"Error: {error}");
                    });

                return response;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[ArenaServiceManager] Exception while posting available opponents | " +
                    $"TxId: {txId} | " +
                    $"AvatarAddress: {avatarAddress ?? "null"} | " +
                    $"Error: {e.Message}");
                throw e;
            }
        }

        public async Task<int> PostTicketsBattlePurchaseAsync(string txId, int ticketCount, FungibleAssetValue amount, string avatarAddress)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("[ArenaServiceManager] Called before initialization");
            }

            try
            {
                await UniTask.SwitchToMainThread();

                decimal calculatedAmount = decimal.Parse(amount.GetQuantityString(), CultureInfo.InvariantCulture);

                var request = new PurchaseTicketRequest
                {
                    TicketCount = ticketCount,
                    PurchasePrice = calculatedAmount,
                    TxId = txId,
                };

                string jwt = CreateJwt(Game.Game.instance.Agent.PrivateKey, avatarAddress);
                int response = -1;

                await Client.PostTicketsBattlePurchaseAsync(jwt, request,
                    on201: result =>
                    {
                        response = result;
                    },
                    on423: _ =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Locked | " +
                            $"TxId: {txId} | " +
                            $"AvatarAddress: {avatarAddress ?? "null"} | " +
                            $"Error: {_}");
                        throw new ArenaServiceException("UI_ARENA_SERVICE_LOCKED");
                    },
                    onError: error =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Failed to post battle ticket purchase | " +
                            $"TxId: {txId} | " +
                            $"AvatarAddress: {avatarAddress ?? "null"} | " +
                            $"Error: {error}");
                    });

                return response;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[ArenaServiceManager] Exception while posting battle ticket purchase | " +
                    $"TxId: {txId} | " +
                    $"AvatarAddress: {avatarAddress ?? "null"} | " +
                    $"Error: {e.Message}");
                throw e;
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
                    on200: result =>
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
                throw e;
            }
        }

        public async Task<string> PostUsersAsync(string avatarAddress, string nameWithHash, int portraitId, Int64 cp, int level)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("[ArenaServiceManager] Called before initialization");
            }

            try
            {
                await UniTask.SwitchToMainThread();
                var request = new UserRegisterRequest
                {
                    NameWithHash = nameWithHash,
                    PortraitId = portraitId,
                    Cp = cp,
                    Level = level
                };

                string seasonResponse = null;
                string jwt = CreateJwt(Game.Game.instance.Agent.PrivateKey, avatarAddress);
                await Client.PostUsersAsync(jwt, request,
                    on201: result =>
                    {
                        seasonResponse = result;
                    },
                    onError: error =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Failed to post users | " +
                            $"AvatarAddress: {avatarAddress ?? "null"} | " +
                            $"Error: {error}");
                    });

                return seasonResponse;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[ArenaServiceManager] Failed to post users | " +
                    $"AvatarAddress: {avatarAddress ?? "null"} | " +
                    $"Error: {e.Message}");
                throw e;
            }
        }

        public async Task<BattleTokenResponse> GetBattleTokenAsync(string opponentAvatarAddress, string avatarAddress)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("[ArenaServiceManager] Called before initialization");
            }

            BattleTokenResponse token = null;
            string jwt = CreateJwt(Game.Game.instance.Agent.PrivateKey, avatarAddress);
            try
            {
                await UniTask.SwitchToMainThread();
                await Client.PostBattleTokenAsync(opponentAvatarAddress, jwt,
                    on201: result =>
                    {
                        token = result;
                    },
                    on401: _ =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Authentication failed | " +
                            $"OpponentAvatarAddress: {opponentAvatarAddress ?? "null"}");
                    },
                    on423: _ =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] {_ ?? "null"} | " +
                            $"OpponentAvatarAddress: {opponentAvatarAddress ?? "null"}");
                        throw new ArenaServiceException("ERROR_ARENA_BATTLE_TOKEN_LOCKED");
                    },
                    onError: error =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Failed to retrieve season battle token | " +
                            $"OpponentAvatarAddress: {opponentAvatarAddress ?? "null"} | " +
                            $"error: {error}");
                    });
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[ArenaServiceManager] Exception while getting battle token | " +
                    $"OpponentAvatarAddress: {opponentAvatarAddress ?? "null"} | " +
                    $"Error: {e.Message}");
                throw e;
            }

            return token;
        }

        public async Task<string> PostSeasonsBattleRequestAsync(string txId, int logId, string avatarAddress)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("[ArenaServiceManager] Called before initialization");
            }

            string response = null;
            string jwt = CreateJwt(Game.Game.instance.Agent.PrivateKey, avatarAddress);
            try
            {
                await UniTask.SwitchToMainThread();
                await Client.PostBattleRequestAsync(logId, jwt, new BattleRequest { TxId = txId },
                    on200: result =>
                    {
                        response = result;
                    },
                    on401: unauthorizedResult =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Unauthorized access | " +
                            $"TxId: {txId} | " +
                            $"LogId: {logId}");
                    },
                    on404: notFoundResult =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Not found | " +
                            $"TxId: {txId} | " +
                            $"LogId: {logId}");
                    },
                    onError: error =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Failed to post seasons battle request | " +
                            $"TxId: {txId} | " +
                            $"LogId: {logId} | " +
                            $"Error: {error}");
                    });
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[ArenaServiceManager] Exception while posting seasons battle request | " +
                    $"TxId: {txId} | " +
                    $"LogId: {logId} | " +
                    $"Error: {e.Message}");
                throw e;
            }

            return response;
        }

        public async Task<BattleResponse> GetBattleAsync(int battleLogId, string avatarAddress)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("[ArenaServiceManager] Called before initialization");
            }

            BattleResponse battleResponse = null;
            string jwt = CreateJwt(Game.Game.instance.Agent.PrivateKey, avatarAddress);
            try
            {
                await UniTask.SwitchToMainThread();
                await Client.GetBattleAsync(battleLogId, jwt,
                    on200: result =>
                    {
                        battleResponse = result;
                    },
                    on401: unauthorizedResult =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Unauthorized access | " +
                            $"BattleLogId: {battleLogId}");
                    },
                    on404: notFoundResult =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Not found | " +
                            $"BattleLogId: {battleLogId}");
                    },
                    onError: error =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Failed to get seasons battle | " +
                            $"BattleLogId: {battleLogId} | " +
                            $"Error: {error}");
                    });
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[ArenaServiceManager] Exception while getting seasons battle | " +
                    $"BattleLogId: {battleLogId} | " +
                    $"Error: {e.Message}");
                throw e;
            }

            return battleResponse;
        }
        public async Task<ArenaInfoResponse> GetArenaInfoAsync(string avatarAddress)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("[ArenaServiceManager] Called before initialization");
            }

            ArenaInfoResponse arenaInfoResponse = null;
            string jwt = CreateJwt(Game.Game.instance.Agent.PrivateKey, avatarAddress);
            try
            {
                await UniTask.SwitchToMainThread();
                // todo : 아레나서비스
                // 인터페이스 수정후 작업해야함
                await Client.GetInfoAsync(jwt,
                    on200: result =>
                    {
                        arenaInfoResponse = result;
                    },
                    on401: unauthorizedResult =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Unauthorized access | AvatarAddress: {avatarAddress}");
                    },
                    on404: notFoundResult =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Not found | AvatarAddress: {avatarAddress}");
                    },
                    onError: error =>
                    {
                        NcDebug.LogError($"[ArenaServiceManager] Failed to get arena info | AvatarAddress: {avatarAddress} | Error: {error}");
                    });
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[ArenaServiceManager] Exception while getting arena info | AvatarAddress: {avatarAddress} | Error: {e.Message}");
                throw e;
            }

            return arenaInfoResponse;
        }
    }
}
public static class ArenaServiceExtentions
{
    public static (long beginning, long end, long current) GetSeasonProgress(
        this SeasonResponse seasonData,
        long blockIndex)
    {
        return (
            seasonData?.StartBlockIndex ?? 0,
            seasonData?.EndBlockIndex ?? 0,
            blockIndex);
    }

    public static RoundResponse GetCurrentRound(this SeasonResponse seasonData, long blockIndex)
    {
        var round = seasonData.Rounds.FirstOrDefault(r => r.StartBlockIndex <= blockIndex && blockIndex <= r.EndBlockIndex);
        if (round == null)
        {
            NcDebug.LogError($"No round found for block index {blockIndex} in season {seasonData.Id}");
            return null;
        }
        return round;
    }
}
