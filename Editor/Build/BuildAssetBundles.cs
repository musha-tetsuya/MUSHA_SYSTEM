using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

namespace KG
{
	/// <summary>
	/// アセットバンドルビルド
	/// </summary>
	public abstract class BuildAssetBundles
	{
		/// <summary>
		/// バージョン別ステージングエリア
		/// AssetBundles/Staging/{PLATFORM}/{VERSION}
		/// </summary>
		public static string GetStagingArea(string version) => $"AssetBundles/Staging/{EditorUserBuildSettings.activeBuildTarget}/{version}";

		/// <summary>
		/// アプリ用ステージングエリア
		/// AssetBundles/Staging/{PLATFORM}/{VERSION}/App
		/// </summary>
		public static string GetStagingAppArea(string version) => $"{GetStagingArea(version)}/App";

		/// <summary>
		/// DLC用ステージングエリア
		/// AssetBundles/Staging/{PLATFORM}/{VERSION}/{DLC_NAME}
		/// </summary>
		public static string GetStagingDLCArea(string version, string dlcName) => $"{GetStagingArea(version)}/{dlcName}";

		/// <summary>
		/// バッチビルド
		/// </summary>
		[MenuItem("KG/Build/BuildAssetBundles")]
		public static void BatchBuild()
		{
			//バッチモード時、プラットフォーム依存コンパイルが効かない場合があるので、.rspでのカスタムdefine設定で「UNITY_」が必要な場合がある。
			//また、未対応エディタやライセンスが無い場合も考慮して、#ifディレクティブの中でのみ各プラットフォーム専用処理を記述する。

			Debug.Log($"########## Start BuildAssetBundles {EditorUserBuildSettings.activeBuildTarget} ##########");

			//リソースバージョン
			string resourceVersion = BuildApplication.GetAppVersion();

			var args = Environment.GetCommandLineArgs();

			//コマンドライン引数解析
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "-resourceVersion":
					resourceVersion = args[i + 1];
					break;
				}
			}

			//吐き出し先
			var outputPath = AssetManager.editorAssetBundleDirectory;
			Directory.CreateDirectory(outputPath);

			//アセットバンドルビルド
			var manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
			if (manifest == null)
			{
				throw new Exception("########## Error BuildAssetBundles : manifest is null ##########");
			}

			var allAssetBundleName = manifest.GetAllAssetBundles();
			if (allAssetBundleName == null)
			{
				throw new Exception("########## Error BuildAssetBundles : allAssetBundleName is null ##########");
			}

			//アセットバンドル情報リスト作成
			var dlcChecker = new DLCChecker();
			var assetBundleInfoListName = Path.ChangeExtension(AssetManager.ASSETBUNDLE_INFOLIST_NAME, null);
			var assetBundleInfoListHashName = EncryptUtility.ToHashString(AssetManager.ASSETBUNDLE_INFOLIST_NAME);
			var newAssetBundleInfoLists = allAssetBundleName
				.Select(assetBundleName =>
				{
					var fileInfo = new FileInfo($"{outputPath}/{assetBundleName}");
					BuildPipeline.GetCRCForAssetBundle(fileInfo.FullName, out uint crc);

					var assetBundleInfo = new AssetBundleInfo();
					assetBundleInfo.assetBundleName = assetBundleName;
					assetBundleInfo.hashName = EncryptUtility.ToHashString(assetBundleName);
					assetBundleInfo.crc = crc;
					assetBundleInfo.dependencies = manifest.GetDirectDependencies(assetBundleName);
					assetBundleInfo.fileSize = fileInfo.Length;
					assetBundleInfo.isDLC = dlcChecker.IsDLC(assetBundleName, out string dlcName);

					var infoListName = assetBundleInfo.isDLC ? dlcName : assetBundleInfoListName;
					return new { assetBundleInfo, infoListName };
				})
				.GroupBy(x => x.infoListName, x => x.assetBundleInfo)
				.Select(x1 =>
				{
					var infoList = x1.ToList();
					var infoListName = x1.Key;
					var infoListStagingArea = infoList.Exists(x2 => x2.isDLC) ? GetStagingDLCArea(resourceVersion, infoListName) : GetStagingAppArea(resourceVersion);
					return new { infoList, infoListName, infoListStagingArea };
				})
				.ToArray();

			//新アセットバンドル情報リストをキャッシュに出力
			foreach (var newInfoList in newAssetBundleInfoLists)
			{
				//新アセットバンドル情報リスト出力先
				//例：AssetBundles/Cache/{PLATFORM}/assetBundleInfoList.json
				//例：AssetBundles/Cache/{PLATFORM}/{dlcName}.json
				var newInfoListPath = $"{outputPath}/{newInfoList.infoListName}.json";

				//シリアライズ
				var newInfoListJson = JsonConvert.SerializeObject(newInfoList.infoList, Formatting.Indented);

				//出力
				File.WriteAllText(newInfoListPath, newInfoListJson);
			}

