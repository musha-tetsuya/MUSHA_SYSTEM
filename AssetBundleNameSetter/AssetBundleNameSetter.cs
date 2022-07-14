using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KG
{
    /// <summary>
    /// AssetBundleNameSetter
    /// </summary>
    public class AssetBundleNameSetter
    {
        /// <summary>
        /// アセットバンドル名取得
        /// </summary>
        public static string GetAssetBundleName(string assetPath)
        {
            return Path.ChangeExtension(assetPath, null).Remove(0, "Assets/".Length).Replace('\\', '/').ToLower();
        }

        /// <summary>
        /// アセットバンドルターゲット情報リストの作成
        /// </summary>
        public virtual List<AssetBundleTargetInfo> CreateAssetBundleTargetInfoList()
        {
            return new List<AssetBundleTargetInfo>();
        }

		/// <summary>
		/// 指定のアセットパスのアセットバンドル名を書き出してもいいかどうかのチェック
		/// </summary>
		public virtual bool CanWriteAssetBundleName(string assetPath)
		{
			return true;
		}

#if UNITY_EDITOR
        /// <summary>
        /// アセットバンドルターゲットリストのパス
        /// </summary>
        private const string ASSETBUNDLE_TARGETLIST_PATH = "AssetBundleTargetList.txt";

        /// <summary>
        /// 指定のアセットにアセットバンドル名を付ける
        /// </summary>
        private static void SetAssetBundleName(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer != null)
            {
                importer.assetBundleName = GetAssetBundleName(assetPath);
            }
        }

        /// <summary>
        /// 右クリックからのアセットバンドル名設定
        /// </summary>
        [MenuItem("Assets/KG/SetSelectedAssetBundleName")]
        private static void SetSelectedAssetBundleName()
        {
            foreach (int instanceId in Selection.instanceIDs)
            {
                //選択しているアセットのパス
                string assetPath = AssetDatabase.GetAssetPath(instanceId);

                //アセットバンドル名付与
                SetAssetBundleName(assetPath);
            }
        }

        /// <summary>
        /// アセットバンドル名の一括設定
        /// </summary>
        [MenuItem("KG/Build/SetAllAssetBundleName")]
        private static void SetAllAssetBundleName()
        {
            if (File.Exists(ASSETBUNDLE_TARGETLIST_PATH))
            {
                bool isOK = EditorUtility.DisplayDialog(
                    title: "SetAllAssetBundleName",
                    message: $"{ASSETBUNDLE_TARGETLIST_PATH}内のすべてのファイルにアセットバンドル名を設定します。\nファイル数が多い場合、importに膨大な時間がかかる場合がありますが本当によろしいですか？",
                    ok: "OK",
                    cancel: "Cancel"
                );

                if (isOK)
                {
                    foreach (var line in File.ReadAllLines(ASSETBUNDLE_TARGETLIST_PATH))
                    {
						var tokens = line.Split(',');
                        SetAssetBundleName(tokens[0]);
                    }
                }
            }
        }

        /// <summary>
        /// アセットバンドルターゲットリストの書き出し
        /// </summary>
        [MenuItem("KG/Build/WriteAssetBundleTargetList")]
        public static List<(string assetPath, string assetBundleName)> WriteAssetBundleTargetList()
        {
            var assemblies = new Assembly[]
            {
                Assembly.Load("Assembly-CSharp"),
                Assembly.Load("Assembly-CSharp-Editor"),
            };

			var nameSetters = assemblies
                .Where(x => x != null)
                .SelectMany(x => x.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(AssetBundleNameSetter)))
                .Select(t => Activator.CreateInstance(t) as AssetBundleNameSetter)
				.ToArray();

            var infoList = nameSetters
                .SelectMany(x => x.CreateAssetBundleTargetInfoList())
                .ToList();

            var targetList = new List<(string assetPath, string assetBundleName)>();

            using (var writer = new StreamWriter(ASSETBUNDLE_TARGETLIST_PATH))
            {
                for (int i = 0; i < infoList.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("WriteAssetBundleTargetList", infoList[i].directoryAssetPath, (float)i / infoList.Count);
                    foreach (var assetPath in infoList[i].GetAssetPaths())
                    {
                        var _assetPath = assetPath.Replace('/', '\\');

						if (nameSetters.All(setter => setter.CanWriteAssetBundleName(_assetPath)))
						{
							var _assetBundleName = GetAssetBundleName(assetPath);
							targetList.Add((_assetPath, _assetBundleName));
							writer.WriteLine($"{_assetPath},{_assetBundleName}");
						}
                    }
                }
                EditorUtility.ClearProgressBar();
            }

            return targetList;
        }
