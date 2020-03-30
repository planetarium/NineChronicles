using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Libplanet.Blockchain;
using MagicOnion.Client;
using Microsoft.Extensions.Hosting;
using Nekoyume.Action;
using Nekoyume.Shared.Hubs;
using Serilog;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone
{
    public class ActionEvaluationPublisher : BackgroundService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly BlockChain<NineChroniclesActionType> _blockChain;
        
        public ActionEvaluationPublisher(
            BlockChain<NineChroniclesActionType> blockChain,
            string host,
            int port
        )
        {
            _blockChain = blockChain;
            _host = host;
            _port = port;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(1000, stoppingToken);
            var client = StreamingHubClient.Connect<IActionEvaluationHub, IActionEvaluationHubReceiver>(
                new Channel(_host, _port, ChannelCredentials.Insecure),
                null
            );
            await client.JoinAsync();

            _blockChain.TipChanged += async (o, ev) =>
            {
                await client.UpdateTipAsync(ev.Index);
            };
            var renderer = new ActionRenderer(ActionBase.RenderSubject, ActionBase.UnrenderSubject);
            renderer.EveryRender<ActionBase>().Subscribe(
                async ev =>
                {
                    var formatter = new BinaryFormatter();
                    using var c = new MemoryStream();
                    using var df = new DeflateStream(c, System.IO.Compression.CompressionLevel.Fastest);

                    try
                    {
                        formatter.Serialize(df, ev);
                        await client.BroadcastAsync(c.ToArray());
                    }
                    catch (SerializationException se)
                    {
                        // FIXME add logger as property
                        Log.Error(se, "Skip broadcasting since given action isn't serializable.");
                    }
                },
                stoppingToken
            );
        }
    }
}
