using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KG
{
	/// <summary>
	/// デバッグ用セーブデータダイアログ
	/// </summary>
	public class DebugSaveDataDialog : MonoBehaviour
	{
		/// <summary>
		/// ステート管理
		/// </summary>
		private StateManager stateManager = new StateManager();

		/// <summary>
		/// セーブデータ
		/// </summary>
		private ISaveData saveDataOp = null;

		/// <summary>
		/// セーブスロットパラメータ
		/// </summary>
		private SaveSlotParams slotParams = null;

		/// <summary>
		/// 閉じたときのコールバック
		/// </summary>
		public Action onClose = null;

		/// <summary>
		/// OnGUI
		/// </summary>
		private void OnGUI()
		{
			(this.stateManager.currentState as MyState)?.OnGUI();
		}

		/// <summary>
		/// 決定時
		/// </summary>
		public void OnSubmit() => (this.stateManager.currentState as MyState)?.OnSubmit();

		/// <summary>
		/// キャンセル時
		/// </summary>
		public void OnCancel() => (this.stateManager.currentState as MyState)?.OnCancel();

		/// <summary>
		/// ↑押下時
		/// </summary>
		public void OnClickUp() => (this.stateManager.currentState as MyState)?.OnClickUp();

		/// <summary>
		/// ↓押下時
		/// </summary>
		public void OnClickDown() => (this.stateManager.currentState as MyState)?.OnClickDown();

		/// <summary>
		/// ↑スクロール時
		/// </summary>
		public void OnScrollUp() => (this.stateManager.currentState as MyState)?.OnScrollUp();

		/// <summary>
		/// ↓スクロール時
		/// </summary>
		public void OnScrollDown() => (this.stateManager.currentState as MyState)?.OnScrollDown();

		/// <summary>
		/// 閉じる
		/// </summary>
		private void Close()
		{
			Destroy(this.gameObject);
			this.onClose?.Invoke();
			this.onClose = null;
		}

		/// <summary>
		/// 指定スロットタイプのセーブデータが存在するか
		/// </summary>
		public static bool IsExists(SaveSlotSearch slotSearch)
		{
			return GeneralSaveDataManager.SearchSlot(slotSearch).Length > 0;
		}

		/// <summary>
		/// セーブ
		/// </summary>
		public void Save(ISaveData saveDataOp, SaveSlotParams slotParams)
		{
			this.saveDataOp = saveDataOp;
			this.slotParams = slotParams;

			//セーブデータ一覧表示へ
			var nextState = new SaveListState { dialog = this };
			nextState.slotNames = GeneralSaveDataManager.SearchSlot(new SaveSlotSearch(new[] { SaveSlotType.UserSlot })).Select(slot => slot.name).ToArray();
			this.stateManager.PushState(nextState, this.Close);
		}

		/// <summary>
		/// オートセーブ
		/// </summary>
		public void AutoSave(ISaveData saveDataOp, SaveSlotParams slotParams, SaveSlotType slotType, int slotNo = 0)
		{
			this.saveDataOp = saveDataOp;
			this.slotParams = slotParams;

			//セーブ処理へ
			var nextState = new SaveProcessState { dialog = this };
			nextState.slotName = GeneralSaveDataManager.GetSlotName(slotType, slotNo);
			this.stateManager.ChangeState(nextState);
		}

		/// <summary>
		/// ロード
		/// </summary>
		public void Load(ISaveData saveDataOp, SaveSlotSearch slotSearch)
		{
			this.saveDataOp = saveDataOp;

			//ロードデータ一覧表示へ
			var nextState = new LoadListState { dialog = this };
			nextState.slotNames = GeneralSaveDataManager.SearchSlot(slotSearch).Select(slot => slot.name).ToArray();
			this.stateManager.PushState(nextState, this.Close);
		}

		/// <summary>
		/// オートロード
		/// </summary>
		public void AutoLoad(ISaveData saveDataOp, SaveSlotType slotType, int slotNo = 0)
		{
			this.saveDataOp = saveDataOp;

			//ロード処理へ
			var nextState = new LoadProcessState { dialog = this };
			nextState.slotName = GeneralSaveDataManager.GetSlotName(slotType, slotNo);
			this.stateManager.ChangeState(nextState);
		}

		/// <summary>
		/// スロット一覧表示
		/// </summary>
		public void ViewList(string[] slotNames, Action<int> onClose)
		{
			//スロット一覧表示へ
			var nextState = new ViewListState { dialog = this };
			nextState.slotNames = slotNames;
			nextState.onClose = onClose;
			this.stateManager.PushState(nextState, this.Close);
		}

		/// <summary>
		/// ステート基底
		/// </summary>
		private abstract class MyState : StateObject
		{
			/// <summary>
			/// 親ダイアログ
			/// </summary>
			public DebugSaveDataDialog dialog = null;

			/// <summary>
			/// OnGUI
			/// </summary>
			public virtual void OnGUI() { }

			/// <summary>
			/// 決定時
			/// </summary>
			public virtual void OnSubmit() { }

			/// <summary>
			/// キャンセル時
			/// </summary>
			public virtual void OnCancel() { }

			/// <summary>
			/// ↑ボタン押下時
			/// </summary>
			public virtual void OnClickUp() { }

			/// <summary>
			/// ↓ボタン押下時
			/// </summary>
			public virtual void OnClickDown() { }

			/// <summary>
			/// ↑スクロール時
			/// </summary>
			public virtual void OnScrollUp() { }

			/// <summary>
			/// ↓スクロール時
			/// </summary>
			public virtual void OnScrollDown() { }
		}

		/// <summary>
		/// リスト表示ステート基底
		/// </summary>
		private class ListState : MyState
		{
			/// <summary>
			/// GUIボタンスタイル
			/// </summary>
			protected GUIStyle buttonStyle = null;

			/// <summary>
			/// ボタン名リスト
			/// </summary>
			protected List<string> buttonNames = new List<string>();

			/// <summary>
			/// スクロール位置
			/// </summary>
			protected Vector2 scrollPosition = Vector2.zero;

			/// <summary>
			/// フォーカス中Index
			/// </summary>
			protected int focusedIndex = 0;

			/// <summary>
			/// OnGUI
			/// </summary>
			public override void OnGUI()
			{
				//ボタンスタイル作成
				if (this.buttonStyle == null)
				{
					this.buttonStyle = new GUIStyle(GUI.skin.button);
					this.buttonStyle.alignment = TextAnchor.MiddleLeft;
					this.buttonStyle.stretchWidth = true;
				}

				this.buttonStyle.fixedHeight = Screen.height / 16f;
				this.buttonStyle.fontSize = (int)(this.buttonStyle.fixedHeight * 0.5f);

				this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition);

				//ボタンリスト表示
				for (int i = 0; i < this.buttonNames.Count; i++)
				{
					if (GUILayout.Button((i == this.focusedIndex ? $">> {this.buttonNames[i]}" : this.buttonNames[i]), this.buttonStyle))
					{
						//決定
						this.focusedIndex = i;
						this.OnSubmit();
					}
				}

				GUILayout.EndScrollView();
			}

			/// <summary>
			/// ↑押下時
			/// </summary>
			public override void OnClickUp()
			{
				this.focusedIndex = (int)Mathf.Repeat(this.focusedIndex - 1, this.buttonNames.Count);
			}

			/// <summary>
			/// ↓押下時
			/// </summary>
			public override void OnClickDown()
			{
				this.focusedIndex = (int)Mathf.Repeat(this.focusedIndex + 1, this.buttonNames.Count);
			}

			/// <summary>
			/// ↑スクロール時
			/// </summary>
			public override void OnScrollUp()
			{
				this.scrollPosition.y -= this.buttonStyle.fixedHeight;
			}

			/// <summary>
			/// ↓スクロール時
			/// </summary>
			public override void OnScrollDown()
			{
				this.scrollPosition.y += this.buttonStyle.fixedHeight;
			}
		}

		/// <summary>
		/// セーブデータ一覧表示ステート
		/// </summary>
		private class SaveListState : ListState
		{
			/// <summary>
			/// 表示するセーブスロット一覧
			/// </summary>
			public string[] slotNames = null;

			/// <summary>
			/// 新規作成が可能かどうか
			/// </summary>
			private bool canCreateNew = false;

			/// <summary>
			/// Start
			/// </summary>
			public override void Start()
			{
				this.buttonNames.Add("キャンセル");

				//スロット数に余裕があるなら
				if (this.slotNames.Length < GeneralSaveDataManager.Instance.maxSaveSlotSize)
				{
					//新規作成可能
					this.canCreateNew = true;
					this.buttonNames.Add("新規作成");
				}

				this.buttonNames.AddRange(this.slotNames);
			}

			/// <summary>
			/// 決定時
			/// </summary>
			public override void OnSubmit()
			{
				if (this.focusedIndex == 0)
				{
					//キャンセル
					this.OnCancel();
					return;
				}
				
				string slotName = null;

				if (this.canCreateNew && this.focusedIndex == 1)
				{
					//新規作成
					slotName = GeneralSaveDataManager.GetNewSlotName(SaveSlotType.UserSlot, this.slotNames);
				}
				else
				{
					//上書き保存
					slotName = this.buttonNames[this.focusedIndex];
				}

				//セーブ処理ステートへ
				var nextState = new SaveProcessState { dialog = this.dialog };
				nextState.slotName = slotName;
				this.manager.ChangeState(nextState);
			}

			/// <summary>
			/// キャンセル時
			/// </summary>
			public override void OnCancel()
			{
				this.manager.PopState();
			}
		}

		/// <summary>
		/// セーブ処理ステート
		/// </summary>
		private class SaveProcessState : MyState
		{
			/// <summary>
			/// スロット名
			/// </summary>
			public string slotName = null;

			/// <summary>
			/// Start
			/// </summary>
			public override void Start()
			{
				//セーブ開始
				GeneralSaveDataManager.Instance.Save(this.dialog.saveDataOp, this.dialog.slotParams, this.slotName, () =>
				{
					//終わったらダイアログ閉じる
					this.manager.ChangeState(null);
				});
			}

			/// <summary>
			/// OnGUI
			/// </summary>
			public override void OnGUI()
			{
				GUILayout.Label("Now Saving...");
			}

			/// <summary>
			/// End
			/// </summary>
			public override void End()
			{
				this.dialog.Close();
			}
		}

		/// <summary>
		/// ロードデータ一覧表示ステート
		/// </summary>
		private class LoadListState : ListState
		{
			/// <summary>
			/// 表示するセーブスロット一覧
			/// </summary>
			public string[] slotNames = null;

			/// <summary>
			/// Start
			/// </summary>
			public override void Start()
			{
				this.buttonNames.Add("キャンセル");
				this.buttonNames.AddRange(this.slotNames);
			}

			/// <summary>
			/// 決定時
			/// </summary>
			public override void OnSubmit()
			{
				if (this.focusedIndex == 0)
				{
					//キャンセル
					this.OnCancel();
					return;
				}

				//ロード処理へ
				var nextState = new LoadProcessState { dialog = this.dialog };
				nextState.slotName = this.buttonNames[this.focusedIndex];
				this.manager.ChangeState(nextState);
			}

			/// <summary>
			/// キャンセル時
			/// </summary>
			public override void OnCancel()
			{
				this.manager.PopState();
			}
		}

		/// <summary>
		/// ロード処理ステート
		/// </summary>
		private class LoadProcessState : MyState
		{
			/// <summary>
			/// スロット名
			/// </summary>
			public string slotName = null;

			/// <summary>
			/// Start
			/// </summary>
			public override void Start()
			{
				//ロード開始
				GeneralSaveDataManager.Instance.Load(this.dialog.saveDataOp, this.slotName, () =>
				{
					//終わったらダイアログ閉じる
					this.manager.ChangeState(null);
				});
			}

			/// <summary>
			/// OnGUI
			/// </summary>
			public override void OnGUI()
			{
				GUILayout.Label("Now Loading...");
			}

			/// <summary>
			/// End
			/// </summary>
			public override void End()
			{
				this.dialog.Close();
			}
		}

		/// <summary>
		/// リスト要素選択ステート
		/// </summary>
		private class ViewListState : LoadListState
		{
			/// <summary>
			/// 閉じた時のコールバック
			/// </summary>
			public Action<int> onClose = null;

			/// <summary>
			/// Start
			/// </summary>
			public override void Start()
			{
				base.Start();

				this.dialog.onClose += () =>
				{
					this.onClose?.Invoke(this.focusedIndex - 1);
				};
			}

			/// <summary>
			/// 決定時
			/// </summary>
			public override void OnSubmit()
			{
				//ダイアログ閉じる
				this.manager.PopState();
			}

			/// <summary>
			/// キャンセル時
			/// </summary>
			public override void OnCancel()
			{
				this.focusedIndex = 0;
				this.manager.PopState();
			}
		}
	}
}