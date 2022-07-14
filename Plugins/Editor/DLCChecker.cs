using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace KG
{
	/// <summary>
	/// DLCチェック
	/// </summary>
	public class DLCChecker
	{
		/// <summary>
		/// アセットパスデータ
		/// </summary>
		public class AssetPathData
		{
			/// <summary>
			/// アセットパス
			/// </summary>
			public string assetPath;

			/// <summary>
			/// 関連するアセットバンドル名一覧
			/// </summary>
			public List<string> assetBundleNameList = new List<string>();
		}

		/// <summary>
		/// DLCデータ
		/// </summary>
		public class DLCData
		{
			/// <summary>
			/// DLC名
			/// </summary>
			public string name;

			/// <summary>
			/// 含まれているアセットパスデータ一覧
			/// </summary>
			public List<AssetPathData> assetPathDataList = new List<AssetPathData>();
		}

		/// <summary>
		/// DLCデータリスト
		/// </summary>
		private List<DLCData> dataList = new List<DLCData>();

		/// <summary>
		/// construct
		/// </summary>
		public DLCChecker()
		{
			//全アセットバンドル名取得
			AssetDatabase.RemoveUnusedAssetBundleNames();
			var allAssetBundleNames = AssetDatabase.GetAllAssetBundleNames();

			//DLCパラメータファイル検索
			var paramList = DLCParam.FindDLCParams();

			foreach (var param in paramList)
			{
				this.AddDLCData(param.dlcName, param.assets, allAssetBundleNames);
			}
		}

		/// <summary>
		/// 指定のアセットバンドルがDLCかどうか
		/// </summary>
		public bool IsDLC(string assetBundleName, out string dlcName)
		{
			for (int i = 0; i < this.dataList.Count; i++)
			{
				for (int j = 0; j < this.dataList[i].assetPathDataList.Count; j++)
				{
					if (this.dataList[i].assetPathDataList[j].assetBundleNameList.Contains(assetBundleName))
					{
						dlcName = this.dataList[i].name;
						return true;
					}
				}
			}

			dlcName = null;
			return false;
		}

		/// <summary>
		/// 指定のアセットがアセットバンドルかどうか
		/// </summary>
		public bool IsAssetBundle(string assetPath)
		{
			for (int i = 0; i < this.dataList.Count; i++)
			{
				for (int j = 0; j < this.dataList[i].assetPathDataList.Count; j++)
				{
					if (this.dataList[i].assetPathDataList[j].assetPath == assetPath)
					{
						return this.dataList[i].assetPathDataList[j].assetBundleNameList.Count > 0;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// DLCデータ追加
		/// </summary>
		private void AddDLCData(string dlcName, UnityEngine.Object[] assets, string[] allAssetBundleNames)
		{
			var dlcData = new DLCData { name = dlcName };
			this.dataList.Add(dlcData);

			if (assets == null)
			{
				return;
			}

			foreach (var assetPath in assets.Select(AssetDatabase.GetAssetPath))
			{
				var assetPathData = new AssetPathData { assetPath = assetPath };
				dlcData.assetPathDataList.Add(assetPathData);

				//DLC対象アセットのアセットバンドル名
				var assetBundleName = AssetDatabase.GetImplicitAssetBundleName(assetPath);

				if (!string.IsNullOrEmpty(assetBundleName))
				{
					assetPathData.assetBundleNameList.Add(assetBundleName);
				}
				else if (AssetDatabase.IsValidFolder(assetPath))
				{
					//子階層のアセットバンドル名取得
					var startName = assetPath.Substring("Assets/".Length).ToLower() + "/";
					var childAssetBundleNames = allAssetBundleNames.Where(x => x.StartsWith(startName)).ToArray();
					assetPathData.assetBundleNameList.AddRange(childAssetBundleNames);
				}
			}
		}
	}
}