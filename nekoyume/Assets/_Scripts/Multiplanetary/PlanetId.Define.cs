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

        public static Planet? GetCurrentPlanet(PlanetId planetId)
        {
            if (planetId == Odin)
            {
                return Planet.Odin;
            }
                
            if (planetId == Heimdall)
            {
                return Planet.Heimdall;
            }
                
            if (planetId == Thor)
            {
                return Planet.Thor;
            }
                
            if (planetId == OdinInternal)
            {
                return Planet.OdinInternal;
            }
                
            if (planetId == HeimdallInternal)
            {
                return Planet.HeimdallInternal;
            }
                
            if (planetId == ThorInternal)
            {
                return Planet.ThorInternal;
            } 
            
            // Default to Odin
            return Planet.Odin;
        }
        
        public static bool IsThor(PlanetId planetId)
        {
            return planetId == Thor || planetId == ThorInternal;
        }
    }
}
