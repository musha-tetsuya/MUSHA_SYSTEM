using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KG
{
	/// <summary>
	/// AssetLoaderインターフェース
	/// </summary>
    public interface IAssetLoader
    {
		/// <summary>
		/// パス
		/// </summary>
        string path { get; }

		/// <summary>
		/// アセットハンドル
		/// </summary>
        AssetHandler handler { get; }

		/// <summary>
		/// ロード完了時コールバック
		/// </summary>
		Action<IAssetLoader> onLoaded { get; set; }

		/// <summary>
		/// 非同期ロード
		/// </summary>
        AssetHandler LoadAsync(int threadId = 0);

		/// <summary>
		/// アンロード
		/// </summary>
        void Unload();
    }

	/// <summary>
	/// アセットローダー
	/// </summary>
    public class AssetLoader<T> : IAssetLoader where T : UnityEngine.Object
    {
		/// <summary>
		/// パス
		/// </summary>
        public string path { get; private set; }

		/// <summary>
		/// アセットハンドル
		/// </summary>
        public AssetHandler handler { get; private set; }

		/// <summary>
		/// ロード完了時コールバック
		/// </summary>
		public Action<IAssetLoader> onLoaded { get; set; }

		/// <summary>
		/// construct
		/// </summary>
        public AssetLoader(string path, Action<IAssetLoader> onLoaded = null)
        {
            this.path = path;
			this.onLoaded = onLoaded;
        }

		/// <summary>
		/// 非同期ロード
		/// </summary>
        public AssetHandler LoadAsync(int threadId = 0)
        {
            return this.handler = AssetManager.Instance.LoadAsync<T>(
				path: this.path,
				threadId: threadId,
				onLoaded: (_) =>
				{
					this.onLoaded?.Invoke(this);
				});
        }

		/// <summary>
		/// アンロード
		/// </summary>
        public void Unload()
        {
            AssetManager.Instance?.Unload(this.handler);
        }
    }

	/// <summary>
	/// シーンアセットローダー
	/// </summary>
	public class SceneAssetLoader : IAssetLoader
	{
		/// <summary>
		/// パス
		/// </summary>
		public string path { get; private set; }

		/// <summary>
		/// アセットハンドル
		/// </summary>
		public AssetHandler handler { get; private set; }

		/// <summary>
		/// ロード完了時コールバック
		/// </summary>
		public Action<IAssetLoader> onLoaded { get; set; }

		/// <summary>
		/// construct
		/// </summary>
		public SceneAssetLoader(string path, Action<IAssetLoader> onLoaded = null)
		{
			this.path = path;
			this.onLoaded = onLoaded;
		}

		/// <summary>
		/// 非同期ロード
		/// </summary>
		public AssetHandler LoadAsync(int threadId = 0)
		{
			return this.handler = AssetManager.Instance.LoadSceneAssetAsync(
				path: this.path,
				threadId: threadId,
				onLoaded: () =>
				{
					this.onLoaded?.Invoke(this);
				});
		}

		/// <summary>
		/// アンロード
		/// </summary>
		public void Unload()
		{
			AssetManager.Instance?.Unload(this.handler);
		}
	}

	/// <summary>
	/// バンドルローダー
	/// </summary>
	public class BundleLoader : IAssetLoader
	{
		/// <summary>
		/// パス
		/// </summary>
		public string path { get; private set; }

		/// <summary>
		/// ハンドラ
		/// </summary>
		public AssetHandler handler { get; private set; }

		/// <summary>
		/// ロード完了時コールバック
		/// </summary>
		public Action<IAssetLoader> onLoaded { get; set; }

		/// <summary>
		/// construct
		/// </summary>
		public BundleLoader(string path, Action<IAssetLoader> onLoaded = null)
		{
			this.path = path;
			this.onLoaded = onLoaded;
		}

		/// <summary>
		/// 非同期ロード
		/// </summary>
		public AssetHandler LoadAsync(int threadId = 0)
		{
			this.handler = AssetManager.Instance.LoadBundleAsync(
				path: this.path,
				threadId: threadId,
				onLoaded: () =>
				{
					this.onLoaded?.Invoke(this);
				});
			this.handler.isDontDestroy = true;
			return this.handler;
		}

		/// <summary>
		/// アンロード
		/// </summary>
		public void Unload()
		{
			AssetManager.Instance?.Unload(this.handler);
		}
	}

	/// <summary>
	/// アセット一括ローダー
	/// </summary>
    public class AssetListLoader : List<IAssetLoader>
    {
		/// <summary>
		/// ロード完了済みかどうか
		/// </summary>
        public bool isLoaded { get; private set; }

		/// <summary>
		/// スレッドID
		/// </summary>
		private int threadId = 0;

		/// <summary>
		/// 全ロード完了時コールバック
		/// </summary>
		private Action onAllLoaded = null;

		/// <summary>
		/// construct
		/// </summary>
        public AssetListLoader() : base() {}

		/// <summary>
		/// construct
		/// </summary>
        public AssetListLoader(IEnumerable<IAssetLoader> collection) : base(collection) {}

		/// <summary>
		/// 非同期ロード
		/// </summary>
        public void LoadAsync(Action onAllLoaded = null, int threadId = 0)
        {
            this.isLoaded = false;
			this.threadId = threadId;
			this.onAllLoaded = onAllLoaded;

            if (this.Count == 0)
            {
				//何も積まれてないなら1フレ後にロード完了通知
                AssetManager.Instance.StartDelayActionCoroutine(null, () =>
				{
					this.isLoaded = true;
					this.onAllLoaded?.Invoke();
				});
                return;
            }

			//スレッドの最大処理数分をロード開始させる
			for (int i = 0; i < AssetManager.Instance.maxThreadCounts[this.threadId]; i++)
			{
				var item = this.Find(_ => _.handler == null);
				if (item != null)
				{
					item.onLoaded += (_) => this.OnLoaded();
					item.LoadAsync(this.threadId);
				}
				else
				{
					break;
				}
			}
        }

		/// <summary>
		/// ロード完了時
		/// </summary>
		private void OnLoaded()
		{
			if (this.isLoaded)
			{
				//全てのロードが完了してるのでreturn
				return;
			}

			//未処理タスクの検索
			var item = this.Find(_ => _.handler == null);
			if (item != null)
			{
				//未処理タスクのロードを開始
				item.onLoaded += (_) => this.OnLoaded();
				item.LoadAsync(this.threadId);
				return;
			}

			if (!this.Exists(_ => _.handler.status != AssetHandler.Status.Completed))
			{
				//全てのロードが完了
				this.isLoaded = true;
				this.onAllLoaded?.Invoke();
			}
		}

		/// <summary>
		/// アンロード
		/// </summary>
        public void Unload()
        {
            for (int i = 0, imax = this.Count; i < imax; i++)
            {
                this[i].Unload();
            }

            this.Clear();
        }
    }
}