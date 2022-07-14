using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;

namespace KG
{
	/// <summary>
	/// アニメーターステートのイベントに対するコールバック制御
	/// アニメーターのステートにAdd Behaviourして使う
	/// レイヤーにAdd Behaviourした際の動作は保証外
	/// </summary>
	public class AnimatorStateEventBehaviour : StateMachineBehaviour
	{
		/// <summary>
		/// Update時イベントデータ
		/// </summary>
		[Serializable]
		private class StateUpdateEvent
		{
			/// <summary>
			/// 発火タイミング
			/// </summary>
			[SerializeField, Range(0f, 1f)]
			public float normalizedTime = 0f;

			/// <summary>
			/// コールバック
			/// </summary>
			[SerializeField]
			public UnityEvent callback = null;

			/// <summary>
			/// 発火したかどうか
			/// </summary>
			public bool isFired { get; set; }
		}

		/// <summary>
		/// Exit時イベントデータ
		/// </summary>
		[Serializable]
		private class StateExitEvent
		{
			/// <summary>
			/// コールバック
			/// </summary>
			[SerializeField]
			public UnityEvent callback = null;

			/// <summary>
			/// 発火したかどうか
			/// </summary>
			public bool isFired { get; set; }
		}

		/// <summary>
		/// Update時イベントデータ
		/// </summary>
		[SerializeField]
		private StateUpdateEvent[] onStateUpdate = null;

		/// <summary>
		/// Exit時イベントデータ
		/// </summary>
		[SerializeField]
		private StateExitEvent onStateExit = null;

		/// <summary>
		/// クリップデータ
		/// </summary>
		private float? clipLength = null;

		/// <summary>
		/// 1フレーム前の時間
		/// </summary>
		private float prevNormalizedTime = 0f;

		/// <summary>
		/// アニメーター
		/// </summary>
		public Animator animator { get; private set;}

		/// <summary>
		/// ステートに入ったとき
		/// </summary>
		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			this.animator = animator;

			for (int i = 0; i < this.onStateUpdate.Length; i++)
			{
				//発火フラグクリア（再発火可能に）
				this.onStateUpdate[i].isFired = false;
			}

			//発火フラグクリア（再発火可能に）
			this.onStateExit.isFired = false;

			//時間クリア
			this.prevNormalizedTime = 0f;
		}

		/// <summary>
		/// ステートUpdate時
		/// </summary>
		public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
		{
			this.animator = animator;

			if (this.onStateUpdate.Length == 0)
			{
				return;
			}

			//クリップ長
			if (this.clipLength == null)
			{
				var clipInfos = controller.GetCurrentAnimatorClipInfo(0);
				this.clipLength = (clipInfos.Length > 0 && clipInfos[0].clip != null) ? clipInfos[0].clip.length : 0f;
			}

			//0秒クリップの場合
			if (this.clipLength.Value == 0f)
			{
				if (stateInfo.loop)
				{
					foreach (var x in this.onStateUpdate)
					{
						//ループするステートなら毎フレーム発火フラグクリア
						x.isFired = false;
					}
				}

				foreach (var x in this.onStateUpdate)
				{
					if (!x.isFired)
					{
						//可能ならコールバック発火
						x.isFired = true;
						x.callback.Invoke();
					}
				}
			}
			else
			{
				var normalizedTime = stateInfo.normalizedTime;

				//ループするステートの場合
				if (stateInfo.loop)
				{
					//0f～1f未満に丸める
					normalizedTime = Mathf.Repeat(normalizedTime, 1f);

					//1フレ前の時間より小さくなったら即ちループしたということ
					if (normalizedTime < this.prevNormalizedTime)
					{
						foreach (var x in this.onStateUpdate)
						{
							//発火フラグクリア
							x.isFired = false;

							if (x.normalizedTime > this.prevNormalizedTime)
							{
								//ループした際に跨いだ時間のコールバックを発火
								x.isFired = true;
								x.callback.Invoke();
							}
						}
					}

					//1フレ前の時間として保存
					this.prevNormalizedTime = normalizedTime;
				}

				foreach (var x in this.onStateUpdate)
				{
					if (!x.isFired && x.normalizedTime <= normalizedTime)
					{
						//未発火イベントの時間を跨いだならコールバック発火
						x.isFired = true;
						x.callback.Invoke();
					}
				}
			}
		}

		/// <summary>
		/// ステートから出たとき
		/// </summary>
		public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			this.animator = animator;

			if (!this.onStateExit.isFired)
			{
				this.onStateExit.isFired = true;
				this.onStateExit.callback.Invoke();
			}
		}
	}
}
