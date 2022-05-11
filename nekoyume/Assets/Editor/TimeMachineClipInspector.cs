using Nekoyume;
using UnityEditor;

namespace Editor
{
    [CustomEditor(typeof(TimeMachineClip))]
    public class TimeMachineClipInspector : UnityEditor.Editor
    {
        private SerializedProperty actionProp, conditionProp;

        private void OnEnable()
        {
            actionProp = serializedObject.FindProperty("action");
            conditionProp = serializedObject.FindProperty("condition");
        }

        public override void OnInspectorGUI()
        {
            var isMarker = false;
            EditorGUILayout.PropertyField(actionProp);
            var index = actionProp.enumValueIndex;
            var actionType = (TimeMachineAction)index;
            switch (actionType)
            {
                case TimeMachineAction.Marker:
                    isMarker = true;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("markerLabel"));
                    break;

                case TimeMachineAction.JumpToMarker:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("markerToJumpTo"));
                    break;

                case TimeMachineAction.JumpToTime:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("timeToJumpTo"));
                    break;
            }

            if (!isMarker)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Logic", EditorStyles.boldLabel);


                index = conditionProp.enumValueIndex;
                var conditionType = (TimeMachineCondition)index;

                switch (conditionType)
                {
                    case TimeMachineCondition.Always:
                        EditorGUILayout.HelpBox("The above action will always be executed.",
                            MessageType.Warning);
                        EditorGUILayout.PropertyField(conditionProp);
                        break;

                    case TimeMachineCondition.Never:
                        EditorGUILayout.HelpBox(
                            "The above action will never be executed. Practically, it's as if clip was disabled.",
                            MessageType.Warning);
                        EditorGUILayout.PropertyField(conditionProp);
                        break;

                    case TimeMachineCondition.IsRewind:
                        EditorGUILayout.HelpBox(
                            "The above action will be executed if any Unit in the Platoon connected below is alive when the play-head reaches this clip.",
                            MessageType.Info);
                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(conditionProp);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("conditionChecker"));
                        break;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
