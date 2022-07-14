using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;

namespace KG
{
	/// <summary>
	/// デバッグメニューボタン
	/// </summary>
	[Serializable]
	public class DebugMenuButton
	{
		/// <summary>
		/// ボタン名
		/// </summary>
		public string name = null;

		/// <summary>
		/// クリック時コールバック
		/// </summary>
		public UnityEvent onClick = new UnityEvent();

		/// <summary>
		/// 値取得
		/// </summary>
		public Func<object> getValue = null;

		/// <summary>
		/// construct
		/// </summary>
		public DebugMenuButton(string name, UnityAction onClick, Func<object> getValue = null)
		{
			this.name = name;
			this.onClick.AddListener(onClick);
			this.getValue = getValue;
		}

		/// <summary>
		/// ボタン名取得
		/// </summary>
		public string GetName() => this.getValue == null ? this.name : $"{this.name}:{this.getValue()}";
	}

	/// <summary>
	/// デバッグメニューステート
	/// </summary>
	public class DebugMenuState : StateObject
	{
		/// <summary>
		/// OnGUI
		/// </summary>
		public virtual void OnGUI() { }

		/// <summary>
		/// GUIスタイルセット
		/// </summary>
		protected void SetGUIStyle(GUIStyle style) => DebugMenu.Instance.SetGUIStyle(style);
	}

	/// <summary>
	/// デバッグメニューボタン一覧ステート
	/// </summary>
	[Serializable]
	public class DebugMenuButtonListState : DebugMenuState
	{
		/// <summary>
		/// タイトル
		/// </summary>
		[SerializeField]
		public string title = null;

		/// <summary>
		/// ボタンリスト
		/// </summary>
		[SerializeField]
		public List<DebugMenuButton> buttons = new List<DebugMenuButton>();

		/// <summary>
		/// フォーカス中ボタン番号
		/// </summary>
		[NonSerialized]
		public int focusedButtonNo = 0;

		/// <summary>
		/// スクロール位置
		/// </summary>
		[NonSerialized]
		public Vector2 scrollPosition;

		/// <summary>
		/// BoxGUIスタイル
		/// </summary>
		private GUIStyle boxStyle = null;

		/// <summary>
		/// ボタンGUIスタイル
		/// </summary>
		public GUIStyle buttonStyle { get; private set; }

		/// <summary>
		/// Start
		/// </summary>
		public override void Start()
		{
			//閉じるボタンを先頭に追加
			this.buttons.Insert(0, new DebugMenuButton("閉じる", () => this.manager.PopState()));
		}

		/// <summary>
		/// OnGUI
		/// </summary>
		public override void OnGUI()
		{
			if (this.boxStyle == null)
			{
				this.boxStyle = new GUIStyle(GUI.skin.box);
				this.boxStyle.alignment = TextAnchor.MiddleCenter;
			}

			if (this.buttonStyle == null)
			{
				this.buttonStyle = new GUIStyle(GUI.skin.button);
				this.buttonStyle.alignment = TextAnchor.MiddleLeft;
			}

			this.SetGUIStyle(this.boxStyle);
			this.SetGUIStyle(this.buttonStyle);

			if (!string.IsNullOrEmpty(this.title))
			{
				//タイトル表示
				GUILayout.Box(this.title, this.boxStyle);
			}

			this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition);

			//ボタンリスト表示
			for (int i = 0; i < this.buttons.Count; i++)
			{
				var buttonName = this.buttons[i].GetName();

				if (i == this.focusedButtonNo)
				{
					//フォーカス中ボタンは矢印で強調
					buttonName = $">> {buttonName}";
				}

				if (GUILayout.Button(buttonName, this.buttonStyle))
				{
					this.focusedButtonNo = i;
					this.InvokeButton();
				}
			}

			GUILayout.EndScrollView();
		}

		/// <summary>
		/// ボタン発火
		/// </summary>
		public void InvokeButton()
		{
			this.buttons[this.focusedButtonNo].onClick?.Invoke();
		}
	}

	/// <summary>
	/// デバッグメニュー
	/// </summary>
	public class DebugMenu : MonoBehaviour
	{
		/// <summary>
		/// インスタンス
		/// </summary>
		public static DebugMenu Instance { get; private set; }

		/// <summary>
		/// オープンコマンド
		/// </summary>
		private static Func<bool> openCommand = null;

		/// <summary>
		/// 追加メニューボタンリスト
		/// </summary>
		private static List<DebugMenuButton> additionalButtons = new List<DebugMenuButton>();

		/// <summary>
		/// トップメニュー
		/// </summary>
		[SerializeField]
		protected DebugMenuButtonListState topMenu = null;
		
		/// <summary>
		/// ステート管理
		/// </summary>
		public StateManager stateManager { get; private set; } = new StateManager();

		/// <summary>
		/// Awake
		/// </summary>
		protected virtual void Awake()
		{
			//追加メニューボタンリストを連結
			this.topMenu.buttons.AddRange(additionalButtons);

			//トップメニュー開く
			this.stateManager.PushState(this.topMenu, () => Destroy(this.gameObject));
		}

		/// <summary>
		/// OnDestroy
		/// </summary>
		protected virtual void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		/// <summary>
		/// OnGUI
		/// </summary>
		protected virtual void OnGUI()
		{
			(this.stateManager.currentState as DebugMenuState)?.OnGUI();
		}

		/// <summary>
		/// GUIスタイルセット
		/// </summary>
		public virtual void SetGUIStyle(GUIStyle style)
		{
			style.fixedHeight = Screen.height / 16f;
			style.stretchWidth = true;
			style.fontSize = (int)(style.fixedHeight * 0.5f);
		}

		/// <summary>
		/// コマンド入力確認
		/// </summary>
		[Conditional("DEBUG")]
		public static void UpdateOpenCommand(DebugMenu prefab, Transform parent)
		{
			if (Instance == null && openCommand != null && openCommand())
			{
				Instance = Instantiate(prefab, parent);
			}
		}

		/// <summary>
		/// オープンコマンドのセット
		/// </summary>
		[Conditional("DEBUG")]
		public static void SetOpenCommand(Func<bool> _openCommand)
		{
			openCommand = _openCommand;
		}

		/// <summary>
		/// ボタン追加
		/// </summary>
		[Conditional("DEBUG")]
		public static void AddButton(DebugMenuButton button)
		{
			additionalButtons.Add(button);
		}

		/// <summary>
		/// ボタン除去
		/// </summary>
		[Conditional("DEBUG")]
		public static void RemoveAdditionalButton(DebugMenuButton button)
		{
			additionalButtons.Remove(button);
		}

		/// <summary>
		/// ボタン除去
		/// </summary>
		[Conditional("DEBUG")]
		public static void RemoveAdditionalButton(string buttonName)
		{
			additionalButtons.RemoveAll(x => x.name == buttonName);
		}

		/// <summary>
		/// 追加ボタンリストクリア
		/// </summary>
		[Conditional("DEBUG")]
		public static void ClearAdditionalButton()
		{
			additionalButtons.Clear();
		}
	}
}