using Newtonsoft.Json.Linq;

namespace NineChronicles.Standalone
{
    public class GraphQLBody
    {
        public string Query { get; set; }

        public JObject Variables { get; set; }
    }
}
