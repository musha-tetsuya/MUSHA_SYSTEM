using UnityEngine;
using System.Collections;

/// <summary>
/// シングルトン
/// </summary>
public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
	/// <summary>
	/// インスタンス
	/// </summary>
	protected static T instance { get; private set; }

	/// <summary>
	/// インスタンス
	/// </summary>
	public static T Instance => instance ?? (instance = FindObjectOfType<T>());

	/// <summary>
	/// Awake
	/// </summary>
	protected virtual void Awake()
	{
		if (instance == null)
		{
			instance = this as T;
		}
		else if (instance != this)
		{
			Debug.LogError($"instance {typeof(T)} is exsits.");
			Destroy(this);
		}
	}

	/// <summary>
	/// OnDestroy
	/// </summary>
	protected virtual void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
	}
}
