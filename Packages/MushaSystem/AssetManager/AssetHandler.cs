using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KG
{
    /// <summary>
    /// アセットハンドラ
    /// </summary>
    public abstract class AssetHandler
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
        /// パス
        /// </summary>
        public string path { get; private set; }

        /// <summary>
        /// アセットタイプ
        /// </summary>
        public Type type { get; private set; }

		/// <summary>
		/// スレッドID
		/// </summary>
		public int threadId = 0;

        /// <summary>
        /// ステータス
        /// </summary>
        public Status status { get; protected set; }

        /// <summary>
        /// アセット
        /// </summary>
        private UnityEngine.Object m_asset = null;

        /// <summary>
        /// アセット
        /// </summary>
        public UnityEngine.Object asset
        {
            get
            {
                return this.m_asset;
            }
            protected set
            {
                Debug.Assert(value != null, $"{this.path} is null. type = {this.type}");
                this.m_asset = value;
            }
        }

        /// <summary>
        /// 参照カウンタ
        /// </summary>
        public int referenceCount = 1;

        /// <summary>
        /// 破棄不可フラグ
        /// </summary>
        public bool isDontDestroy = false;

        /// <summary>
        /// construct
        /// </summary>
        protected AssetHandler(string path, Type type)
        {
            this.path = path.Replace('\\', '/');
            this.type = type;
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
                    this.status = Status.Completed;
                    this.LoadInternal();
                }
                break;

                case Status.Loading:
                {
                    Debug.LogErrorFormat("アセット:{0}は非同期ロード中のため、同期ロード出来ません。", this.path);
                }
                break;
            }
        }

        /// <summary>
        /// 同期ロード
        /// </summary>
        protected abstract void LoadInternal();

        /// <summary>
        /// 非同期ロード
        /// </summary>
        public abstract void LoadAsync(Action onLoaded);

        /// <summary>
        /// アンロード
        /// </summary>
        public virtual void Unload()
		{
			this.status = Status.None;
		}

#if UNITY_EDITOR
		/// <summary>
		/// アセットバンドル名チェック用リスト
		/// </summary>
		public static List<AssetBundleInfo> checkList = null;

		/// <summary>
		/// アセットバンドル名チェック
		/// </summary>
		protected static void CheckAssetBundle(string path)
		{
			if (checkList != null && checkList.Find(path) == null)
			{
				Debug.LogError($"AssetBundle名が設定されていません：{path}");
			}
		}
#endif
	}
}