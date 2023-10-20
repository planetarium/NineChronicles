using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using UnityEngine.Networking;
using System.Text.Json.Nodes;
using System.Linq;

namespace Nekoyume
{
    public class OpenApiGenerator : EditorWindow
    {
        private static string _downloadJsonUrl;
        private static string _className;

        [MenuItem("Tools/Generate OpenAPI Class")]
        private static void Init()
        {
            _downloadJsonUrl = "https://xd2n1dpmce.execute-api.us-east-2.amazonaws.com/internal/openapi.json";
            var window = GetWindow(typeof(OpenApiGenerator));
            window.Show();
        }

        private void OnGUI()
        {
            _downloadJsonUrl = EditorGUILayout.TextField("DownloadJsonURL: ", _downloadJsonUrl);
            _className = EditorGUILayout.TextField("ClassName: ", _className);

            if (GUILayout.Button("Create"))
            {
                GenerateOpenApiClass(_downloadJsonUrl, _className);
            }
        }

        public static void GenerateOpenApiClass(string url, string className)
        {
            string apiUrl = url;
            string outputDir = "Assets/GeneratedApi";

            string jsonSpec = DownloadOpenApiSpec(apiUrl);
            if (string.IsNullOrEmpty(jsonSpec))
            {
                UnityEngine.Debug.LogError("Failed to download OpenAPI spec.");
                return;
            }

            string generatedCode = GenerateCSharpFromOpenApiSpec(jsonSpec, className);
            if (string.IsNullOrEmpty(generatedCode))
            {
                UnityEngine.Debug.LogError("Failed to generate C# code from OpenAPI spec.");
                return;
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            File.WriteAllText(Path.Combine(outputDir, $"{className}.cs"), generatedCode);
            AssetDatabase.Refresh();
        }

        private static string DownloadOpenApiSpec(string url)
        {
            using (var www = UnityWebRequest.Get(url))
            {
                www.SendWebRequest();
                while (!www.isDone) { }
                if (www.result != UnityWebRequest.Result.Success)
                {
                    UnityEngine.Debug.LogError("Failed to download: " + www.error);
                    return null;
                }
                return www.downloadHandler.text;
            }
        }

        private static string GenerateCSharpFromOpenApiSpec(string jsonSpec, string rootClassName)
        {
            StringBuilder sb = new StringBuilder();
            JsonNode rootNode = JsonNode.Parse(jsonSpec);

            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections;");

            sb.AppendLine();

            sb.AppendLine($"public class {rootClassName}");
            sb.AppendLine("{");
            sb.AppendLine($"    public string Url;");
            // Generate classes for schemas
            if (rootNode["components"]?["schemas"] is JsonObject schemas)
            {
                foreach (var schema in schemas)
                {
                    string className = schema.Key;
                    sb.AppendLine($"    public class {className}");
                    sb.AppendLine("    {");

                    if (schema.Value["properties"] is JsonObject properties)
                    {
                        foreach (var property in properties)
                        {
                            string propName = property.Key;
                            string propType = ConvertToCSharpType(property.Value);

                            if (property.Value["type"]?.ToString() == "array")
                            {
                                propType = $"List<{ConvertToCSharpType(property.Value["items"])}>";
                            }

                            string formattedPropName = char.ToUpper(propName[0]) + propName.Substring(1).Replace("_", string.Empty);
                            sb.AppendLine($"        [JsonPropertyName(\"{propName}\")]");
                            sb.AppendLine($"        public {propType} {formattedPropName} {{ get; set; }}");
                        }
                    }

                    sb.AppendLine("    }");
                    sb.AppendLine();
                }
            }

            // Generate web request functions based on paths
            var paths = rootNode["paths"];
            AppendWebRequestsFromPaths(sb, paths, rootNode["components"]?["schemas"]);
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string ConvertToCSharpType(JsonNode jsonNode)
        {
            string jsonType = string.Empty;
            if (jsonNode["type"] != null)
            {
                jsonType = jsonNode["type"].ToString();
            }
            else if(jsonNode["$ref"] != null)
            {
                var splited = jsonNode["$ref"]?.ToString().Split("/");
                return splited[splited.Length-1];
            }
            else if(jsonNode["anyOf"] != null)
            {
                return ConvertToCSharpType(jsonNode["anyOf"][0])+"?";
            }
            switch (jsonType)
            {
                case "string": return "string";
                case "integer": return "int";
                case "boolean": return "bool";
                    
                default: return "object";
            }
        }

        private static void AppendWebRequestsFromPaths(StringBuilder sb, JsonNode pathsNode, JsonNode schemasNode)
        {
            foreach (var pathItem in pathsNode as JsonObject)
            {
                string path = pathItem.Key;
                if (!path.Contains("api"))
                    continue;
                if (pathItem.Value is JsonObject methods)
                {
                    foreach (var method in methods)
                    {
                        string httpMethod = method.Key.ToUpper();

                        string methodName = GenerateMethodNameFromPathAndMethod(path, httpMethod);
                        string returnType = "string";

                        bool hasReturnType = false;

                        // Check for response schema to generate class if necessary
                        var responseSchema = method.Value["responses"]?["200"]?["content"]?["application/json"]?["schema"];
                        if (responseSchema != null)
                        {
                            if (responseSchema["$ref"] != null)
                            {
                                string schemaRef = responseSchema["$ref"].ToString();
                                string[] parts = schemaRef.Split('/');
                                returnType = parts[parts.Length - 1]; // Extract class name from reference (e.g. "#/components/schemas/MySchema" -> "MySchema")
                                hasReturnType = true;
                            }
                        }

                        sb.AppendLine($"    public IEnumerator {methodName}(Action<{returnType}> onSuccess, Action<string> onError)");
                        sb.AppendLine("    {");
                        sb.AppendLine($"        string url = Url + \"{path}\";");
                        sb.AppendLine($"        using (var request = new UnityEngine.Networking.UnityWebRequest(url, \"{httpMethod}\"))");
                        sb.AppendLine("        {");
                        sb.AppendLine("            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();");
                        sb.AppendLine("            yield return request.SendWebRequest();");
                        sb.AppendLine("            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)");
                        sb.AppendLine("            {");
                        if (hasReturnType)
                        {
                            sb.AppendLine($"                {returnType} result = System.Text.Json.JsonSerializer.Deserialize<{returnType}>(request.downloadHandler.text);");
                            sb.AppendLine("                onSuccess?.Invoke(result);");
                        }
                        else
                        {
                            sb.AppendLine("                onSuccess?.Invoke(request.downloadHandler.text);");
                        }
                        sb.AppendLine("            }");
                        sb.AppendLine("            else");
                        sb.AppendLine("            {");
                        sb.AppendLine("                onError?.Invoke(request.error);");
                        sb.AppendLine("            }");
                        sb.AppendLine("        }");
                        sb.AppendLine("    }");
                        sb.AppendLine();
                    }
                }
            }
        }
        private static string GenerateMethodNameFromPathAndMethod(string path, string httpMethod)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(httpMethod.ToLower());
            string[] segments = path.Split('/');
            foreach (var segment in segments)
            {
                if (!string.IsNullOrEmpty(segment))
                {
                    sb.Append(char.ToUpper(segment[0]) + segment.Substring(1));
                }
            }
            return sb.ToString().Replace("-","");
        }
    }
}
