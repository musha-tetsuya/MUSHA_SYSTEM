using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KG
{
	/// <summary>
	/// ViewportRect制御
	/// </summary>
	public class ViewportRectController : SingletonMonoBehaviour<ViewportRectController>
	{
		/// <summary>
		/// ターゲットアスペクト比
		/// </summary>
		[SerializeField]
		public float targetAspect = 16f / 9f;

		/// <summary>
		/// 無視するカメラ
		/// </summary>
		[SerializeField]
		public List<Camera> ignoredCameras = new List<Camera>();

		/// <summary>
		/// カメラに設定するViewportRect
		/// </summary>
		private Rect rect = new Rect(0, 0, 1, 1);

		/// <summary>
		/// OnDestroy
		/// </summary>
		protected override void OnDestroy()
		{
			Camera.onPreCull -= this.OnPreCullCallback;
			base.OnDestroy();
		}

		/// <summary>
		/// Awake
		/// </summary>
		protected override void Awake()
		{
			base.Awake();
			Camera.onPreCull += this.OnPreCullCallback;
		}

		/// <summary>
		/// Update
		/// </summary>
		private void Update()
		{
			//現在の画面アスペクト比
			var currentAspect = (float)Screen.width / Screen.height;
			this.rect.x = 0;
			this.rect.y = 0;
			this.rect.width = 1;
			this.rect.height = 1;

			//widthが長い
			if (this.targetAspect < currentAspect)
			{
				//左右に黒帯
				this.rect.width = this.targetAspect / currentAspect;
				this.rect.x = (1 - this.rect.width) * 0.5f;
			}
			//heightが長い
			else
			{
				//上下に黒帯
				this.rect.height = currentAspect / this.targetAspect;
				this.rect.y = (1 - this.rect.height) * 0.5f;
			}

			//無効なカメラは無視リストから除去
			for (int i = 0; i < this.ignoredCameras.Count; i++)
			{
				if (this.ignoredCameras[i] == null || this.ignoredCameras[i].gameObject == null)
				{
					this.ignoredCameras.RemoveAt(i);
					i--;
				}
			}
		}

		/// <summary>
		/// OnPreCullイベント時コールバック
		/// </summary>
		private void OnPreCullCallback(Camera camera)
		{
			if (!this.ignoredCameras.Contains(camera))
			{
				//ViewportRectを設定
				camera.rect = this.rect;
			}
		}
	}
}
