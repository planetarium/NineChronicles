using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Anima2D
{
	public class SerializedCache : ScriptableObject, ISerializationCallbackReceiver
	{
		public void OnBeforeSerialize() { DoOnBeforeSerialize(); }
		public void OnAfterDeserialize() { DoOnAfterDeserialize(); }

		protected virtual void DoOnBeforeSerialize() {}
		protected virtual void DoOnAfterDeserialize() {}

		public virtual void RegisterUndo(string undoName)
		{
			RegisterObjectUndo(this,undoName);
		}

		public void RegisterCreatedObjectUndo(string undoName)
		{
			if(!string.IsNullOrEmpty(undoName))
			{
				Undo.RegisterCreatedObjectUndo(this,undoName);
			}
		}
			
		static public void RegisterObjectUndo(Object objectToUndo, string undoName)
		{
			if(objectToUndo && !string.IsNullOrEmpty(undoName))
			{
				Undo.RegisterCompleteObjectUndo(objectToUndo,undoName);
			}
		}

		static public void RegisterCreatedObjectUndo(Object objectToUndo, string undoName)
		{
			if(objectToUndo && !string.IsNullOrEmpty(undoName))
			{
				Undo.RegisterCreatedObjectUndo(objectToUndo,undoName);
			}
		}

		static public void DestroyObjectImmediate(Object objectToDestroy)
		{
			if(objectToDestroy)
			{
				if(!string.IsNullOrEmpty(Undo.GetCurrentGroupName()))
				{
					Undo.DestroyObjectImmediate(objectToDestroy);
				}else{
					GameObject.DestroyImmediate(objectToDestroy);
				}
			}
		}
	}
}
