namespace Launcher
{
    public class LauncherSetting
    {
        public string StorePath { get; set; }

        public string KeyStorePath { get; set; }

        public string Passphrase { get; set; }

        public int AppProtocolVersion { get; set; }

        public string IceServer { get; set; }

        public string Seed { get; set; }

        public bool NoMiner { get; set; }

        public string GenesisBlockPath { get; set; }
        
        public string GameBinaryPath { get; set; }
    }
}
