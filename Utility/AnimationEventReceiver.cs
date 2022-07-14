using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace KG
{
	/// <summary>
	/// アニメーションイベントレシーバー
	/// </summary>
	public class AnimationEventReceiver : MonoBehaviour
	{
		/// <summary>
		/// アニメーションイベント
		/// </summary>
		[Serializable]
		public class AnimationEvent
		{
			/// <summary>
			/// イベント名
			/// </summary>
			public string name = null;

			/// <summary>
			/// コールバック
			/// </summary>
			public UnityEvent callback = new UnityEvent();
		}

		/// <summary>
		/// イベントコールバック
		/// </summary>
		[SerializeField]
		public List<AnimationEvent> onEvents = null;

		/// <summary>
		/// イベント受信時
		/// </summary>
		private void OnEvent(string eventName)
		{
			for (int i = 0; i < this.onEvents.Count; i++)
			{
				if (this.onEvents[i].name == eventName)
				{
					this.onEvents[i].callback.Invoke();
				}
			}
		}
	}
}
