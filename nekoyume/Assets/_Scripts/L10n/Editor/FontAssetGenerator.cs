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
        class ProxyList<T>
        {
            List<T> list;
            FieldInfo fieldInfo;
            TMPro_FontAssetCreatorWindow window;

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

            void RefreshValue()
            {
                list = (List<T>) fieldInfo.GetValue(window);
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
            Dictionary<K, V> dict;
            TMPro_FontAssetCreatorWindow window;
            FieldInfo fieldInfo;

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

            void RefreshValue()
            {
                dict = (Dictionary<K, V>) fieldInfo.GetValue(window);
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

        enum GlyphRasterModes
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

        TMPro_FontAssetCreatorWindow window;

        FieldInfo GetField(string name)
        {
            return typeof(TMPro_FontAssetCreatorWindow)
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        MethodInfo GetNonStaticMethod(string name)
        {
            return typeof(TMPro_FontAssetCreatorWindow)
                .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        MethodInfo GetStaticMethod(string name)
        {
            return typeof(TMPro_FontAssetCreatorWindow)
                .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        }

        System.Object GetObject(FieldInfo field)
        {
            return field.GetValue(window);
        }

        T GetValue<T>(FieldInfo field)
        {
            var value = GetObject(field);
            if (value == null)
            {
                return default(T);
            }

            return (T) value;
        }

        void SetValue<T>(FieldInfo field, T value)
        {
            field.SetValue(window, value);
        }

        #region Fields

        FieldInfo atlasGenerationProgress;

        float m_AtlasGenerationProgress
        {
            get { return GetValue<float>(atlasGenerationProgress); }
            set { SetValue<float>(atlasGenerationProgress, value); }
        }

        FieldInfo isProcessing;

        bool m_IsProcessing
        {
            get { return GetValue<bool>(isProcessing); }
            set { SetValue<bool>(isProcessing, value); }
        }

        FieldInfo sourceFontFile;

        UnityEngine.Object m_SourceFontFile
        {
            get { return GetValue<UnityEngine.Object>(sourceFontFile); }
            set { SetValue<UnityEngine.Object>(sourceFontFile, value); }
        }

        FieldInfo fontAtlasTexture;

        Texture2D m_FontAtlasTexture
        {
            get
            {
                var result = GetValue<Texture2D>(fontAtlasTexture);
                return result;
            }
            set { SetValue<Texture2D>(fontAtlasTexture, value); }
        }

        FieldInfo savedFontAtlas;

        Texture2D m_SavedFontAtlas
        {
            get { return GetValue<Texture2D>(savedFontAtlas); }
            set { SetValue<Texture2D>(savedFontAtlas, value); }
        }

        FieldInfo outputFeedback;

        string m_OutputFeedback
        {
            get { return GetValue<string>(outputFeedback); }
            set { SetValue<string>(outputFeedback, value); }
        }

        FieldInfo characterSetSelectionMode;

        int m_CharacterSetSelectionMode
        {
            get { return GetValue<int>(characterSetSelectionMode); }
            set { SetValue<int>(characterSetSelectionMode, value); }
        }

        FieldInfo characterSequence;

        string m_CharacterSequence
        {
            get { return GetValue<string>(characterSequence); }
            set { SetValue<string>(characterSequence, value); }
        }

        FieldInfo glyphRectPreviewTexture;

        Texture2D m_GlyphRectPreviewTexture
        {
            get { return GetValue<Texture2D>(glyphRectPreviewTexture); }
            set { SetValue<Texture2D>(glyphRectPreviewTexture, value); }
        }

        FieldInfo characterCount;

        int m_CharacterCount
        {
            get { return GetValue<int>(characterCount); }
            set { SetValue<int>(characterCount, value); }
        }

        FieldInfo isGenerationCancelled;

        bool m_IsGenerationCancelled
        {
            get { return GetValue<bool>(isGenerationCancelled); }
            set { SetValue<bool>(isGenerationCancelled, value); }
        }

        FieldInfo glyphRenderMode;

        GlyphRenderMode m_GlyphRenderMode
        {
            get { return GetValue<GlyphRenderMode>(glyphRenderMode); }
            set { SetValue<GlyphRenderMode>(glyphRenderMode, value); }
        }

        Stopwatch m_StopWatch;

        ProxyDictionary<uint, uint> m_CharacterLookupMap;
        ProxyDictionary<uint, List<uint>> m_GlyphLookupMap;

        ProxyList<uint> m_AvailableGlyphsToAdd;
        ProxyList<uint> m_MissingCharacters;
        ProxyList<Glyph> m_GlyphsToPack;
        ProxyList<Glyph> m_GlyphsPacked;
        ProxyList<GlyphRect> m_FreeGlyphRects;
        ProxyList<GlyphRect> m_UsedGlyphRects;
        ProxyList<Glyph> m_FontGlyphTable;
        ProxyList<Glyph> m_GlyphsToRender;
        ProxyList<TMP_Character> m_FontCharacterTable;
        ProxyList<uint> m_ExcludedCharacters;

        FieldInfo pointSizeSamplingMode;

        int m_PointSizeSamplingMode
        {
            get { return GetValue<int>(pointSizeSamplingMode); }
            set { SetValue<int>(pointSizeSamplingMode, value); }
        }

        FieldInfo atlasWidth;

        int m_AtlasWidth
        {
            get { return GetValue<int>(atlasWidth); }
            set { SetValue<int>(atlasWidth, value); }
        }

        FieldInfo atlasHeight;

        int m_AtlasHeight
        {
            get { return GetValue<int>(atlasHeight); }
            set { SetValue<int>(atlasHeight, value); }
        }

        FieldInfo pointSize;

        int m_PointSize
        {
            get { return GetValue<int>(pointSize); }
            set { SetValue<int>(pointSize, value); }
        }

        FieldInfo atlasGenerationProgressLabel;

        string m_AtlasGenerationProgressLabel
        {
            get { return GetValue<string>(atlasGenerationProgressLabel); }
            set { SetValue<string>(atlasGenerationProgressLabel, value); }
        }

        FieldInfo padding;

        int m_Padding
        {
            get { return GetValue<int>(padding); }
            set { SetValue<int>(padding, value); }
        }

        FieldInfo packingMode;

        int m_PackingMode
        {
            get { return GetValue<int>(packingMode); }
            set { SetValue<int>(packingMode, value); }
        }

        FieldInfo glyphPackingGenerationTime;

        double m_GlyphPackingGenerationTime
        {
            get { return GetValue<double>(glyphPackingGenerationTime); }
            set { SetValue<double>(glyphPackingGenerationTime, value); }
        }

        FieldInfo isGlyphPackingDone;

        bool m_IsGlyphPackingDone
        {
            get { return GetValue<bool>(isGlyphPackingDone); }
            set { SetValue<bool>(isGlyphPackingDone, value); }
        }

        FieldInfo isRenderingDone;

        bool m_IsRenderingDone
        {
            get { return GetValue<bool>(isRenderingDone); }
            set { SetValue<bool>(isRenderingDone, value); }
        }

        FieldInfo faceInfo;

        FaceInfo m_FaceInfo
        {
            get { return GetValue<FaceInfo>(faceInfo); }
            set { SetValue<FaceInfo>(faceInfo, value); }
        }

        FieldInfo atlasTextureBuffer;

        byte[] m_AtlasTextureBuffer
        {
            get { return GetValue<byte[]>(atlasTextureBuffer); }
            set { SetValue<byte[]>(atlasTextureBuffer, value); }
        }

        FieldInfo glyphRenderingGenerationTime;

        double m_GlyphRenderingGenerationTime
        {
            get { return GetValue<double>(glyphRenderingGenerationTime); }
            set { SetValue<double>(glyphRenderingGenerationTime, value); }
        }

        FieldInfo isGlyphRenderingDone;

        bool m_IsGlyphRenderingDone
        {
            get { return GetValue<bool>(isGlyphRenderingDone); }
            set { SetValue<bool>(isGlyphRenderingDone, value); }
        }

        private FieldInfo referencedFontAsset;

        TMP_FontAsset m_ReferencedFontAsset
        {
            get { return GetValue<TMP_FontAsset>(referencedFontAsset); }
            set { SetValue<TMP_FontAsset>(referencedFontAsset, value); }
        }

        FieldInfo includeFontFeatures;

        bool m_IncludeFontFeatures
        {
            get { return GetValue<bool>(includeFontFeatures); }
            set { SetValue<bool>(includeFontFeatures, value); }
        }

        #endregion

        #region Methods

        MethodInfo parseHexNumberSequence;

        uint[] ParseHexNumberSequence(string charList)
        {
            return (uint[]) parseHexNumberSequence.Invoke(window, new object[] { charList });
        }

        MethodInfo parseNumberSequence;

        uint[] ParseNumberSequence(string charList)
        {
            return (uint[]) parseNumberSequence.Invoke(window, new object[] {charList});
        }

        MethodInfo tryPackGlyphsInAtlas;

        void TryPackGlyphsInAtlas(List<Glyph> m_GlyphsToPack, List<Glyph> m_GlyphsPacked, int m_Padding,
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

        MethodInfo renderGlyphsToTexture;

        void RenderGlyphsToTexture(List<Glyph> m_GlyphsToRender, int m_Padding, GlyphRenderMode m_GlyphRenderMode,
            byte[] m_AtlasTextureBuffer, int m_AtlasWidth, int m_AtlasHeight)
        {
            renderGlyphsToTexture.Invoke(window,
                new object[]
                {
                    m_GlyphsToRender, m_Padding, m_GlyphRenderMode, m_AtlasTextureBuffer, m_AtlasWidth, m_AtlasHeight
                });
        }

        MethodInfo saveCreationSettingsToEditorPrefs;

        void SaveCreationSettingsToEditorPrefs(FontAssetCreationSettings settings)
        {
            saveCreationSettingsToEditorPrefs.Invoke(window, new object[] {settings});
        }

        MethodInfo saveFontCreationSettings;

        FontAssetCreationSettings SaveFontCreationSettings()
        {
            return (FontAssetCreationSettings) saveFontCreationSettings.Invoke(window, new object[0]);
        }

        MethodInfo save_SDF_FontAsset;

        void Save_SDF_FontAsset(string filePath)
        {
            save_SDF_FontAsset.Invoke(window, new object[] {filePath});
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
                if (method.Name != "RenderGlyphsToTexture") continue;
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

        void DestroyImmediate(UnityEngine.Object obj)
        {
            UnityEngine.Object.DestroyImmediate(obj);
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
            UnityEngine.Debug.Log(stringBuilder.ToString());

            if (!m_IsProcessing && m_SourceFontFile != null)
            {
                try
                {
                    DestroyImmediate(m_FontAtlasTexture);
                    DestroyImmediate(m_GlyphRectPreviewTexture);
                }
                catch (Exception e) { }
                m_FontAtlasTexture = null;
                m_SavedFontAtlas = null;
                m_OutputFeedback = string.Empty;

                // Initialize font engine
                FontEngineError errorCode = FontEngine.InitializeFontEngine();
                if (errorCode != FontEngineError.Success)
                {
                    UnityEngine.Debug.Log("Font Asset Creator - Error [" + errorCode +
                                          "] has occurred while Initializing the FreeType Library.");
                }

                // Get file path of the source font file.
                string fontPath = AssetDatabase.GetAssetPath(m_SourceFontFile);

                if (errorCode == FontEngineError.Success)
                {
                    errorCode = FontEngine.LoadFontFace(fontPath);

                    if (errorCode != FontEngineError.Success)
                    {
                        UnityEngine.Debug.Log(
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
                        List<uint> char_List = new List<uint>();

                        for (int i = 0; i < m_CharacterSequence.Length; i++)
                        {
                            uint unicode = m_CharacterSequence[i];

                            // Handle surrogate pairs
                            if (i < m_CharacterSequence.Length - 1 && char.IsHighSurrogate((char) unicode) &&
                                char.IsLowSurrogate(m_CharacterSequence[i + 1]))
                            {
                                unicode = (uint) char.ConvertToUtf32(m_CharacterSequence[i],
                                    m_CharacterSequence[i + 1]);
                                i += 1;
                            }

                            // Check to make sure we don't include duplicates
                            if (char_List.FindIndex(item => item == unicode) == -1)
                                char_List.Add(unicode);
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

                    GlyphLoadFlags glyphLoadFlags =
                        ((GlyphRasterModes) m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_HINTED) ==
                        GlyphRasterModes.RASTER_MODE_HINTED
                            ? GlyphLoadFlags.LOAD_RENDER
                            : GlyphLoadFlags.LOAD_RENDER | GlyphLoadFlags.LOAD_NO_HINTING;

                    glyphLoadFlags = ((GlyphRasterModes) m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_MONO) ==
                                     GlyphRasterModes.RASTER_MODE_MONO
                        ? glyphLoadFlags | GlyphLoadFlags.LOAD_MONOCHROME
                        : glyphLoadFlags;

                    //
                    AutoResetEvent autoEvent = new AutoResetEvent(false);

                    // Worker thread to pack glyphs in the given texture space.
                    ThreadPool.QueueUserWorkItem(PackGlyphs =>
                    {
                        // Start Stop Watch
                        m_StopWatch = System.Diagnostics.Stopwatch.StartNew();

                        // Clear the various lists used in the generation process.
                        m_AvailableGlyphsToAdd.Clear();
                        m_MissingCharacters.Clear();
                        m_ExcludedCharacters.Clear();
                        m_CharacterLookupMap.Clear();
                        m_GlyphLookupMap.Clear();
                        m_GlyphsToPack.Clear();
                        m_GlyphsPacked.Clear();

                        // Check if requested characters are available in the source font file.
                        for (int i = 0; i < characterSet.Length; i++)
                        {
                            uint unicode = characterSet[i];
                            uint glyphIndex;

                            if (FontEngine.TryGetGlyphIndex(unicode, out glyphIndex))
                            {
                                // Skip over potential duplicate characters.
                                if (m_CharacterLookupMap.ContainsKey(unicode))
                                    continue;

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
                                m_GlyphLookupMap.Add(glyphIndex, new List<uint>() {unicode});

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
                            int packingModifier =
                                ((GlyphRasterModes) m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) ==
                                GlyphRasterModes.RASTER_MODE_BITMAP
                                    ? 0
                                    : 1;

                            if (m_PointSizeSamplingMode == 0) // Auto-Sizing Point Size Mode
                            {
                                // Estimate min / max range for auto sizing of point size.
                                int minPointSize = 0;
                                int maxPointSize =
                                    (int) Mathf.Sqrt((m_AtlasWidth * m_AtlasHeight) / m_AvailableGlyphsToAdd.Count) * 3;

                                m_PointSize = (maxPointSize + minPointSize) / 2;

                                bool optimumPointSizeFound = false;
                                for (int iteration = 0; iteration < 15 && optimumPointSizeFound == false; iteration++)
                                {
                                    m_AtlasGenerationProgressLabel = "Packing glyphs - Pass (" + iteration + ")";

                                    FontEngine.SetFaceSize(m_PointSize);

                                    m_GlyphsToPack.Clear();
                                    m_GlyphsPacked.Clear();

                                    m_FreeGlyphRects.Clear();
                                    m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier,
                                        m_AtlasHeight - packingModifier));
                                    m_UsedGlyphRects.Clear();

                                    for (int i = 0; i < m_AvailableGlyphsToAdd.Count; i++)
                                    {
                                        uint glyphIndex = m_AvailableGlyphsToAdd[i];
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
                                        (GlyphPackingMode) m_PackingMode, m_GlyphRenderMode, m_AtlasWidth,
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

                                for (int i = 0; i < m_AvailableGlyphsToAdd.Count; i++)
                                {
                                    uint glyphIndex = m_AvailableGlyphsToAdd[i];
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
                                    (GlyphPackingMode) m_PackingMode, m_GlyphRenderMode, m_AtlasWidth, m_AtlasHeight,
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
                            int packingModifier =
                                ((GlyphRasterModes) m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) ==
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
                        if (m_GlyphRenderMode == GlyphRenderMode.SDF32 && m_PointSize > 512 ||
                            m_GlyphRenderMode == GlyphRenderMode.SDF16 && m_PointSize > 1024 ||
                            m_GlyphRenderMode == GlyphRenderMode.SDF8 && m_PointSize > 2048)
                        {
                            int upSampling = 1;
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

                            UnityEngine.Debug.Log("Glyph rendering has been aborted due to sampling point size of [" +
                                                  m_PointSize + "] x SDF [" + upSampling +
                                                  "] up sampling exceeds 16,384 point size. Please revise your generation settings to make sure the sampling point size x SDF up sampling mode does not exceed 16,384.");

                            m_IsRenderingDone = true;
                            m_AtlasGenerationProgress = 0;
                            m_IsGenerationCancelled = true;
                        }

                        // Add glyphs and characters successfully added to texture to their respective font tables.
                        foreach (Glyph glyph in m_GlyphsPacked.GetList())
                        {
                            uint glyphIndex = glyph.index;

                            m_FontGlyphTable.Add(glyph);

                            // Add glyphs to list of glyphs that need to be rendered.
                            if (glyph.glyphRect.width > 0 && glyph.glyphRect.height > 0)
                                m_GlyphsToRender.Add(glyph);

                            foreach (uint unicode in m_GlyphLookupMap[glyphIndex])
                            {
                                // Create new Character
                                m_FontCharacterTable.Add(new TMP_Character(unicode, glyph));
                            }
                        }

                        //
                        foreach (Glyph glyph in m_GlyphsToPack.GetList())
                        {
                            foreach (uint unicode in m_GlyphLookupMap[glyph.index])
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
                            m_StopWatch = System.Diagnostics.Stopwatch.StartNew();

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

            if (!(((GlyphRasterModes) m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) ==
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
