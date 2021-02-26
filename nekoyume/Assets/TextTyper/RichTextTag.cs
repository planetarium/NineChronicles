namespace RedBlueGames.Tools.TextTyper
{
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// RichTextTags help parse text that contains HTML style tags, used by Unity's RichText text components.
    /// </summary>
    public class RichTextTag
    {
        public static readonly RichTextTag ClearColorTag = new RichTextTag("<color=#00000000>");

        private const char OpeningNodeDelimeter = '<';
        private const char CloseNodeDelimeter = '>';
        private const char EndTagDelimeter = '/';
        private const char ParameterDelimeter = '=';

        /// <summary>
        /// Initializes a new instance of the <see cref="RichTextTag"/> class.
        /// </summary>
        /// <param name="tagText">Tag text.</param>
        public RichTextTag(string tagText)
        {
            this.TagText = tagText;
        }

        /// <summary>
        /// Gets the full tag text including markers.
        /// </summary>
        /// <value>The tag full text.</value>
        public string TagText { get; private set; }

        /// <summary>
        /// Gets the text for this tag if it's used as a closing tag. Closing tags are unchanged.
        /// </summary>
        /// <value>The closing tag text.</value>
        public string ClosingTagText
        {
            get
            {
                return this.IsClosingTag ? this.TagText : string.Format("</{0}>", this.TagType);
            }
        }

        /// <summary>
        /// Gets the TagType, the body of the tag as a string
        /// </summary>
        /// <value>The type of the tag.</value>
        public string TagType
        {
            get
            {
                // Strip start and end tags
                var tagType = this.TagText.Substring(1, this.TagText.Length - 2);
                tagType = tagType.TrimStart(EndTagDelimeter);

                var tagEndDelimeters = new char[] { ' ', ParameterDelimeter };
                var delimeterIndex = tagType.IndexOfAny(tagEndDelimeters);
                var tagEndIndex = delimeterIndex > 0 ? delimeterIndex : tagType.Length;
                tagType = tagType.Substring(0, tagEndIndex);

                return tagType;
            }
        }

        /// <summary>
        /// Gets the parameter as a string. Ex: For tag Color=#FF00FFFF the parameter would be #FF00FFFF.
        /// </summary>
        /// <value>The parameter.</value>
        public string Parameter
        {
            get
            {
                var parameterDelimeterIndex = this.TagText.IndexOf(ParameterDelimeter);
                if (parameterDelimeterIndex < 0)
                {
                    return string.Empty;
                }

                // Subtract two, one for the delimeter and one for the closing character
                var parameterLength = this.TagText.Length - parameterDelimeterIndex - 2;
                var parameter = this.TagText.Substring(parameterDelimeterIndex + 1, parameterLength);

                // Kill optional enclosing quotes
                if (parameter.Length > 0)
                {
                    if (parameter[0] == '\"' && parameter[parameter.Length - 1] == '\"')
                    {
                        parameter = parameter.Substring(1, parameter.Length - 2);
                    }
                }

                return parameter;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is an opening tag.
        /// </summary>
        /// <value><c>true</c> if this instance is an opening tag; otherwise, <c>false</c>.</value>
        public bool IsOpeningTag
        {
            get
            {
                return !this.IsClosingTag;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a closing tag.
        /// </summary>
        /// <value><c>true</c> if this instance is a closing tag; otherwise, <c>false</c>.</value>
        public bool IsClosingTag
        {
            get
            {
                return this.TagText.Length > 2 && this.TagText[1] == EndTagDelimeter;
            }
        }

        /// <summary>
        /// Gets the length of the tag. Shorcut for the length of the full TagText.
        /// </summary>
        /// <value>The text length.</value>
        public int Length
        {
            get
            {
                return this.TagText.Length;
            }
        }

        /// <summary>
        /// Checks if the specified String starts with a tag.
        /// </summary>
        /// <returns><c>true</c>, if the first character begins a tag <c>false</c> otherwise.</returns>
        /// <param name="text">Text to check for tags.</param>
        public static bool StringStartsWithTag(string text)
        {
            return text.StartsWith(RichTextTag.OpeningNodeDelimeter.ToString());
        }

        /// <summary>
        /// Parses the text for the next RichTextTag.
        /// </summary>
        /// <returns>The next RichTextTag in the sequence. Null if the sequence contains no RichTextTag</returns>
        /// <param name="text">Text to parse.</param>
        public static RichTextTag ParseNext(string text)
        {
            // Trim up to the first delimeter
            var openingDelimeterIndex = text.IndexOf(RichTextTag.OpeningNodeDelimeter);

            // No opening delimeter found. Might want to throw.
            if (openingDelimeterIndex < 0)
            {
                return null;
            }

            var closingDelimeterIndex = text.IndexOf(RichTextTag.CloseNodeDelimeter);

            // No closingDelimeter found. Might want to throw.
            if (closingDelimeterIndex < 0)
            {
                return null;
            }

            var tagText = text.Substring(openingDelimeterIndex, closingDelimeterIndex - openingDelimeterIndex + 1);
            return new RichTextTag(tagText);
        }

        /// <summary>
        /// Removes all copies of the tag of the specified type from the text string.
        /// </summary>
        /// <returns>The text string without any tag of the specified type.</returns>
        /// <param name="text">Text to remove Tags from.</param>
        /// <param name="tagType">Tag type to remove.</param>
        public static string RemoveTagsFromString(string text, string tagType)
        {
            var bodyWithoutTags = text;
            for (int i = 0; i < text.Length; ++i)
            {
                var remainingText = text.Substring(i, text.Length - i);
                if (StringStartsWithTag(remainingText))
                {
                    var parsedTag = ParseNext(remainingText);
                    if (parsedTag.TagType == tagType)
                    {
                        bodyWithoutTags = bodyWithoutTags.Replace(parsedTag.TagText, string.Empty);
                    }

                    i += parsedTag.Length - 1;
                }
            }

            return bodyWithoutTags;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="RichTextTag"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="RichTextTag"/>.</returns>
        public override string ToString()
        {
            return this.TagText;
        }
    }
}