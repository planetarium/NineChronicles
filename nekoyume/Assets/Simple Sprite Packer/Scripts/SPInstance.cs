using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SimpleSpritePacker
{
	public class SPInstance : ScriptableObject {
		
		public enum PackingMethod
		{
			MaxRects,
			Unity
		}
		
		[SerializeField] private Texture2D m_Texture;
		[SerializeField] private int m_Padding = 1;
		[SerializeField] private int m_MaxSize = 4096;
		[SerializeField] private PackingMethod m_PackingMethod = PackingMethod.MaxRects;
		[SerializeField] private SpriteAlignment m_DefaultPivot = SpriteAlignment.Center;
		[SerializeField] private Vector2 m_DefaultCustomPivot = new Vector2(0.5f, 0.5f);
		
		[SerializeField] private List<SPSpriteInfo> m_Sprites = new List<SPSpriteInfo>();
		[SerializeField] private List<SPAction> m_PendingActions = new List<SPAction>();
		
		/// <summary>
		/// Gets or sets the atlas texture.
		/// </summary>
		/// <value>The texture.</value>
		public Texture2D texture
		{
			get { return this.m_Texture; }
			set
			{
				this.m_Texture = value;
				#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
				#endif
			}
		}
		
		/// <summary>
		/// Gets or sets the packing padding.
		/// </summary>
		/// <value>The padding.</value>
		public int padding
		{
			get { return this.m_Padding; }
			set
			{
				this.m_Padding = value;
				#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
				#endif
			}
		}
		
		/// <summary>
		/// Gets or sets the max packing size.
		/// </summary>
		/// <value>The size of the max.</value>
		public int maxSize
		{
			get { return this.m_MaxSize; }
			set
			{
				this.m_MaxSize = value;
				#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
				#endif
			}
		}
		
		/// <summary>
		/// Gets or sets the packing method.
		/// </summary>
		/// <value>The packing method.</value>
		public PackingMethod packingMethod
		{
			get { return this.m_PackingMethod; }
			set
			{
				this.m_PackingMethod = value;
				#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
				#endif
			}
		}
		
		/// <summary>
		/// Gets or sets the default pivot.
		/// </summary>
		/// <value>The default pivot.</value>
		public SpriteAlignment defaultPivot
		{
			get { return this.m_DefaultPivot; }
			set 
			{
				this.m_DefaultPivot = value;
				#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
				#endif
			}
		}
		
		/// <summary>
		/// Gets or sets the default custom pivot.
		/// </summary>
		/// <value>The default custom pivot.</value>
		public Vector2 defaultCustomPivot
		{
			get { return this.m_DefaultCustomPivot; }
			set
			{
				this.m_DefaultCustomPivot = value;
				#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
				#endif
			}
		}
		
		/// <summary>
		/// Gets the list of sprites.
		/// </summary>
		/// <value>The sprites.</value>
		public List<SPSpriteInfo> sprites
		{
			get { return this.m_Sprites; }
		}
		
		/// <summary>
		/// Gets a copy of the list of sprites.
		/// </summary>
		/// <value>The copy of sprites.</value>
		public List<SPSpriteInfo> copyOfSprites
		{
			get
			{
				List<SPSpriteInfo> list = new List<SPSpriteInfo>();
				foreach (SPSpriteInfo i in this.m_Sprites)
					list.Add(i);
				return list;
			}
		}
		
		/// <summary>
		/// Gets the list of pending actions.
		/// </summary>
		/// <value>The pending actions.</value>
		public List<SPAction> pendingActions
		{
			get { return this.m_PendingActions; }
		}
		
		/// <summary>
		/// Changes the sprite source.
		/// </summary>
		/// <param name="spriteInfo">Sprite info.</param>
		/// <param name="newSource">New source.</param>
		public void ChangeSpriteSource(SPSpriteInfo spriteInfo, Object newSource)
		{
			// Validate the new source
			if (newSource == null)
			{
				spriteInfo.source = null;
			}
			else if (newSource is Texture2D || newSource is Sprite)
			{
				spriteInfo.source = newSource;
			}
			
			#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
			#endif
		}
		
		/// <summary>
		/// Queues add sprite action.
		/// </summary>
		/// <param name="resource">Resource.</param>
		public void QueueAction_AddSprite(Object resource)
		{
			if (resource is Texture2D || resource is Sprite)
			{
				// Check if that sprite is already added to the queue
				if (this.m_PendingActions.Find(a => (a.actionType == SPAction.ActionType.Sprite_Add && a.resource == resource)) != null)
					return;
				
				SPAction action = new SPAction();
				action.actionType = SPAction.ActionType.Sprite_Add;
				action.resource = resource;
				this.m_PendingActions.Add(action);
			}
		}
		
		/// <summary>
		/// Queues add sprites action.
		/// </summary>
		/// <param name="resources">Resources.</param>
		public void QueueAction_AddSprites(Object[] resources)
		{
			foreach (Object resource in resources)
			{
				this.QueueAction_AddSprite(resource);
			}
		}
		
		/// <summary>
		/// Queues remove sprite action.
		/// </summary>
		/// <param name="spriteInfo">Sprite info.</param>
		public void QueueAction_RemoveSprite(SPSpriteInfo spriteInfo)
		{
			if (spriteInfo == null)
				return;
			
			if (!this.m_Sprites.Contains(spriteInfo))
				return;
			
			// Check if that sprite is already added to the queue
			if (this.m_PendingActions.Find(a => (a.actionType == SPAction.ActionType.Sprite_Remove && a.spriteInfo == spriteInfo)) != null)
				return;
			
			SPAction action = new SPAction();
			action.actionType = SPAction.ActionType.Sprite_Remove;
			action.spriteInfo = spriteInfo;
			this.m_PendingActions.Add(action);
		}
		
		/// <summary>
		/// Unqueues action.
		/// </summary>
		/// <param name="action">Action.</param>
		public void UnqueueAction(SPAction action)
		{
			if (this.m_PendingActions.Contains(action))
				this.m_PendingActions.Remove(action);
		}
		
		/// <summary>
		/// Gets the a list of add sprite actions.
		/// </summary>
		/// <returns>The add sprite actions.</returns>
		protected List<SPAction> GetAddSpriteActions()
		{
			List<SPAction> actions = new List<SPAction>();
			
			foreach (SPAction action in this.m_PendingActions)
			{
				if (action.actionType == SPAction.ActionType.Sprite_Add)
				{
					actions.Add(action);
				}
			}
			
			return actions;
		}
		
		/// <summary>
		/// Gets a list of remove sprite actions.
		/// </summary>
		/// <returns>The remove sprite actions.</returns>
		protected List<SPAction> GetRemoveSpriteActions()
		{
			List<SPAction> actions = new List<SPAction>();
			
			foreach (SPAction action in this.m_PendingActions)
			{
				if (action.actionType == SPAction.ActionType.Sprite_Remove)
				{
					actions.Add(action);
				}
			}
			
			return actions;
		}
		
		/// <summary>
		/// Clears the current sprites collection data.
		/// </summary>
		public void ClearSprites()
		{
			this.m_Sprites.Clear();
		}
		
		/// <summary>
		/// Adds sprite to the sprite collection.
		/// </summary>
		/// <param name="spriteInfo">Sprite info.</param>
		public void AddSprite(SPSpriteInfo spriteInfo)
		{
			if (spriteInfo != null)
				this.m_Sprites.Add(spriteInfo);
		}
		
		/// <summary>
		/// Clears the current actions.
		/// </summary>
		public void ClearActions()
		{
			this.m_PendingActions.Clear();
		}
		
		/// <summary>
		/// Gets a sprite list with applied actions.
		/// </summary>
		/// <returns>The sprite list with applied actions.</returns>
		public List<SPSpriteInfo> GetSpriteListWithAppliedActions()
		{
			// Create temporary sprite info list
			List<SPSpriteInfo> spriteInfoList = new List<SPSpriteInfo>();
			
			// Add the current sprites
			foreach (SPSpriteInfo si in this.m_Sprites)
				spriteInfoList.Add(si);
				
			// Apply the remove actions
			foreach (SPAction ra in this.GetRemoveSpriteActions())
			{
				if (spriteInfoList.Contains(ra.spriteInfo))
					spriteInfoList.Remove(ra.spriteInfo);
			}
			
			// Apply the add actions
			foreach (SPAction asa in this.GetAddSpriteActions())
			{
				SPSpriteInfo si = new SPSpriteInfo();
				si.source = asa.resource;
				spriteInfoList.Add(si);
			}
			
			// return the list
			return spriteInfoList;
		}
	}
}