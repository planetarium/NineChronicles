using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Planetarium.Crypto.Extension;
using Planetarium.Crypto.Keys;
using Planetarium.SDK.Address;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.Move
{
    [Serializable]
    internal class Response
    {
        public ResultCode result = ResultCode.Error;
        public List<MoveBase> moves = null;
    }

    public class Agent
    {
        public event EventHandler<MoveBase> DidReceiveAction;
        private readonly string apiUrl;
        private readonly PrivateKey privateKey;
        private readonly float interval;
        private readonly OrderedDictionary moves;
        public byte[] UserAddress => privateKey.ToAddress();
        private readonly List<MoveBase> requestedMoves;

        public delegate void MoveFetched(IEnumerable<MoveBase> moves);

        private long? lastBlockOffset;

        private static readonly Newtonsoft.Json.JsonConverter moveJsonConverter = new JsonConverter();

        public Agent(string apiUrl, PrivateKey privateKey, float interval = 3.0f, long? lastBlockOffset = null)
        {
            if (string.IsNullOrEmpty(apiUrl))
            {
                throw new ArgumentException("apiUrl should not be empty or null.", nameof(apiUrl));
            }

            this.apiUrl = apiUrl;
            this.privateKey = privateKey;
            this.interval = interval;
            this.moves = new OrderedDictionary();
            this.requestedMoves = new List<MoveBase>();
            this.lastBlockOffset = lastBlockOffset;
        }

        public void Send(MoveBase move)
        {
            move.Sign(privateKey);
            requestedMoves.Add(move);
        }

        public IEnumerable<MoveBase> Moves => moves.Values.Cast<MoveBase>();

        public IEnumerator Sync()
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);
                var chunks = new List<MoveBase>(requestedMoves);
                requestedMoves.Clear();

                foreach (var m in chunks)
                {
                    yield return SendMove(m);
                }

                yield return FetchMove(delegate(IEnumerable<MoveBase> fetched)
                {
                    foreach (var move in fetched)
                    {
                        moves[move.Id] = move;
                        DidReceiveAction?.Invoke(this, move);
                        lastBlockOffset = move.BlockId;
                    }
                });
            }
        }

        public IEnumerator FetchMove(MoveFetched callback)
        {
            var url = $"{apiUrl}/users/0x{UserAddress.Hex()}/moves/";

            if (lastBlockOffset.HasValue)
            {
                url += $"?block_offset={lastBlockOffset}";
            }

            var www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (!www.isNetworkError)
            {
                var jsonPayload = www.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<Response>(jsonPayload, moveJsonConverter);
                callback(response.moves);
            }
            else
            {
                // FIXME logging
            }
        }

        private IEnumerator SendMove(MoveBase move)
        {
            var serialized = JsonConvert.SerializeObject(move, moveJsonConverter);
            var url = $"{apiUrl}/moves";
            var request = new UnityWebRequest(url, "POST");
            var payload = Encoding.UTF8.GetBytes(serialized);

            request.uploadHandler = new UploadHandlerRaw(payload);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (!request.isDone || request.isHttpError)
            {
                // FIXME implement better retry logic.(e.g. jitter)
                yield return SendMove(move);
            }
        }
    }
}
