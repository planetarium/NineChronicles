/*
 * reference: https://gitlab.com/-/snippets/2077829
 */

using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Nekoyume.L10n.Editor
{
    public class FontAssetGenerator
    {
        private class ProxyList<T>
        {
            private List<T> list;
            private FieldInfo fieldInfo;
            private TMPro_FontAssetCreatorWindow window;

            public T this[int index]
            {
                get
                {
                    RefreshValue();
                    return list[index];
                }
                set
                {
                    RefreshValue();
                    list[index] = value;
                    UpdateValue();
                }
            }

            public int Count
            {
                get
                {
                    RefreshValue();
                    return list.Count;
                }
            }

            private void RefreshValue()
            {
                list = (List<T>)fieldInfo.GetValue(window);
            }

            public void UpdateValue()
            {
                fieldInfo.SetValue(window, list);
            }

            public ProxyList(string name, TMPro_FontAssetCreatorWindow window)
            {
                this.window = window;
                fieldInfo = typeof(TMPro_FontAssetCreatorWindow)
                    .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                RefreshValue();
            }

            public void Add(T value)
            {
                RefreshValue();
                list.Add(value);
                UpdateValue();
            }

            public void Clear()
            {
                list.Clear();
                UpdateValue();
            }

            public List<T> GetList()
            {
                RefreshValue();
                return list;
            }
        }

        public class ProxyDictionary<K, V>
        {
            private Dictionary<K, V> dict;
            private TMPro_FontAssetCreatorWindow window;
            private FieldInfo fieldInfo;

            public V this[K key]
            {
                get
                {
                    RefreshValue();
                    return dict[key];
                }
                set
                {
                    RefreshValue();
                    dict[key] = value;
                    UpdateValue();
                }
            }

            public ProxyDictionary(string name, TMPro_FontAssetCreatorWindow window)
            {
                this.window = window;
                fieldInfo = typeof(TMPro_FontAssetCreatorWindow)
                    .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                RefreshValue();
            }

            private void RefreshValue()
            {
                dict = (Dictionary<K, V>)fieldInfo.GetValue(window);
            }

            public void UpdateValue()
            {
                fieldInfo.SetValue(window, dict);
            }

            public void Clear()
            {
                dict.Clear();
                UpdateValue();
            }

            public bool ContainsKey(K key)
            {
                RefreshValue();
                return dict.ContainsKey(key);
            }

            public void Add(K key, V value)
            {
                RefreshValue();
                dict[key] = value;
                UpdateValue();
            }
        }

        private enum GlyphRasterModes
        {
            RASTER_MODE_8BIT = 1,
            RASTER_MODE_MONO = 2,
            RASTER_MODE_NO_HINTING = 4,
            RASTER_MODE_HINTED = 8,
            RASTER_MODE_BITMAP = 16,
            RASTER_MODE_SDF = 32,
            RASTER_MODE_SDFAA = 64,
            RASTER_MODE_MSDF = 256,
            RASTER_MODE_MSDFA = 512,
            RASTER_MODE_1X = 4096,
            RASTER_MODE_8X = 8192,
            RASTER_MODE_16X = 16384,
            RASTER_MODE_32X = 32768
        }

        private TMPro_FontAssetCreatorWindow window;

        private FieldInfo GetField(string name)
        {
            return typeof(TMPro_FontAssetCreatorWindow)
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private MethodInfo GetNonStaticMethod(string name)
        {
            return typeof(TMPro_FontAssetCreatorWindow)
                .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private MethodInfo GetStaticMethod(string name)
        {
            return typeof(TMPro_FontAssetCreatorWindow)
                .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        }

        private object GetObject(FieldInfo field)
        {
            return field.GetValue(window);
        }

        private T GetValue<T>(FieldInfo field)
        {
            var value = GetObject(field);
            if (value == null)
            {
                return default;
            }

            return (T)value;
        }

        private void SetValue<T>(FieldInfo field, T value)
        {
            field.SetValue(window, value);
        }

#region Fields

        private FieldInfo atlasGenerationProgress;

        private float m_AtlasGenerationProgress
        {
            get => GetValue<float>(atlasGenerationProgress);
            set => SetValue<float>(atlasGenerationProgress, value);
        }

        private FieldInfo isProcessing;

        private bool m_IsProcessing
        {
            get => GetValue<bool>(isProcessing);
            set => SetValue<bool>(isProcessing, value);
        }

        private FieldInfo sourceFontFile;

        private Object m_SourceFontFile
        {
            get => GetValue<Object>(sourceFontFile);
            set => SetValue<Object>(sourceFontFile, value);
        }

        private FieldInfo fontAtlasTexture;

        private Texture2D m_FontAtlasTexture
        {
            get
            {
                var result = GetValue<Texture2D>(fontAtlasTexture);
                return result;
            }
            set => SetValue<Texture2D>(fontAtlasTexture, value);
        }

        private FieldInfo savedFontAtlas;

        private Texture2D m_SavedFontAtlas
        {
            get => GetValue<Texture2D>(savedFontAtlas);
            set => SetValue<Texture2D>(savedFontAtlas, value);
        }

        private FieldInfo outputFeedback;

        private string m_OutputFeedback
        {
            get => GetValue<string>(outputFeedback);
            set => SetValue<string>(outputFeedback, value);
        }

        private FieldInfo characterSetSelectionMode;

        private int m_CharacterSetSelectionMode
        {
            get => GetValue<int>(characterSetSelectionMode);
            set => SetValue<int>(characterSetSelectionMode, value);
        }

        private FieldInfo characterSequence;

        private string m_CharacterSequence
        {
            get => GetValue<string>(characterSequence);
            set => SetValue<string>(characterSequence, value);
        }

        private FieldInfo glyphRectPreviewTexture;

        private Texture2D m_GlyphRectPreviewTexture
        {
            get => GetValue<Texture2D>(glyphRectPreviewTexture);
            set => SetValue<Texture2D>(glyphRectPreviewTexture, value);
        }

        private FieldInfo characterCount;

        private int m_CharacterCount
        {
            get => GetValue<int>(characterCount);
            set => SetValue<int>(characterCount, value);
        }

        private FieldInfo isGenerationCancelled;

        private bool m_IsGenerationCancelled
        {
            get => GetValue<bool>(isGenerationCancelled);
            set => SetValue<bool>(isGenerationCancelled, value);
        }

        private FieldInfo glyphRenderMode;

        private GlyphRenderMode m_GlyphRenderMode
        {
            get => GetValue<GlyphRenderMode>(glyphRenderMode);
            set => SetValue<GlyphRenderMode>(glyphRenderMode, value);
        }

        private Stopwatch m_StopWatch;

        private ProxyDictionary<uint, uint> m_CharacterLookupMap;
        private ProxyDictionary<uint, List<uint>> m_GlyphLookupMap;

        private ProxyList<uint> m_AvailableGlyphsToAdd;
        private ProxyList<uint> m_MissingCharacters;
        private ProxyList<Glyph> m_GlyphsToPack;
        private ProxyList<Glyph> m_GlyphsPacked;
        private ProxyList<GlyphRect> m_FreeGlyphRects;
        private ProxyList<GlyphRect> m_UsedGlyphRects;
        private ProxyList<Glyph> m_FontGlyphTable;
        private ProxyList<Glyph> m_GlyphsToRender;
        private ProxyList<TMP_Character> m_FontCharacterTable;
        private ProxyList<uint> m_ExcludedCharacters;

        private FieldInfo pointSizeSamplingMode;

        private int m_PointSizeSamplingMode
        {
            get => GetValue<int>(pointSizeSamplingMode);
            set => SetValue<int>(pointSizeSamplingMode, value);
        }

        private FieldInfo atlasWidth;

        private int m_AtlasWidth
        {
            get => GetValue<int>(atlasWidth);
            set => SetValue<int>(atlasWidth, value);
        }

        private FieldInfo atlasHeight;

        private int m_AtlasHeight
        {
            get => GetValue<int>(atlasHeight);
            set => SetValue<int>(atlasHeight, value);
        }

        private FieldInfo pointSize;

        private int m_PointSize
        {
            get => GetValue<int>(pointSize);
            set => SetValue<int>(pointSize, value);
        }

        private FieldInfo atlasGenerationProgressLabel;

        private string m_AtlasGenerationProgressLabel
        {
            get => GetValue<string>(atlasGenerationProgressLabel);
            set => SetValue<string>(atlasGenerationProgressLabel, value);
        }

        private FieldInfo padding;

        private int m_Padding
        {
            get => GetValue<int>(padding);
            set => SetValue<int>(padding, value);
        }

        private FieldInfo packingMode;

        private int m_PackingMode
        {
            get => GetValue<int>(packingMode);
            set => SetValue<int>(packingMode, value);
        }

        private FieldInfo glyphPackingGenerationTime;

        private double m_GlyphPackingGenerationTime
        {
            get => GetValue<double>(glyphPackingGenerationTime);
            set => SetValue<double>(glyphPackingGenerationTime, value);
        }

        private FieldInfo isGlyphPackingDone;

        private bool m_IsGlyphPackingDone
        {
            get => GetValue<bool>(isGlyphPackingDone);
            set => SetValue<bool>(isGlyphPackingDone, value);
        }

        private FieldInfo isRenderingDone;

        private bool m_IsRenderingDone
        {
            get => GetValue<bool>(isRenderingDone);
            set => SetValue<bool>(isRenderingDone, value);
        }

        private FieldInfo faceInfo;

        private FaceInfo m_FaceInfo
        {
            get => GetValue<FaceInfo>(faceInfo);
            set => SetValue<FaceInfo>(faceInfo, value);
        }

        private FieldInfo atlasTextureBuffer;

        private byte[] m_AtlasTextureBuffer
        {
            get => GetValue<byte[]>(atlasTextureBuffer);
            set => SetValue<byte[]>(atlasTextureBuffer, value);
        }

        private FieldInfo glyphRenderingGenerationTime;

        private double m_GlyphRenderingGenerationTime
        {
            get => GetValue<double>(glyphRenderingGenerationTime);
            set => SetValue<double>(glyphRenderingGenerationTime, value);
        }

        private FieldInfo isGlyphRenderingDone;

        private bool m_IsGlyphRenderingDone
        {
            get => GetValue<bool>(isGlyphRenderingDone);
            set => SetValue<bool>(isGlyphRenderingDone, value);
        }

        private FieldInfo referencedFontAsset;

        private TMP_FontAsset m_ReferencedFontAsset
        {
            get => GetValue<TMP_FontAsset>(referencedFontAsset);
            set => SetValue<TMP_FontAsset>(referencedFontAsset, value);
        }

        private FieldInfo includeFontFeatures;

        private bool m_IncludeFontFeatures
        {
            get => GetValue<bool>(includeFontFeatures);
            set => SetValue<bool>(includeFontFeatures, value);
        }

#endregion

#region Methods

        private MethodInfo parseHexNumberSequence;

        private uint[] ParseHexNumberSequence(string charList)
        {
            return (uint[])parseHexNumberSequence.Invoke(window, new object[] { charList });
        }

        private MethodInfo parseNumberSequence;

        private uint[] ParseNumberSequence(string charList)
        {
            return (uint[])parseNumberSequence.Invoke(window, new object[] { charList });
        }

        private MethodInfo tryPackGlyphsInAtlas;

        private void TryPackGlyphsInAtlas(List<Glyph> m_GlyphsToPack, List<Glyph> m_GlyphsPacked, int m_Padding,
            GlyphPackingMode m_PackingMode, GlyphRenderMode m_GlyphRenderMode, int m_AtlasWidth, int m_AtlasHeight,
            List<GlyphRect> m_FreeGlyphRects, List<GlyphRect> m_UsedGlyphRects)
        {
            tryPackGlyphsInAtlas.Invoke(window,
                new object[]
                {
                    m_GlyphsToPack, m_GlyphsPacked, m_Padding, m_PackingMode, m_GlyphRenderMode, m_AtlasWidth,
                    m_AtlasHeight, m_FreeGlyphRects, m_UsedGlyphRects
                });
        }

        private MethodInfo renderGlyphsToTexture;

        private void RenderGlyphsToTexture(List<Glyph> m_GlyphsToRender, int m_Padding, GlyphRenderMode m_GlyphRenderMode,
            byte[] m_AtlasTextureBuffer, int m_AtlasWidth, int m_AtlasHeight)
        {
            renderGlyphsToTexture.Invoke(window,
                new object[]
                {
                    m_GlyphsToRender, m_Padding, m_GlyphRenderMode, m_AtlasTextureBuffer, m_AtlasWidth, m_AtlasHeight
                });
        }

        private MethodInfo saveCreationSettingsToEditorPrefs;

        private void SaveCreationSettingsToEditorPrefs(FontAssetCreationSettings settings)
        {
            saveCreationSettingsToEditorPrefs.Invoke(window, new object[] { settings });
        }

        private MethodInfo saveFontCreationSettings;

        private FontAssetCreationSettings SaveFontCreationSettings()
        {
            return (FontAssetCreationSettings)saveFontCreationSettings.Invoke(window, new object[0]);
        }

        private MethodInfo save_SDF_FontAsset;

        private void Save_SDF_FontAsset(string filePath)
        {
            save_SDF_FontAsset.Invoke(window, new object[] { filePath });
        }

#endregion

        public FontAssetGenerator(TMPro_FontAssetCreatorWindow window)
        {
            this.window = window;

            // Fields
            sourceFontFile = GetField("m_SourceFontFile");
            pointSizeSamplingMode = GetField("m_PointSizeSamplingMode");
            pointSize = GetField("m_PointSize");
            padding = GetField("m_Padding");
            packingMode = GetField("m_PackingMode");
            atlasWidth = GetField("m_AtlasWidth");
            atlasHeight = GetField("m_AtlasHeight");
            characterSetSelectionMode = GetField("m_CharacterSetSelectionMode");
            referencedFontAsset = GetField("m_ReferencedFontAsset");
            characterSequence = GetField("m_CharacterSequence");
            glyphRenderMode = GetField("m_GlyphRenderMode");
            includeFontFeatures = GetField("m_IncludeFontFeatures");
            //////////////////////
            atlasGenerationProgress = GetField("m_AtlasGenerationProgress");
            isProcessing = GetField("m_IsProcessing");
            fontAtlasTexture = GetField("m_FontAtlasTexture");
            savedFontAtlas = GetField("m_SavedFontAtlas");
            outputFeedback = GetField("m_OutputFeedback");
            glyphRectPreviewTexture = GetField("m_GlyphRectPreviewTexture");
            characterCount = GetField("m_CharacterCount");
            isGenerationCancelled = GetField("m_IsGenerationCancelled");
            atlasGenerationProgressLabel = GetField("m_AtlasGenerationProgressLabel");
            glyphPackingGenerationTime = GetField("m_GlyphPackingGenerationTime");
            isGlyphPackingDone = GetField("m_IsGlyphPackingDone");
            isRenderingDone = GetField("m_IsRenderingDone");
            faceInfo = GetField("m_FaceInfo");
            atlasTextureBuffer = GetField("m_AtlasTextureBuffer");
            glyphRenderingGenerationTime = GetField("m_GlyphRenderingGenerationTime");
            isGlyphRenderingDone = GetField("m_IsGlyphRenderingDone");
            // Lists
            m_AvailableGlyphsToAdd = new ProxyList<uint>("m_AvailableGlyphsToAdd", window);
            m_MissingCharacters = new ProxyList<uint>("m_MissingCharacters", window);
            m_GlyphsToPack = new ProxyList<Glyph>("m_GlyphsToPack", window);
            m_GlyphsPacked = new ProxyList<Glyph>("m_GlyphsPacked", window);
            m_FreeGlyphRects = new ProxyList<GlyphRect>("m_FreeGlyphRects", window);
            m_UsedGlyphRects = new ProxyList<GlyphRect>("m_UsedGlyphRects", window);
            m_FontGlyphTable = new ProxyList<Glyph>("m_FontGlyphTable", window);
            m_GlyphsToRender = new ProxyList<Glyph>("m_GlyphsToRender", window);
            m_FontCharacterTable = new ProxyList<TMP_Character>("m_FontCharacterTable", window);
            m_ExcludedCharacters = new ProxyList<uint>("m_ExcludedCharacters", window);
            m_CharacterLookupMap = new ProxyDictionary<uint, uint>("m_CharacterLookupMap", window);
            m_GlyphLookupMap = new ProxyDictionary<uint, List<uint>>("m_GlyphLookupMap", window);

            // Methods
            parseHexNumberSequence = GetStaticMethod("ParseHexNumberSequence");
            parseNumberSequence = GetStaticMethod("ParseNumberSequence");
            saveCreationSettingsToEditorPrefs = GetNonStaticMethod("SaveCreationSettingsToEditorPrefs");
            saveFontCreationSettings = GetNonStaticMethod("SaveFontCreationSettings");
            save_SDF_FontAsset = GetNonStaticMethod("Save_SDF_FontAsset");

            tryPackGlyphsInAtlas = typeof(FontEngine)
                .GetMethod("TryPackGlyphsInAtlas", BindingFlags.Static | BindingFlags.NonPublic);

            var methods = typeof(FontEngine).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (method.Name != "RenderGlyphsToTexture")
                {
                    continue;
                }

                var pars = method.GetParameters();
                if (
                    pars[0].ParameterType == typeof(List<Glyph>) &&
                    pars[1].ParameterType == typeof(int) &&
                    pars[2].ParameterType == typeof(GlyphRenderMode) &&
                    pars[3].ParameterType == typeof(byte[]) &&
                    pars[4].ParameterType == typeof(int) &&
                    pars[5].ParameterType == typeof(int)
                )
                {
                    renderGlyphsToTexture = method;
                    break;
                }
            }
        }

        private void DestroyImmediate(Object obj)
        {
            Object.DestroyImmediate(obj);
        }

        public void GenerateAtlas(FontAssetGenerationSetting settings)
        {
            SetValue(sourceFontFile, settings.sourceFontFile);
            SetValue(pointSizeSamplingMode, settings.pointSizeSamplingMode);
            SetValue(pointSize, settings.pointSize);
            SetValue(padding, settings.padding);
            SetValue(packingMode, settings.packingMode);
            SetValue(atlasWidth, settings.atlasWidth);
            SetValue(atlasHeight, settings.atlasHeight);
            SetValue(characterSetSelectionMode, settings.characterSetSelectionMode);
            SetValue(referencedFontAsset, settings.referencedFontAsset);
            SetValue(characterSequence, settings.characterSequence);
            SetValue(glyphRenderMode, settings.renderMode);
            SetValue(includeFontFeatures, settings.includeFontFeatures);

            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"sourceFontFile : {AssetDatabase.GetAssetPath(GetValue<Object>(sourceFontFile))}\n");
            stringBuilder.Append($"pointSizeSamplingMode : {GetValue<int>(pointSizeSamplingMode)}\n");
            stringBuilder.Append($"pointSize : {GetValue<int>(pointSize)}\n");
            stringBuilder.Append($"padding : {GetValue<int>(padding)}\n");
            stringBuilder.Append($"packingMode : {GetValue<int>(packingMode)}\n");
            stringBuilder.Append($"atlasWidth : {GetValue<int>(atlasWidth)}\n");
            stringBuilder.Append($"atlasHeight : {GetValue<int>(atlasHeight)}\n");
            stringBuilder.Append($"characterSetSelectionMode : {GetValue<int>(characterSetSelectionMode)}\n");
            stringBuilder.Append($"characterSequence : {GetValue<string>(characterSequence)}\n");
            stringBuilder.Append($"glyphRenderMode : {GetValue<int>(glyphRenderMode)}\n");
            Debug.Log(stringBuilder.ToString());

            if (!m_IsProcessing && m_SourceFontFile != null)
            {
                try
                {
                    DestroyImmediate(m_FontAtlasTexture);
                    DestroyImmediate(m_GlyphRectPreviewTexture);
                }
                catch (Exception e)
                {
                }

                m_FontAtlasTexture = null;
                m_SavedFontAtlas = null;
                m_OutputFeedback = string.Empty;

                // Initialize font engine
                var errorCode = FontEngine.InitializeFontEngine();
                if (errorCode != FontEngineError.Success)
                {
                    Debug.Log("Font Asset Creator - Error [" + errorCode +
                        "] has occurred while Initializing the FreeType Library.");
                }

                // Get file path of the source font file.
                var fontPath = AssetDatabase.GetAssetPath(m_SourceFontFile);

                if (errorCode == FontEngineError.Success)
                {
                    errorCode = FontEngine.LoadFontFace(fontPath);

                    if (errorCode != FontEngineError.Success)
                    {
                        Debug.Log(
                            "Font Asset Creator - Error Code [" + errorCode + "] has occurred trying to load the [" +
                            m_SourceFontFile.name +
                            "] font file. This typically results from the use of an incompatible or corrupted font file.",
                            m_SourceFontFile);
                    }
                }


                // Define an array containing the characters we will render.
                if (errorCode == FontEngineError.Success)
                {
                    uint[] characterSet = null;

                    // Get list of characters that need to be packed and rendered to the atlas texture.
                    if (m_CharacterSetSelectionMode == 7 || m_CharacterSetSelectionMode == 8)
                    {
                        var char_List = new List<uint>();

                        for (var i = 0; i < m_CharacterSequence.Length; i++)
                        {
                            uint unicode = m_CharacterSequence[i];

                            // Handle surrogate pairs
                            if (i < m_CharacterSequence.Length - 1 && char.IsHighSurrogate((char)unicode) &&
                                char.IsLowSurrogate(m_CharacterSequence[i + 1]))
                            {
                                unicode = (uint)char.ConvertToUtf32(m_CharacterSequence[i],
                                    m_CharacterSequence[i + 1]);
                                i += 1;
                            }

                            // Check to make sure we don't include duplicates
                            if (char_List.FindIndex(item => item == unicode) == -1)
                            {
                                char_List.Add(unicode);
                            }
                        }

                        characterSet = char_List.ToArray();
                    }
                    else if (m_CharacterSetSelectionMode == 6)
                    {
                        characterSet = ParseHexNumberSequence(m_CharacterSequence);
                    }
                    else
                    {
                        characterSet = ParseNumberSequence(m_CharacterSequence);
                    }

                    m_CharacterCount = characterSet.Length;

                    m_AtlasGenerationProgress = 0;
                    m_IsProcessing = true;
                    m_IsGenerationCancelled = false;

                    var glyphLoadFlags =
                        ((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_HINTED) ==
                        GlyphRasterModes.RASTER_MODE_HINTED
                            ? GlyphLoadFlags.LOAD_RENDER
                            : GlyphLoadFlags.LOAD_RENDER | GlyphLoadFlags.LOAD_NO_HINTING;

                    glyphLoadFlags = ((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_MONO) ==
                        GlyphRasterModes.RASTER_MODE_MONO
                            ? glyphLoadFlags | GlyphLoadFlags.LOAD_MONOCHROME
                            : glyphLoadFlags;

                    //
                    var autoEvent = new AutoResetEvent(false);

                    // Worker thread to pack glyphs in the given texture space.
                    ThreadPool.QueueUserWorkItem(PackGlyphs =>
                    {
                        // Start Stop Watch
                        m_StopWatch = Stopwatch.StartNew();

                        // Clear the various lists used in the generation process.
                        m_AvailableGlyphsToAdd.Clear();
                        m_MissingCharacters.Clear();
                        m_ExcludedCharacters.Clear();
                        m_CharacterLookupMap.Clear();
                        m_GlyphLookupMap.Clear();
                        m_GlyphsToPack.Clear();
                        m_GlyphsPacked.Clear();

                        // Check if requested characters are available in the source font file.
                        for (var i = 0; i < characterSet.Length; i++)
                        {
                            var unicode = characterSet[i];
                            uint glyphIndex;

                            if (FontEngine.TryGetGlyphIndex(unicode, out glyphIndex))
                            {
                                // Skip over potential duplicate characters.
                                if (m_CharacterLookupMap.ContainsKey(unicode))
                                {
                                    continue;
                                }

                                // Add character to character lookup map.
                                m_CharacterLookupMap.Add(unicode, glyphIndex);

                                // Skip over potential duplicate glyph references.
                                if (m_GlyphLookupMap.ContainsKey(glyphIndex))
                                {
                                    // Add additional glyph reference for this character.
                                    m_GlyphLookupMap[glyphIndex].Add(unicode);
                                    continue;
                                }

                                // Add glyph reference to glyph lookup map.
                                m_GlyphLookupMap.Add(glyphIndex, new List<uint>() { unicode });

                                // Add glyph index to list of glyphs to add to texture.
                                m_AvailableGlyphsToAdd.Add(glyphIndex);
                            }
                            else
                            {
                                // Add Unicode to list of missing characters.
                                m_MissingCharacters.Add(unicode);
                            }
                        }

                        // Pack available glyphs in the provided texture space.
                        if (m_AvailableGlyphsToAdd.Count > 0)
                        {
                            var packingModifier =
                                ((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) ==
                                GlyphRasterModes.RASTER_MODE_BITMAP
                                    ? 0
                                    : 1;

                            if (m_PointSizeSamplingMode == 0) // Auto-Sizing Point Size Mode
                            {
                                // Estimate min / max range for auto sizing of point size.
                                var minPointSize = 0;
                                var maxPointSize =
                                    (int)Mathf.Sqrt(m_AtlasWidth * m_AtlasHeight / m_AvailableGlyphsToAdd.Count) * 3;

                                m_PointSize = (maxPointSize + minPointSize) / 2;

                                var optimumPointSizeFound = false;
                                for (var iteration = 0; iteration < 15 && optimumPointSizeFound == false; iteration++)
                                {
                                    m_AtlasGenerationProgressLabel = "Packing glyphs - Pass (" + iteration + ")";

                                    FontEngine.SetFaceSize(m_PointSize);

                                    m_GlyphsToPack.Clear();
                                    m_GlyphsPacked.Clear();

                                    m_FreeGlyphRects.Clear();
                                    m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier,
                                        m_AtlasHeight - packingModifier));
                                    m_UsedGlyphRects.Clear();

                                    for (var i = 0; i < m_AvailableGlyphsToAdd.Count; i++)
                                    {
                                        var glyphIndex = m_AvailableGlyphsToAdd[i];
                                        Glyph glyph;

                                        if (FontEngine.TryGetGlyphWithIndexValue(glyphIndex, glyphLoadFlags, out glyph))
                                        {
                                            if (glyph.glyphRect.width > 0 && glyph.glyphRect.height > 0)
                                            {
                                                m_GlyphsToPack.Add(glyph);
                                            }
                                            else
                                            {
                                                m_GlyphsPacked.Add(glyph);
                                            }
                                        }
                                    }

                                    TryPackGlyphsInAtlas(m_GlyphsToPack.GetList(), m_GlyphsPacked.GetList(), m_Padding,
                                        (GlyphPackingMode)m_PackingMode, m_GlyphRenderMode, m_AtlasWidth,
                                        m_AtlasHeight, m_FreeGlyphRects.GetList(), m_UsedGlyphRects.GetList());
                                    m_GlyphsToPack.UpdateValue();
                                    m_GlyphsPacked.UpdateValue();
                                    m_FreeGlyphRects.UpdateValue();
                                    m_UsedGlyphRects.UpdateValue();


                                    if (m_IsGenerationCancelled)
                                    {
                                        DestroyImmediate(m_FontAtlasTexture);
                                        m_FontAtlasTexture = null;
                                        return;
                                    }

                                    //Debug.Log("Glyphs remaining to add [" + m_GlyphsToAdd.Count + "]. Glyphs added [" + m_GlyphsAdded.Count + "].");

                                    if (m_GlyphsToPack.Count > 0)
                                    {
                                        if (m_PointSize > minPointSize)
                                        {
                                            maxPointSize = m_PointSize;
                                            m_PointSize = (m_PointSize + minPointSize) / 2;

                                            //Debug.Log("Decreasing point size from [" + maxPointSize + "] to [" + m_PointSize + "].");
                                        }
                                    }
                                    else
                                    {
                                        if (maxPointSize - minPointSize > 1 && m_PointSize < maxPointSize)
                                        {
                                            minPointSize = m_PointSize;
                                            m_PointSize = (m_PointSize + maxPointSize) / 2;

                                            //Debug.Log("Increasing point size from [" + minPointSize + "] to [" + m_PointSize + "].");
                                        }
                                        else
                                        {
                                            //Debug.Log("[" + iteration + "] iterations to find the optimum point size of : [" + m_PointSize + "].");
                                            optimumPointSizeFound = true;
                                        }
                                    }
                                }
                            }
                            else // Custom Point Size Mode
                            {
                                m_AtlasGenerationProgressLabel = "Packing glyphs...";

                                // Set point size
                                FontEngine.SetFaceSize(m_PointSize);

                                m_GlyphsToPack.Clear();
                                m_GlyphsPacked.Clear();

                                m_FreeGlyphRects.Clear();
                                m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier,
                                    m_AtlasHeight - packingModifier));
                                m_UsedGlyphRects.Clear();

                                for (var i = 0; i < m_AvailableGlyphsToAdd.Count; i++)
                                {
                                    var glyphIndex = m_AvailableGlyphsToAdd[i];
                                    Glyph glyph;

                                    if (FontEngine.TryGetGlyphWithIndexValue(glyphIndex, glyphLoadFlags, out glyph))
                                    {
                                        if (glyph.glyphRect.width > 0 && glyph.glyphRect.height > 0)
                                        {
                                            m_GlyphsToPack.Add(glyph);
                                        }
                                        else
                                        {
                                            m_GlyphsPacked.Add(glyph);
                                        }
                                    }
                                }

                                TryPackGlyphsInAtlas(m_GlyphsToPack.GetList(), m_GlyphsPacked.GetList(), m_Padding,
                                    (GlyphPackingMode)m_PackingMode, m_GlyphRenderMode, m_AtlasWidth, m_AtlasHeight,
                                    m_FreeGlyphRects.GetList(), m_UsedGlyphRects.GetList());
                                m_GlyphsToPack.UpdateValue();
                                m_GlyphsPacked.UpdateValue();
                                m_FreeGlyphRects.UpdateValue();
                                m_UsedGlyphRects.UpdateValue();

                                if (m_IsGenerationCancelled)
                                {
                                    DestroyImmediate(m_FontAtlasTexture);
                                    m_FontAtlasTexture = null;
                                    return;
                                }
                                //Debug.Log("Glyphs remaining to add [" + m_GlyphsToAdd.Count + "]. Glyphs added [" + m_GlyphsAdded.Count + "].");
                            }
                        }
                        else
                        {
                            var packingModifier =
                                ((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) ==
                                GlyphRasterModes.RASTER_MODE_BITMAP
                                    ? 0
                                    : 1;

                            FontEngine.SetFaceSize(m_PointSize);

                            m_GlyphsToPack.Clear();
                            m_GlyphsPacked.Clear();

                            m_FreeGlyphRects.Clear();
                            m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier,
                                m_AtlasHeight - packingModifier));
                            m_UsedGlyphRects.Clear();
                        }

                        //Stop StopWatch
                        m_StopWatch.Stop();
                        m_GlyphPackingGenerationTime = m_StopWatch.Elapsed.TotalMilliseconds;
                        m_IsGlyphPackingDone = true;
                        m_StopWatch.Reset();

                        m_FontCharacterTable.Clear();
                        m_FontGlyphTable.Clear();
                        m_GlyphsToRender.Clear();

                        // Handle Results and potential cancellation of glyph rendering
                        if ((m_GlyphRenderMode == GlyphRenderMode.SDF32 && m_PointSize > 512) ||
                            (m_GlyphRenderMode == GlyphRenderMode.SDF16 && m_PointSize > 1024) ||
                            (m_GlyphRenderMode == GlyphRenderMode.SDF8 && m_PointSize > 2048))
                        {
                            var upSampling = 1;
                            switch (m_GlyphRenderMode)
                            {
                                case GlyphRenderMode.SDF8:
                                    upSampling = 8;
                                    break;
                                case GlyphRenderMode.SDF16:
                                    upSampling = 16;
                                    break;
                                case GlyphRenderMode.SDF32:
                                    upSampling = 32;
                                    break;
                            }

                            Debug.Log("Glyph rendering has been aborted due to sampling point size of [" +
                                m_PointSize + "] x SDF [" + upSampling +
                                "] up sampling exceeds 16,384 point size. Please revise your generation settings to make sure the sampling point size x SDF up sampling mode does not exceed 16,384.");

                            m_IsRenderingDone = true;
                            m_AtlasGenerationProgress = 0;
                            m_IsGenerationCancelled = true;
                        }

                        // Add glyphs and characters successfully added to texture to their respective font tables.
                        foreach (var glyph in m_GlyphsPacked.GetList())
                        {
                            var glyphIndex = glyph.index;

                            m_FontGlyphTable.Add(glyph);

                            // Add glyphs to list of glyphs that need to be rendered.
                            if (glyph.glyphRect.width > 0 && glyph.glyphRect.height > 0)
                            {
                                m_GlyphsToRender.Add(glyph);
                            }

                            foreach (var unicode in m_GlyphLookupMap[glyphIndex])
                            {
                                // Create new Character
                                m_FontCharacterTable.Add(new TMP_Character(unicode, glyph));
                            }
                        }

                        //
                        foreach (var glyph in m_GlyphsToPack.GetList())
                        {
                            foreach (var unicode in m_GlyphLookupMap[glyph.index])
                            {
                                m_ExcludedCharacters.Add(unicode);
                            }
                        }

                        // Get the face info for the current sampling point size.
                        m_FaceInfo = FontEngine.GetFaceInfo();

                        autoEvent.Set();
                    });

                    // Worker thread to render glyphs in texture buffer.
                    ThreadPool.QueueUserWorkItem(RenderGlyphs =>
                    {
                        autoEvent.WaitOne();

                        if (m_IsGenerationCancelled == false)
                        {
                            // Start Stop Watch
                            m_StopWatch = Stopwatch.StartNew();

                            m_IsRenderingDone = false;

                            // Allocate texture data
                            m_AtlasTextureBuffer = new byte[m_AtlasWidth * m_AtlasHeight];

                            m_AtlasGenerationProgressLabel = "Rendering glyphs...";

                            // Render and add glyphs to the given atlas texture.
                            if (m_GlyphsToRender.Count > 0)
                            {
                                RenderGlyphsToTexture(m_GlyphsToRender.GetList(), m_Padding, m_GlyphRenderMode,
                                    m_AtlasTextureBuffer, m_AtlasWidth, m_AtlasHeight);
                                m_GlyphsToRender.UpdateValue();
                            }

                            m_IsRenderingDone = true;

                            // Stop StopWatch
                            m_StopWatch.Stop();
                            m_GlyphRenderingGenerationTime = m_StopWatch.Elapsed.TotalMilliseconds;
                            m_IsGlyphRenderingDone = true;
                            m_StopWatch.Reset();
                        }
                    });
                }

                SaveCreationSettingsToEditorPrefs(SaveFontCreationSettings());
            }
        }

        public void SaveFontAssetToSDF(string filePath)
        {
            if (filePath.Length == 0)
            {
                NcDebug.LogError("SaveFontAssetToSDF: File Path is empty.");
                return;
            }

            if (!(((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) ==
                GlyphRasterModes.RASTER_MODE_BITMAP))
            {
                Save_SDF_FontAsset(filePath);
                NcDebug.Log("Font Asset has been saved to disk.");
            }
            else
            {
                NcDebug.LogError("Glyph Raster Mode is invalid : It must be SDF.");
            }
        }
    }
}
