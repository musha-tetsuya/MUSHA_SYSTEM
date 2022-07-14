using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KG
{
	/// <summary>
	/// 遅延処理
	/// </summary>
	public class DelayAction
	{
		/// <summary>
		/// ルーチン
		/// </summary>
		public IEnumerator routine { get; private set; }

		/// <summary>
		/// construct
		/// </summary>
		public DelayAction(object obj, Action callback)
		{
			this.routine = this.KeepWaiting(obj, callback);
		}

		/// <summary>
		/// 遅延処理ルーチン
		/// </summary>
		private IEnumerator KeepWaiting(object obj, Action callback)
		{
			yield return obj;

			callback?.Invoke();
		}
	}
}