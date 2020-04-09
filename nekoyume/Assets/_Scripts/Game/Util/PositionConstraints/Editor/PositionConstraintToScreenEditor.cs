using UnityEditor;
using UnityEngine;

namespace Nekoyume.Game.Util
{
    [CustomEditor(typeof(PositionConstraintToScreen))]
    public class PositionConstraintToScreenEditor : Editor
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
