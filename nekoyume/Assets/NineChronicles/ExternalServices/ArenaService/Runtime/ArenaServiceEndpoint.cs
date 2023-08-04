using System;

namespace NineChronicles.ExternalServices.ArenaService.Runtime
{
    public struct ArenaServiceEndpoint
    {
        public readonly string Url;
        public readonly Uri Ping;
        public readonly Uri ArenaInfo;
        public readonly Uri ArenaParticipantList;
        public readonly Uri DummyArenaMy;
        public readonly Uri DummyArenaBoard;

        public ArenaServiceEndpoint(string url)
        {
            Url = url;
            Ping = new Uri(Url + "/ping");

            ArenaInfo = new Uri(Url + "/api/arena");
            ArenaParticipantList = new Uri(Url + "/api/arena/participant-list");

            DummyArenaMy = new Uri(Url + "/api/dummy-arena/my");
            DummyArenaBoard = new Uri(Url + "/api/dummy-arena/board");
        }
    }
}
