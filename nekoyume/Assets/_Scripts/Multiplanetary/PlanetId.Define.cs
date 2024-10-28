namespace Nekoyume.Multiplanetary
{
    public partial struct PlanetId
    {
        // Main
        public static readonly PlanetId Odin = new("0x000000000000");
        public static readonly PlanetId Heimdall = new("0x000000000001");
        public static readonly PlanetId Thor = new("0x000000000003");

        // Internal
        public static readonly PlanetId OdinInternal = new("0x100000000000");
        public static readonly PlanetId HeimdallInternal = new("0x100000000001");
        public static readonly PlanetId ThorInternal = new("0x100000000003");
    }
}
