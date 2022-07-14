using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KG
{
	/// <summary>
	/// セーブスロットタイプ
	/// </summary>
	public enum SaveSlotType
	{
		/// <summary>
		/// ユーザーセーブスロット
		/// </summary>
		UserSlot,

		/// <summary>
		/// オートセーブスロット
		/// </summary>
		AutoSlot,

		/// <summary>
		/// システムセーブスロット
		/// </summary>
		SystemSlot
	}

	/// <summary>
	/// セーブスロットパラメータ
	/// </summary>
	public class SaveSlotParams
	{
		/// <summary>
		/// 新規作成データアイテム表示時のタイトル（現状PS4/PS5用）
		/// </summary>
		[NonSerialized]
		public string newItemTitle;

		/// <summary>
		/// 新規作成データアイテムアイコンPNG（画面スクショとか）（現状PS4/PS5用）
		/// </summary>
		[NonSerialized]
		public byte[] newItemRawPNG;

		/// <summary>
		/// セーブデータタイトル
		/// </summary>
		public string title;

		/// <summary>
		/// セーブデータサブタイトル
		/// </summary>
		public string subTitle;

		/// <summary>
		/// セーブデータ詳細
		/// </summary>
		public string detail;

		/// <summary>
		/// ユーザーパラメータ
		/// </summary>
		public uint userParam;

		/// <summary>
		/// スロット番号決定時（現状PS4/PS5用）
		/// </summary>
		public virtual void OnSetSlotNo(int slotNo) { }
	}

	/// <summary>
	/// セーブスロット検索
	/// </summary>
	public sealed class SaveSlotSearch
	{
		/// <summary>
		/// 検索対象スロットタイプ文字列
		/// </summary>
		private string[] slotTypeNames = null;

		/// <summary>
		/// パラメータ一致条件
		/// </summary>
		private Func<SaveSlotParams, bool> match = (_) => true;

		/// <summary>
		/// construct
		/// </summary>
		public SaveSlotSearch(SaveSlotType[] slotTypes, Func<SaveSlotParams, bool> match = null)
		{
			this.slotTypeNames = slotTypes.Select(x => x.ToString()).ToArray();

			if (match != null)
			{
				this.match = match;
			}
		}

		/// <summary>
		/// 対象スロットが条件にあてはまっているかどうか
		/// </summary>
		public bool IsMatch(string slotName, SaveSlotParams param)
		{
			var slotTypeName = Regex.Replace(slotName, @"[0-9]", "");

			return this.slotTypeNames.Contains(slotTypeName) && this.match(param);
		}
	}
}
