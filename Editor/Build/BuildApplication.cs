using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace KG
{
	/// <summary>
	/// アプリケーションビルド
	/// </summary>
	public abstract class BuildApplication
	{
		/// <summary>
		/// ビルドモード
		/// </summary>
		public enum BuildMode
		{
			Debug,      //デバッグ
			Release,    //リリース
			Production, //製品
		}

		/// <summary>
		/// ビルドモード
		/// </summary>
		protected BuildMode buildMode { get; private set; } = BuildMode.Debug;

		/// <summary>
		/// ビルド番号
		/// </summary>
		protected int buildNumber { get; private set; } = 1;

		/// <summary>
		/// 出力先
		/// </summary>
		protected string outputDirectory = $"Bin/{EditorUserBuildSettings.activeBuildTarget}";

		/// <summary>
		/// ビルドオプション値
		/// </summary>
		protected BuildOptions buildOptions = BuildOptions.None;

		/// <summary>
		/// ビルド結果
		/// </summary>
		private BuildReport buildReport = null;

		/// <summary>
		/// 出力パス
		/// </summary>
		protected abstract string locationPathName { get; }

		/// <summary>
		/// バッチビルド
		/// </summary>
		[MenuItem("KG/Build/BuildApplication")]
		public static void BatchBuild()
		{
			//バッチモード時、プラットフォーム依存コンパイルが効かない場合があるので、.rspでのカスタムdefine設定で「UNITY_」が必要な場合がある。
			//また、未対応エディタやライセンスが無い場合も考慮して、#ifディレクティブの中でのみ各プラットフォーム専用処理を記述する。

			BuildApplication builder = null;

#if UNITY_PS4
			if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.PS4)
			{
				builder = new BuildApplicationPS4();
			}
#endif
#if UNITY_PS5
			if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.PS5)
			{
				builder = new BuildApplicationPS5();
			}
#endif
#if UNITY_STANDALONE_WIN
			if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows
			||  EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
			{
#if STEAMWORKS_NET
				builder = new BuildApplicationSteam();
#else
				builder = new BuildApplicationStandaloneWindows();
#endif
			}
#endif
			if (builder == null)
			{
				throw new Exception($"########## Error BuildApplication 未対応プラットフォーム:{EditorUserBuildSettings.activeBuildTarget}, buildTargetとdefineが一致しません ##########");
			}
			
			Debug.Log($"########## Start BuildApplication {EditorUserBuildSettings.activeBuildTarget} ##########");

			builder.Build();

			Debug.Log($"########## End BuildApplication {EditorUserBuildSettings.activeBuildTarget} ##########");
		}

		/// <summary>
		/// ビルド
		/// </summary>
		public void Build()
		{
			//コマンドライン引数解析
			this.AnalysisCommandLineArgs();

			//development設定
			EditorUserBuildSettings.development = this.buildMode == BuildMode.Debug;

			if (EditorUserBuildSettings.development)
			{
				//ビルドオプション値
				this.buildOptions |= BuildOptions.Development;
				this.buildOptions |= BuildOptions.ConnectWithProfiler;
				this.buildOptions |= BuildOptions.AllowDebugging;
			}

			//アプリバージョンの設定
			PlayerSettings.bundleVersion = GetAppVersion();

			//ビルド実行
			this.BuildPlayer();

			if (this.buildReport)
			{
				if (this.buildReport.summary.result != BuildResult.Succeeded)
				{
					//ビルド失敗
					throw new Exception($"########## Error BuildApplication : BuildResult is {this.buildReport.summary.result}");
				}

				if (!Application.isBatchMode)
				{
					//ビルド出力したフォルダを開く
					EditorUtility.RevealInFinder(this.buildReport.summary.outputPath);
				}
			}
		}

		/// <summary>
		/// コマンドライン引数取得
		/// </summary>
		protected virtual List<string> GetCommandLineArgs()
		{
			var args = new List<string>(Environment.GetCommandLineArgs());

			if (!Application.isBatchMode)
			{
				//ビルドモード選択
				var _buildMode = (BuildMode)EditorUtility.DisplayDialogComplex(
					"BuildApplication",
					"ビルドモードを選択して下さい。",
					"Debug",
					"Release",
					"Production"
				);
				args.Add("-buildMode");
				args.Add(_buildMode.ToString());
				Debug.Log($"buildMode={_buildMode}");
			}

			return args;
		}

		/// <summary>
		/// コマンドライン引数解析
		/// </summary>
		private void AnalysisCommandLineArgs()
		{
			var args = this.GetCommandLineArgs();

			for (int i = 0; i < args.Count; i++)
			{
				switch (args[i])
				{
					case "-buildMode":
					{
						this.buildMode = (BuildMode)Enum.Parse(typeof(BuildMode), args[i + 1]);
					}
					break;

					case "-buildNumber":
					{
						this.buildNumber = int.Parse(args[i + 1]);
					}
					break;

					case "-outputDirectory":
					{
						this.outputDirectory += $"/{args[i + 1]}";
					}
					break;

					case "-addDefineSymbols":
					{
						//既に設定されているDefineSymbols取得
						var oldDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

						//新たに設定するDefineSymbols
						var newDefineSymbols = new List<string>();

						if (!string.IsNullOrEmpty(oldDefineSymbols))
						{
							newDefineSymbols.AddRange(oldDefineSymbols.Split(','));
						}

						newDefineSymbols.AddRange(args[i + 1].Split(','));

						if (newDefineSymbols.Count > 0)
						{
							//DefineSymbols設定
							PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newDefineSymbols.Distinct().Aggregate((a, b) => a + ";" + b));
						}
					}
					break;

					case "-useStreamingAssetBundle":
					{
						(this as BuildApplicationWithStreamingAssetBundle).useStreamingAssetBundle = bool.Parse(args[i + 1]);
					}
					break;
#if UNITY_PS4
					case "-ps4BuildSubtarget":
					{
						EditorUserBuildSettings.ps4BuildSubtarget = (PS4BuildSubtarget)Enum.Parse(typeof(PS4BuildSubtarget), args[i + 1]);
					}
					break;
#elif UNITY_PS5
					case "-ps5BuildSubtarget":
					{
						EditorUserBuildSettings.ps5BuildSubtarget = (PS5BuildSubtarget)Enum.Parse(typeof(PS5BuildSubtarget), args[i + 1]);
					}
					break;
#elif UNITY_STANDALONE_WIN && STEAMWORKS_NET
					case "-steamBuildFlag":
					{
						(this as BuildApplicationSteam).buildFlag = (BuildApplicationSteam.BuildFlag)Enum.Parse(typeof(BuildApplicationSteam.BuildFlag), args[i + 1]);
					}
					break;

					case "-steamcmdPath":
					{
						(this as BuildApplicationSteam).exePath = args[i + 1].Replace("/", "\\").Replace("\"", null);
					}
					break;

					case "-steamAccount":
					{
						(this as BuildApplicationSteam).account = args[i + 1];
					}
					break;

					case "-steamPassword":
					{
						(this as BuildApplicationSteam).password = args[i + 1];
					}
					break;
#endif
				}
			}
		}

		/// <summary>
		/// ビルド実行
		/// </summary>
		protected virtual void BuildPlayer()
		{
			//出力先フォルダ作成
			Directory.CreateDirectory(Path.GetDirectoryName(this.locationPathName));

			//ビルド実行
			this.buildReport = BuildPipeline.BuildPlayer(new BuildPlayerOptions
			{
				scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray(),
				locationPathName = this.locationPathName,
				assetBundleManifestPath = $"{AssetManager.editorAssetBundleDirectory}/{EditorUserBuildSettings.activeBuildTarget}.manifest",
				targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup,
				target = EditorUserBuildSettings.activeBuildTarget,
				options = this.buildOptions,
			});
		}

		/// <summary>
		/// アプリバージョンの取得
		/// </summary>
		public static string GetAppVersion()
		{
#if UNITY_PS4
			if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.PS4)
			{
				if (string.IsNullOrEmpty(PlayerSettings.PS4.paramSfxPath))
				{
					return PlayerSettings.PS4.appVersion;
				}
				else
				{
					var paramFile = new UnityEditor.PS4.ParamFile();
					paramFile.Read(PlayerSettings.PS4.paramSfxPath);
					return paramFile.Get("APP_VER", PlayerSettings.PS4.appVersion);
				}
			}
