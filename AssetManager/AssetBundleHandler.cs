using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KG
{
    /// <summary>
    /// アセットバンドルハンドラ
    /// </summary>
    public class AssetBundleHandler
    {
        /// <summary>
        /// ステータス
        /// </summary>
        public enum Status
        {
            None,
            Loading,
            Completed,
        }

        /// <summary>
        /// ロード中 or ロード済みのアセットバンドルリスト
        /// </summary>
        private static List<AssetBundleHandler> handlers = new List<AssetBundleHandler>();

        /// <summary>
        /// アセットバンドル情報
        /// </summary>
        public AssetBundleInfo info { get; private set; }

        /// <summary>
        /// ステータス
        /// </summary>
        public Status status { get; private set; }

        /// <summary>
        /// アセットバンドル
        /// </summary>
        public AssetBundle assetBundle { get; private set; }

        /// <summary>
        /// 依存関係のアセットバンドルハンドラ一覧
        /// </summary>
        private AssetBundleHandler[] dependencies = new AssetBundleHandler[0];

        /// <summary>
        /// 参照しているユーザー
        /// </summary>
        private List<AssetBundleAssetHandler> referenceUsers = new List<AssetBundleAssetHandler>();

        /// <summary>
        /// ロード完了時コールバック
        /// </summary>
        private Action onLoaded = null;

        /// <summary>
        /// construct
        /// </summary>
        private AssetBundleHandler(AssetBundleInfo info)
        {
            handlers.Add(this);
            
            this.info = info;

            if (this.info.dependencies != null)
            {
                this.dependencies = this.info.dependencies
                    .Select(AssetManager.Instance.FindAssetBundleInfo)
                    .Select(GetOrCreate)
                    .ToArray();
            }
        }

        /// <summary>
        /// アセットバンドルハンドラの取得 or 生成
        /// </summary>
        public static AssetBundleHandler GetOrCreate(AssetBundleInfo info)
        {
            var handler = handlers.Find(x => x.info == info);
            if (handler == null)
            {
                handler = new AssetBundleHandler(info);
            }
            return handler;
        }

        /// <summary>
        /// 参照ユーザー登録
        /// </summary>
        public void AddReferenceUser(AssetBundleAssetHandler user)
        {
            if (!this.referenceUsers.Contains(user))
            {
                this.referenceUsers.Add(user);

                //依存関係のアセットバンドルにも参照ユーザー登録
                foreach (var dependency in this.dependencies)
                {
                    dependency.AddReferenceUser(user);
                }
            }
        }

        /// <summary>
        /// 同期ロード
        /// </summary>
        public void Load()
        {
            switch (this.status)
            {
                case Status.None:
                {
                    //依存関係のロード
                    foreach (var dependency in this.dependencies)
                    {
                        dependency.Load();
                    }

					if (this.info.isDLC)
					{
						//DLC領域からアセットバンドル確保
						this.assetBundle = DLCManager.Instance.LoadAssetBundle(this.info);
					}
					else
					{
						string path = AssetManager.GetAssetBundlePath(this.info);

						//アセットバンドル確保
						this.assetBundle = AssetBundle.LoadFromFile(path);
					}

                    //ロード完了
                    this.status = Status.Completed;
                }
                break;

                case Status.Loading:
                {
                    Debug.LogErrorFormat("アセットバンドル:{0}は非同期ロード中のため、同期ロード出来ません。", this.info.assetBundleName);
                }
                break;
            }
        }

		/// <summary>
		/// 指定のアセットバンドルを依存関係に持っているか
		/// </summary>
		private int HasDependency(AssetBundleHandler item, List<AssetBundleHandler> items = null)
		{
			if (items == null)
			{
				items = new List<AssetBundleHandler>();
			}

			if (items.Contains(this))
			{
				return 0;
			}

			items.Add(this);

			foreach (var dependency in this.dependencies)
			{
				if (dependency == item)
				{
					return 1;
				}
			}

			foreach (var dependency in this.dependencies)
			{
				if (dependency.HasDependency(item, items) == 1)
				{
					return 1;
				}
			}

			return -1;
		}

        /// <summary>
        /// 非同期ロード
        /// </summary>
        public void LoadAsync(Action onLoaded)
        {
            switch (this.status)
            {
                case Status.None:
                {
                    //ステータスをロード中に
                    this.status = Status.Loading;

                    //依存関係を先にロード
                    foreach (var dependency in this.dependencies)
                    {
						if (dependency.status == Status.Loading)
						{
							if (dependency.HasDependency(this) == 1)
							{
								//依存関係のロード中の理由が自分自身ならスキップ
								continue;
							}
						}

                        if (dependency.status != Status.Completed)
                        {
                            dependency.LoadAsync(() =>
                            {
                                this.status = Status.None;
                                this.LoadAsync(onLoaded);
                            });

                            return;
                        }
                    }

					this.onLoaded += onLoaded;

					if (this.info.isDLC)
					{
						//DLC領域から自身のロードを開始
						DLCManager.Instance.LoadAsyncAssetBundle(this.info, this.OnLoadedAssetBundle);
					}
					else
					{
						//自身のロードを開始
						string path = AssetManager.GetAssetBundlePath(this.info);
						var request = AssetBundle.LoadFromFileAsync(path);
						request.completed += (_) =>
						{
							this.OnLoadedAssetBundle(request.assetBundle);
						};
					}
                }
                break;

                case Status.Loading:
                {
                    //ロード中なのでコールバック追加
                    this.onLoaded += onLoaded;
                }
                break;

                case Status.Completed:
                {
                    //ロード済みなのでコールバック実行
                    AssetManager.Instance.StartDelayActionCoroutine(null, onLoaded);
                }
                break;
            }
        }

		private void OnLoadedAssetBundle(AssetBundle assetBundle)
		{
			//アセットバンドル確保
			this.assetBundle = assetBundle;

			//ロード完了を通知
			this.status = Status.Completed;
			this.onLoaded?.Invoke();
			this.onLoaded = null;
		}

        /// <summary>
        /// アンロード
        /// </summary>
        public void Unload(AssetBundleAssetHandler user, List<AssetBundleHandler> unloadedHandlers = null)
        {
			if (unloadedHandlers == null)
			{
				unloadedHandlers = new List<AssetBundleHandler>();
			}

			unloadedHandlers.Add(this);

            //参照ユーザーの解除
            this.referenceUsers.Remove(user);

            //参照ユーザーがいなくなった
            if (this.referenceUsers.Count == 0)
            {
                //アセットバンドル破棄
                this.assetBundle?.Unload(true);
                this.assetBundle = null;

				if (this.info.isDLC)
				{
					//アセットバンドルが破棄されたことをDLC側に通知
					DLCManager.Instance.OnUnloadAssetBundle(this.info);
				}

                //自身をリストから除去
                handlers.Remove(this);
            }

            //依存関係のアンロード
            foreach (var dependency in this.dependencies)
            {
				if (!unloadedHandlers.Contains(dependency))
				{
					dependency.Unload(user, unloadedHandlers);
				}
            }
        }
    }
}