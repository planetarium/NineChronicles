using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace NineChronicles.Standalone.Properties
{
    public class ServiceBindingProperties
    {
        public bool NoMiner { get; set; }
        public string AppProtocolVersion { get; set; }
        public string GenesisBlockPath { get; set; }
        public string SwarmHost { get; set; }
        public ushort? SwarmPort { get; set; }
        public int MinimumDifficulty { get; set; }
        public string PrivateKeyString { get; set; }
        public string StoreType { get; set; }
        public string StorePath { get; set; }
        public string[] IceServerStrings { get; set; }
        public string[] PeerStrings { get; set; }
        public bool NoTrustedStateValidators { get; set; }
        public string[] TrustedAppProtocolVersionSigners { get; set; }
        public bool RpcServer { get; set; }
        public string RpcListenHost { get; set; }
        public int? RpcListenPort { get; set; }

    }
}
