namespace Updater.Common
{
    public class LauncherSettings
    {
        public string StorePath { get; set; }

        public string StoreType { get; set; }

        public string KeyStorePath { get; set; }

        public string AppProtocolVersionToken { get; set; }

        public string[] IceServers { get; set; }

        public string[] Peers { get; set; }

        public bool NoMiner { get; set; }

        public bool NoTrustedStateValidators { get; set; }

        public string GenesisBlockPath { get; set; }

        public string[] TrustedAppProtocolVersionSigners { get; set; }

        /// <summary>
        /// A criteria for deployment flows, in other words,
        /// the name of directory which grouped deployments in s3 storage like branch.
        /// </summary>
        public string DeployBranch { get; set; }
    }
}
