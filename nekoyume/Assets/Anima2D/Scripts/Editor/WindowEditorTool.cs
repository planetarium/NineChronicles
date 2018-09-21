using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Anima2D
{
	public abstract class WindowEditorTool
	{
		public delegate void Callback();
		public delegate bool BoolCallback();

		static int s_WindowID = 0;

		public BoolCallback canShow;
		public Callback onShow;
		public Callback onGUIChanged;
		public Callback onHide;

		public Rect windowRect = new Rect(0f, 0f, 100f, 100f);
		public string header { get { return GetHeader(); } }

		int m_WindowID = -1;
		public int windowID {
			get {
				if(m_WindowID < 0)
				{
					m_WindowID = ++s_WindowID;
				}
				return m_WindowID;
			}
		}

		public bool isShown { get; private set; }

		protected virtual bool CanShow()
		{
			if(canShow != null)
			{
				return canShow();
			}

			return true;
		}

		protected virtual void DoShow()
		{
			if(onShow != null)
			{
				onShow();
			}
		}
		
		protected virtual void DoGUIChanged()
		{
			if(onGUIChanged != null)
			{
				onGUIChanged();
			}
		}
		
		protected virtual void DoHide()
		{
			if(onHide != null)
			{
				onHide();
			}
		}

		public virtual void OnWindowGUI(Rect viewRect)
		{
			if(!isShown && CanShow())
			{
				isShown = true;
				DoShow();
			}
			
			if(isShown && !CanShow())
			{
				isShown = false;
				DoHide();
			}

			if(CanShow())
			{
				windowRect = GUILayout.Window(windowID, windowRect, DoWindow, header);

				DoGUI();

				if(isHovered)
				{
					int controlID = GUIUtility.GetControlID("WindowHovered".GetHashCode(), FocusType.Passive);
					
					if(Event.current.GetTypeForControl(controlID) == EventType.Layout)
					{
						HandleUtility.AddControl(controlID,0f);
					}
				}
			}
		}

		public bool isHovered
		{
			get {
				return isShown && windowRect.Contains(Event.current.mousePosition);
			}
		}

		protected abstract string GetHeader();
		protected abstract void DoWindow(int windowId);
		protected virtual void DoGUI() {}
	}
}
