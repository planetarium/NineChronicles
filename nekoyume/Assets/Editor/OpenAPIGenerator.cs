using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using UnityEngine.Networking;
using System.Text.Json.Nodes;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json;

namespace Nekoyume
{
    public class GeneratedApiTester : EditorWindow
    {
        private object clientInstance;
        private string url = "";
        private string responseMessage;
        private string className = "";
        private List<string> methodNames = new List<string>();
        private List<MethodInfo> methodInfos = new List<MethodInfo>();
        private int selectedMethodIndex = 0;
        private List<object> methodParameters = new List<object>();
        private Dictionary<string, object> customClassValues = new Dictionary<string, object>();
        private Vector2 scrollPosition;

        public static void ShowWindow(string startURl, string className)
        {
            var window = GetWindow(typeof(GeneratedApiTester)) as GeneratedApiTester;
            window.methodInfos.Clear();
            window.methodNames.Clear();
            window.selectedMethodIndex = 0;
            window.methodParameters.Clear();
            window.className = className;
            window.url = startURl;
        }

        private void OnGUI()
        {
            GUILayout.Label("Generated API Tester", EditorStyles.boldLabel);

            url = EditorGUILayout.TextField("Service URL: ", url);
            className = EditorGUILayout.TextField("Class Name: ", className);

            if (GUILayout.Button("Create Client Instance"))
            {
                selectedMethodIndex = 0;
                CreateClientInstance();
                LoadMethodsFromClientType();
            }

            if (clientInstance != null)
            {
                int newSelectedMethodIndex = EditorGUILayout.Popup("Select Method", selectedMethodIndex, methodNames.ToArray());

                if (newSelectedMethodIndex != selectedMethodIndex)
                {
                    selectedMethodIndex = newSelectedMethodIndex;
                    LoadMethodsFromClientType();
                    LoadMethodParameters();
                }

                DrawMethodParameters();

                if (GUILayout.Button("Invoke Selected Method"))
                {
                    InvokeSelectedMethod();
                    scrollPosition = Vector2.zero;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Response:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.TextArea(responseMessage, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void LoadMethodParameters()
        {
            methodParameters.Clear();

            ParameterInfo[] parameters = methodInfos[selectedMethodIndex].GetParameters();
            foreach (var param in parameters)
            {
                if (param.ParameterType == typeof(string))
                {
                    methodParameters.Add("");
                }
                else if (param.ParameterType.IsValueType)
                {
                    methodParameters.Add(Activator.CreateInstance(param.ParameterType));
                }
                else if (IsCallback(param.ParameterType))
                {
                    var callbackType = param.ParameterType.GetGenericArguments()[0];
                    var callback = CreateDynamicCallback(callbackType);
                    methodParameters.Add(callback);
                }
                else
                {
                    methodParameters.Add(null);
                }
            }
        }

        private void DrawMethodParameters()
        {
            ParameterInfo[] parameters = methodInfos[selectedMethodIndex].GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                string paramName = parameters[i].Name;
                Type paramType = parameters[i].ParameterType;

                if (paramType == typeof(string))
                {
                    methodParameters[i] = EditorGUILayout.TextField(paramName + " (string)", (string)methodParameters[i]);
                }
                else if (paramType == typeof(int))
                {
                    methodParameters[i] = EditorGUILayout.IntField(paramName + " (int)", (int)methodParameters[i]);
                }
                else if (paramType == typeof(float))
                {
                    methodParameters[i] = EditorGUILayout.FloatField(paramName + " (float)", (float)methodParameters[i]);
                }
                else if (IsCallback(paramType))
                {

                }
                else if (IsCustomClass(paramType))
                {
                    DrawCustomClassFields(paramName, paramType, out var classInstance);
                    methodParameters[i] = classInstance;
                }
                else if (paramType.IsEnum)
                {
                    methodParameters[i] = EditorGUILayout.EnumPopup(paramName, (Enum)methodParameters[i]);
                }
                else
                {
                    EditorGUILayout.LabelField($"Parameter {paramName} of type {paramType.Name} not supported.");
                }
            }
        }

        private void DrawCustomClassFields(string paramName, Type paramType, out object classIntance)
        {
            FieldInfo[] fields = paramType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo[] properties = paramType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            EditorGUILayout.LabelField($"Editing {paramName} ({paramType.Name}):");
            EditorGUI.indentLevel++;

            object customClassInstance;
            if (!customClassValues.TryGetValue(paramName, out customClassInstance))
            {
                customClassInstance = Activator.CreateInstance(paramType);
                customClassValues[paramName] = customClassInstance;
            }
            classIntance = customClassInstance;

            foreach (var prop in properties)
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    UpdatePropertyValue(customClassInstance, prop);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void UpdatePropertyValue(object instance, PropertyInfo property)
        {
            var propertyType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propertyType);
            var isNullable = underlyingType != null;
            if (isNullable)
            {
                propertyType = underlyingType;
            }

            var propertyValue = property.GetValue(instance);

            if (propertyType == typeof(int))
            {
                propertyValue = isNullable ? HandleNullableInt((int?)propertyValue, property.Name) : EditorGUILayout.IntField(property.Name + " (int)", (int)propertyValue);
            }
            else if (propertyType == typeof(string))
            {
                propertyValue = EditorGUILayout.TextField(property.Name + " (string)", (string)propertyValue);
            }
            else if (propertyType == typeof(float))
            {
                propertyValue = isNullable ? HandleNullableFloat((float?)propertyValue, property.Name) : EditorGUILayout.FloatField(property.Name + " (float)", (float)propertyValue);
            }
            else if (propertyType == typeof(bool))
            {
                propertyValue = isNullable ? HandleNullableBool((bool?)propertyValue, property.Name) : EditorGUILayout.Toggle(property.Name + " (bool)", (bool)propertyValue);
            }
            else if (propertyType.IsEnum)
            {
                if (propertyValue == null)
                {
                    var enumValues = Enum.GetValues(propertyType);
                    propertyValue = enumValues.Length > 0 ? enumValues.GetValue(0) : null;
                }
                else
                {
                    propertyValue = EditorGUILayout.EnumPopup(property.Name, (Enum)propertyValue);
                }
            }

            property.SetValue(instance, propertyValue);
        }

        private int? HandleNullableInt(int? value, string name)
        {
            if (!value.HasValue)
            {
                value = 0;
            }

            value = EditorGUILayout.IntField(name + " (int?)", value.Value);
            return value;
        }

        private float? HandleNullableFloat(float? value, string name)
        {
            if (!value.HasValue)
            {
                value = 0.0f;
            }
            
            value = EditorGUILayout.FloatField(name + " (float?)", value.Value);
            return value;
        }

        private bool? HandleNullableBool(bool? value, string name)
        {
            if (value.HasValue)
            {
                value = false;
            }

            value = EditorGUILayout.Toggle(name + " (bool?)", value.Value);
            return value;
        }

        private bool IsCustomClass(Type type)
        {
            return type.IsClass && type != typeof(string);
        }

        private static Type FindTypeInAllAssemblies(string className)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == className && HasField(type, "Url", typeof(string)))
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        private static bool HasField(Type type, string fieldName, Type fieldType = null)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if(fieldType == null)
            {
                return field != null;
            }
            return field != null && field.FieldType == fieldType;
        }

        private void CreateClientInstance()
        {
            Type clientType = FindTypeInAllAssemblies(className);

            if (clientType != null)
            {
                clientInstance = Activator.CreateInstance(clientType, url);
                responseMessage = "Client instance created!";
            }
            else
            {
                responseMessage = $"Failed to find {className} type.";
            }
        }

        private void LoadMethodsFromClientType()
        {
            if (clientInstance != null)
            {
                methodNames.Clear();
                methodInfos.Clear();
                customClassValues.Clear();
                MethodInfo[] methods = clientInstance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    methodNames.Add(method.Name);
                    methodInfos.Add(method);
                }
                responseMessage = $"Loaded {methodNames.Count} methods!";
            }
            else
            {
                responseMessage = "Client instance not created.";
            }
        }

