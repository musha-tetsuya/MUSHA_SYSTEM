using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KG
{
	/// <summary>
	/// オブジェクトプール管理
	/// </summary>
	public class ObjectPoolManager : SingletonMonoBehaviour<ObjectPoolManager>
	{
		/// <summary>
		/// オブジェクトプール
		/// </summary>
		[Serializable]
		private class ObjectPool
		{
			/// <summary>
			/// パス
			/// </summary>
			public string path = null;

			/// <summary>
			/// プレハブ
			/// </summary>
			public GameObject prefab = null;

			/// <summary>
			/// オブジェクトリスト
			/// </summary>
			public List<GameObject> objs = new List<GameObject>();
		}

		/// <summary>
		/// プールリスト
		/// </summary>
		private List<ObjectPool> pools = new List<ObjectPool>();

		/// <summary>
		/// プール検索
		/// </summary>
		private ObjectPool FindPool(string path)
		{
			return this.pools.Find(x => x.path.Equals(path, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// オブジェクト生成依頼
		/// </summary>
		public GameObject CreateObject(string path)
		{
			//プールを検索
			var pool = this.FindPool(path);
			if (pool == null)
			{
				//無ければ作成
				pool = new ObjectPool();
				pool.path = path;
				this.pools.Add(pool);
			}

			//どっかで破棄されてしまったオブジェクトは管理から外す
			pool.objs.RemoveAll(x => x == null);

			//プールから空いているオブジェクトを検索
			var freeObj = pool.objs.Find(x => !x.activeSelf && x.transform.parent == this.transform);
			if (freeObj == null)
			{
				if (pool.prefab == null)
				{
					//ロード済みのはずのハンドルを検索
					var handle = AssetManager.Instance.FindAssetHandler(path);
					if (handle == null)
					{
						Debug.LogError($"{path}はロードされていません。");
						return null;
					}

					//空いているオブジェクトが無ければ作成
					if (handle.asset is MonoBehaviour)
					{
						pool.prefab = (handle.asset as MonoBehaviour).gameObject;
					}
					else
					{
						pool.prefab = handle.asset as GameObject;
					}
				}

				freeObj = Instantiate(pool.prefab, this.transform, false);

				pool.objs.Add(freeObj);
			}

			//プールから出し、アクティブ化して返す
			freeObj.transform.SetParent(null);
			freeObj.SetActive(true);

			return freeObj;
		}

		/// <summary>
		/// オブジェクト生成依頼
		/// </summary>
		public GameObject CreateObject(GameObject prefab)
		{
			var path = GetPrefabPath(prefab);

			//プールを検索
			var pool = this.FindPool(path);
			if (pool == null)
			{
				//無ければ作成
				pool = new ObjectPool();
				pool.path = path;
				pool.prefab = prefab;
				this.pools.Add(pool);
			}

			//どっかで破棄されてしまったオブジェクトは管理から外す
			pool.objs.RemoveAll(x => x == null);

			//プールから空いているオブジェクトを検索
			var freeObj = pool.objs.Find(x => !x.activeSelf && x.transform.parent == this.transform);
			if (freeObj == null)
			{
				freeObj = Instantiate(pool.prefab, this.transform, false);

				pool.objs.Add(freeObj);
			}

			//プールから出し、アクティブ化して返す
			freeObj.transform.SetParent(null);
			freeObj.SetActive(true);

			return freeObj;
		}

		/// <summary>
		/// プレハブパスの取得
		/// </summary>
		public static string GetPrefabPath(GameObject prefab)
		{
			return prefab.GetInstanceID().ToString();
		}

		/// <summary>
		/// プレハブ登録
		/// </summary>
		public string RegisterObject(GameObject prefab, int objNum = 0)
		{
			string path = GetPrefabPath(prefab);

			//プールを検索
			var pool = this.FindPool(path);
			if (pool == null)
			{
				//無ければ作成
				pool = new ObjectPool();
				pool.path = path;
				pool.prefab = prefab;
				this.pools.Add(pool);

				// あらかじめ指定数をインスタンス化
				for (int i = 0, imax = objNum; i < imax; i++)
				{
					var obj = Instantiate(pool.prefab, this.transform, false);
					obj.SetActive(false);
					pool.objs.Add(obj);
				}
			}

			return path;
		}

		/// <summary>
		/// オブジェクト破棄依頼
		/// </summary>
		public void DestroyObject(GameObject obj)
		{
			if (this.pools.Exists(x => x.objs.Contains(obj)))
			{
				//管理しているオブジェクトならプールに入れて、非アクティブ化
				obj.transform.SetParent(this.transform);
				obj.SetActive(false);
			}
			else
			{
				//管理していないオブジェクトはガチ破棄
				Destroy(obj);
			}
		}

		/// <summary>
		/// プール破棄
		/// </summary>
		public void DestroyPool(string path)
		{
			//プール検索
			var pool = this.FindPool(path);
			if (pool != null)
			{
				//プールリストから除去
				this.pools.Remove(pool);

				//プール内オブジェクト全破棄
				for (int i = 0, imax = pool.objs.Count; i < imax; i++)
				{
					Destroy(pool.objs[i]);
				}
			}
		}

		/// <summary>
		/// プール破棄
		/// </summary>
		public void DestroyPool(GameObject prefab)
		{
			this.DestroyPool(GetPrefabPath(prefab));
		}
	}
}