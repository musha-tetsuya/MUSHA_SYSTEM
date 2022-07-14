using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KG
{
    /// <summary>
    /// アセットマネージャー
    /// </summary>
    public class AssetManager : SingletonMonoBehaviour<AssetManager>
    {
        /// <summary>
        /// アセットバンドル情報リストJsonファイル名
        /// </summary>
        public const string ASSETBUNDLE_INFOLIST_NAME = "assetBundleInfoList.json";
#if UNITY_EDITOR
        /// <summary>
        /// Editor用アセットバンドルディレクトリ　
        /// </summary>
        public static string editorAssetBundleDirectory => $"AssetBundles/Cache/{EditorUserBuildSettings.activeBuildTarget}";
#endif
        /// <summary>
        /// StreamingAssetsアセットバンドルディレクトリ
        /// </summary>
        public static string streamingAssetBundleDirectory => $"{Application.streamingAssetsPath}/AssetBundles~";

        /// <summary>
        /// PersistentDataアセットバンドルディレクトリ
        /// </summary>
        public static string persistentAssetBundleDirectory => $"{Application.persistentDataPath}/AssetBundles";

        /// <summary>
        /// 最大スレッド数
        /// </summary>
        [SerializeField]
		public int[] maxThreadCounts = { 5, 1 };

        /// <summary>
        /// アセットバンドル情報リスト
        /// </summary>
        public List<AssetBundleInfo> infoList = new List<AssetBundleInfo>();

        /// <summary>
        /// ロード中 or ロード済みのアセットリスト
        /// </summary>
        public List<AssetHandler> handlers = new List<AssetHandler>();

        /// <summary>
        /// 積まれているコールバック一覧
        /// </summary>
        private List<(AssetHandler handler, Action callback)> callbacks = new List<(AssetHandler, Action)>();

        /// <summary>
        /// アセットバンドルパス取得
        /// </summary>
        private static string GetAssetBundlePath(string fileName)
        {
#if UNITY_EDITOR
            return $"{editorAssetBundleDirectory}/{fileName}";
#elif UNITY_STANDALONE
            return File.Exists($"{streamingAssetBundleDirectory}/{fileName}")
                ? $"{streamingAssetBundleDirectory}/{fileName}"
                : $"{persistentAssetBundleDirectory}/{fileName}";
#elif UNITY_PS4 || UNITY_PS5
            return $"{streamingAssetBundleDirectory}/{fileName}";
#else
            Debug.LogError("未対応");
            return null;
#endif
        }

        /// <summary>
        /// アセットバンドルパス取得
        /// </summary>
        public static string GetAssetBundlePath(AssetBundleInfo info)
        {
#if UNITY_EDITOR
            return GetAssetBundlePath(info.assetBundleName);
#else
            return GetAssetBundlePath(info.hashName);
#endif
        }

        /// <summary>
        /// アセットバンドル情報リストJsonパス取得
        /// </summary>
        public static string GetInfoListPath()
        {
#if UNITY_EDITOR
            return GetAssetBundlePath(ASSETBUNDLE_INFOLIST_NAME);
#else
            return GetAssetBundlePath(EncryptUtility.ToHashString(ASSETBUNDLE_INFOLIST_NAME));
#endif
        }

        /// <summary>
        /// アセットバンドル情報の検索
        /// </summary>
        public AssetBundleInfo FindAssetBundleInfo(string path)
        {
            return this.infoList.Find(path);
        }

        /// <summary>
        /// ロード済み or ロード中のアセットハンドラを検索
        /// </summary>
        public AssetHandler FindAssetHandler(string path, Type type = null, bool isBundle = false)
        {
            //バックスラッシュは通常のスラッシュに直す
            path = path.Replace('\\', '/');

			if (isBundle)
			{
				return this.handlers.Find(handler =>
				{
					return handler is AssetBundleAssetHandler
						&& handler.type == null
						&& path.Equals(handler.path, StringComparison.OrdinalIgnoreCase);
				});
			}

            return this.handlers.Find(handler =>
            {
                if (path.Equals(handler.path, StringComparison.OrdinalIgnoreCase))
                {
                    return (type == null)
                        || (type == handler.type)
                        || (handler.type != null && handler.type.IsSubclassOf(type));
                }
                return false;
            });
        }

        /// <summary>
        /// 同期ロード
        /// </summary>
        public AssetHandler Load<T>(string path) where T : UnityEngine.Object
        {
            //既に読み込みがかかっているか検索
            var handler = this.FindAssetHandler(path, typeof(T));

            //ハンドルが既存
            if (handler != null)
            {
                //参照カウンタ増加
                handler.referenceCount++;
            }
            else
            {
                //アセットバンドルかどうかの情報を検索
                var info = this.FindAssetBundleInfo(path);
                if (info == null)
                {
                    //Resources用アセットハンドラ生成
                    handler = new ResourcesAssetHandler(path, typeof(T));
                }
                else
                {
                    //AssetBundle用アセットハンドラ生成
                    handler = new AssetBundleAssetHandler(path, typeof(T), info);
                }

                //リストにアセットハンドラを保持
                this.handlers.Add(handler);
            }

            //ロード
            handler.Load();

            return handler;
        }

        /// <summary>
        /// 非同期ロード
        /// </summary>
        public AssetHandler LoadAsync<T>(string path, Action<T> onLoaded = null, int threadId = 0) where T : UnityEngine.Object
        {
            //既に読み込みがかかっているか検索
            var handler = this.FindAssetHandler(path, typeof(T));

            //ハンドルが既存
            if (handler != null)
            {
                //参照カウンタ増加
                handler.referenceCount++;

                //コールバックを積む
                this.callbacks.Add((handler, () => onLoaded?.Invoke(handler.asset as T)));

                //ハンドルがロード済みなら積んであるコールバックの消化を試みる（1フレ後に）
                if (handler.status == AssetHandler.Status.Completed)
                {
                    this.StartDelayActionCoroutine(null, this.OnLoadedAssetHandler);
                }
            }
            //ハンドルが存在しなかったら
            else
            {
                //アセットバンドルかどうかの情報を検索
                var info = this.FindAssetBundleInfo(path);
                if (info == null)
                {
                    //Resources用アセットハンドラ生成
                    handler = new ResourcesAssetHandler(path, typeof(T));
                }
                else
                {
                    //AssetBundle用アセットハンドラ生成
                    handler = new AssetBundleAssetHandler(path, typeof(T), info);
                }

				//どのスレッドを使用するかセット
				handler.threadId = threadId;

                //リストにアセットハンドラを保持
                this.handlers.Add(handler);

                //コールバックを積む
                this.callbacks.Add((handler, () => onLoaded?.Invoke(handler.asset as T)));

                //ロード開始
                this.LoadStartIfCan();
            }

            return handler;
        }

		/// <summary>
		/// バンドルの非同期ロード
		/// </summary>
		public AssetHandler LoadBundleAsync(string path, Action onLoaded = null, int threadId = 0)
		{
			//既に読み込みがかかっているか検索
			var handler = this.FindAssetHandler(path, null, true);

			//ハンドルが既存
			if (handler != null)
			{
				//参照カウンタ増加
				handler.referenceCount++;

				//コールバックを積む
				this.callbacks.Add((handler, () => onLoaded?.Invoke()));

				//ハンドルがロード済みなら積んであるコールバックの消化を試みる（1フレ後に）
				if (handler.status == AssetHandler.Status.Completed)
				{
					this.StartDelayActionCoroutine(null, this.OnLoadedAssetHandler);
				}
			}
			//ハンドルが存在しなかったら
			else
			{
				//アセットバンドルかどうかの情報を検索
				var info = this.FindAssetBundleInfo(path);
				Debug.Assert(info != null);

				//AssetBundle用アセットハンドラ生成
				handler = new AssetBundleAssetHandler(path, null, info);

				//どのスレッドを使用するかセット
				handler.threadId = threadId;

				//リストにアセットハンドラを保持
				this.handlers.Add(handler);

				//コールバックを積む
				this.callbacks.Add((handler, () => onLoaded?.Invoke()));

				//ロード開始
				this.LoadStartIfCan();
			}

			return handler;
		}

        /// <summary>
        /// シーンアセットの同期ロード
        /// </summary>
        public AssetHandler LoadSceneAsset(string path)
        {
            //既に読み込みがかかっているか検索
            var handler = this.FindAssetHandler(path);

            //ハンドルが既存
            if (handler != null)
            {
                //参照カウンタ増加
                handler.referenceCount++;
            }
            //ハンドルが存在しなかったら
            else
            {
                //アセットバンドル情報を検索
                var info = this.FindAssetBundleInfo(path);
                if (info == null)
                {
#if UNITY_EDITOR
                    //ダミー用アセットハンドラ生成
                    handler = new DummyAssetHandler(path);
#else
                    Debug.LogErrorFormat("{0}のアセットバンドル情報がありません。", path);
                    return null;
#endif
                }
                else
                {
                    //AssetBundle用アセットハンドラ生成
                    handler = new AssetBundleAssetHandler(path, null, info);
                }

                //リストにアセットハンドラを保持
                this.handlers.Add(handler);
            }

            //ロード
            handler.Load();
            return handler;
        }

        /// <summary>
        /// シーンアセットの非同期ロード
        /// </summary>
        public AssetHandler LoadSceneAssetAsync(string path, Action onLoaded = null, int threadId = 0)
        {
            //既に読み込みがかかっているか検索
            var handler = this.FindAssetHandler(path);

            //ハンドルが既存
            if (handler != null)
            {
                //参照カウンタ増加
                handler.referenceCount++;

                //コールバックを積む
                this.callbacks.Add((handler, onLoaded));

                //ハンドルがロード済みなら積んであるコールバックの消化を試みる（1フレ後に）
                if (handler.status == AssetHandler.Status.Completed)
                {
                    this.StartDelayActionCoroutine(null, this.OnLoadedAssetHandler);
                }
            }
            //ハンドルが存在しなかったら
            else
            {
                //アセットバンドル情報を検索
                var info = this.FindAssetBundleInfo(path);
                if (info == null)
                {
#if UNITY_EDITOR
                    //ダミー用アセットハンドラ生成
                    handler = new DummyAssetHandler(path);
#else
                    Debug.LogErrorFormat("{0}のアセットバンドル情報がありません。", path);
                    return null;
#endif
                }
                else
                {
                    //AssetBundle用アセットハンドラ生成
                    handler = new AssetBundleAssetHandler(path, null, info);
                }

				//どのスレッドを使用するかセット
				handler.threadId = threadId;

                //リストにアセットハンドラを保持
                this.handlers.Add(handler);

                //コールバックを積む
                this.callbacks.Add((handler, onLoaded));

                //ロード開始
                this.LoadStartIfCan();
            }

            return handler;
        }

		/// <summary>
		/// スレッドに余裕があるならロード開始
		/// </summary>
		private void LoadStartIfCan()
		{
			int[] loadingCount = new int[this.maxThreadCounts.Length];

			for (int i = 0, imax = this.handlers.Count; i < imax; i++)
			{
				if (this.handlers[i].status == AssetHandler.Status.Loading)
				{
					//各スレッドでロード中のハンドラ数をカウント
					loadingCount[this.handlers[i].threadId]++;
				}
			}

			for (int i = 0; i < this.maxThreadCounts.Length; i++)
			{
				//スレッドに余裕があるなら
				for (int j = loadingCount[i]; j < this.maxThreadCounts[i]; j++)
				{
					var handler = this.handlers.Find(_ => _.threadId == i && _.status == AssetHandler.Status.None);
					if (handler != null)
					{
						//未処理ハンドラのロード開始
						handler.LoadAsync(this.OnLoadedAssetHandler);
					}
					else
					{
						break;
					}
				}
			}
		}

        /// <summary>
        /// アセットハンドラのロード完了時
        /// </summary>
        private void OnLoadedAssetHandler()
        {
			for (int i = 0; i < this.maxThreadCounts.Length; i++)
			{
				while (true)
				{
					int j = this.callbacks.FindIndex(_ => _.handler.threadId == i);

					if (j < 0 || this.callbacks[j].handler.status != AssetHandler.Status.Completed)
					{
						//スレッドが一致するコールバックが存在しない or 先頭のコールバックに対応するハンドラがロード未完了
						break;
					}

					if (this.callbacks[j].handler.referenceCount > 0)
					{
						//参照があるならコールバック呼び出し
						this.callbacks[j].callback?.Invoke();
						this.callbacks.RemoveAt(j);
					}
					else
					{
						//参照が無いので自信をアンロード
						Unload(this.callbacks[j].handler);
					}
				}
			}

            //スレッドに余裕があるならロード開始
            this.LoadStartIfCan();
        }

        /// <summary>
        /// アンロード
        /// </summary>
        public void Unload(AssetHandler handler)
        {
            if (handler == null)
            {
                return;
            }

            if (handler.referenceCount > 0)
            {
                //参照カウンタ減少
                handler.referenceCount--;
            }

            //・まだ参照がある
            //・破棄不可フラグが立っている
            //・リストに入ってない
            if (handler.referenceCount > 0
            ||  handler.isDontDestroy
            || !this.handlers.Contains(handler))
            {
                //破棄不可
                return;
            }

			//ロード中
			if (handler.status == AssetHandler.Status.Loading)
			{
				//ロード完了時に今積まれているコールバックは呼ばれないようにする
				for (int i = 0; i < this.callbacks.Count; i++)
				{
					if (this.callbacks[i].handler == handler)
					{
						this.callbacks[i] = (handler, null);
					}
				}

				//ロード完了後にアンロード試行するので、今は破棄不可
				return;
			}

            //破棄実行
            handler.Unload();
            this.handlers.Remove(handler);
            this.callbacks.RemoveAll(x => x.handler == handler);
        }

        /// <summary>
        /// 遅延実行
        /// </summary>
        private IEnumerator DelayAction(object obj, Action callback)
        {
            yield return obj;
            callback?.Invoke();
        }

        /// <summary>
        /// 遅延実行コルーチンの開始
        /// </summary>
        public Coroutine StartDelayActionCoroutine(object obj, Action callback)
        {
            return StartCoroutine(this.DelayAction(obj, callback));
        }
    }
}
