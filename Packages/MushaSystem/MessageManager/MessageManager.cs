using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KG
{
	/// <summary>
	/// メッセージデータ管理
	/// </summary>
	public class MessageManager
	{
		/// <summary>
		/// 任意のEnumをサポートしている言語として定義するAttribute
		/// </summary>
		[AttributeUsage(AttributeTargets.Enum)]
		public class SupportedLanguageAttribute : Attribute { }

		/// <summary>
		/// データベース
		/// </summary>
		public static JObject db { get; private set; } = new JObject();

		/// <summary>
		/// サポートしているテキスト言語一覧
		/// </summary>
		public readonly static string[] supportedLanguages = GetSupportedLanguages();

		/// <summary>
		/// 言語
		/// </summary>
		public static string language = supportedLanguages[0];
#if DEBUG
		/// <summary>
		/// メッセージデータではなくExcel名、Sheet名、Keyをそのまま返すフラグ（主にデバッグ用）
		/// </summary>
		public static bool isDebugGetKey = false;
#endif
		/// <summary>
		/// サポートしている言語一覧の取得
		/// </summary>
		private static string[] GetSupportedLanguages()
		{
			var t = Assembly
				.Load("Assembly-CSharp")
				.GetTypes()
				.Where(_ => _.IsEnum && _.GetCustomAttribute<SupportedLanguageAttribute>() != null)
				.FirstOrDefault();

			return (t == null) ? new string[] { "Japanese" } : Enum.GetNames(t);
		}

		/// <summary>
		/// 初期化
		/// </summary>
		public static void Init()
		{
			db = new JObject();
		}

		/// <summary>
		/// ロード
		/// </summary>
		public static void Load(string excelName, string sheetName, string json)
		{
			string dbName = $"{excelName}/{sheetName}";

			if (!db.TryGetValue(dbName, StringComparison.OrdinalIgnoreCase, out JToken sheet))
			{
				db[dbName] = sheet = new JObject();
			}

			var jarray = JsonConvert.DeserializeObject(json) as JArray;

			foreach (var token in jarray)
			{
				var keyData = token["key"];
				if (keyData == null)
				{
					Debug.LogError($"{excelName}/{sheetName}に「key」列が存在しません。");
					break;
				}

				sheet[(string)keyData] = token;
			}
		}

		/// <summary>
		/// アンロード
		/// </summary>
		public static void Unload(string excelName, string sheetName)
		{
			db.Remove($"{excelName}/{sheetName}");
		}

		/// <summary>
		/// メッセージ取得
		/// </summary>
		public static string GetMessage(string excelName, string sheetName, string key)
		{
#if DEBUG
			if (GetHardCodingMessage(excelName, sheetName, key, out string hardCodingMessage))
			{
				if (isDebugGetKey)
				{
					return $"{key}";
				}

				//ハードコーディングメッセージがあるなら返す
				return hardCodingMessage;
			}
#endif
			string dbName = $"{excelName}/{sheetName}";

			if (!db.TryGetValue(dbName, StringComparison.OrdinalIgnoreCase, out JToken sheet))
			{
#if UNITY_EDITOR
				if (!Application.isPlaying)
				{
					TextAsset json = null;

					if (!string.IsNullOrEmpty(excelName) && !string.IsNullOrEmpty(sheetName))
					{

						string jsonPath = AssetDatabase
							.FindAssets($"{sheetName} t:TextAsset", new string[] { "Assets" })
							.Select(AssetDatabase.GUIDToAssetPath)
							.Where(x => x.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
							.FirstOrDefault(x => Path.ChangeExtension(x, null).EndsWith($"{dbName}", StringComparison.OrdinalIgnoreCase));

						json = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
					}

					if (json == null)
					{
						Debug.LogError($"{dbName}に一致するjsonファイルが見つかりません。");
						return null;
					}

					Load(excelName, sheetName, json.text);
				}
				else
#endif
				{
					Debug.LogError($"{dbName}のJsonがセットされていません。");
					return "Message Not Found.";
				}
			}

			if (!(db[dbName] as JObject).TryGetValue(key, out JToken data))
			{
				Debug.LogError($"{dbName}内に{key}に一致するデータは存在しません。");
				return "Message Not Found.";
			}

			if (!(data as JObject).TryGetValue(language, StringComparison.OrdinalIgnoreCase, out JToken text))
			{
				Debug.LogError($"{dbName}/{key}は言語:{language}に対応するデータを持っていません。");
				return "Message Not Found.";
			}

#if DEBUG
			if (isDebugGetKey)
			{
				return $"{key}";
			}
#endif

			return (string)text;
		}

#if DEBUG
		/// <summary>
		/// Excelにメッセージデータを書くのが面倒臭いときのハードコーディングメッセージ用インターフェース（DEBUGのみ）
		/// </summary>
		public interface IHardCodingMessage
		{
			/// <summary>
			/// メッセージ取得
			/// </summary>
			string GetMessage(string excelName, string sheetName, string key);
		}

		/// <summary>
		/// ハードコーディングメッセージインターフェース一覧
		/// </summary>
		private static IHardCodingMessage[] hardCodingMessages = null;

		/// <summary>
		/// ハードコーディングメッセージ取得
		/// </summary>
		private static bool GetHardCodingMessage(string excelName, string sheetName, string key, out string message)
		{
			if (hardCodingMessages == null)
			{
				//定義されているハードコーディングメッセージインターフェース一覧を取得する
				hardCodingMessages = Assembly
					.Load("Assembly-CSharp")
					.GetTypes()
					.Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(i => i == typeof(IHardCodingMessage)))
					.Select(t => Activator.CreateInstance(t) as IHardCodingMessage)
					.ToArray();
			}

			//ハードコーディングメッセージ取得
			message = hardCodingMessages
				.Select(x => x.GetMessage(excelName, sheetName, key))
				.FirstOrDefault(x => x != null);

			//メッセージがあったらtrue
			return message != null;
		}
#endif

#if UNITY_EDITOR
		/// <summary>
		/// 言語設定ウィンドウ
		/// </summary>
		private class SetLanguageWindow : EditorWindow
		{
			/// <summary>
			/// スクロール位置
			/// </summary>
			private Vector2 scrollPosition = Vector2.zero;

			/// <summary>
			/// 言語設定ウィンドウを開く
			/// </summary>
			[MenuItem("KG/SetLanguage")]
			private static void Open()
			{
				GetWindow<SetLanguageWindow>();
			}

			/// <summary>
			/// OnGUI
			/// </summary>
			private void OnGUI()
			{
				GUILayout.Label("テキスト言語");

				this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition);

				for (int i = 0; i < supportedLanguages.Length; i++)
				{
					GUILayout.BeginHorizontal();

					EditorGUILayout.Toggle(supportedLanguages[i] == language, GUILayout.Width(20));

					if (GUILayout.Button(supportedLanguages[i]))
					{
						language = supportedLanguages[i];
						MessageControllerBase.UpdateMessage();
					}

					GUILayout.EndHorizontal();
				}

				GUILayout.EndScrollView();
			}
		}
#endif
	}
}