			//ステージングディレクトリ作成 例：AssetBundles/Staging/{PLATFORM}/{VERSION}
			var stagingAreaPath = GetStagingArea(resourceVersion);
			Directory.CreateDirectory(stagingAreaPath);

			//ステージングエリア内に必要なサブディレクトリ名一覧
			var usingStagingAreaSubDirNames = new List<string>();
			usingStagingAreaSubDirNames.Add("App");
			usingStagingAreaSubDirNames.AddRange(DLCParam.FindDLCParams(_ => _.isEnabled).Select(_ => _.dlcName));

			//ステージングエリア内の不要なサブディレクトリを削除
			foreach (var subDirPath in Directory.GetDirectories(stagingAreaPath))
			{
				if (!usingStagingAreaSubDirNames.Contains(Path.GetFileName(subDirPath)))
				{
					FileUtil.DeleteFileOrDirectory(subDirPath);
				}
			}

			foreach (var obj in newAssetBundleInfoLists)
			{
				if (!usingStagingAreaSubDirNames.Contains(Path.GetFileName(obj.infoListStagingArea)))
				{
					//不要なアセットバンドルならステージングエリアへの出力はスキップ
					continue;
				}

				//例：AssetBundles/Staging/{PLATFORM}/{VERSION}/App/0123456789abcdef
				//例：AssetBundles/Staging/{PLATFORM}/{VERSION}/{dlcName}/0123456789abcdef
				var infoListJsonHashPath = $"{obj.infoListStagingArea}/{assetBundleInfoListHashName}";

				var oldInfoList = new List<AssetBundleInfo>();

				//古いアセットバンドル情報リストがあるなら
				if (File.Exists(infoListJsonHashPath))
				{
					//読み込み
					var oldInfoListJson = File.ReadAllText(infoListJsonHashPath);
					//複合化
					oldInfoListJson = EncryptUtility.defaultAes.Decrypt(oldInfoListJson);
					//デシリアライズ
					oldInfoList = JsonConvert.DeserializeObject<List<AssetBundleInfo>>(oldInfoListJson);
				}

				//ステージングディレクトリ作成
				//例：AssetBundles/Staging/PS4/01.00/App
				//例：AssetBundles/Staging/PS4/01.00/{DLC_NAME}
				Directory.CreateDirectory(obj.infoListStagingArea);

				//新アセットバンドルでは不要なファイルを削除
				var unusedFiles = Directory
					.GetFiles(obj.infoListStagingArea)
					.Select(Path.GetFileName)
					.Except(obj.infoList.Select(_ => _.hashName));
				foreach (var file in unusedFiles)
				{
					File.Delete($"{obj.infoListStagingArea}/{file}");
				}

				//キャッシュ内のアセットバンドル情報リストをハッシュ名にしてステージングディレクトリに出力
				var newInfoListPath = $"{outputPath}/{obj.infoListName}.json";
				var newInfoListJson = File.ReadAllText(newInfoListPath);
#if UNITY_STANDALONE_WIN && STEAMWORKS_NET
				//Steamはアセットバンドル情報リストを暗号化
				newInfoListJson = EncryptUtility.defaultAes.Encrypt(newInfoListJson);
#endif
				File.WriteAllText(infoListJsonHashPath, newInfoListJson);

				for (int i = 0, imax = obj.infoList.Count; i < imax; i++)
				{
					var newInfo = obj.infoList[i];
					var sourcePath = $"{outputPath}/{newInfo.assetBundleName}";
					var destPath = $"{obj.infoListStagingArea}/{newInfo.hashName}";
					EditorUtility.DisplayProgressBar($"Copy to Staging : {obj.infoListName}", $"{i} / {imax} : {newInfo.assetBundleName}", (float)i / imax);

					//そもそもファイルが存在しないのでコピー
					if (!File.Exists(destPath))
					{
						new FileInfo(sourcePath).CopyTo(destPath, true);
					}
					else
					{
						//CRCに差異があるならコピー
						var oldInfo = oldInfoList.Find(_ => _.hashName == newInfo.hashName);
						if (oldInfo == null || oldInfo.crc != newInfo.crc)
						{
							new FileInfo(sourcePath).CopyTo(destPath, true);
							oldInfoList.Remove(oldInfo);
						}
					}
				}

				EditorUtility.ClearProgressBar();
			}

			Debug.Log($"########## End BuildAssetBundles {EditorUserBuildSettings.activeBuildTarget} ##########");
		}
	}
}