#endif
#if UNITY_PS5
			if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.PS5)
			{
				if (string.IsNullOrEmpty(PlayerSettings.PS5.paramFilePath))
				{
					return "01.000.000";
				}
				else
				{
					var paramFile = new UnityEditor.PS5.ParamFile();
					paramFile.Read(PlayerSettings.PS5.paramFilePath);
					return paramFile.Get("contentVersion");
				}
			}
#endif
#if UNITY_STANDALONE_WIN
			if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows
			||  EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
			{
#if STEAMWORKS_NET
				return SteamAppParam.FindAppParam()?.version ?? "01.00";
#else
				return string.IsNullOrEmpty(PlayerSettings.bundleVersion) ? "01.00" : PlayerSettings.bundleVersion;
#endif
			}
#endif
			return null;
		}
	}

	/// <summary>
	/// StreamingAssetsにアセットバンドルを格納するタイプのアプリのビルド基礎
	/// </summary>
	public abstract class BuildApplicationWithStreamingAssetBundle : BuildApplication
	{
		/// <summary>
		/// StreamingAssets内にアセットバンドルを格納するかどうか
		/// </summary>
		public bool useStreamingAssetBundle = true;

		/// <summary>
		/// アセットバンドル移動元ディレクトリ：AssetBundles/Staging/{PLATFORM}/{VERSION}/App
		/// </summary>
		private string assetBundleSrcDir = null;

		/// <summary>
		/// アセットバンドル移動先ディレクトリ：Assets/StreamingAssets/AssetBundles~
		/// </summary>
		private string assetBundleDstDir = null;

		/// <summary>
		/// コマンドライン引数取得
		/// </summary>
		protected override List<string> GetCommandLineArgs()
		{
			var args = base.GetCommandLineArgs();

			if (!Application.isBatchMode)
			{
				//StreamingAssets内にアセットバンドルを格納するかどうか確認
				var _useStreamingAssetBundle = EditorUtility.DisplayDialog(
					"BuildApplication",
					"StreamingAssets内にAssetBundleを格納しますか？",
					"Yes",
					"No or 手動で格納済み"
				);
				args.Add("-useStreamingAssetBundle");
				args.Add(_useStreamingAssetBundle.ToString());
				Debug.Log($"useStreamingAssetBundle={_useStreamingAssetBundle}");
			}

			return args;
		}

		/// <summary>
		/// ビルド実行
		/// </summary>
		protected override void BuildPlayer()
		{
			if (this.useStreamingAssetBundle)
			{
				try
				{
					this.assetBundleSrcDir = BuildAssetBundles.GetStagingAppArea(PlayerSettings.bundleVersion);
					this.assetBundleDstDir = AssetManager.streamingAssetBundleDirectory;

					//StreamingAssets内にアセットバンドルを移動
					Directory.CreateDirectory(Path.GetDirectoryName(this.assetBundleDstDir));
					Directory.Move(this.assetBundleSrcDir, this.assetBundleDstDir);
					Debug.Log($"Move \"{this.assetBundleSrcDir}\" to \"{this.assetBundleDstDir}\"");
				}
				catch (Exception e)
				{
					throw new Exception($"########## Error BuildApplication : {e.Message}, {e.Source} ##########");
				}
			}

			//ビルド実行
			base.BuildPlayer();

			if (this.useStreamingAssetBundle)
			{
				try
				{
					//StreamingAssets内に移動したアセットバンドルを元のディレクトリに戻す
					Directory.Move(this.assetBundleDstDir, this.assetBundleSrcDir);
					Debug.Log($"Return \"{this.assetBundleDstDir}\" to \"{this.assetBundleSrcDir}\"");
				}
				catch (Exception e)
				{
					Debug.LogError($"########## Error BuildApplication : {e.Message}, {e.Source} ##########");
				}
			}
		}
	}

	/// <summary>
	/// PS4アプリケーションビルド
	/// </summary>
	public class BuildApplicationPS4 : BuildApplicationWithStreamingAssetBundle
	{
		/// <summary>
		/// 出力パス
		/// </summary>
		protected override string locationPathName => $"{this.outputDirectory}/";

		/// <summary>
		/// construct
		/// </summary>
		public BuildApplicationPS4()
		{
			this.buildOptions |= BuildOptions.SymlinkLibraries;
		}

		/// <summary>
		/// ビルド実行
		/// </summary>
		protected override void BuildPlayer()
		{
#if UNITY_PS4
			EditorUserBuildSettings.compressWithPsArc = true;
			EditorUserBuildSettings.ps4HardwareTarget = PS4HardwareTarget.ProAndBase;
#endif
			base.BuildPlayer();
		}
	}

	/// <summary>
	/// PS5アプリケーションビルド
	/// </summary>
	public class BuildApplicationPS5 : BuildApplicationWithStreamingAssetBundle
	{
		/// <summary>
		/// 出力パス
		/// </summary>
		protected override string locationPathName => $"{this.outputDirectory}/";

		/// <summary>
		/// construct
		/// </summary>
		public BuildApplicationPS5()
		{
#if UNITY_2021_2_OR_NEWER
			this.buildOptions |= BuildOptions.SymlinkSources;
#else
			this.buildOptions |= BuildOptions.SymlinkLibraries;
#endif
		}

		protected override void BuildPlayer()
		{
#if UNITY_PS5
			EditorUserBuildSettings.compressWithPsArc = true;
#endif
			base.BuildPlayer();
		}
	}

	/// <summary>
	/// StandaloneWindowsアプリケーションビルド
	/// </summary>
	public class BuildApplicationStandaloneWindows : BuildApplicationWithStreamingAssetBundle
	{
		/// <summary>
		/// 出力パス
		/// </summary>
		protected override string locationPathName => $"{this.outputDirectory}/{PlayerSettings.productName}/{PlayerSettings.productName}.exe";
	}

	/// <summary>
	/// Steamアプリケーションビルド
	/// </summary>
	public class BuildApplicationSteam : BuildApplicationStandaloneWindows
	{
		/// <summary>
		/// ビルドフラグ
		/// </summary>
		public enum BuildFlag
		{
			App = 1, //Appのみ
			Vdf = 2, //Vdfのみ
			All = 3, //全て
		}

		/// <summary>
		/// ビルドフラグ
		/// </summary>
		public BuildFlag buildFlag = BuildFlag.All;

		/// <summary>
		/// steamcmd.exeパス
		/// </summary>
		public string exePath = null;

		/// <summary>
		/// Steamアカウント
		/// </summary>
		public string account = "account";

		/// <summary>
		/// Steamパスワード
		/// </summary>
		public string password = "password";

		/// <summary>
		/// コマンドライン引数取得
		/// </summary>
		protected override List<string> GetCommandLineArgs()
		{
			var args = base.GetCommandLineArgs();

			if (!Application.isBatchMode)
			{
				//ビルドフラグ選択
				var _buildFlag = (BuildFlag)(1 + EditorUtility.DisplayDialogComplex(
					"BuildApplication",
					"ビルドフラグを選択して下さい。",
					"App",
					"vdf",
					"All"
				));
				args.Add("-steamBuildFlag");
				args.Add(_buildFlag.ToString());
				Debug.Log($"buildFlag={_buildFlag}");

				if (_buildFlag.HasFlag(BuildFlag.Vdf))
				{
					//steamcmd.exeの選択
					var _exePath = EditorUtility.OpenFilePanel("steamcmd.exeを選択して下さい", "", "exe");
					
					if (!string.IsNullOrEmpty(_exePath))
					{
						args.Add("-steamcmdPath");
						args.Add(_exePath);
						Debug.Log($"steamcmd.exe={_exePath}");
					}
				}
			}

			return args;
		}

		/// <summary>
		/// ビルド実行
		/// </summary>
		protected override void BuildPlayer()
		{
			if (this.buildFlag.HasFlag(BuildFlag.App))
			{
				//ビルド実行
				base.BuildPlayer();
			}

			if (this.buildFlag.HasFlag(BuildFlag.Vdf))
			{
				var appParam = SteamAppParam.FindAppParam();
				var dlcParams = DLCParam.FindDLCParams(_ => _.isEnabled);

				if (appParam == null)
				{
					return;
				}

				//Steamビルドルートフォルダ　例：Bin/StandaloneWindows64/steam_build
				var output_root = $"{this.outputDirectory}/steam_build";

				//DLCビルドスクリプト出力先　例：C:\work\STELLA\Unity\DLC\StandaloneWindows64\Steam_1000
				var output_dlcvdf = $"{Path.GetDirectoryName(Application.dataPath)}/DLC/{EditorUserBuildSettings.activeBuildTarget}/Steam_{appParam.appId}".Replace("/", "\\");

				//プロダクト名
				var productName = PlayerSettings.productName;

				Directory.CreateDirectory(output_root);

				//アプリビルドスクリプト作成
				var sb = new StringBuilder();
				sb.AppendLine($"AppBuild");
				sb.AppendLine($"{{");
				sb.AppendLine($"    \"AppID\" \"{appParam.appId}\"");
				sb.AppendLine($"    \"Desc\" \"{this.outputDirectory}\"");
				sb.AppendLine($"    \"BuildOutput\" \"BuildOutput\"");
				sb.AppendLine($"    \"ContentRoot\" \"\"");
				sb.AppendLine($"    \"Depots\"");
				sb.AppendLine($"    {{");
				sb.AppendLine($"        \"{appParam.depotId}\" \"depot_{appParam.depotId}.vdf\"");
				foreach (var dlcParam in dlcParams)
				{
					sb.AppendLine($"        \"{dlcParam.steam.depotId}\" \"{output_dlcvdf}\\{dlcParam.dlcName}\\depot_{dlcParam.steam.depotId}.vdf\"");
				}
				sb.AppendLine($"    }}");
				sb.AppendLine($"}}");
				File.WriteAllText($"{output_root}/app_{appParam.appId}.vdf", sb.ToString());

				//メインデポビルドスクリプト作成
				sb.Clear();
				sb.AppendLine($"DepotBuildConfig");
				sb.AppendLine($"{{");
				sb.AppendLine($"    \"DepotID\" \"{appParam.depotId}\"");
				sb.AppendLine($"    \"ContentRoot\" \"..\\\"");
				sb.AppendLine($"    \"FileMapping\"");
				sb.AppendLine($"    {{");
				sb.AppendLine($"        \"LocalPath\" \"{productName}\\{productName}_Data\\*\"");
				sb.AppendLine($"        \"DepotPath\" \"{productName}_Data\\\"");
				sb.AppendLine($"        \"Recursive\" \"1\"");
				sb.AppendLine($"    }}");
				sb.AppendLine($"    \"FileMapping\"");
				sb.AppendLine($"    {{");
				sb.AppendLine($"        \"LocalPath\" \"{productName}\\*.dll\"");
				sb.AppendLine($"        \"DepotPath\" \".\"");
				sb.AppendLine($"    }}");
				sb.AppendLine($"    \"FileMapping\"");
				sb.AppendLine($"    {{");
				sb.AppendLine($"        \"LocalPath\" \"steam_build\\{productName}.exe\"");
				sb.AppendLine($"        \"DepotPath\" \".\"");
				sb.AppendLine($"    }}");
				sb.AppendLine($"}}");
				File.WriteAllText($"{output_root}/depot_{appParam.depotId}.vdf", sb.ToString());

				if (!string.IsNullOrEmpty(this.exePath))
				{
					//ビルドバッチ作成
					sb.Clear();
					sb.AppendLine("cd ../");
					sb.AppendLine(
						$"\"{this.exePath}\"" +
						$" +login {this.account} {this.password}" +
						$" +drm_wrap {appParam.appId} \"%cd%\\{productName}\\{productName}.exe\" \"%cd%\\steam_build\\{productName}.exe\" drmtoolp 0" +
						$" +run_app_build \"%cd%\\steam_build\\app_{appParam.appId}.vdf\"" +
						$" +quit");
					File.WriteAllText($"{output_root}/run_app_build.bat", sb.ToString());
				}
			}
		}
	}
}