using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KG
{
    /// <summary>
    /// マスターデータ基底
    /// </summary>
    public abstract class MasterModel
    {
        /// <summary>
        /// ID
        /// </summary>
        public int id;

		/// <summary>
		/// データ検証
		/// </summary>
		public virtual void Validate() { }

		/// <summary>
		/// データ検証
		/// </summary>
		protected void Validate(string checkName, int checkId, IMasterDB db, bool condition = true)
		{
			Debug.Assert(!condition || db.HasId(checkId), this.GetValidateErrorLog(checkName, checkId, db.jsonPath));
		}

		/// <summary>
		/// データ検証
		/// </summary>
		protected void Validate<T>(string checkName, object checkValue, IMasterDB<T> db, Predicate<T> match, bool condition = true) where T : MasterModel
		{
			Debug.Assert(!condition || db.Validate(match), this.GetValidateErrorLog(checkName, checkValue, db.jsonPath));
		}

		/// <summary>
		/// バリデーションエラーログ取得
		/// </summary>
		protected string GetValidateErrorLog(string checkName, object checkValue, string dbName)
		{
			return $"{this.GetType()}[{this.id}].{checkName}={checkValue} is not contains in {dbName}";
		}
    }

	/// <summary>
	/// マスターデータベースインターフェース
	/// </summary>
	public interface IMasterDB
	{
		/// <summary>
		/// Jsonファイル名
		/// </summary>
		string jsonName { get; }

		/// <summary>
		/// Jsonパス
		/// </summary>
		string jsonPath { get; }

		/// <summary>
		/// Jsonのセット
		/// </summary>
		void SetJson(string json);

		/// <summary>
		/// IDが存在するかどうか
		/// </summary>
		bool HasId(int id);

		/// <summary>
		/// データ検証
		/// </summary>
		void Validate();
	}

	/// <summary>
	/// マスターデータベースインターフェース
	/// </summary>
	public interface IMasterDB<T> : IMasterDB where T : MasterModel
	{
		/// <summary>
		/// データ検証
		/// </summary>
		bool Validate(Predicate<T> match);
	}

	/// <summary>
	/// マスターデータベース基底
	/// </summary>
	public abstract class MasterDB<InstanceType, ModelType, ListType> : IMasterDB<ModelType>
		where InstanceType : new()
		where ModelType : MasterModel
		where ListType : class, IEnumerable, ICollection
	{
		/// <summary>
		/// インスタンス
		/// </summary>
		public readonly static InstanceType Instance = new InstanceType();

		/// <summary>
		/// Jsonファイル名
		/// </summary>
		public abstract string jsonName { get; }

		/// <summary>
		/// Jsonパス
		/// </summary>
		public abstract string jsonPath { get; }

		/// <summary>
		/// データリスト
		/// </summary>
		private ListType dataList = null;

		/// <summary>
		/// データリスト
		/// </summary>
		public ListType DataList
		{
			get
			{
#if UNITY_EDITOR
				if (this.dataList == null && !Application.isPlaying)
				{
					var jsonAssetPath = AssetDatabase
						.FindAssets($"{Path.GetFileNameWithoutExtension(this.jsonName)} t:TextAsset", new string[] { "Assets" })
						.Select(AssetDatabase.GUIDToAssetPath)
						.Where(x => !x.EndsWith(".cs"))
						.FirstOrDefault(x => Path.ChangeExtension(x, null).EndsWith(this.jsonPath, StringComparison.OrdinalIgnoreCase));

					if (!string.IsNullOrEmpty(jsonAssetPath))
					{
						var json = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonAssetPath);
						this.SetJson(json.text);
					}
				}
#endif
				return this.dataList;
			}
		}

		/// <summary>
		/// データ辞書
		/// </summary>
		private Dictionary<int, ModelType> dataDic = new Dictionary<int, ModelType>();

		/// <summary>
		/// データリストのセット
		/// </summary>
		public void SetDataList(ListType dataList)
		{
			this.dataList = dataList;
		}

		/// <summary>
		/// Jsonのセット
		/// </summary>
		public abstract void SetJson(string json);

		/// <summary>
		/// JsonからListに
		/// </summary>
		protected List<ModelType> ToList(string json)
		{
			var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ModelType>>(json);
			this.dataDic = list.ToDictionary(x => x.id, x => x);
			return list;

		}

		/// <summary>
		/// JsonからDictionaryに
		/// </summary>
		protected Dictionary<TKey, ModelType> ToDictionary<TKey>(string json, Func<ModelType, TKey> keySelector)
		{
			var list = Newtonsoft.Json.JsonConvert.DeserializeObject<ModelType[]>(json);
			this.dataDic = list.ToDictionary(x => x.id, x => x);
			return list.ToDictionary(keySelector);
		}

		/// <summary>
		/// IDで取得
		/// </summary>
		public ModelType GetById(int id) => this.dataDic.TryGetValue(id, out var data) ? data : null;

		/// <summary>
		/// IDが存在するかどうか
		/// </summary>
		public bool HasId(int id)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				var collection = (this.DataList is IDictionary _DataList) ? _DataList.Values : this.DataList;

				foreach (ModelType data in collection)
				{
					if (data.id == id)
					{
						return true;
					}
				}
				return false;
			}
#endif
			return this.dataDic.ContainsKey(id);
		}

		/// <summary>
		/// データ検証
		/// </summary>
		void IMasterDB.Validate()
		{
			var collection = (this.DataList is IDictionary _DataList) ? _DataList.Values : this.DataList;

			foreach (ModelType data in collection)
			{
				data.Validate();
			}
		}

		/// <summary>
		/// データ検証
		/// </summary>
		bool IMasterDB<ModelType>.Validate(Predicate<ModelType> match)
		{
			var collection = (this.DataList is IDictionary _DataList) ? _DataList.Values : this.DataList;

			foreach (ModelType data in collection)
			{
				if (match(data))
				{
					return true;
				}
			}
			return false;
		}
    }

	/// <summary>
	/// マスターデータユーティリティ
	/// </summary>
	public static class MasterDataUtility
	{
		/// <summary>
		/// 定義されているマスターDB一覧を取得する
		/// </summary>
		public static IMasterDB[] GetMasterDBList()
		{
			return Assembly.Load("Assembly-CSharp")
				.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(i => i == typeof(IMasterDB)))
				.Select(t =>t.BaseType.GetField("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null) as IMasterDB)
				.ToArray();
		}

#if UNITY_EDITOR
		/// <summary>
		/// マスターデータ検証
		/// </summary>
		[MenuItem("KG/MasterData Validate")]
		private static void Validate()
		{
			//コンパイル終わったらマスターデータ検証を開始する
			Action<object> compilationFinished = null;
			compilationFinished = (_) =>
			{
				try
				{
					var masterDBs = GetMasterDBList();

					for (int i = 0; i < masterDBs.Length; i++)
					{
						EditorUtility.DisplayProgressBar("MasterData Validate", masterDBs[i].jsonPath, (float)i / masterDBs.Length);

						masterDBs[i].Validate();
					}
				}
				catch (Exception e)
				{
					Debug.LogError(e);
				}

				EditorUtility.ClearProgressBar();

				UnityEditor.Compilation.CompilationPipeline.compilationFinished -= compilationFinished;
			};

			EditorUtility.DisplayProgressBar("Script Compiling...", "Please Wait...", 0.5f);

			//staticインスタンスを解放するために強制的にコンパイルを走らせる
			UnityEditor.Compilation.CompilationPipeline.compilationFinished += compilationFinished;
			UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
		}
#endif
	}
}
