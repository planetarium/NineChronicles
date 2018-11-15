using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Nekoyume.Move;
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
        public ResultCode result = ResultCode.ERROR;
        public List<Move> moves;
    }

    public class Agent
    {
        public event EventHandler<Move> DidReceiveAction;
        private readonly string apiUrl;
        private readonly PrivateKey privateKey;
        private float interval;
        private OrderedDictionary moves;
        public byte[] UserAddress => privateKey.ToAddress();
        public List<Move> requestedMoves;

        public delegate void MoveFetched(IEnumerable<Move> moves);

        private long? lastBlockOffset;

        private static Newtonsoft.Json.JsonConverter moveJsonConverter = new JsonConverter();
        public Agent(string apiUrl, PrivateKey privateKey, float interval = 1.0f)
        {
            if (string.IsNullOrEmpty(apiUrl))
            {
                throw new ArgumentException("apiUrl should not be empty or null.", nameof(apiUrl));
            }

            this.apiUrl = apiUrl;
            this.privateKey = privateKey;
            this.interval = interval;
            this.moves = new OrderedDictionary();
            this.requestedMoves = new List<Move>();
        }

        public void Send(Move move)
        {
            move.Sign(privateKey);
            requestedMoves.Add(move);
        }

        public IEnumerable<Move> Moves => moves.Values.Cast<Move>();

        public IEnumerator Sync()
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);
                var chunks = new List<Move>(requestedMoves);
                requestedMoves.Clear();

                foreach (var m in chunks)
                {
                    yield return SendMove(m);
                }

                yield return FetchMove(delegate (IEnumerable<Move> fetched)
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
            var url = string.Format("{0}/users/0x{1}/moves/", apiUrl, UserAddress.Hex());

            if (lastBlockOffset.HasValue)
            {
                url += string.Format("?block_offset={0}", lastBlockOffset);
            }
            var www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();
            if (!www.isNetworkError)
            {
                var jsonPayload = www.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<Response>(jsonPayload, moveJsonConverter);
                callback(response.moves);
            }
        }

        private IEnumerator SendMove(Move move)
        {
            var serialized = JsonConvert.SerializeObject(move, moveJsonConverter);
            var url = string.Format("{0}/moves", apiUrl);
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