        private void InvokeSelectedMethod()
        {
            MethodInfo method = methodInfos[selectedMethodIndex];

            try
            {
                responseMessage = "Requesting~";
                Repaint();
                
                object result = method.Invoke(clientInstance, methodParameters.ToArray());
                if (result is Task task)
                {
                    WaitForTaskCompletion(task);
                }
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }

            Repaint();
        }

        private bool IsCallback(Type type)
        {
            if (!type.IsGenericType) return false;
            return type.GetGenericTypeDefinition() == typeof(Action<>);
        }

        private Delegate CreateDynamicCallback(Type callbackType)
        {
            MethodInfo method = this.GetType().GetMethod(nameof(GenericCallback), BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo generic = method.MakeGenericMethod(callbackType);
            return (Delegate)generic.CreateDelegate(typeof(Action<>).MakeGenericType(callbackType), this);
        }
        private async void WaitForTaskCompletion(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                responseMessage = $"Task Error: {ex.Message}";
                Repaint();
            }
        }

        private void GenericCallback<T>(T obj)
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            };
            string serializedData = System.Text.Json.JsonSerializer.Serialize(obj, options);
            responseMessage = $"{serializedData}";
            Repaint();
        }
    }


    public class OpenApiGenerator : EditorWindow
    {
        private const string _outputDir = "Assets/_Scripts/GeneratedApi";
        private const int _timeOut = 10;

        private static string _downloadJsonUrl;
        private static string _className;
        private static List<KeyValuePair<string, string>> _dataList = new List<KeyValuePair<string, string>>();
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Generate OpenAPI Class")]
        private static void Init()
        {
            RefreshGeneratedInfo();
            var window = GetWindow(typeof(OpenApiGenerator));
            window.Show();
        }

        private void OnGUI()
        {
            RefreshGeneratedInfo();
            EditorGUILayout.BeginVertical();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _downloadJsonUrl = EditorGUILayout.TextField("DownloadJsonURL: ", _downloadJsonUrl);
            _className = EditorGUILayout.TextField("ClassName: ", _className);

            if (GUILayout.Button("Create"))
            {
                GenerateOpenApiClass(_downloadJsonUrl, _className);
            }

            GUILayout.Space(10);

            if (GUILayout.Button("RefreshAll"))
            {
                foreach (var item in _dataList)
                {
                    GenerateOpenApiClass(item.Value, item.Key);
                }
            }

            GUILayout.Space(10);

            if (_dataList != null)
            {
                GUILayout.BeginVertical();
                foreach (var data in _dataList)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(data.Key, data.Value));
                    if (GUILayout.Button("Apply"))
                    {
                        _className = data.Key;
                        _downloadJsonUrl = data.Value;
                    }
                    if (GUILayout.Button("TesterWindow"))
                    {
                        GeneratedApiTester.ShowWindow(data.Value.Replace("/openapi.json",""), data.Key);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        public static void GenerateOpenApiClass(string url, string className)
        {
            string apiUrl = url;

            string jsonSpec = DownloadOpenApiSpec(apiUrl);
            if (string.IsNullOrEmpty(jsonSpec))
            {
                UnityEngine.Debug.LogError("Failed to download OpenAPI spec.");
                return;
            }

            string generatedCode = GenerateCSharpFromOpenApiSpec(jsonSpec, className, url);
            if (string.IsNullOrEmpty(generatedCode))
            {
                UnityEngine.Debug.LogError("Failed to generate C# code from OpenAPI spec.");
                return;
            }

            if (!Directory.Exists(_outputDir))
            {
                Directory.CreateDirectory(_outputDir);
            }

            File.WriteAllText(Path.Combine(_outputDir, $"{className}.cs"), generatedCode);
            AssetDatabase.Refresh();
            RefreshGeneratedInfo();
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

        private static string GenerateCSharpFromOpenApiSpec(string jsonSpec, string rootClassName, string url)
        {
            StringBuilder sb = new StringBuilder();
            JsonNode rootNode = JsonNode.Parse(jsonSpec);

            sb.AppendLine("//------------------------------------------------------------------------------");
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("//     This code was generated by a tool.");
            sb.AppendLine("//     Do not modify the contents of this file directly.");
            sb.AppendLine("//     Changes might be overwritten the next time the code is generated.");
            sb.AppendLine("//     Source URL: " + url);
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine("//------------------------------------------------------------------------------");

            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections;");
            sb.AppendLine("using System.Text.Json;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using System.Net.Http;");
            sb.AppendLine("using UnityEngine.Networking;");
            sb.AppendLine("using Cysharp.Threading.Tasks;");
            sb.AppendLine("using System.Text;");

            sb.AppendLine();

            sb.AppendLine($"public class {rootClassName}");
            sb.AppendLine("{");
            sb.AppendLine("    private string Url;");
            sb.AppendLine();
            sb.AppendLine($"    public {rootClassName}(string url)");
            sb.AppendLine("    {");
            sb.AppendLine($"        Url = url;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public void Dispose()");
            sb.AppendLine("    {");
            sb.AppendLine("    }");
            sb.AppendLine();

            if (rootNode["components"]?["schemas"] is JsonObject schemas)
            {
                foreach (var schema in schemas)
                {
                    string className = schema.Key;

                    if (schema.Value["enum"] is JsonArray enumValues)
                    {
                        sb.AppendLine($"    [JsonConverter(typeof({schema.Key}TypeConverter))]");
                        sb.AppendLine($"    public enum {schema.Key}");
                        sb.AppendLine("    {");

                        string description = schema.Value["description"] != null ? schema.Value["description"].ToString() : string.Empty;
                        Dictionary<string, string> enumMapping = new Dictionary<string, string>();

                        var enumType = schema.Value["type"]?.ToString();

                        if (enumType != "string")
                        {
                            foreach (var line in description.Split('\n'))
                            {
                                if (line.Contains(":"))
                                {
                                    var parts = line.Trim().Split(':');
                                    if (parts.Length == 2)
                                    {
                                        var name = parts[1];
                                        string pattern = "`(.*?)`";
                                        Match match = Regex.Match(name, pattern);
                                        if (match.Success)
                                        {
                                            name = match.Groups[0].Value.Trim('`');
                                        }
                                        enumMapping[parts[0].Trim('*').Trim().Replace("- **", string.Empty).TrimEnd('\"')] = name;
                                    }
                                }
                            }
                        }

                        string preText = string.Empty;

                        bool isStringPreText = false;
                        bool isString = false;
                        bool isNumberPreText = false;
                        bool isNumber = false;

                        for (int i = 0; i < enumValues.Count; i++)
                        {
                            var enumVal = enumValues[i].ToString();
                            if (enumMapping.ContainsKey(enumVal))
                            {
                                string enumName = enumMapping[enumVal];

                                if (enumName == enumVal)
                                {
                                    if (int.TryParse(enumName[0].ToString(), out var number))
                                    {
                                        sb.AppendLine($"        _{enumName},");
                                        preText = "\"_\"+";
                                        isStringPreText = true;
                                    }
                                    else
                                    {
                                        sb.AppendLine($"        {enumName},");
                                        isString = true;
                                    }
                                }
                                else
                                {
                                    if (int.TryParse(enumName[0].ToString(), out var number))
                                    {
                                        if (int.TryParse(enumVal[0].ToString(), out var enumValNumber))
                                        {
                                            sb.AppendLine($"        _{enumName} = {enumVal},");
                                            preText = "\"_\"+";
                                            isNumberPreText = true;
                                        }
                                        else
                                        {
                                            sb.AppendLine($"        _{enumName},");
                                            preText = "\"_\"+";
                                            isStringPreText = true;
                                        }
                                    }
                                    else
                                    {
                                        if (int.TryParse(enumVal[0].ToString(), out var enumValNumber))
                                        {
                                            sb.AppendLine($"        {enumName} = {enumVal},");
                                            isNumber = true;
                                        }
                                        else
                                        {
                                            sb.AppendLine($"        {enumName},");
                                            isString = true;
                                        }
                                    }
                                }
                            }
                            else
                            {

                                if (int.TryParse(enumVal[0].ToString(), out var number))
                                {
                                    sb.AppendLine($"        _{enumVal},");
                                    preText = "\"_\"+";
                                    isStringPreText = true;
                                }
                                else
                                {
                                    sb.AppendLine($"        {enumVal},");
                                    isString = true;
                                }
                            }
                        }

                        sb.AppendLine("    }");
                        sb.AppendLine();

                        sb.AppendLine($"    public class {schema.Key}TypeConverter : JsonConverter<{schema.Key}>");
                        sb.AppendLine("    {");
                        sb.AppendLine("        public override " + schema.Key + " Read(");
                        sb.AppendLine("            ref Utf8JsonReader reader,");
                        sb.AppendLine("            Type typeToConvert,");
                        sb.AppendLine("            JsonSerializerOptions options)");
                        sb.AppendLine("        {");
                        sb.AppendLine("            return reader.TokenType switch");
                        sb.AppendLine("            {");
                        sb.AppendLine("                JsonTokenType.Number => (" + schema.Key + ")reader.GetInt32(),");
                        sb.AppendLine("                JsonTokenType.String => Enum.Parse<" + schema.Key + ">(" + preText + "reader.GetString()),");
                        sb.AppendLine("                _ => throw new JsonException(");
                        sb.AppendLine("                    $\"Expected token type to be {string.Join(\" or \", new[] { JsonTokenType.Number, JsonTokenType.String })} but got {reader.TokenType}\")");
                        sb.AppendLine("            };");
                        sb.AppendLine("        }");

                        sb.AppendLine("        public override void Write(");
                        sb.AppendLine("            Utf8JsonWriter writer,");
                        sb.AppendLine($"            {schema.Key} value,");
                        sb.AppendLine("            JsonSerializerOptions options)");
                        sb.AppendLine("        {");
                        if (isStringPreText)
                        {
                            sb.AppendLine("            writer.WriteStringValue(value.ToString().Substring(1));");
                        }
                        else if (isString)
                        {
                            sb.AppendLine("            writer.WriteStringValue(value.ToString());");
                        }
                        else if (isNumber)
                        {
                            sb.AppendLine("            writer.WriteNumberValue((int)value);");
                        }
                        else if (isNumberPreText)
                        {
                            sb.AppendLine("            writer.WriteNumberValue((int)value);");
                        }
                        sb.AppendLine("        }");
                        sb.AppendLine("    }");
                        sb.AppendLine();

                        continue;
                    }

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

                            var splited = propName.Split("_");
                            for (int i = 0; i < splited.Length; i++)
                            {
                                splited[i] = char.ToUpper(splited[i][0]) + splited[i].Substring(1);
                            }
                            string formattedPropName = string.Concat(splited);
                            sb.AppendLine($"        [JsonPropertyName(\"{propName}\")]");
                            sb.AppendLine($"        public {propType} {formattedPropName} {{ get; set; }}");
                        }
                    }

                    sb.AppendLine("    }");
                    sb.AppendLine();
                }
            }

            var paths = rootNode["paths"];
            AppendWebRequestsFromPaths(sb, paths);
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
            else if (jsonNode["$ref"] != null)
            {
                var splited = jsonNode["$ref"]?.ToString().Split("/");
                return splited[splited.Length - 1];
            }
            else if (jsonNode["anyOf"] != null)
            {
                return ConvertToCSharpType(jsonNode["anyOf"][0]) + "?";
            }
            switch (jsonType)
            {
                case "string": return "string";
                case "integer": return "int";
                case "boolean": return "bool";
                case "number": return "decimal";

                default: return "object";
            }
        }

        private static void AppendWebRequestsFromPaths(StringBuilder sb, JsonNode pathsNode)
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

                        StringBuilder parameterDefinitions = new StringBuilder();
                        StringBuilder parameterUsages = new StringBuilder();

                        var parameters = method.Value["parameters"];
                        if (parameters != null)
                        {
                            List<string> queryParameters = new List<string>();
                            List<string> headerParameters = new List<string>();

                            foreach (var parameter in parameters as JsonArray)
                            {
                                string parameterName = parameter["name"].ToString();
                                string parameterType = ConvertToCSharpType(parameter["schema"]);
                                parameterDefinitions.Append($"{parameterType} {parameterName}, ");

                                switch (parameter["in"].ToString())
                                {
                                    case "query":
                                        queryParameters.Add($"{parameterName}={{{parameterName}}}");
                                        break;
                                    case "header":
                                        headerParameters.Add(parameterName);
                                        break;
                                }
                            }

                            if (queryParameters.Any())
                            {
                                string queryString = string.Join("&", queryParameters);
                                parameterUsages.AppendLine($"            url += $\"?{queryString}\";");
                            }

                            parameterUsages.AppendLine("            request.uri = new Uri(url);");

                            foreach (var headerName in headerParameters)
                            {
                                parameterUsages.AppendLine($"            request.SetRequestHeader(\"{headerName}\", {headerName}.ToString());");
                            }
                        }

                        var requestBody = method.Value["requestBody"];
                        if (requestBody != null)
                        {
                            string requestBodyType = "string";
                            if (requestBody["content"]["application/json"]["schema"]["$ref"] != null)
                            {
                                string schemaRef = requestBody["content"]["application/json"]["schema"]["$ref"].ToString();
                                string[] parts = schemaRef.Split('/');
                                requestBodyType = parts[parts.Length - 1];
                            }

                            parameterDefinitions.Append($"{requestBodyType} requestBody, ");
                            parameterUsages.AppendLine($"            var bodyString = System.Text.Json.JsonSerializer.Serialize(requestBody);");
                            parameterUsages.AppendLine($"            var jsonToSend = new UTF8Encoding().GetBytes(bodyString);");
                            parameterUsages.AppendLine($"            request.uploadHandler = new UploadHandlerRaw(jsonToSend);");
                            parameterUsages.AppendLine($"            request.uploadHandler.contentType = \"application/json\";");
                        }

                        bool hasReturnType = false;
                        var responseSchema = method.Value["responses"]?["200"]?["content"]?["application/json"]?["schema"];
                        if (responseSchema != null)
                        {
                            if (responseSchema["$ref"] != null)
                            {
                                string schemaRef = responseSchema["$ref"].ToString();
                                string[] parts = schemaRef.Split('/');
                                returnType = parts[parts.Length - 1];
                                hasReturnType = true;
                            }

                            if (responseSchema["type"] != null &&
                                responseSchema["type"].ToString() == "array" &&
                                responseSchema["items"] != null)
                            {
                                string schemaRef;

                                if (responseSchema["items"]["$ref"] != null)
                                {
                                    schemaRef = responseSchema["items"]["$ref"].ToString();
                                    string[] parts = schemaRef.Split('/');
                                    returnType = parts[parts.Length - 1] + "[]";
                                }

                                if (responseSchema["items"]["type"] != null)
                                {
                                    schemaRef = responseSchema["items"]["type"].ToString();
                                    returnType = schemaRef + "[]";
                                }
                                hasReturnType = true;
                            }
                        }

                        sb.AppendLine($"    public async Task {methodName}({parameterDefinitions}Action<{returnType}> onSuccess, Action<string> onError)");
                        sb.AppendLine("    {");
                        sb.AppendLine($"        string url = Url + \"{path}\";");
                        sb.AppendLine($"        using (var request = new UnityWebRequest(url, \"{httpMethod}\"))");
                        sb.AppendLine("        {");
                        sb.Append(parameterUsages);
                        if (hasReturnType)
                        {
                            sb.AppendLine($"            request.downloadHandler = new DownloadHandlerBuffer();");
                        }
                        sb.AppendLine($"            request.SetRequestHeader(\"accept\", \"application/json\");");
                        sb.AppendLine($"            request.SetRequestHeader(\"Content-Type\", \"application/json\");");
                        sb.AppendLine($"            request.timeout = {_timeOut};");
                        sb.AppendLine("            try");
                        sb.AppendLine("            {");
                        sb.AppendLine("                await request.SendWebRequest();");
                        sb.AppendLine("                if (request.result != UnityWebRequest.Result.Success)");
                        sb.AppendLine("                {");
                        sb.AppendLine("                    onError?.Invoke(request.error);");
                        sb.AppendLine("                    return;");
                        sb.AppendLine("                }");
                        sb.AppendLine("                string responseBody = request.downloadHandler.text;");
                        if (hasReturnType)
                        {
                            sb.AppendLine($"                {returnType} result = System.Text.Json.JsonSerializer.Deserialize<{returnType}>(responseBody);");
                            sb.AppendLine("                onSuccess?.Invoke(result);");
                        }
                        else
                        {
                            sb.AppendLine("                onSuccess?.Invoke(responseBody);");
                        }
                        sb.AppendLine("            }");
                        sb.AppendLine("            catch (Exception ex)");
                        sb.AppendLine("            {");
                        sb.AppendLine("                onError?.Invoke(ex.Message);");
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
            var methodName = httpMethod.ToLower();
            methodName = char.ToUpper(methodName[0]) + methodName.Substring(1);
            sb.Append(methodName);
            string[] segments = path.Split('/');
            foreach (var segment in segments)
            {
                if (!string.IsNullOrEmpty(segment))
                {
                    sb.Append(char.ToUpper(segment[0]) + segment.Substring(1));
                }
            }
            return sb.ToString().Replace("-", "").Replace("Api", string.Empty) + "Async";
        }

        private static void RefreshGeneratedInfo()
        {
            if (Directory.Exists(_outputDir))
            {
                string[] filePaths = Directory.GetFiles(_outputDir, "*.cs", SearchOption.AllDirectories);
                if (filePaths.Length == _dataList.Count)
                    return;
                _dataList.Clear();
                foreach (var filePath in filePaths)
                {
                    string content = File.ReadAllText(filePath);
                    string pattern = @"//     Source URL: \S+";
                    Match match = Regex.Match(content, pattern);
                    if (match.Success)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        var sourceUrl = match.Value.Replace("//     Source URL: ", string.Empty);
                        _dataList.Add(new KeyValuePair<string, string>(fileName, sourceUrl));
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }
    }
}
