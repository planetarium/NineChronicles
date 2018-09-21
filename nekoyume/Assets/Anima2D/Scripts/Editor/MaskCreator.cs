using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Anima2D
{
	public class MaskCreator
	{
		[MenuItem("Window/Anima2D/Create Mask", true)]
		static bool CreateMaskValidate()
		{
			return Selection.activeGameObject && Selection.activeGameObject.GetComponent<Animator>();
		}
		
		[MenuItem("Window/Anima2D/Create Mask", false, 20)]
		static void CreateMask()
		{
			Animator animator;

			if(!Selection.activeGameObject)
			{
				return;
			}

			animator = Selection.activeGameObject.GetComponent<Animator>();

			if(!animator)
			{
				return;
			}

			List<Transform> transforms = new List<Transform>();

			AvatarMask avatarMask = new AvatarMask();

			animator.GetComponentsInChildren<Transform>(true,transforms);

			avatarMask.transformCount = transforms.Count;
			
			int index = 0;
			
			foreach(Transform transform in transforms)
			{
				avatarMask.SetTransformPath(index, AnimationUtility.CalculateTransformPath(transform,animator.transform));
				avatarMask.SetTransformActive(index, true);
				index++;
			}
			
			ScriptableObjectUtility.CreateAssetWithSavePanel<AvatarMask>(avatarMask,"Create Mask",animator.name+".mask","mask","Create a new Avatar Mask");
		}
	}
}
