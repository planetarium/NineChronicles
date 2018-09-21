using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Reflection;

namespace Anima2D
{
	public class AnimationWindowImpl_54 : AnimationWindowImpl_51_52_53
	{
		PropertyInfo m_AllCurvesProperty;

		public override void InitializeReflection()
		{
			base.InitializeReflection();

			m_AllCurvesProperty = m_AnimationWindowStateType.GetProperty( "allCurves", BindingFlags.Instance | BindingFlags.Public );
		}

		protected override IList GetAllCurves()
		{
			if(state != null)
			{ 
				return m_AllCurvesProperty.GetValue( state, null ) as IList;
			}
			
			return null;
		}
	}
}
