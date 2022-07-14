using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KG
{
    /// <summary>
    /// ダミーアセットハンドラ
    /// </summary>
    public class DummyAssetHandler : AssetHandler
    {
        /// <summary>
        /// construct
        /// </summary>
        public DummyAssetHandler(string path)
            : base(path, null)
        {
        }

        /// <summary>
        /// 同期ロード
        /// </summary>
        protected override void LoadInternal()
		{
#if UNITY_EDITOR
			CheckAssetBundle(this.path);
#endif
		}

        /// <summary>
        /// 非同期ロード
        /// </summary>
        public override void LoadAsync(Action onLoaded)
        {
#if UNITY_EDITOR
			CheckAssetBundle(this.path);
#endif
            //ステータスをロード中に
            this.status = Status.Loading;

            //1フレ後にロード完了
            AssetManager.Instance.StartDelayActionCoroutine(null, () =>
            {
                //ステータスを完了に
                this.status = Status.Completed;

                //ロード完了を通知
                onLoaded?.Invoke();
            });
        }
    }
}