using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public class OnionSkinWindow : EditorWindow
	{
		const int MaxFrames = 60;

		[SerializeField]
		float m_AlphaMultiplier = 1f;

		[SerializeField]
		int m_Step = 1;

		[SerializeField]
		int m_NumFrames = 15;

		[SerializeField]
		Color m_ColorPrevFrames = Color.red;

		[SerializeField]
		Color m_ColorNextFrames = Color.green;

		[SerializeField]
		bool m_EnableOnionSkin = true;

		[SerializeField]
		GameObject m_InstanceRoot;

		int frameCount
		{
			get {
				if(AnimationWindowExtra.activeAnimationClip)
				{
					return (int)(AnimationWindowExtra.activeAnimationClip.length * AnimationWindowExtra.activeAnimationClip.frameRate);
				}
				
				return 0;
			}
		}
		
		int clampedFrame
		{
			get {
				return Mathf.Clamp(AnimationWindowExtra.frame,0,frameCount);
			}
		}

		bool m_OldInAnimationMode;
		AnimationClip m_OldClip;
		int m_OldFrame;

		OnionLayerManager m_OnionLayerManager = new OnionLayerManager();

		[MenuItem("Window/Anima2D/Onion Skin",false,30)]
		static void ContextInitialize()
		{
			EditorWindow.GetWindow<OnionSkinWindow>("Onion Skin");
		}

		void OnEnable()
		{
			EditorApplication.update += OnUpdate;
			Undo.undoRedoPerformed += UndoRedoPerformed;
		}

		void OnDisable()
		{
			EditorApplication.update -= OnUpdate;
			Undo.undoRedoPerformed -= UndoRedoPerformed;

			DestroyPreview();
		}

		void UndoRedoPerformed()
		{
			UpdatePreview();
			ResamplePreview();
		}

		void OnGUI()
		{
			EditorGUIUtility.labelWidth = 60f;
			EditorGUIUtility.fieldWidth = 22f;

			EditorGUILayout.Space();

			EditorGUI.BeginChangeCheck();

			m_EnableOnionSkin = EditorGUILayout.Toggle("Activate", m_EnableOnionSkin);

			if(EditorGUI.EndChangeCheck())
			{
				if(m_EnableOnionSkin)
				{
					CreatePreview();
					UpdatePreview();
				}else{
					DestroyPreview();
				}

				SceneView.RepaintAll();
			}

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.LabelField("Frames:");
			m_NumFrames = EditorGUILayout.IntSlider(GUIContent.none, m_NumFrames,0,MaxFrames); 

			EditorGUILayout.LabelField("Step:");
			m_Step = EditorGUILayout.IntSlider(GUIContent.none,m_Step,1,MaxFrames);

			EditorGUILayout.LabelField("Alpha:");
			m_AlphaMultiplier = EditorGUILayout.Slider(GUIContent.none,m_AlphaMultiplier,0f,1f);

			EditorGUILayout.LabelField("Previous:");
			m_ColorPrevFrames = EditorGUILayout.ColorField(m_ColorPrevFrames);

			EditorGUILayout.LabelField("Next:");
			m_ColorNextFrames = EditorGUILayout.ColorField(m_ColorNextFrames);

			if(EditorGUI.EndChangeCheck() && m_EnableOnionSkin && m_OldInAnimationMode)
			{
				UpdatePreview();
				SceneView.RepaintAll();
			}
		}

		void OnUpdate()
		{
			if(m_EnableOnionSkin)
			{
				if(AnimationMode.InAnimationMode() != m_OldInAnimationMode)
				{
					if(AnimationMode.InAnimationMode())
					{
						CreatePreview();
						UpdatePreview();
					}else{
						DestroyPreview();
					}
				}

				if(m_OldInAnimationMode)
				{
					if(m_OldClip != AnimationWindowExtra.activeAnimationClip)
					{
						CreatePreview();
						UpdatePreview();
					}

					if(m_OldFrame != clampedFrame)
					{
						UpdatePreview();
					}

					if(AnimationWindowExtra.refresh > 0)
					{
						ResamplePreview();
					}
				}
			}

			m_OldClip = AnimationWindowExtra.activeAnimationClip;
			m_OldFrame = clampedFrame;
			m_OldInAnimationMode = AnimationMode.InAnimationMode();
		}

		void CreatePreview()
		{
			DestroyPreview();

			if(!AnimationMode.InAnimationMode())
			{
				return;
			}

			m_InstanceRoot = EditorExtra.InstantiateForAnimatorPreview(AnimationWindowExtra.rootGameObject) as GameObject;

			EditorExtra.InitInstantiatedPreviewRecursive(m_InstanceRoot);

			List<Ik2D> ik2Ds = new List<Ik2D>();
			m_InstanceRoot.GetComponentsInChildren<Ik2D>(ik2Ds);
			
			IkUtils.UpdateAttachedIKs(ik2Ds);
			
			m_OnionLayerManager.source = m_InstanceRoot;

			m_InstanceRoot.SetActive(false);
		}

		void ResamplePreview()
		{
			if(!m_EnableOnionSkin || !AnimationMode.InAnimationMode()) return;

			m_OnionLayerManager.ResampleOnionLayers(AnimationWindowExtra.activeAnimationClip);
		}

		void UpdatePreview()
		{
			if(!m_EnableOnionSkin || !AnimationMode.InAnimationMode()) return;

			m_OnionLayerManager.UpdateOnionLayers(AnimationWindowExtra.activeAnimationClip,
			                                      clampedFrame,
			                                      m_NumFrames,
			                                      m_Step,
			                                      m_AlphaMultiplier,
			                                      m_ColorPrevFrames,
			                                      m_ColorNextFrames);
		}

		void DestroyPreview()
		{
			m_OnionLayerManager.source = null;

			if(m_InstanceRoot)
			{
				GameObject.DestroyImmediate(m_InstanceRoot);
			}
		}
	}
}
