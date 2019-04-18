using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D.Pool
{
	public interface ICreationPolicy<T>
	{
		T Create();
		void Destroy(T o);
	}

	public class DefaultCreationPolicy<T> : ICreationPolicy<T> where T : new ()
	{
		public T Create()
		{
			return new T();
		}

		public virtual void Destroy(T o) {}
	}

	public class InstantiateCreationPolicy<T> : ICreationPolicy<T> where T : UnityEngine.Object
	{
		public T original { get { return m_Original; } }

		public InstantiateCreationPolicy(T _original)
		{
			m_Original = _original;
		}
		
		public virtual T Create()
		{
			if(m_Original)
			{
				return GameObject.Instantiate(m_Original) as T;
			}

			return null;
		}

		public void Destroy(T o)
		{
			if(o)
			{
				GameObject.DestroyImmediate(o);
			}

		}

		T m_Original;
	}

	public abstract class ObjectPool< T >
	{
		List<T> m_AvaliableObject;
		List<T> m_DispatchedObjects;

		public List<T> availableObjects { get { return m_AvaliableObject; } }
		public List<T> dispatchedObjects { get { return m_DispatchedObjects; } }

		ICreationPolicy<T> m_CreationPolicy = null;

		protected ObjectPool()
		{
			m_AvaliableObject = new List<T>();
			m_DispatchedObjects = new List<T>();
		}

		public ObjectPool(ICreationPolicy<T> _creationPolicy) : this()
		{
			m_CreationPolicy = _creationPolicy;
		}

		public T Get()
		{
			T l_instance = default(T);
			
			if(availableObjects.Count == 0)
			{
				l_instance = m_CreationPolicy.Create();
			}else{
				l_instance = availableObjects[availableObjects.Count-1];
				availableObjects.Remove(l_instance);
			}
			
			dispatchedObjects.Add(l_instance);
			
			return l_instance;
		}
		
		public void Return(T instance)
		{
			if(instance != null && dispatchedObjects.Contains(instance))
			{
				dispatchedObjects.Remove(instance);
				availableObjects.Add(instance);
			}
		}
		
		public void ReturnAll()
		{
			while(dispatchedObjects.Count > 0)
			{
				Return(dispatchedObjects[dispatchedObjects.Count-1]);
			}
		}
		
		public void Clear()
		{
			ReturnAll();
			
			for (int i = 0; i < availableObjects.Count; i++)
			{
				T l_obj = availableObjects[i];

				if(l_obj != null)
				{
					m_CreationPolicy.Destroy(l_obj);
				}
			}
			
			availableObjects.Clear();
			dispatchedObjects.Clear();
		}
	}

	public class DefaultObjectPool<T> : ObjectPool<T> where T : new ()
	{
		public DefaultObjectPool() : base( new DefaultCreationPolicy<T>() ) {}
	}
}
