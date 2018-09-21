using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	[Serializable]
	public class VertexSelection : ISerializationCallbackReceiver
	{
		[SerializeField]
		int[] m_Keys = new int[0];

		HashSet<int> m_Selection = new HashSet<int>();

		HashSet<int> m_TemporalSelection = new HashSet<int>();

		bool m_SelectionInProgress = false;

		HashSet<int> selection {
			get {
				if(m_SelectionInProgress)
				{
					return m_TemporalSelection;
				}

				return m_Selection;
			}
		}

		public void OnBeforeSerialize()
		{
			m_Keys = m_Selection.ToArray();
		}

		public void OnAfterDeserialize()
		{
			m_Selection.Clear();

			m_Selection.UnionWith(m_Keys);
		}
		
		public int Count {
			get {
				return m_Selection.Count;
			}
		}

		public int First()
		{
			return m_Selection.First();
		}

		public void Clear()
		{
			selection.Clear();
		}

		public void BeginSelection()
		{
			m_TemporalSelection.Clear();

			m_SelectionInProgress = true;
		}

		public void EndSelection(bool select)
		{
			m_SelectionInProgress = false;

			if(select)
			{
				m_Selection.UnionWith(m_TemporalSelection);
			}else{
				foreach(int value in m_TemporalSelection)
				{
					if(m_Selection.Contains(value))
					{
						m_Selection.Remove(value);
					}
				}
			}

			m_TemporalSelection.Clear();
		}

		public void Select(int index, bool select)
		{
			if(select)
			{
				selection.Add(index);
			}else if(IsSelected(index))
			{
				selection.Remove(index);
			}
		}

		public bool IsSelected(int index)
		{
			return m_Selection.Contains(index) || m_TemporalSelection.Contains(index);
		}
	}
}
