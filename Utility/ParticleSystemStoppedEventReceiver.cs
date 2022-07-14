using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace KG
{
	/// <summary>
	/// ParticleSystem終了時イベント受信機
	/// </summary>
	public class ParticleSystemStoppedEventReceiver : MonoBehaviour
	{
		/// <summary>
		/// コールバック
		/// </summary>
		[SerializeField]
		private UnityEvent callback = null;

		/// <summary>
		/// OnParticleSystemStopped
		/// </summary>
		private void OnParticleSystemStopped()
		{
			this.callback.Invoke();
		}
	}
}