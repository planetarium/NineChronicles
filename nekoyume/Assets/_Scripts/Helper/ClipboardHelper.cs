using UnityEngine;

namespace Nekoyume.Helper
{
    public class ClipboardHelper
    {
        private static TextEditor TextEditor { get; }

        static ClipboardHelper()
        {
            TextEditor = new TextEditor();
        }

        public static void CopyToClipboard(string value)
        {
            TextEditor.text = value;
            TextEditor.SelectAll();
            TextEditor.Copy();   
        }
    }
}
