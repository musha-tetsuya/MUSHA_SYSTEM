using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KG
{
	/// <summary>
	/// メッセージ制御基底
	/// </summary>
	public abstract class MessageControllerBase : MonoBehaviour
	{
		/// <summary>
		/// Excel名
		/// </summary>
		[SerializeField]
		public string excelName = null;

		/// <summary>
		/// Sheet名
		/// </summary>
		[SerializeField]
		public string sheetName = null;

		/// <summary>
		/// キー
		/// </summary>
		[SerializeField]
		public string key = null;

		/// <summary>
		/// テキストオブジェクト
		/// </summary>
		protected abstract UnityEngine.Object textObject { get; }

		/// <summary>
		/// Start
		/// </summary>
		protected virtual void Start()
		{
			this.SetMessage();
		}

		/// <summary>
		/// メッセージ取得
		/// </summary>
		public string GetMessage()
		{
			return MessageManager.GetMessage(this.excelName, this.sheetName, this.key);
		}

		/// <summary>
		/// メッセージセット
		/// </summary>
		public abstract void SetMessage();

		/// <summary>
		/// メッセージ更新
		/// </summary>
		public static void UpdateMessage()
		{
			foreach (var obj in FindObjectsOfType<MessageControllerBase>())
			{
				obj.SetMessage();
			}
#if UNITY_EDITOR
			if (!EditorApplication.isPlaying)
			{
				//再生してない時、TextMeshProの描画が更新されないので手動で更新をかける
				EditorApplication.QueuePlayerLoopUpdate();
			}
#endif
		}

#if UNITY_EDITOR
		/// <summary>
		/// カスタムインスペクター
		/// </summary>
		[CustomEditor(typeof(MessageControllerBase), true)]
		private class MyInspector : Editor
		{
			/// <summary>
			/// OnInspectorGUI
			/// </summary>
			public override void OnInspectorGUI()
			{
				var t = this.target as MessageControllerBase;
				var prevTextObject = t.textObject;
				var prevExcelName = t.excelName;
				var prevSheetName = t.sheetName;
				var prevKey = t.key;

				base.OnInspectorGUI();

				if (t.textObject != prevTextObject
				||  t.excelName != prevExcelName
				||  t.sheetName != prevSheetName
				||  t.key != prevKey)
				{
					//設定に変更があったらメッセージ表示更新
					t.SetMessage();
				}
			}
		}
#endif
	}
}