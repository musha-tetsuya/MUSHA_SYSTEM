using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KG
{
	/// <summary>
	/// TextMeshProUGUIメッセージ制御
	/// </summary>
	public class MessageControllerForTMProUI : MessageControllerBase
	{
		/// <summary>
		/// テキスト
		/// </summary>
		[SerializeField]
		public TextMeshProUGUI text = null;

		/// <summary>
		/// テキストオブジェクト
		/// </summary>
		protected override UnityEngine.Object textObject => this.text;

		/// <summary>
		/// メッセージセット
		/// </summary>
		public override void SetMessage()
		{
			if (this.text != null)
			{
				this.text.text = this.GetMessage();
			}
		}
	}
}