using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KG
{
	/// <summary>
	/// ステートオブジェクト基底
	/// </summary>
	public class StateObject
	{
		/// <summary>
		/// マネージャー
		/// </summary>
		protected StateManager manager { get; private set; }

		/// <summary>
		/// マネージャーセット
		/// </summary>
		public void SetManager(StateManager manager) => this.manager = manager;

		/// <summary>
		/// Start
		/// </summary>
		public virtual void Start() { }

		/// <summary>
		/// Update
		/// </summary>
		public virtual void Update() { }

		/// <summary>
		/// End
		/// </summary>
		public virtual void End() { }
	}

	/// <summary>
	/// ステート管理
	/// </summary>
	public class StateManager
	{
		/// <summary>
		/// 現在のステート
		/// </summary>
		public StateObject currentState { get; private set; }

		/// <summary>
		/// ステート一覧
		/// </summary>
		private Dictionary<Type, StateObject> stateList = new Dictionary<Type, StateObject>();

		/// <summary>
		/// ステートスタック
		/// </summary>
		private Stack<(StateObject state, Action onPop)> stateStack = new Stack<(StateObject, Action)>();

		/// <summary>
		/// Update
		/// </summary>
		public void Update()
		{
			this.currentState?.Update();
		}

		/// <summary>
		/// ステート追加
		/// </summary>
		public void AddState(StateObject state)
		{
			state.SetManager(this);
			this.stateList[state.GetType()] = state;
		}

		/// <summary>
		/// ステート取得
		/// </summary>
		public T GetState<T>() where T : StateObject
		{
			return this.stateList[typeof(T)] as T;
		}

		/// <summary>
		/// ステート切り替え
		/// </summary>
		public void ChangeState(StateObject state)
		{
			this.currentState?.End();
			this.currentState = state;
			this.currentState?.SetManager(this);
			this.currentState?.Start();
		}

		/// <summary>
		/// ステート切り替え
		/// </summary>
		public void ChangeState<T>() where T : StateObject
		{
			this.ChangeState(this.GetState<T>());
		}

		/// <summary>
		/// ステートプッシュ
		/// </summary>
		public void PushState(StateObject state, Action onPop = null)
		{
			this.stateStack.Push((this.currentState, onPop));
			this.currentState = state;
			this.currentState?.SetManager(this);
			this.currentState?.Start();
		}

		/// <summary>
		/// ステートプッシュ
		/// </summary>
		public void PushState<T>(Action onPop = null) where T : StateObject
		{
			this.PushState(this.GetState<T>(), onPop);
		}

		/// <summary>
		/// ステートポップ
		/// </summary>
		public void PopState()
		{
			this.currentState?.End();

			var item = this.stateStack.Pop();
			this.currentState = item.state;
			item.onPop?.Invoke();
		}
	}
}