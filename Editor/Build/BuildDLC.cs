using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

namespace KG
{
	/// <summary>
	/// DLCビルド
	/// </summary>
	public abstract class BuildDLC
	{
		/// <summary>
		/// 出力先
		/// </summary>
		protected string outputDirectory = $"DLC/{EditorUserBuildSettings.activeBuildTarget}";

		/// <summary>
		/// DLCチェッカー
		/// </summary>
		private DLCChecker dlcChecker = null;

		/// <summary>
		/// construct
		/// </summary>
		protected BuildDLC()
		{
			this.dlcChecker = new DLCChecker();
		}

		/// <summary>
		/// バッチビルド
		/// </summary>
		[MenuItem("KG/Build/BuildDLC")]
		public static void BatchBuild()
		{
			//バッチモード時、プラットフォーム依存コンパイルが効かない場合があるので、.rspでのカスタムdefine設定で「UNITY_」が必要な場合がある。
			//また、未対応エディタやライセンスが無い場合も考慮して、#ifディレクティブの中でのみ各プラットフォーム専用処理を記述する。

			BuildDLC builder = null;

#if UNITY_PS4
			if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.PS4)
			{
				builder = new BuildDLCPS4();
			}
#endif
#if UNITY_PS5
			if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.PS5)
			{
				builder = new BuildDLCPS5();
			}
#endif
#if UNITY_STANDALONE_WIN && STEAMWORKS_NET
			if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows
			||  EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
			{
				builder = new BuildDLCSteam();
			}
#endif
			if (builder == null)
			{ 
				throw new Exception($"########## Error BuildDLC 未対応プラットフォーム:{EditorUserBuildSettings.activeBuildTarget}, buildTargetとdefineが一致しません ##########");
			}
			
			Debug.Log($"########## Start BuildDLC {EditorUserBuildSettings.activeBuildTarget} ##########");

			builder.Build();

