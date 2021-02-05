namespace RedBlueGames.Tools.TextTyper
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    /// <summary>
    /// Type text component types out Text one character at a time. Heavily adapted from synchrok's GitHub project.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TextTyper : MonoBehaviour
    {
        /// <summary>
        /// The print delay setting. Could make this an option some day, for fast readers.
        /// </summary>
        private const float PrintDelaySetting = 0.02f;

        /// <summary>
        /// Default delay setting will be multiplied by this when the character is a punctuation mark
        /// </summary>
        private const float PunctuationDelayMultiplier = 8f;

        // Characters that are considered punctuation in this language. TextTyper pauses on these characters
        // a bit longer by default. Could be a setting sometime since this doesn't localize.
        private static readonly List<char> punctutationCharacters = new List<char>
        {
            '.',
            ',',
            '!',
            '?'
        };

#pragma warning disable 0649 // Ignore "Field is never assigned to" warning, as it's assigned in inspector
        [SerializeField]
        [Tooltip("The library of ShakePreset animations that can be used by this component.")]
        private ShakeLibrary shakeLibrary;

        [SerializeField]
        [Tooltip("The library of CurvePreset animations that can be used by this component.")]
        private CurveLibrary curveLibrary;

        [SerializeField]
        [Tooltip("If set, the typer will type text even if the game is paused (Time.timeScale = 0)")]
        private bool useUnscaledTime;
#pragma warning restore 0649

        [SerializeField]
        [Tooltip("Event that's called when the text has finished printing.")]
        private UnityEvent printCompleted = new UnityEvent();

        [SerializeField]
        [Tooltip("Event called when a character is printed. Inteded for audio callbacks.")]
        private CharacterPrintedEvent characterPrinted = new CharacterPrintedEvent();

        private TextMeshProUGUI textComponent;
        private float defaultPrintDelay;
        private List<TypableCharacter> charactersToType;
        private List<TextAnimation> animations;
        private Coroutine typeTextCoroutine;

        /// <summary>
        /// Gets the PrintCompleted callback event.
        /// </summary>
        /// <value>The print completed callback event.</value>
        public UnityEvent PrintCompleted
        {
            get
            {
                return this.printCompleted;
            }
        }

        /// <summary>
        /// Gets the CharacterPrinted event, which includes a string for the character that was printed.
        /// </summary>
        /// <value>The character printed event.</value>
        public CharacterPrintedEvent CharacterPrinted
        {
            get
            {
                return this.characterPrinted;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="TextTyper"/> is currently printing text.
        /// </summary>
        /// <value><c>true</c> if printing; otherwise, <c>false</c>.</value>
        public bool IsTyping
        {
            get
            {
                return this.typeTextCoroutine != null;
            }
        }

        private TextMeshProUGUI TextComponent
        {
            get
            {
                if (this.textComponent == null)
                {
                    this.textComponent = this.GetComponent<TextMeshProUGUI>();
                }

                return this.textComponent;
            }
        }

        /// <summary>
        /// Types the text into the Text component character by character, using the specified (optional) print delay per character.
        /// </summary>
        /// <param name="text">Text to type.</param>
        /// <param name="printDelay">Print delay (in seconds) per character.</param>
        public void TypeText(string text, float printDelay = -1)
        {
            this.CleanupCoroutine();

            // Remove all existing TextAnimations
            // TODO - Would be better to pool/reuse these components
            foreach (var anim in GetComponents<TextAnimation>())
            {
                Destroy(anim);
            }

            this.defaultPrintDelay = printDelay > 0 ? printDelay : PrintDelaySetting;
            this.ProcessTags(text);

            // Fix Issue-38 by clearing any old textInfo like sprites, so that SubMesh objects don't reshow their contents.
            var textInfo = this.TextComponent.textInfo;
            textInfo.ClearMeshInfo(false);

            this.typeTextCoroutine = this.StartCoroutine(this.TypeTextCharByChar(text));
        }

        /// <summary>
        /// Skips the typing to the end.
        /// </summary>
        public void Skip()
        {
            this.CleanupCoroutine();

            this.TextComponent.maxVisibleCharacters = int.MaxValue;
            this.UpdateMeshAndAnims();

            this.OnTypewritingComplete();
        }

        /// <summary>
        /// Determines whether this instance is skippable.
        /// </summary>
        /// <returns><c>true</c> if this instance is skippable; otherwise, <c>false</c>.</returns>
        public bool IsSkippable()
        {
            // For now there's no way to configure this. Just make sure it's currently typing.
            return this.IsTyping;
        }

        private void CleanupCoroutine()
        {
            if (this.typeTextCoroutine != null)
            {
                this.StopCoroutine(this.typeTextCoroutine);
                this.typeTextCoroutine = null;
            }
        }

        private IEnumerator TypeTextCharByChar(string text)
        {
            this.TextComponent.text = TextTagParser.RemoveCustomTags(text);
            for (int numPrintedCharacters = 0; numPrintedCharacters < this.charactersToType.Count; ++numPrintedCharacters)
            {
                this.TextComponent.maxVisibleCharacters = numPrintedCharacters + 1;
                this.UpdateMeshAndAnims();

                var printedChar = this.charactersToType[numPrintedCharacters];
                this.OnCharacterPrinted(printedChar.ToString());

                if (this.useUnscaledTime)
                {
                    yield return new WaitForSecondsRealtime(printedChar.Delay);
                }
                else
                {
                    yield return new WaitForSeconds(printedChar.Delay);
                }
            }

            this.typeTextCoroutine = null;
            this.OnTypewritingComplete();
        }

        private void UpdateMeshAndAnims()
        {
            // This must be done here rather than in each TextAnimation's OnTMProChanged
            // b/c we must cache mesh data for all animations before animating any of them

            // Update the text mesh data (which also causes all attached TextAnimations to cache the mesh data)
            this.TextComponent.ForceMeshUpdate();

            // Force animate calls on all TextAnimations because TMPro has reset the mesh to its base state
            // NOTE: This must happen immediately. Cannot wait until end of frame, or the base mesh will be rendered
            for (int i = 0; i < this.animations.Count; i++)
            {
                this.animations[i].AnimateAllChars();
            }
        }

        /// <summary>
        /// Calculates print delays for every visible character in the string.
        /// Processes delay tags, punctuation delays, and default delays
        /// Also processes shake and curve animations and spawns
        /// the appropriate TextAnimation components
        /// </summary>
        /// <param name="text">Full text string with tags</param>
        private void ProcessTags(string text)
        {
            this.charactersToType = new List<TypableCharacter>();
            this.animations = new List<TextAnimation>();
            var textAsSymbolList = TextTagParser.CreateSymbolListFromText(text);

            int printedCharCount = 0;
            int customTagOpenIndex = 0;
            string customTagParam = "";
            float nextDelay = this.defaultPrintDelay;
            foreach (var symbol in textAsSymbolList)
            {
                // Sprite prints a character so we need to throw it out and treat it like a character
                if (symbol.IsTag && !symbol.IsReplacedWithSprite)
                {
                    // TODO - Verification that custom tags are not nested, b/c that will not be handled gracefully
                    if (symbol.Tag.TagType == TextTagParser.CustomTags.Delay)
                    {
                        if (symbol.Tag.IsClosingTag)
                        {
                            nextDelay = this.defaultPrintDelay;
                        }
                        else
                        {
                            nextDelay = symbol.GetFloatParameter(this.defaultPrintDelay);
                        }
                    }
                    else if (symbol.Tag.TagType == TextTagParser.CustomTags.Anim ||
                             symbol.Tag.TagType == TextTagParser.CustomTags.Animation)
                    {
                        if (symbol.Tag.IsClosingTag)
                        {
                            // Add a TextAnimation component to process this animation
                            TextAnimation anim = null;
                            if (this.IsAnimationShake(customTagParam))
                            {
                                anim = gameObject.AddComponent<ShakeAnimation>();
                                ((ShakeAnimation)anim).LoadPreset(this.shakeLibrary, customTagParam);
                            }
                            else if (this.IsAnimationCurve(customTagParam))
                            {
                                anim = gameObject.AddComponent<CurveAnimation>();
                                ((CurveAnimation)anim).LoadPreset(this.curveLibrary, customTagParam);
                            }
                            else
                            {
                                // Could not find animation. Should we error here?
                            }

                            anim.UseUnscaledTime = this.useUnscaledTime;
                            anim.SetCharsToAnimate(customTagOpenIndex, printedCharCount - 1);
                            anim.enabled = true;
                            this.animations.Add(anim);
                        }
                        else
                        {
                            customTagOpenIndex = printedCharCount;
                            customTagParam = symbol.Tag.Parameter;
                        }
                    }
                    else
                    {
                        // Tag type is likely a Unity tag, but it might be something we don't know... could error if unrecognized.
                    }

                }
                else
                {
                    printedCharCount++;

                    TypableCharacter characterToType = new TypableCharacter();
                    if (symbol.IsTag && symbol.IsReplacedWithSprite)
                    {
                        characterToType.IsSprite = true;
                    }
                    else
                    {
                        characterToType.Char = symbol.Character;
                    }

                    characterToType.Delay = nextDelay;
                    if (punctutationCharacters.Contains(symbol.Character))
                    {
                        characterToType.Delay *= PunctuationDelayMultiplier;
                    }

                    this.charactersToType.Add(characterToType);
                }
            }
        }

        private bool IsAnimationShake(string animName)
        {
            return this.shakeLibrary.ContainsKey(animName);
        }

        private bool IsAnimationCurve(string animName)
        {
            return this.curveLibrary.ContainsKey(animName);
        }

        private void OnCharacterPrinted(string printedCharacter)
        {
            if (this.CharacterPrinted != null)
            {
                this.CharacterPrinted.Invoke(printedCharacter);
            }
        }

        private void OnTypewritingComplete()
        {
            if (this.PrintCompleted != null)
            {
                this.PrintCompleted.Invoke();
            }
        }

        /// <summary>
        /// Event that signals a Character has been printed to the Text component.
        /// </summary>
        [System.Serializable]
        public class CharacterPrintedEvent : UnityEvent<string>
        {
        }

        /// <summary>
        /// This class represents a printed character moment, which should correspond with a
        /// delay in the text typer. It became necessary to make this a class when I had
        /// to account for Sprite tags which are replaced by a sprite that counts as a "visble"
        /// character. These sprites would not be in the Text string stripped of tags,
        /// so this allows us to track and print them with a delay.
        /// </summary>
        private class TypableCharacter
        {
            public char Char { get; set; }

            public float Delay { get; set; }

            public bool IsSprite { get; set; }

            public override string ToString()
            {
                return this.IsSprite ? "Sprite" : Char.ToString();
            }
        }
    }
}
