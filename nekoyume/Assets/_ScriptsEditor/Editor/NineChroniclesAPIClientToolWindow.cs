using System.Threading.Tasks;
using Nekoyume.GraphQL;
using UnityEditor;
using UnityEngine;

namespace NekoyumeEditor
{
    public class NineChroniclesAPIClientToolWindow : EditorWindow
    {
        private string _host = "http://9c-main-rpc-1.nine-chronicles.com/graphql";
        private NineChroniclesAPIClient _client;
        private string _result = "";

        [MenuItem("Tools/9C/API Client Tool")]
        private static void Init()
        {
            GetWindow<NineChroniclesAPIClientToolWindow>("API Client Tool", true).Show();
        }

        private void OnGUI()
        {
            _client ??= new NineChroniclesAPIClient(_host);

            if (GUILayout.Button("Get block hash"))
            {
                Task.Run(GetBlockHashAsync);
            }

            EditorGUILayout.TextArea(_result);
        }

        private async Task GetBlockHashAsync()
        {
            var blockHash = await _client.GetBlockHashAsync(6992848);
            _result = blockHash.HasValue
                ? blockHash.Value.ToString()
                : "Failed getting block hash";
        }
    }
}
