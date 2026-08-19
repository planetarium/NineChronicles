using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Libplanet.Common;
using Nekoyume;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Nekoyume.ApiClient
{
    /// <summary>
    /// (NCU) 포탈 by-address 연동상태 조회. 팝업 오픈 시 호출.
    ///   9c 서명으로 소유권 증명: Agent.PrivateKey.Sign(UTF8(ts)) = ECDSA(secp256k1) over
    ///   SHA256(UTF8(ts)), DER. 포탈은 sha256(utf8(ts)) digest로 서명자 주소를 복구·대조.
    ///   ⚠️ 서명 계약은 포탈 엔드포인트에 실 서명을 넣어 200 확인으로 최종 검증 필요.
    /// </summary>
    public class NcuServiceManager
    {
        // 응답이 없으면 스피너가 영구 고정되므로 상한을 둔다.
        private const int TimeoutSeconds = 10;

        public string Url { get; }
        public bool IsInitialized => !string.IsNullOrEmpty(Url);

        public NcuServiceManager(string url)
        {
            Url = url;
        }

        public class NcuConnection
        {
            [JsonProperty("key")] public string Key;
            [JsonProperty("linked")] public bool Linked;
            [JsonProperty("address")] public string Address;
            [JsonProperty("nickname")] public string Nickname;
            [JsonProperty("kaiaWalletAddr")] public string KaiaWalletAddr;
            [JsonProperty("role")] public string Role;
            [JsonProperty("error")] public bool Error;
        }

        public class NcuProject
        {
            [JsonProperty("projectId")] public string ProjectId;
            [JsonProperty("connections")] public NcuConnection[] Connections;
        }

        public class NcuLinkStatusResponse
        {
            [JsonProperty("agentAddress")] public string AgentAddress;
            [JsonProperty("projects")] public NcuProject[] Projects;
        }

        public async UniTask<NcuLinkStatusResponse> FetchLinkStatusAsync()
        {
            if (!IsInitialized)
            {
                return null;
            }

            var agent = Game.Game.instance.Agent;
            if (agent?.PrivateKey == null)
            {
                return null;
            }

            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // Libplanet PrivateKey.Sign(msg) = ECDSA over SHA256(msg), DER. 포탈 digest=sha256(utf8(ts)).
            var signature = agent.PrivateKey.Sign(Encoding.UTF8.GetBytes(ts.ToString()));
            var payload = JsonConvert.SerializeObject(new
            {
                agentAddress = "0x" + agent.Address.ToHex(),
                signature = ByteUtil.Hex(signature),
                timestamp = ts,
            });

            var url = Url.TrimEnd('/') + "/api/ncu/link-status";
            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = TimeoutSeconds;

                // UniTask의 SendWebRequest awaiter는 통신/HTTP 오류에서 예외를 던진다.
                //   (result를 검사하는 코드는 도달하지 못한다.) 실패는 여기서 null로 흡수하고,
                //   호출부가 "조회 실패" 상태로 렌더해 재시도 버튼을 띄운다.
                try
                {
                    await request.SendWebRequest();
                }
                catch (Exception e)
                {
                    NcDebug.LogError(
                        $"[NcuServiceManager] link-status failed: {request.responseCode} {request.error} :: {e.Message}");
                    return null;
                }

                try
                {
                    return JsonConvert.DeserializeObject<NcuLinkStatusResponse>(
                        request.downloadHandler.text);
                }
                catch (Exception e)
                {
                    NcDebug.LogError($"[NcuServiceManager] parse failed: {e.Message}");
                    return null;
                }
            }
        }
    }
}
