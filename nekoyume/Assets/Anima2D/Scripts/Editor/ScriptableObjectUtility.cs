using UnityEngine;
using UnityEditor;

namespace Anima2D
{
	public static class ScriptableObjectUtility
	{
		public static T CreateAssetWithSavePanel<T>(string title, string defaultName, string extension, string message) where T : ScriptableObject
		{
			string path = EditorUtility.SaveFilePanelInProject(title,defaultName,extension,message);

			T asset = null;

			if(path.Length != 0)
			{
				asset = ScriptableObject.CreateInstance<T> ();
				
				AssetDatabase.CreateAsset(asset,path);
				
				AssetDatabase.Refresh();
			}

			return asset;
		}

		public static T CreateAssetWithSavePanel<T>(T obj, string title, string defaultName, string extension, string message) where T : Object
		{
			string path = EditorUtility.SaveFilePanelInProject(title,defaultName,extension,message);

			T asset = null;

			if(path.Length != 0)
			{
				AssetDatabase.CreateAsset(obj,path);
				
				AssetDatabase.Refresh();

				asset = AssetDatabase.LoadAssetAtPath(path,typeof(T)) as T;
			}
			
			return asset;
		}

		public static T CreateAsset<T>() where T : ScriptableObject
		{
			return CreateAsset<T>("Assets/New " + typeof(T).Name + ".asset");
		}

		public static T CreateAsset<T>(string path) where T : ScriptableObject
		{
			T asset = ScriptableObject.CreateInstance<T> ();
			
			ProjectWindowUtil.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
			
			AssetDatabase.Refresh();
			
			return asset;
		}

	}
}