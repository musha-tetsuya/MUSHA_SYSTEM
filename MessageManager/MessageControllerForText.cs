using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KG
{
	/// <summary>
	/// Textメッセージ制御
	/// </summary>
	public class MessageControllerForText : MessageControllerBase
	{
		/// <summary>
		/// テキスト
		/// </summary>
		[SerializeField]
		public Text text = null;

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