			Debug.Log($"########## End BuildDLC {EditorUserBuildSettings.activeBuildTarget} ##########");
		}

		/// <summary>
		/// ビルド
		/// </summary>
		public virtual void Build(){}

		/// <summary>
		/// コンテンツを詰める
		/// </summary>
		protected Dictionary<string, string> GetAddFiles(string dlcName, UnityEngine.Object[] assets)
		{
			//詰めるファイル一覧の準備
			var files = new Dictionary<string, string>();

			//ステージングDLCアセットバンドルエリア AssetBundles/Staging/PS4/01.00/{dlcName}
			var stagingDLCArea = BuildAssetBundles.GetStagingDLCArea(BuildApplication.GetAppVersion(), dlcName);

			//アセットバンドル情報リスト
			var assetBundleInfoListHashName = EncryptUtility.ToHashString(AssetManager.ASSETBUNDLE_INFOLIST_NAME);//例：0123456789abcdef
			var assetBundleInfoListJsonPath = $"{stagingDLCArea}/{assetBundleInfoListHashName}";//例：AssetBundles/Staging/PS4/01.00/{contentId}/0123456789abcdef
			var assetBundleInfoList = new List<AssetBundleInfo>();
			
			//アセットバンドル情報リストが既存なら
			if (File.Exists(assetBundleInfoListJsonPath))
			{
				//読み込み
				var assetBundleInfoListJson = File.ReadAllText(assetBundleInfoListJsonPath);
				//複合化
				assetBundleInfoListJson = EncryptUtility.defaultAes.Decrypt(assetBundleInfoListJson);
				//デシリアライズ
				assetBundleInfoList = JsonConvert.DeserializeObject<List<AssetBundleInfo>>(assetBundleInfoListJson);
			}

			if (assetBundleInfoList.Count > 0)
			{
				//アセットバンドル情報リストを詰める
				string key = $"{DLCManager.ASSETBUNDLE_DIRECTORY}/{assetBundleInfoListHashName}";//例：AssetBundles/0123456789abcdef
				string path = assetBundleInfoListJsonPath;
				files[key] = path;

				//アセットバンドルを詰める
				for (int i = 0, imax = assetBundleInfoList.Count; i < imax; i++)
				{
					string hash = assetBundleInfoList[i].hashName;//例：0123456789abcdef
					key = $"{DLCManager.ASSETBUNDLE_DIRECTORY}/{hash}";//例：AssetBundles/0123456789abcdef
					path = $"{stagingDLCArea}/{hash}";//例：AssetBundles/Staging/PS4/01.00/{contentId}/0123456789abcdef
					files[key] = path;
				}
			}

			foreach (var asset in assets)
			{
				//アセットのパス
				string assetPath = AssetDatabase.GetAssetPath(asset);

				//アセットバンドルじゃない
				if (!this.dlcChecker.IsAssetBundle(assetPath))
				{
					//生ファイルを含むフォルダの場合
					if (AssetDatabase.IsValidFolder(assetPath))
					{
						//子階層の生ファイル取得
						string[] childFiles = Directory.GetFiles(assetPath, "*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".meta")).ToArray();

						//子階層の生ファイルを詰める
						foreach (var childFile in childFiles)
						{
							assetPath = childFile.Replace(Application.dataPath, "Assets/");
							string key = assetPath;//Assets/Hoge/Fuga/aaa.png
							string path = assetPath;//Assets/Hoge/Fuga/aaa.png
							files[key] = path;
						}
					}
					//生ファイルの場合
					else
					{
						string key = assetPath;//Assets/Hoge/Fuga/aaa.png
						string path = assetPath;//Assets/Hoge/Fuga/aaa.png
						files[key] = path;
					}
				}
			}

			return files;
		}
	}

	/// <summary>
	/// PS4DLCビルド
	/// </summary>
	public class BuildDLCPS4 : BuildDLC
	{
		/// <summary>
		/// 一時作業ディレクトリ
		/// </summary>
		protected const string TMP_DIR = "Temp/PS4DLC";

		/// <summary>
		/// ProcessStartInfo
		/// </summary>
		protected System.Diagnostics.ProcessStartInfo psi { get; private set; }
		
		/// <summary>
		/// エラー
		/// </summary>
		protected Exception exception = null;

		/// <summary>
		/// 初期化
		/// </summary>
		protected void Init(string psiFileName)
		{
			this.psi = new System.Diagnostics.ProcessStartInfo
			{
				FileName = psiFileName,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = Directory.GetCurrentDirectory(),
				RedirectStandardError = true,
			};
		}

#if UNITY_PS4
		/// <summary>
		/// gp4ファイルパス
		/// </summary>
		private const string GP4_PATH = TMP_DIR + "/project.gp4";

		/// <summary>
		/// construct
		/// </summary>
		public BuildDLCPS4() : base()
		{
			this.Init(UnityEditor.PS4.PS4SDKTools.GetTool("orbis-pub-cmd"));
		}

		/// <summary>
		/// ビルド
		/// </summary>
		public override void Build()
		{
			//出力先
			this.outputDirectory += $"/{DLCParam.PS4.GetApplicationTitleId()}";

			//各出力先フォルダ作成
			FileUtil.DeleteFileOrDirectory(TMP_DIR);
			Directory.CreateDirectory(TMP_DIR);
			Directory.CreateDirectory(this.outputDirectory);

			//sfx, sfoパス
			var sfxPath = $"{TMP_DIR}/param.sfx";
			var sfoPath = $"{TMP_DIR}/param.sfo";

			//PS4用DLCパラメータファイルの検索
			var paramList = DLCParam.FindDLCParams(_ => _.isEnabled);

			//パラメータからDLC作成
			for (int i = 0, imax = paramList.Length; i < imax; i++)
			{
				var param = paramList[i];

				EditorUtility.DisplayProgressBar("PS4DLCBuild", param.name, (float)i / imax);

				//コンテンツID取得
				var contentId = param.ps4.GetContentId();

				if (Directory.GetFiles(this.outputDirectory).Select(Path.GetFileName).Any(_ => _.StartsWith(contentId, StringComparison.OrdinalIgnoreCase)))
				{
					Debug.LogWarning($"BuildDLC Warning : {contentId} is already exists.");
					continue;
				}

				if (!UnityEditor.PS4.PS4SDKTools.ValidateContentID(contentId))
				{
					this.exception = new Exception($"Invalid contentId : {contentId}");
					break;
				}

				//sfx出力
				File.WriteAllText(sfxPath, param.ps4.GetParamSfx());

				//sfoファイル作成
				if (!this.SfoCreate(sfxPath, sfoPath))
				{
					Debug.LogError($"SfoCreate : index={i}, paramName={param.name}");
					break;
				}

				if (!this.Gp4ProjCreate(param, contentId))
				{
					Debug.LogError($"Gp4ProjCreate : index={i}, paramName={param.name}");
					break;
				}

				if (!this.Gp4FileAdd(param, sfoPath))
				{
					Debug.LogError($"Gp4FileAdd : index={i}, paramName={param.name}");
					break;
				}

				if (!this.ImgCreate())
				{
					Debug.LogError($"ImgCreate : index={i}, paramName={param.name}");
					break;
				}
			}

			EditorUtility.ClearProgressBar();

			if (this.exception != null)
			{
				//エラー通知
				throw this.exception;
			}
		}

		/// <summary>
		/// SDKToolコマンド処理
		/// </summary>
		private bool RunCommand(string args)
		{
			var errors = new StringBuilder("");

			if (!UnityEditor.PS4.PS4SDKTools.RunCommand2(this.psi, args, errors, null))
			{
				this.exception = new Exception(errors.ToString());
			}

			return this.exception == null;
		}

		/// <summary>
		/// sfoからsfxを生成
		/// </summary>
		private bool SfoExport(string sfoPath, string sfxPath)
		{
			return this.RunCommand($"sfo_export \"{sfoPath}\" \"{sfxPath}\"");
		}

		/// <summary>
		/// sfxからsfoを作成
		/// </summary>
		private bool SfoCreate(string sfxPath, string sfoPath)
		{
			return this.RunCommand($"sfo_create \"{sfxPath}\" \"{sfoPath}\"");
		}

		/// <summary>
		/// .gp4生成
		/// </summary>
		private bool Gp4ProjCreate(DLCParam param, string contentId)
		{
			string volume_type = (param.assets.Length == 0) ? "pkg_ps4_ac_nodata" : "pkg_ps4_ac_data";

			File.Delete(GP4_PATH);

			return this.RunCommand(
				$"gp4_proj_create" +
				$" --volume_type {volume_type}" +
				$" --content_id {contentId}" +
				$" --passcode {PlayerSettings.PS4.passcode}" +
				$" --entitlement_key {param.ps4.entitlementKey}" +
				$" {GP4_PATH}"
			);
		}

		/// <summary>
		/// コンテンツを詰める
		/// </summary>
		private bool Gp4FileAdd(DLCParam param, string sfoPath)
		{
			//詰めるファイル一覧の準備
			var files = this.GetAddFiles(param.dlcName, param.assets);

			//param.sfoを詰める
			files["sce_sys/param.sfo"] = sfoPath;

			//iconを詰める
			var iconFiles = AssetDatabase
				.FindAssets("t:Texture2D", new string[] { AssetDatabase.GetAssetPath(param.ps4.icons) })
				.Select(AssetDatabase.GUIDToAssetPath)
				.ToArray();
			foreach (var iconFile in iconFiles)
			{
				string key = $"sce_sys/{Path.GetFileName(iconFile)}";
				string path = iconFile;
				files[key] = path;
			}

			//ファイルを詰める
			foreach (var file in files)
			{
				bool isSuccess = this.RunCommand(
					$"gp4_file_add" +
					$" \"{file.Value}\"" +
					$" \"{file.Key}\"" +
					$" {GP4_PATH}"
				);
				if (!isSuccess)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// .pkg生成
		/// </summary>
		private bool ImgCreate()
		{
			return this.RunCommand($"img_create --oformat pkg+subitem {GP4_PATH} \"{this.outputDirectory}\"");
		}
#endif
	}

	/// <summary>
	/// PS5DLCビルド
	/// </summary>
	public class BuildDLCPS5 : BuildDLCPS4
	{
#if UNITY_PS5
		/// <summary>
		/// gp5ファイルパス
		/// </summary>
		private const string GP5_PATH = TMP_DIR + "/project.gp5";

		/// <summary>
		/// construct
		/// </summary>
		public BuildDLCPS5() : base()
		{
			this.Init(UnityEditor.PS5.PS5SDKTools.GetTool("prospero-pub-cmd"));
		}

		/// <summary>
		/// ビルド
		/// </summary>
		public override void Build()
		{
			//アプリケーションのparamファイル取得
			var appParamFile = DLCParam.PS5.GetApplicationParamFile();

			//出力先
			this.outputDirectory += $"/{appParamFile["titleId"]}";

			//各出力先フォルダ作成
			FileUtil.DeleteFileOrDirectory(TMP_DIR);
			Directory.CreateDirectory(TMP_DIR);
			Directory.CreateDirectory(this.outputDirectory);

			//PS5用DLCパラメータファイルの検索
			var paramList = DLCParam.FindDLCParams(_ => _.isEnabled);

			//jsonパス
			var paramJsonPath = $"{TMP_DIR}/param.json";

			//パラメータリストからDLC作成
			for (int i = 0, imax = paramList.Length; i < imax; i++)
			{
				var param = paramList[i];

				EditorUtility.DisplayProgressBar("PS5DLCBuild", param.name, (float)i / imax);

				//コンテンツID取得
				var contentId = param.ps5.GetContentId(appParamFile);

				if (!UnityEditor.PS5.PS5SDKTools.ValidateContentID(contentId))
				{
					this.exception = new Exception($"Invalid contentId : {contentId}");
					break;
				}

				//コンテンツパラメータ取得
				var contentParam = param.ps5.GetContentParam(appParamFile);

				//パッケージ名
				var pkgName = $"{contentId}-A{contentParam.contentVersion.Replace(".", "")}-V{contentParam.masterVersion.Replace(".", "")}";

				if (Directory.GetFiles(this.outputDirectory).Select(Path.GetFileName).Any(_ => _.StartsWith(pkgName, StringComparison.OrdinalIgnoreCase)))
				{
					Debug.LogWarning($"BuildDLC Warning : {pkgName} is already exists.");
					continue;
				}

				//json出力
				File.WriteAllText(paramJsonPath, param.ps5.GetParamJson(appParamFile));

				if (param.assets.Length == 0)
				{
					if (!this.CreateZip(param, paramJsonPath, pkgName))
					{
						Debug.LogError($"CreateZip : index={i}, paramName={param.name}");
						break;
					}
					continue;
				}

				if (!this.Gp5ProjCreate(param))
				{
					Debug.LogError($"Gp5ProjCreate : index={i}, paramName={param.name}");
					break;
				}

				if (!this.Gp5FileAdd(param, paramJsonPath))
				{
					Debug.LogError($"Gp5FileAdd : index={i}, paramName={param.name}");
					break;
				}

				if (!this.ImgCreate(pkgName))
				{
					Debug.LogError($"ImgCreate : index={i}, paramName={param.name}");
					break;
				}
			}

			EditorUtility.ClearProgressBar();

			if (this.exception != null)
			{
				//エラー通知
				throw this.exception;
			}
		}

		/// <summary>
		/// SDKToolコマンド処理
		/// </summary>
		private bool RunCommand(string args)
		{
			var errors = new StringBuilder("");

			if (!UnityEditor.PS5.PS5SDKTools.RunCommand2(this.psi, args, errors, null))
			{
				this.exception = new Exception(errors.ToString());
			}

			return this.exception == null;
		}

		/// <summary>
		/// .gp4生成
		/// </summary>
		private bool Gp5ProjCreate(DLCParam param)
		{
			File.Delete(GP5_PATH);

			return this.RunCommand(
				$"gp5_proj_create" +
				$" --volume_type prospero_ac" +
				$" --passcode {PlayerSettings.PS5.passcode}" +
				$" --entitlement_key {param.ps5.entitlementKey}" +
				$" {GP5_PATH}"
			);
		}

		/// <summary>
		/// コンテンツを詰める
		/// </summary>
		private bool Gp5FileAdd(DLCParam param, string paramJsonPath)
		{
			//詰めるファイル一覧の準備
			var files = this.GetAddFiles(param.dlcName, param.assets);

			//param.jsonを詰める
			files["sce_sys/param.json"] = paramJsonPath;

			//iconを詰める
			var iconFiles = AssetDatabase
				.FindAssets("t:Texture2D", new string[] { AssetDatabase.GetAssetPath(param.ps5.icons) })
				.Select(AssetDatabase.GUIDToAssetPath)
				.ToArray();
			foreach (var iconFile in iconFiles)
			{
				string key = $"sce_sys/{Path.GetFileName(iconFile)}";
				string path = iconFile;
				files[key] = path;
			}

			//ファイルを詰める
			foreach (var file in files)
			{
				bool isSuccess = this.RunCommand(
					$"gp5_file_add" +
					$" --src_path \"{file.Value}\"" +
					$" --dst_path \"{file.Key}\"" +
					$" {GP5_PATH}"
				);
				if (!isSuccess)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// .pkg生成
		/// </summary>
		private bool ImgCreate(string pkgName)
		{
			return this.RunCommand($"img_create --for_submission {GP5_PATH} \"{this.outputDirectory}/{pkgName}.pkg\"");
		}

		/// <summary>
		/// エクストラデータなしDLC用Zip作成
		/// </summary>
		private bool CreateZip(DLCParam param, string paramJsonPath, string pkgName)
		{
			if (param.ps5.entitlementKey.Length != 32)
			{
				return false;
			}

			//Ionic.Zipを使うには https://archive.codeplex.com/?p=dotnetzip からDotNetZip.dllが必要
			using (var zip = new Ionic.Zip.ZipFile(Encoding.UTF8))
			using (var jsonStream = new FileStream(paramJsonPath, FileMode.Open))
			{
				//param.jsonの追加
				zip.AddEntry("param.json", jsonStream);

				//iconの追加
				foreach (var iconFile in AssetDatabase.FindAssets("t:Texture2D", new string[] { AssetDatabase.GetAssetPath(param.ps5.icons) }).Select(AssetDatabase.GUIDToAssetPath))
				{
					zip.AddFile(iconFile, "");
				}

				//entitlement.keyの追加
				zip.AddEntry("entitlement.key", Regex.Split(param.ps5.entitlementKey, @"(?<=\G.{2})(?!$)").Select(s => byte.Parse(s, NumberStyles.HexNumber)).ToArray());

				//zip作成
				zip.Save($"{this.outputDirectory}/{pkgName}.zip");
			}

			return true;
		}
#endif
	}

	/// <summary>
	/// SteamDLCビルド
	/// </summary>
	public class BuildDLCSteam : BuildDLC
	{
		/// <summary>
		/// ビルド
		/// </summary>
		public override void Build()
		{
			var appParam = SteamAppParam.FindAppParam();

			//出力先：DLC/StandaloneWindows64/Steam_1000
			this.outputDirectory += $"/Steam_{appParam.appId}";

			//Steam用DLCパラメータファイルの検索
			var dlcParams = DLCParam.FindDLCParams(_ => _.isEnabled);

			//パラメータからDLC作成
			for (int i = 0, imax = dlcParams.Length; i < imax; i++)
			{
				var param = dlcParams[i];

				EditorUtility.DisplayProgressBar("SteamDLCBuild", param.dlcName, (float)i / imax);

				//DLC/StandaloneWindows64/Steam_1000/{dlcName}
				var dlcProjectPath = $"{this.outputDirectory}/{param.dlcName}";

				if (Directory.Exists(dlcProjectPath))
				{
					Debug.LogWarning($"BuildDLC Warning : {dlcProjectPath} is already exists.");
					continue;
				}

				//C:\work\STELLA\Unity\DLC\StandaloneWindows64\Steam_1000\{dlcName}
				var dlcContentRoot = $"{Path.GetDirectoryName(Application.dataPath)}/{dlcProjectPath}".Replace("/", "\\");

				//DLC/StandaloneWindows64/Steam_1000/{dlcName}/depot_1001.vdf
				var dlcScriptPath = $"{dlcProjectPath}/depot_{param.steam.depotId}.vdf";

				//DLC/StandaloneWindows64/Steam_1000/{dlcName}/{depotId}
				var dlcContentPath = $"{dlcProjectPath}/{param.steam.depotId}";
				Directory.CreateDirectory(dlcContentPath);

				//アセットパスとハッシュ名の組み合わせリスト
				var assetHashList = new Dictionary<string, string>();

				var files = this.GetAddFiles(param.dlcName, param.assets);

				foreach (var file in files)
				{
					//例：AssetBundles/Staging/StandaloneWindows64/01.00/{dlcName}/0123456789abcdef
					//例：Assets/Hoge/Fuga/aaa.png
					var fileSourcePath = file.Value;

					var fileDestPath = "";

					if (fileSourcePath.StartsWith("AssetBundles/", StringComparison.Ordinal))
					{
						//例：DLC/StandaloneWindows64/Steam/{dlcName}/{depotId}/0123456789abcdef
						fileDestPath = $"{dlcContentPath}/{Path.GetFileName(fileSourcePath)}";
					}
					else
					{
						//アセットパスとハッシュ名を関連付ける
						assetHashList[fileSourcePath] = EncryptUtility.ToHashString(fileSourcePath);

						//例：DLC/StandaloneWindows64/Steam/{dlcName}/{depotId}/0123456789abcdef
						fileDestPath = $"{dlcContentPath}/{assetHashList[fileSourcePath]}";
					}

					File.Copy(fileSourcePath, fileDestPath);
				}

				//アセットハッシュリストの書き出しが必要なら
				if (assetHashList.Count > 0)
				{
					//シリアライズ
					var assetHashListJson = JsonConvert.SerializeObject(assetHashList, Formatting.Indented);
					//暗号化
					assetHashListJson = EncryptUtility.defaultAes.Encrypt(assetHashListJson);
					//書き出し　例：DLC/StandaloneWindows64/Steam/{dlcName}/{depotId}/0123456789abcdef
					File.WriteAllText($"{dlcContentPath}/{EncryptUtility.ToHashString(DLCManager.DLCDataSteam.ASSET_HASHLIST_NAME)}", assetHashListJson);
				}

				//vdfスクリプト作成
				var sb = new StringBuilder();
				sb.AppendLine($"\"DepotBuild\"");
				sb.AppendLine($"{{");
				sb.AppendLine($"    \"DepotID\" \"{param.steam.depotId}\"");
				sb.AppendLine($"    \"ContentRoot\" \"{dlcContentRoot}\"");
				sb.AppendLine($"    \"FileMapping\"");
				sb.AppendLine($"    {{");
				sb.AppendLine($"        \"LocalPath\" \"*\"");
				sb.AppendLine($"        \"DepotPath\" \"DLC\\\"");
				sb.AppendLine($"        \"Recursive\" \"1\"");
				sb.AppendLine($"    }}");
				sb.AppendLine($"    \"FileExclusion\" \"depot_{param.steam.depotId}.vdf\"");
				sb.AppendLine($"}}");
				File.WriteAllText(dlcScriptPath, sb.ToString());
			}

			EditorUtility.ClearProgressBar();
		}
	}
}