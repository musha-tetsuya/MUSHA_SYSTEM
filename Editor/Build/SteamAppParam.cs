using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace KG
{
	/// <summary>
	/// Steam用アプリケーションパラメータ
	/// </summary>
	[CreateAssetMenu(menuName = "KG/ScriptableObject/SteamAppParam")]
	public class SteamAppParam : ScriptableObject
	{
		/// <summary>
		/// AppID
		/// </summary>
		public string appId = "1000";
		
		/// <summary>
		/// DepotID
		/// </summary>
		public string depotId = "1001";

		/// <summary>
		/// アプリバージョン
		/// </summary>
		public string version = "01.00";

		/// <summary>
		/// アプリケーションパラメータファイルの検索
		/// </summary>
		public static SteamAppParam FindAppParam()
		{
			return AssetDatabase
				.FindAssets("t:SteamAppParam")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<SteamAppParam>)
				.FirstOrDefault();
		}
	}
}