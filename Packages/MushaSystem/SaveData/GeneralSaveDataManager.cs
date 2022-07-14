using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using UnityEngine;
using Newtonsoft.Json;

namespace KG
{
	/// <summary>
	/// セーブデータマネージャ
	/// </summary>
	public class GeneralSaveDataManager : SingletonMonoBehaviour<GeneralSaveDataManager>
	{
		/// <summary>
		/// ファイル名
		/// </summary>
		private const string FILENAME = "SaveData.dat";

		/// <summary>
		/// パラメータファイル名
		/// </summary>
		private const string PARAM_FILENAME = "Param.dat";

		/// <summary>
		/// ルートディレクトリ
		/// </summary>
		private static string rootDirectory =>
#if UNITY_EDITOR
			"SaveData";
#elif UNITY_STANDALONE_WIN
			$"{Path.GetDirectoryName(Application.dataPath)}/SaveData";
#else
			$"{Application.persistentDataPath}/SaveData";
#endif

		/// <summary>
		/// 最大スロット数
		/// </summary>
		[SerializeField]
		public int maxSaveSlotSize = 5;

		/// <summary>
		/// 暗号化・複合化用AESオブジェクト
		/// </summary>
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN && STEAMWORKS_NET
		private static AesManaged aes => EncryptUtility.defaultAes;
#else
		private static AesManaged aes => null;
#endif

		/// <summary>
		/// スロット検索
		/// </summary>
		public static (string name, SaveSlotParams param)[] SearchSlot(SaveSlotSearch slotSearch)
		{
			if (!Directory.Exists(rootDirectory))
			{
				return new (string name, SaveSlotParams param)[0];
			}

			return Directory
				.GetDirectories(rootDirectory)
				.Select(Path.GetFileName)
				.Select(dirName =>
				{
					var slotParamsPath = $"{rootDirectory}/{dirName}/{PARAM_FILENAME}";

					SaveSlotParams slotParams = null;

					if (File.Exists(slotParamsPath))
					{
						var json = File.ReadAllText(slotParamsPath);

						if (aes != null)
						{
							//複合化
							json = aes.Decrypt(json);
						}

						slotParams = JsonConvert.DeserializeObject<SaveSlotParams>(json);
					}
					else
					{
						slotParams = new SaveSlotParams();
					}

					return (dirName, slotParams);
				})
				.Where(x => slotSearch.IsMatch(x.dirName, x.slotParams))
				.GroupBy(x =>
				{
					//スロットタイプでグループ分け
					var slotTypeName = Regex.Replace(x.dirName, @"[0-9]", "");
					var slotType = (SaveSlotType)Enum.Parse(typeof(SaveSlotType), slotTypeName);
					return slotType;
				})
				.SelectMany(dirGroup =>
				{
					//スロットの最大数
					int maxSize = 1;

					if (dirGroup.Key == SaveSlotType.UserSlot)
					{
						maxSize = Instance.maxSaveSlotSize;
					}

					if (dirGroup.Count() > maxSize)
					{
						//スロットの最大数を超えているなら取得数を制限
						return dirGroup.Take(maxSize);
					}
					else
					{
						return dirGroup.AsEnumerable();
					}
				})
				.OrderBy(x => x.dirName)
				.ToArray();
		}

		/// <summary>
		/// スロットに最後に保存した日時を取得
		/// </summary>
		public static DateTime GetSlotLastWriteTime(string slotName)
		{
			return new FileInfo($"{rootDirectory}/{slotName}/{FILENAME}").LastWriteTime;
		}

		/// <summary>
		/// セーブスロット名の取得
		/// </summary>
		public static string GetSlotName(SaveSlotType slotType, int slotNo)
		{
			return $"{slotType}{slotNo:0000}";
		}

		/// <summary>
		/// 新規作成するセーブスロット名の取得
		/// </summary>
		public static string GetNewSlotName(SaveSlotType slotType, string[] slotNames)
		{
			int slotNo = 0;

			while (slotNames.Any(x => x == GetSlotName(slotType, slotNo)))
			{
				slotNo++;
			}

			return GetSlotName(slotType, slotNo);
		}

		/// <summary>
		/// セーブ
		/// </summary>
		public void Save(ISaveData saveDataOp, SaveSlotParams slotParams, string slotName, Action onFinished = null)
		{
			StartCoroutine(this.SaveCoroutine(saveDataOp, slotParams, slotName, onFinished));
		}

		/// <summary>
		/// セーブコルーチン
		/// </summary>
		private IEnumerator SaveCoroutine(ISaveData saveDataOp, SaveSlotParams slotParams, string slotName, Action onFinished = null)
		{
			Directory.CreateDirectory($"{rootDirectory}/{slotName}");
			
			string json = JsonConvert.SerializeObject(slotParams, Formatting.Indented);

			if (aes != null)
			{
				//暗号化
				json = aes.Encrypt(json);
			}

			File.WriteAllText($"{rootDirectory}/{slotName}/{PARAM_FILENAME}", json);
			
			using (var fs = File.Open($"{rootDirectory}/{slotName}/{FILENAME}", FileMode.Create, FileAccess.Write))
			{
				saveDataOp.SetFileStream(fs);
				
				saveDataOp.SetAesManaged(aes);

				yield return saveDataOp.Save();
			}

			onFinished?.Invoke();
		}

		/// <summary>
		/// ロード
		/// </summary>
		public void Load(ISaveData saveDataOp, string slotName, Action onFinished = null)
		{
			StartCoroutine(this.LoadCoroutine(saveDataOp, slotName, onFinished));
		}

		/// <summary>
		/// ロードコルーチン
		/// </summary>
		private IEnumerator LoadCoroutine(ISaveData saveDataOp, string slotName, Action onFinished = null)
		{
			if (File.Exists($"{rootDirectory}/{slotName}/{FILENAME}"))
			{
				using (var fs = File.OpenRead($"{rootDirectory}/{slotName}/{FILENAME}"))
				{
					saveDataOp.SetFileStream(fs);

					saveDataOp.SetAesManaged(aes);

					yield return saveDataOp.Load();
				}
			}

			onFinished?.Invoke();
		}
	}
}