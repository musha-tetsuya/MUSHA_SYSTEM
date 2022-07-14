using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KG
{
    /// <summary>
    /// アセットバンドル情報
    /// </summary>
    public class AssetBundleInfo
    {
        /// <summary>
        /// アセットバンドル名
        /// </summary>
        public string assetBundleName;

        /// <summary>
        /// ハッシュ名
        /// </summary>
        public string hashName;

        /// <summary>
        /// CRC
        /// </summary>
        public uint crc;

        /// <summary>
        /// 依存関係
        /// </summary>
        public string[] dependencies;

        /// <summary>
        /// ファイルサイズ
        /// </summary>
        public long fileSize;

		/// <summary>
		/// DLCかどうか
		/// </summary>
		public bool isDLC;
    }

	/// <summary>
	/// アセットバンドル情報リスト拡張
	/// </summary>
	public static class AssetBundleInfoListExtension
	{
		/// <summary>
		/// アセットバンドル情報の検索
		/// </summary>
		public static AssetBundleInfo Find(this List<AssetBundleInfo> list, string path)
		{
			//バックスラッシュは通常のスラッシュに直す
			path = path.Replace('\\', '/');

			int imax = list.Count;

			//パスとアセットバンドル名の完全一致検索
			for (int i = 0; i < imax; i++)
			{
				if (path.Equals(list[i].assetBundleName, StringComparison.OrdinalIgnoreCase))
				{
					return list[i];
				}
			}

			string lowerPath = path.ToLower();

			//パスがアセットバンドル名を含んでいるか検索
			for (int i = 0; i < imax; i++)
			{
				if (lowerPath.StartsWith($"{list[i].assetBundleName}/", StringComparison.Ordinal))
				{
					return list[i];
				}
			}

			return null;
		}
	}
}
