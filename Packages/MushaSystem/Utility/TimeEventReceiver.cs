using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace KG
{
	/// <summary>
	/// 時限イベント
	/// </summary>
	public class TimeEventReceiver : MonoBehaviour
	{
		/// <summary>
		/// イベントデータ
		/// </summary>
		[Serializable]
		private class EventData
		{
			/// <summary>
			/// 時間
			/// </summary>
			[SerializeField]
			public float time;

			/// <summary>
			/// コールバック
			/// </summary>
			[SerializeField]
			public UnityEvent callback = null;
		}

		/// <summary>
		/// イベントデータリスト
		/// </summary>
		[SerializeField]
		private List<EventData> eventDataList = null;

		/// <summary>
		/// イベント開始
		/// </summary>
		public void EventStart(float startTime = 0f)
		{
			for (int i = 0; i < this.eventDataList.Count; i++)
			{
				StartCoroutine(this.SendEvent(this.eventDataList[i], startTime));
			}
		}

		/// <summary>
		/// イベント送信コルーチン
		/// </summary>
		private IEnumerator SendEvent(EventData eventData, float startTime)
		{
			if (startTime < eventData.time)
			{
				yield return new WaitForSeconds(eventData.time - startTime);

				eventData.callback.Invoke();
			}
		}
	}
}