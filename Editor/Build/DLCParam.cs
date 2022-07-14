using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KG
{
	/// <summary>
	/// DLCパラメータ
	/// </summary>
	[CreateAssetMenu(menuName = "KG/ScriptableObject/DLCParam")]
	public class DLCParam : ScriptableObject
	{
		/// <summary>
		/// タイトル（PS4, PS5用）
		/// </summary>
		[SerializeField, Tooltip("先頭がデフォルトになる")]
		public TitleName[] titleNames = null;

		/// <summary>
		/// 内包アセット
		/// </summary>
		[SerializeField]
		public UnityEngine.Object[] assets = null;

		/// <summary>
		/// PS4パラメータ
		/// </summary>
		[SerializeField]
		public PS4 ps4 = new PS4();

		/// <summary>
		/// PS5パラメータ
		/// </summary>
		[SerializeField]
		public PS5 ps5 = new PS5();

		/// <summary>
		/// Steamパラメータ
		/// </summary>
		[SerializeField]
		public Steam steam = new Steam();

		/// <summary>
		/// DLC名
		/// </summary>
		public string dlcName => this.name;

		/// <summary>
		/// 有効かどうか
		/// </summary>
		public bool isEnabled =>
#if UNITY_PS4
			this.ps4.enabled;
#elif UNITY_PS5
			this.ps5.enabled;
#elif UNITY_STANDALONE_WIN && STEAMWORKS_NET
			this.steam.enabled;
#else
			false;
#endif


		/// <summary>
		/// OnEnable
		/// </summary>
		private void OnEnable()
		{
			this.ps4.owner = this;
			this.ps5.owner = this;
			this.steam.owner = this;
		}

		/// <summary>
		/// パラメータファイルリストの検索
		/// </summary>
		public static DLCParam[] FindDLCParams(Func<DLCParam, bool> predicate = null)
		{
			var paramList = AssetDatabase
				.FindAssets("t:DLCParam")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<DLCParam>);

			if (predicate != null)
			{
				paramList = paramList.Where(predicate);
			}

			return paramList.ToArray();
		}

		/// <summary>
		/// タイトル名データ
		/// </summary>
		[Serializable]
		public class TitleName
		{
			/// <summary>
			/// 言語
			/// </summary>
			public enum Language
			{
				Japanese,
				English,
				French,
				Spanish,
				German,
				Italian,
				Korean,
				ChineseTraditional
			}

			/// <summary>
			/// 言語
			/// </summary>
			public Language language;

			/// <summary>
			/// テキスト
			/// </summary>
			public string titleName;

			/// <summary>
			/// PS4用言語IDへの変換
			/// </summary>
			public int GetPS4LanguageId()
			{
				switch (this.language)
				{
					case Language.Japanese:           return  0;
					case Language.English:            return  1;
					case Language.French:             return  2;
					case Language.Spanish:            return  3;
					case Language.German:             return  4;
					case Language.Italian:            return  5;
					case Language.Korean:             return  9;
					case Language.ChineseTraditional: return 10;
				}
				return 0;
			}

			/// <summary>
			/// PS5用言語IDへの変換
			/// </summary>
			public string GetPS5LanguageId()
			{
				switch (this.language)
				{
					case Language.Japanese:           return "ja-JP";
					case Language.English:            return "en-US";
					case Language.French:             return "fr-FR";
					case Language.Spanish:            return "es-ES";
					case Language.German:             return "de-DE";
					case Language.Italian:            return "it-IT";
					case Language.Korean:             return "ko-KR";
					case Language.ChineseTraditional: return "zh-Hant";
				}
				return "ja-JP";
			}
		}

		/// <summary>
		/// プラットフォーム基底
		/// </summary>
		public class Platform
		{
			/// <summary>
			/// オーナー
			/// </summary>
			[SerializeField, HideInInspector]
			public DLCParam owner;

			/// <summary>
			/// ビルドするかどうか
			/// </summary>
			[SerializeField]
			public bool enabled;
		}

		/// <summary>
		/// PS4パラメータ
		/// </summary>
		[Serializable]
		public class PS4 : Platform
		{
			/// <summary>
			/// コンテンツパラメータ
			/// </summary>
			[Serializable]
			public class ContentParam
			{
				/// <summary>
				/// タイトルID
				/// </summary>
				[SerializeField]
				public string titleId;

				/// <summary>
				/// バージョン
				/// </summary>
				[SerializeField]
				public string version = "01.00";
			}

			/// <summary>
			/// アイコン
			/// </summary>
			[SerializeField]
			public DefaultAsset icons;

			/// <summary>
			/// コンテンツパラメータ
			/// </summary>
			[SerializeField]
			public ContentParam[] contentParams = new ContentParam[1];

			/// <summary>
			/// エンタイトルメントキー
			/// </summary>
			[SerializeField]
			public string entitlementKey;

			/// <summary>
			/// アプリケーションのコンテンツID取得
			/// 例：JP0117-ABCD12345_00-SAMPLE0000000000
			/// </summary>
			private static string GetApplicationContentId()
			{
#if UNITY_PS4
				return PlayerSettings.PS4.contentID;
#else
				return null;
#endif
			}

			/// <summary>
			/// アプリケーションのタイトルID取得
			/// 例：ABCD12345
			/// </summary>
			public static string GetApplicationTitleId()
			{
				var appContentId = GetApplicationContentId().Split('-');
				return appContentId[1].Split('_')[0];
			}

			/// <summary>
			/// コンテンツIDの取得
			/// </summary>
			public string GetContentId()
			{
				var appContentId = GetApplicationContentId().Split('-');
				return $"{appContentId[0]}-{appContentId[1]}-{this.owner.dlcName}";
			}

			/// <summary>
			/// sfx内容の取得
			/// </summary>
			public string GetParamSfx()
			{
				var contentId =this.GetContentId();
				var titleId = GetApplicationTitleId();
				var param = this.contentParams.FirstOrDefault(_ => _.titleId == titleId) ?? this.contentParams[0];

				return $"<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>\n"
					 + $"<paramsfo>\n"
					 + $"  <param key=\"FORMAT\">obs</param>\n"
					 + $"  <param key=\"CATEGORY\">ac</param>\n"
					 + $"  <param key=\"CONTENT_ID\">{contentId}</param>\n"
					 + $"  <param key=\"TITLE_ID\">{titleId}</param>\n"
					 + $"  <param key=\"VERSION\">{param.version}</param>\n"
					 + $"  <param key=\"TITLE\">{this.owner.titleNames[0].titleName}</param>\n"
					 + this.owner.titleNames.Select(_ => $"  <param key=\"TITLE_{_.GetPS4LanguageId():00}\">{_.titleName}</param>\n").Aggregate((a, b) => a + b)
					 + $"  <param key=\"ATTRIBUTE\">0</param>\n"
					 + $"</paramsfo>";
			}
		}

		/// <summary>
		/// PS5パラメータ
		/// </summary>
		[Serializable]
		public class PS5 : Platform
		{
			/// <summary>
			/// コンテンツパラメータ
			/// </summary>
			[Serializable]
			public class ContentParam
			{
				/// <summary>
				/// タイトルID
				/// </summary>
				[SerializeField]
				public string titleId;

				/// <summary>
				/// コンテンツバージョン
				/// </summary>
				[SerializeField]
				public string contentVersion = "01.000.000";

				/// <summary>
				/// マスターバージョン
				/// </summary>
				[SerializeField]
				public string masterVersion = "01.00";
			}

			/// <summary>
			/// アイコン
			/// </summary>
			[SerializeField]
			public DefaultAsset icons;

			/// <summary>
			/// コンテンツパラメータ
			/// </summary>
			[SerializeField]
			public ContentParam[] contentParams = new ContentParam[1];

			/// <summary>
			/// エンタイトルメントキー
			/// </summary>
			[SerializeField]
			public string entitlementKey;

			/// <summary>
			/// アプリケーションのparamファイル取得
			/// </summary>
			public static JObject GetApplicationParamFile()
			{
				string paramFilePath = null;
#if UNITY_PS5
				paramFilePath = PlayerSettings.PS5.paramFilePath;
#endif
				var json = File.ReadAllText(paramFilePath);
				return JsonConvert.DeserializeObject<JObject>(json);
			}

			/// <summary>
			/// コンテンツIDの取得
			/// </summary>
			public string GetContentId(JObject appParamFile = null)
			{
				if (appParamFile == null)
				{
					appParamFile = GetApplicationParamFile();
				}

				var appContentId = ((string)appParamFile["contentId"]).Split('-');

				return $"{appContentId[0]}-{appContentId[1]}-{this.owner.dlcName}";
			}

			/// <summary>
			/// コンテンツパラメータの取得
			/// </summary>
			public ContentParam GetContentParam(JObject appParamFile = null)
			{
				if (appParamFile == null)
				{
					appParamFile = GetApplicationParamFile();
				}

				var titleId = (string)appParamFile["titleId"];

				return this.contentParams.FirstOrDefault(_ => _.titleId == titleId) ?? this.contentParams[0];
			}

			/// <summary>
			/// json内容の取得
			/// </summary>
			public string GetParamJson(JObject appParamFile = null)
			{
				if (appParamFile == null)
				{
					appParamFile = GetApplicationParamFile();
				}

				var param = this.GetContentParam(appParamFile);

				var paramFile = new JObject();
				paramFile["contentId"] = this.GetContentId(appParamFile);
				paramFile["titleId"] = appParamFile["titleId"];
				paramFile["conceptId"] = appParamFile["conceptId"];
				paramFile["masterVersion"] = param.masterVersion;
				paramFile["localizedParameters"] = new JObject();
				paramFile["localizedParameters"]["defaultLanguage"] = this.owner.titleNames[0].GetPS5LanguageId();
				for (int i = 0; i < this.owner.titleNames.Length; i++)
				{
					var language = this.owner.titleNames[i].GetPS5LanguageId();
					paramFile["localizedParameters"][language] = new JObject();
					paramFile["localizedParameters"][language]["titleName"] = this.owner.titleNames[i].titleName;
				}

				if (this.owner.assets != null && this.owner.assets.Length > 0)
				{
					//※SDK4.0からはエクストラデータ無しでも必要になりそう
					paramFile["contentVersion"] = param.contentVersion;
					paramFile["versionFileUri"] = appParamFile["versionFileUri"];
				}

				return paramFile.ToString();
			}
		}

		/// <summary>
		/// SteamDLCパラメータ
		/// </summary>
		[Serializable]
		public class Steam : Platform
		{
			/// <summary>
			/// デポID
			/// </summary>
			[SerializeField]
			public string depotId = "1001";
		}
	}
}