using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace KG
{
    /// <summary>
    /// シーン管理
    /// </summary>
    public class GameSceneManager : SingletonMonoBehaviour<GameSceneManager>
    {
		/// <summary>
		/// プッシュしたシーンリスト
		/// </summary>
		private Stack<PushItem> pushItems = new Stack<PushItem>();

		/// <summary>
		/// アセットバンドルのシーンかどうか
		/// </summary>
		private bool IsAssetBundleScene(string sceneName)
		{
			bool isAssetBundle = AssetManager.Instance.FindAssetBundleInfo(sceneName) != null;
#if UNITY_EDITOR
			isAssetBundle |= SceneAssetInfo.Infos.Any(x => x.isAssetBundle && x.name.Equals(sceneName, StringComparison.OrdinalIgnoreCase));
#endif
			return isAssetBundle;
		}

		/// <summary>
		/// シーン切り替え
		/// </summary>
		/// <param name="sceneName">BuildSettingsに含まれている場合はシーン名を。含まれていないアセットバンドルのシーンの場合はシーンパスを入れる。（「Assets/」と「.unity」は不要）</param>
		public void ChangeSceneAsync(string sceneName, Action onUnloaded = null, Action onLoaded = null)
		{
			//プッシュしたシーンはクリア
			this.pushItems.Clear();

			//アンロードするシーン達
			var unloadTargetScenes = new string[SceneManager.sceneCount];
			for (int i = 0; i < unloadTargetScenes.Length; i++)
			{
				unloadTargetScenes[i] = SceneManager.GetSceneAt(i).name;
			}

			//シーンをアンロードする前にHierarchyが空にならないように空シーンを作成
			SceneManager.CreateScene("Empty");

			//シーンのアンロード
			this.UnloadScenesAsync(unloadTargetScenes, () =>
			{
#if DEBUG
				string log = $"ChangeScene : {unloadTargetScenes[0]} -> {sceneName}";

				//シーン消えたけどUnloadされてないアセット一覧をログ表示
				foreach (var handler in AssetManager.Instance.handlers.OrderBy(_ => _.isDontDestroy))
				{
					string txt = $"未アンロード：{handler.path}, referenceCount={handler.referenceCount}, isDontDestroy={handler.isDontDestroy}";

					if (handler.isDontDestroy)
					{
						//log += $"\n{txt}";
					}
					else
					{
						log += $"\n<color=yellow>{txt}</color>";
					}
				}

				Debug.Log(log);
#endif
				//シーンのアンロード完了通知
				onUnloaded?.Invoke();

				//リソースアンロード
				Resources.UnloadUnusedAssets().completed += (_) =>
				{
					//GC整理
					GC.Collect();

					//シーンロード
					this.LoadSceneAsync(sceneName, LoadSceneMode.Single, onLoaded);
				};
			});
		}

		/// <summary>
		/// シーン加算
		/// </summary>
		/// <param name="sceneName">BuildSettingsに含まれている場合はシーン名を。含まれていないアセットバンドルのシーンの場合はシーンパスを入れる。（「Assets/」と「.unity」は不要）</param>
		public void AddSceneAsync(string sceneName, bool setActive, Action onLoaded = null)
		{
			//シーンロード
			this.LoadSceneAsync(sceneName, LoadSceneMode.Additive, () =>
			{
				if (setActive)
				{
					//アクティブシーン切り替え
					SceneManager.SetActiveScene(SceneManager.GetSceneByName(Path.GetFileName(sceneName)));
				}

				//完了通知
				onLoaded?.Invoke();
			});
		}

		/// <summary>
		/// シーンプッシュ
		/// </summary>
		/// <param name="sceneName">BuildSettingsに含まれている場合はシーン名を。含まれていないアセットバンドルのシーンの場合はシーンパスを入れる。（「Assets/」と「.unity」は不要）</param>
		public void PushSceneAsync(string sceneName, Action onPop = null)
		{
			//今存在しているシーン一覧
			var scenes = new Scene[SceneManager.sceneCount];
			for (int i = 0; i < scenes.Length; i++)
			{
				scenes[i] = SceneManager.GetSceneAt(i);
			}

			//アクティブなRootGameObject一覧
			var activeRootGameObjects = scenes.SelectMany(x => x.GetRootGameObjects()).Where(x => x.activeSelf).ToArray();

			//アクティブなRootGameObjectをすべて非アクティブに
			foreach (var gobj in activeRootGameObjects)
			{
				gobj.SetActive(false);
			}

			//シーンプッシュ情報を保存
			PushItem item;
			item.sceneName = sceneName;
			item.prevScene = SceneManager.GetActiveScene();
			item.activeRootGameObjects = activeRootGameObjects;
			item.onPop = onPop;
			this.pushItems.Push(item);

			//シーンプッシュ
			this.AddSceneAsync(sceneName, true);
		}

		/// <summary>
		/// シーンポップ
		/// </summary>
		public void PopSceneAsync()
		{
			var item = this.pushItems.Pop();

			//プッシュしたシーンの破棄
			this.UnloadSceneAsync(item.sceneName, () =>
			{
				//アクティブシーンを戻す
				SceneManager.SetActiveScene(item.prevScene);

				//プッシュ時に非アクティブにしたRootGameObjectをアクティブに戻す
				foreach (var gobj in item.activeRootGameObjects)
				{
					gobj.SetActive(true);
				}

				//ポップ完了通知
				item.onPop?.Invoke();
			});
		}

		/// <summary>
		/// 非同期アンロード
		/// </summary>
		public void UnloadSceneAsync(string sceneName, Action onCompleted = null)
		{
			this.UnloadScenesAsync(new[] { sceneName }, onCompleted);
		}

		/// <summary>
		/// 非同期アンロード
		/// </summary>
		private void UnloadScenesAsync(string[] sceneNames, Action onCompleted = null)
		{
			int busyCnt = 0;

			for (int i = 0; i < sceneNames.Length; i++)
			{
				var op = SceneManager.UnloadSceneAsync(Path.GetFileName(sceneNames[i]));
				if (op != null)
				{
					busyCnt++;
					op.completed += (_) => busyCnt--;
				}
			}

			StartCoroutine(new DelayAction(new WaitWhile(() => busyCnt > 0), onCompleted).routine);
		}

		/// <summary>
		/// 非同期ロード
		/// </summary>
		/// <param name="sceneName">BuildSettingsに含まれている場合はシーン名を。含まれていないアセットバンドルのシーンの場合はシーンパスを入れる。（「Assets/」と「.unity」は不要）</param>
		private void LoadSceneAsync(string sceneName, LoadSceneMode mode, Action onLoaded = null)
		{
#if UNITY_EDITOR
			//Debug.Log($"LoadStart:{sceneName}:{Time.realtimeSinceStartup}");
			//onLoaded += () => Debug.Log($"LoadEnd:{sceneName}:{Time.realtimeSinceStartup}");

			if (!SceneAssetInfo.Infos.Any(x => x.name.Equals(sceneName, StringComparison.OrdinalIgnoreCase)))
			{
				Debug.LogError($"{sceneName}に一致するシーンは存在しません。");
				return;
			}
#endif
			//アセットバンドルシーンなら
			if (this.IsAssetBundleScene(sceneName))
			{
				//アセットバンドルがロード済みかチェック
				var handle = AssetManager.Instance.FindAssetHandler(sceneName);
				if (handle == null || handle.status != AssetHandler.Status.Completed)
				{
					Debug.LogError($"シーン:{sceneName}はロードされていません。");
					return;
				}
#if UNITY_EDITOR
				//Editor用ダミーハンドルの場合
				if (handle is DummyAssetHandler)
				{
					//生データを読み込む
					EditorSceneManager.LoadSceneAsyncInPlayMode($"Assets/{sceneName}.unity", new LoadSceneParameters(mode)).completed += (_) => onLoaded?.Invoke();
					return;
				}
#endif
			}

#if UNITY_EDITOR
			var buildSettingsScene = EditorBuildSettings.scenes.FirstOrDefault(x => x.path.EndsWith($"/{sceneName}.unity", StringComparison.OrdinalIgnoreCase));
			if (buildSettingsScene != null && !buildSettingsScene.enabled)
			{
				//無効化されているシーンなので生データを読み込む
				Debug.LogWarning($"{buildSettingsScene.path}はBuildSettingsで無効化されています。");
				EditorSceneManager.LoadSceneAsyncInPlayMode(buildSettingsScene.path, new LoadSceneParameters(mode)).completed += (_) => onLoaded?.Invoke();
				return;
			}
#endif
			//シーンロード
			SceneManager.LoadSceneAsync(sceneName, mode).completed += (_) => onLoaded?.Invoke();
		}

		/// <summary>
		/// シーンプッシュ情報
		/// </summary>
		private struct PushItem
		{
			/// <summary>
			/// シーン名
			/// </summary>
			public string sceneName;

			/// <summary>
			/// 前のシーン
			/// </summary>
			public Scene prevScene;
			
			/// <summary>
			/// プッシュ時にアクティブだったRootGameObject一覧
			/// </summary>
			public GameObject[] activeRootGameObjects;

			/// <summary>
			/// ポップ時コールバック
			/// </summary>
			public Action onPop;
		}

#if UNITY_EDITOR
        /// <summary>
        /// シーン情報
        /// </summary>
        private class SceneAssetInfo
        {
            /// <summary>
            /// パス
            /// </summary>
            public string path;

            /// <summary>
            /// 名前
            /// </summary>
            public string name;

            /// <summary>
            /// アセットバンドルかどうか
            /// </summary>
            public bool isAssetBundle;

            /// <summary>
            /// Assets内全シーン情報
            /// </summary>
            private static SceneAssetInfo[] infos = null;

            /// <summary>
            /// Assets内全シーン情報
            /// </summary>
            public static SceneAssetInfo[] Infos
            {
                get
                {
                    if (infos == null)
                    {
                        //BuildSettingsに含まれているシーンのパス一覧
                        var buildSettingsScenePaths = EditorBuildSettings.scenes
                            .Select(x => x.path)
                            .ToArray();

                        //Assets内全シーン情報
                        infos = AssetDatabase
                            .FindAssets("t:SceneAsset", new string[]{ "Assets" })
                            .Select(AssetDatabase.GUIDToAssetPath)
                            .Select(path =>
                            {
                                var info = new SceneAssetInfo();
                                info.path = path;
                                info.isAssetBundle = !buildSettingsScenePaths.Contains(path);
                                info.name = info.isAssetBundle
                                    ? path.Remove(0, "Assets/".Length).Replace(".unity", null)
                                    : Path.GetFileNameWithoutExtension(path);
                                return info;
                            })
                            .ToArray();
                    }
                    return infos;
                }
            }
        }
#endif
    }
}