using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
	public class Background : MonoBehaviour
	{
		private void Start ()
		{
			
		}

		public void Clear()
		{
			// while (transform.childrenCount)
			// {
			// 	transform.removeChild(0);
			// }
		}

		public void Load(string filename, float width = 1136)
		{
			Clear();

			Sprite sprite = Resources.Load<Sprite>(
				string.Format("images/background_{0}", filename));
			if (sprite != null)
			{
				//int num = Mathf.Floor(width / sprite.width);
				//GameObject bg = new GameObject("bg", transform);
				//bg.transform.position = new Vector3();
			}
		}
	}
}
