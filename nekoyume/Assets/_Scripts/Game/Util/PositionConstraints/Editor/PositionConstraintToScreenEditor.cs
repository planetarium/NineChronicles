using UnityEditor;
using UnityEngine;

namespace Nekoyume.Game.Util
{
    [CanEditMultipleObjects,
     CustomEditor(typeof(PositionConstraintToScreen))]
    public class PositionConstraintToScreenEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var comp = (PositionConstraintToScreen) target;
            if (GUILayout.Button("Constraint To Screen"))
            {
                comp.Constraint();
            }
        }
    }
}
