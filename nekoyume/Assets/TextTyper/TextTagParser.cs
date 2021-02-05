namespace RedBlueGames.Tools.TextTyper
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// "Utility class to assist with parsing HTML-style tags in strings
    /// </summary>
    public sealed class TextTagParser
    {
        /// <summary>
        /// Define custom tags here. These should also be added to the CustomTagTypes List below
        /// </summary>
        public struct CustomTags
        {
            public const string Delay = "delay";
            public const string Anim = "anim";
            public const string Animation = "animation";
        }

        private static readonly string[] UnityTags = new string[]
        {
            "b",
            "i",
            "s",
            "u",
            "br",
            "nobr",
            "size",
            "color",
            "style",
            "width",
            "align",
            "alpha",
            "cspace",
            "font",
            "indent",
            "line-height",
            "line-indent",
            "link",
            "lowercase",
            "uppercase",
            "smallcaps",
            "margin",
            "mark",
            "mspace",
            "noparse",
            "page",
            "pos",
            "space",
            "sprite",
            "sup",
            "sub",
            "voffset",
            "gradient"
        };

        private static readonly string[] CustomTagTypes = new string[]
        {
            CustomTags.Delay,
            CustomTags.Anim,
            CustomTags.Animation,
        };

        public static List<TextSymbol> CreateSymbolListFromText(string text)
        {
            var symbolList = new List<TextSymbol>();
            int parsedCharacters = 0;
            while (parsedCharacters < text.Length)
            {
                TextSymbol symbol = null;

                // Check for tags
                var remainingText = text.Substring(parsedCharacters, text.Length - parsedCharacters);
                if (RichTextTag.StringStartsWithTag(remainingText))
                {
                    var tag = RichTextTag.ParseNext(remainingText);
                    symbol = new TextSymbol(tag);
                }
                else
                {
                    symbol = new TextSymbol(remainingText.Substring(0, 1));
                }

                parsedCharacters += symbol.Length;
                symbolList.Add(symbol);
            }

            return symbolList;
        }

        public static string RemoveAllTags(string textWithTags)
        {
            string textWithoutTags = textWithTags;
            textWithoutTags = RemoveUnityTags(textWithoutTags);
            textWithoutTags = RemoveCustomTags(textWithoutTags);

            return textWithoutTags;
        }

        public static string RemoveCustomTags(string textWithTags)
        {
            return RemoveTags(textWithTags, CustomTagTypes);
        }

        public static string RemoveUnityTags(string textWithTags)
        {
            return RemoveTags(textWithTags, UnityTags);
        }

        private static string RemoveTags(string textWithTags, params string[] tags)
        {
            string textWithoutTags = textWithTags;
            foreach (var tag in tags)
            {
                textWithoutTags = RichTextTag.RemoveTagsFromString(textWithoutTags, tag);
            }

            return textWithoutTags;
        }

        public class TextSymbol
        {
            public TextSymbol(string character)
            {
                this.Character = character[0];
            }

            public TextSymbol(RichTextTag tag)
            {
                this.Tag = tag;
            }

            public char Character { get; private set; }

            public RichTextTag Tag { get; private set; }

            public int Length
            {
                get
                {
                    return this.Text.Length;
                }
            }

            public string Text
            {
                get
                {
                    if (this.IsTag)
                    {
                        return this.Tag.TagText;
                    }
                    else
                    {
                        return this.Character.ToString();
                    }
                }
            }

            public bool IsTag
            {
                get
                {
                    return this.Tag != null;
                }
            }

            /// <summary>
            /// Gets a value indicating this Symbol represents a Sprite, which is treated
            /// as a visible character by TextMeshPro.
            /// See Issue #35 for details.
            /// </summary>
            /// <value></value>
            public bool IsReplacedWithSprite
            {
                get
                {
                    return this.IsTag && this.Tag.TagType == "sprite";
                }
            }

            public float GetFloatParameter(float defaultValue = 0f)
            {
                if (!this.IsTag)
                {
                    Debug.LogWarning("Attempted to retrieve parameter from symbol that is not a tag.");
                    return defaultValue;
                }

                float paramValue;
                if (!float.TryParse(this.Tag.Parameter, out paramValue))
                {
                    var warning = string.Format(
                                  "Found Invalid parameter format in tag [{0}]. " +
                                  "Parameter [{1}] does not parse to a float.",
                                  this.Tag,
                                  this.Tag.Parameter);
                    Debug.LogWarning(warning);
                    paramValue = defaultValue;
                }

                return paramValue;
            }
        }
    }
}