#endif
    }

    /// <summary>
    /// ターゲットオプション
    /// </summary>
    public enum AssetBundleTargetOption
    {
        /// <summary>
        /// 自分自身にアセットバンドル名を付ける
        /// </summary>
        MySelf = 1 << 0,

        /// <summary>
        /// ファイルにアセットバンドル名を付ける
        /// </summary>
        File = 1 << 1,

        /// <summary>
        /// 子フォルダにアセットバンドル名を付ける
        /// </summary>
        Directory = 1 << 2,
    }

    /// <summary>
    /// ターゲットフォルダ情報
    /// </summary>
    public class AssetBundleTargetInfo
    {
        /// <summary>
        /// 対象フォルダのアセットパス
        /// </summary>
        public string directoryAssetPath;

        /// <summary>
        /// ターゲットオプション
        /// </summary>
        public AssetBundleTargetOption option;

        /// <summary>
        /// ターゲット拡張子
        /// </summary>
        public string extension;

        /// <summary>
        /// 再帰的
        /// </summary>
        public bool recursive;

        /// <summary>
        /// アセットバンドル名設定
        /// </summary>
        public List<string> GetAssetPaths()
        {
            var assetPaths = new List<string>();

            if (!Directory.Exists(this.directoryAssetPath))
            {
                Debug.Log($"ディレクトリが見つかりません:{this.directoryAssetPath}");
                return assetPaths;
            }

            if (this.option.HasFlag(AssetBundleTargetOption.MySelf))
            {
                //自分自身にアセットバンドル名を付ける
                assetPaths.Add(this.directoryAssetPath);
                return assetPaths;
            }

            if (option.HasFlag(AssetBundleTargetOption.File))
            {
                //フォルダ内のファイルにアセットバンドル名を付ける
                foreach (var fileAssetPath in Directory.GetFiles(this.directoryAssetPath))
                {
                    //ファイルの拡張子
                    string fileExtension = Path.GetExtension(fileAssetPath);

                    //拡張子チェック
                    if (!string.IsNullOrEmpty(fileExtension))
                    {
                        if (fileExtension.Equals(".meta"))
                        {
                            //metaファイルは無視
                            continue;
                        }

                        if (!string.IsNullOrEmpty(this.extension))
                        {
                            if (!fileExtension.Equals(this.extension, StringComparison.OrdinalIgnoreCase))
                            {
                                //ターゲット拡張子と違うファイルは無視
                                continue;
                            }
                        }

                        //アセットバンドル名付与
                        assetPaths.Add(fileAssetPath);
                    }
                }
            }

            foreach (var subDirectoryAssetPath in Directory.GetDirectories(this.directoryAssetPath))
            {
                if (option.HasFlag(AssetBundleTargetOption.Directory))
                {
                    //子フォルダにアセットバンドル名付与
                    var info = new AssetBundleTargetInfo
                    {
                        directoryAssetPath = subDirectoryAssetPath,
                        option = AssetBundleTargetOption.MySelf,
                        extension = null,
                        recursive = false,
                    };
                    assetPaths.AddRange(info.GetAssetPaths());
                }
                else if (recursive)
                {
                    //子フォルダ内へ
                    var info = new AssetBundleTargetInfo
                    {
                        directoryAssetPath = subDirectoryAssetPath,
                        option = this.option,
                        extension = this.extension,
                        recursive = this.recursive,
                    };
                    assetPaths.AddRange(info.GetAssetPaths());
                }
            }

            return assetPaths;
        }
    }
}
