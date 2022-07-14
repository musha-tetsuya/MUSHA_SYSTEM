using UnityEngine;
using UnityEngine.UI;

namespace KG
{
	/// <summary>
	/// FPSカウンター
	/// </summary>
    public class FPSCounter : Text
    {
		/// <summary>
		/// 更新間隔
		/// </summary>
        private const float UPDATE_INTERVAL = 0.5f;

		/// <summary>
		/// テキストフォーマット
		/// </summary>
		private string textFormat = $"{{0:F1}} FPS({UPDATE_INTERVAL}sec)";

		/// <summary>
		/// 前回の時間
		/// </summary>
		private float prevTime = 0f;

		/// <summary>
		/// Update
		/// </summary>
        private void Update()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying) return;
#endif
			if (Time.realtimeSinceStartup - this.prevTime >= UPDATE_INTERVAL)
			{ 
				this.text = string.Format(this.textFormat, 1f / Time.deltaTime);
				this.prevTime = Time.realtimeSinceStartup;
			}
		}
	}
}
