using UnityEngine;
using System;

namespace Anima2D
{
#if UNITY_EDITOR
	[ExecuteInEditMode]
	public class EditorUpdaterProxy : MonoBehaviour
	{
		public Action onLateUpdate;
		
		static EditorUpdaterProxy m_Instance;
		public static EditorUpdaterProxy Instance
		{
			get
			{
				if (!m_Instance)
				{
					m_Instance = GameObject.FindObjectOfType<EditorUpdaterProxy>();
					
					if (!m_Instance)
					{
						GameObject l_instanceGO = new GameObject("EditorUpdaterProxy");
						
						m_Instance = l_instanceGO.AddComponent<EditorUpdaterProxy>();
						
						l_instanceGO.hideFlags = HideFlags.HideAndDontSave;
						m_Instance.hideFlags = HideFlags.HideAndDontSave;
					}
				}
				
				return m_Instance;
			}
		}
		
		public static bool isActive { get { return m_Instance != null; } }
		
		void Awake()
		{
			if (Instance == this)
			{
				
			} else {
				Destroy(gameObject);
			}
		}
		
		void LateUpdate()
		{
			if(onLateUpdate != null)
			{
				onLateUpdate.Invoke();
			}
		}
	}
#endif
}