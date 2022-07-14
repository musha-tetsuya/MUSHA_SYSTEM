using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KG
{
    /// <summary>
    /// リソースアセットハンドラ
    /// </summary>
    public class ResourcesAssetHandler : AssetHandler
    {
		/// <summary>
		/// 読込用パス
		/// </summary>
		private string loadPath = null;

        /// <summary>
        /// construct
        /// </summary>
        public ResourcesAssetHandler(string path, Type type)
            : base(path, type)
        {
			if (this.path.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
			{
				int removeLength = "Resources/".Length;
				this.loadPath = Path.ChangeExtension(this.path.Remove(0, removeLength), null);
			}
			else
			{
				int i = this.path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase);
				if (i >= 0)
				{
					int removeLength = i + "/Resources/".Length;
					this.loadPath = Path.ChangeExtension(this.path.Remove(0, removeLength), null);
				}
			}
        }

#if UNITY_EDITOR
		/// <summary>
		/// アセットパスの取得
		/// </summary>
		private string GetAssetPath(string typeName)
		{
			//アセットが入ってるフォルダ
			var folder = Path.GetDirectoryName($"Assets/{this.path}");
			
			//フォルダ内のパスに一致するファイル一覧
			var files = Directory.GetFiles(folder)
				.Where(_ => !_.EndsWith(".meta", StringComparison.Ordinal))
				.Where(_ => !_.EndsWith(".cs", StringComparison.Ordinal))
				.Select(_ => _.Replace('\\', '/'))
				.Where(_ => Path.ChangeExtension(_, null).EndsWith(this.path, StringComparison.OrdinalIgnoreCase))
				.ToArray();

			if (files.Length == 1)
			{
				//パスが一致するファイルが一つしかないならそれを返す（FindAssetsが遅いので）
				return files[0];
			}
			else
			{
				//パスに一致するファイルが複数あるので、型に一致するものを検索する
				return AssetDatabase
					.FindAssets($"{Path.GetFileNameWithoutExtension(this.path)} t:{typeName}", new[] { folder })
					.Select(AssetDatabase.GUIDToAssetPath)
					.Where(_ => !_.EndsWith(".cs", StringComparison.Ordinal))
					.Where(_ => Path.ChangeExtension(_, null).EndsWith(this.path, StringComparison.OrdinalIgnoreCase))
					.FirstOrDefault();
			}
		}

        /// <summary>
        /// アセットの確保
        /// </summary>
        private void SetAsset()
        {
			CheckAssetBundle(this.path);

            if (this.type.IsSubclassOf(typeof(MonoBehaviour)))
            {
                //MonoBehaviourを継承しているアセットはGameObject型でロードする
                this.asset = AssetDatabase.LoadAssetAtPath<GameObject>(this.GetAssetPath("GameObject"));
                this.asset = (this.asset as GameObject).GetComponent(this.type);
            }
            else
            {
                this.asset = AssetDatabase.LoadAssetAtPath(this.GetAssetPath(this.type.Name), this.type);

                /*if (!(this.asset is GameObject))
                {
                    //GameObject以外のアセットは生データを書き換えないようにInstantiateしたアセットを確保する
                    this.asset = UnityEngine.Object.Instantiate(this.asset);
                    this.asset.name = this.asset.name.Replace("(Clone)", null);
                }*/
            }
        }
#endif

        /// <summary>
        /// 同期ロード
        /// </summary>
        protected override void LoadInternal()
        {
#if UNITY_EDITOR
			if (string.IsNullOrEmpty(this.loadPath))
			{
                //アセットの確保
                this.SetAsset();
                return;
            }
#endif
            //アセットを確保
            this.asset = Resources.Load(this.loadPath, this.type);
        }

        /// <summary>
        /// 非同期ロード
        /// </summary>
        public override void LoadAsync(Action onLoaded)
        {
            //ステータスをロード中に
            this.status = Status.Loading;

#if UNITY_EDITOR
            if (string.IsNullOrEmpty(this.loadPath))
            {
                AssetManager.Instance.StartDelayActionCoroutine(null, () =>
                {
					//アセットの確保
                    this.SetAsset();

                    //ステータスを完了に
                    this.status = Status.Completed;

                    //ロード完了を通知
                    onLoaded?.Invoke();
                });
                return;
            }
#endif

            //ロード開始
            var request = Resources.LoadAsync(this.loadPath, this.type);

            request.completed += (_) =>
            {
                //アセットを確保
                this.asset = request.asset;

                //ステータスを完了に
                this.status = Status.Completed;

                //ロード完了を通知
                onLoaded?.Invoke();
            };
        }
    }
}