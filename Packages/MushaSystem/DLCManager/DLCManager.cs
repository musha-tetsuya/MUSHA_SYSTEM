using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
#if UNITY_PS4
using UnityEngine.PS4;
#elif UNITY_PS5
using UnityEngine.PS5;
using Unity.PSN.PS5;
using Unity.PSN.PS5.Aysnc;
using Unity.PSN.PS5.Entitlement;
#elif UNITY_STANDALONE_WIN && STEAMWORKS_NET
using Steamworks;
#endif

namespace KG
{
	/// <summary>
	/// DLC管理
	/// </summary>
	public partial class DLCManager : SingletonMonoBehaviour<DLCManager>
	{
		/// <summary>
		/// 初期化ステータス
		/// </summary>
		public enum InitStatus
		{
			None,   //未初期化
			Success,//初期化成功
			Error,  //初期化失敗
		}

		/// <summary>
		/// PS4/PS5用DLCアセットバンドルディレクトリ
		/// </summary>
		public const string ASSETBUNDLE_DIRECTORY = "AssetBundles";

		/// <summary>
		/// 初期化ステータス
		/// </summary>
		public InitStatus initStatus { get; private set; }

		/// <summary>
		/// DLCManager
		/// </summary>
		private IDLCManager dlcManager = null;

		/// <summary>
		/// Awake
		/// </summary>
		protected override void Awake()
		{
			base.Awake();
#if UNITY_EDITOR
			//Editor時のDLC処理は無し
#elif UNITY_PS4
			this.dlcManager = new DLCManagerPS4();
#elif UNITY_PS5
			this.dlcManager = new DLCManagerPS5();
#elif UNITY_STANDALONE_WIN && STEAMWORKS_NET
			this.dlcManager = new DLCManagerSteam();
#endif
		}

		/// <summary>
		/// インストール
		/// </summary>
		public void Initialize()
		{
			if (this.dlcManager == null)
			{
				this.initStatus = InitStatus.Success;
			}
			else
			{
				this.dlcManager.Initialize();
			}
		}

		/// <summary>
		/// DLCデータ一覧の取得
		/// </summary>
		public IDLCData[] GetDataList()
		{
			return (this.dlcManager == null) ? new IDLCData[0] : this.dlcManager.dataList.ToArray();
		}

		/// <summary>
		/// アセットバンドル同期ロード
		/// </summary>
		public AssetBundle LoadAssetBundle(AssetBundleInfo assetBundleInfo)
		{
			return (this.dlcManager == null) ? null : this.dlcManager.LoadAssetBundle(assetBundleInfo);
		}

		/// <summary>
		/// アセットバンドル非同期ロード
		/// </summary>
		public void LoadAsyncAssetBundle(AssetBundleInfo assetBundleInfo, Action<AssetBundle> onLoaded)
		{
			this.dlcManager?.LoadAsyncAssetBundle(assetBundleInfo, onLoaded);
		}

		/// <summary>
		/// アセットバンドル破棄時
		/// </summary>
		public void OnUnloadAssetBundle(AssetBundleInfo assetBundleInfo)
		{
			this.dlcManager?.OnUnloadAssetBundle(assetBundleInfo);
		}

		/// <summary>
		/// DLCデータインターフェース
		/// </summary>
		public abstract class IDLCData
		{
			/// <summary>
			/// ラベル
			/// PS4, PS5の例：ROCK100000000000
			/// Steamの例：1002
			/// </summary>
			public string label { get; protected set; }

			/// <summary>
			/// マウントポイント
			/// PS4, PS5の例：/addcont0
			/// Steamの例：C:/{APP_ROOT}/DLC/1002
			/// </summary>
			public string mountpoint { get; protected set; }

			/// <summary>
			/// ファイル一覧
			/// 例：Assets/Hoge/Fuga.dat
			/// 例：AssetBundles/0123456789abcdef
			/// </summary>
			public string[] files { get; protected set; } = new string[0];

			/// <summary>
			/// 初期化
			/// </summary>
			public virtual void Initialize() { }

			/// <summary>
			/// マウント
			/// </summary>
			public virtual bool Mount() => false;

			/// <summary>
			/// アンマウント
			/// </summary>
			public virtual void Unmount() { }

			/// <summary>
			/// filePathで始まるfileがあるなら取得する
			/// </summary>
			public virtual bool GetFileStartsWith(string filePath, out string file)
			{
				file = this.files.FirstOrDefault(_ => _.StartsWith(filePath, StringComparison.OrdinalIgnoreCase));

				return !string.IsNullOrEmpty(file);
			}
		}

		/// <summary>
		/// PS4用DLCデータ
		/// </summary>
		public class DLCDataPS4 : IDLCData
		{
			/// <summary>
			/// 参照カウンタ
			/// </summary>
			protected int referenceCount { get; private set; }

			/// <summary>
			/// ファイルメモリ
			/// </summary>
			public Dictionary<string, byte[]> fileMemory = new Dictionary<string, byte[]>();

			/// <summary>
			/// construct
			/// </summary>
			public DLCDataPS4(string entitlementLabel)
			{
				this.label = entitlementLabel;
			}

			/// <summary>
			/// 初期化
			/// </summary>
			public override void Initialize()
			{
				if (this.Mount())
				{
					//DLC内ファイルパス一覧取得
					this.files = Directory.GetFiles(this.mountpoint, "*", SearchOption.AllDirectories).Select(x => x.Replace($"{this.mountpoint}/", null)).ToArray();

					//アセットバンドル情報リストのパス　例：/addcont0/AssetBundles/0123456789abcdef
					var infoListPath = $"{this.mountpoint}/{ASSETBUNDLE_DIRECTORY}/{EncryptUtility.ToHashString(AssetManager.ASSETBUNDLE_INFOLIST_NAME)}";

					//DLC内にアセットバンドル情報リストが入っているなら
					if (File.Exists(infoListPath))
					{
						//読み込み
						var infoListJson = File.ReadAllText(infoListPath);
						//デシリアライズ
						var infoList = JsonConvert.DeserializeObject<AssetBundleInfo[]>(infoListJson);

						//AssetManagerにマージ
						foreach (var info in infoList)
						{
							if (!AssetManager.Instance.infoList.Exists(x => x.assetBundleName.Equals(info.assetBundleName, StringComparison.OrdinalIgnoreCase)))
							{
								AssetManager.Instance.infoList.Add(info);
							}
						}
					}

					//マウント解除
					this.Unmount();
				}
			}

			/// <summary>
			/// マウント
			/// </summary>
			public override bool Mount()
			{
				if (!string.IsNullOrEmpty(this.mountpoint))
				{
					//マウント済み
					this.referenceCount++;
					return true;
				}

				if (this.DRMContentOpen())
				{
					if (this.DRMContentGetMountPoint(out string mountpoint))
					{
						//マウント成功
						this.mountpoint = mountpoint;
						this.referenceCount++;
						return true;
					}
				}

				return false;
			}

			/// <summary>
			/// アンマウント
			/// </summary>
			public override void Unmount()
			{
				if (!string.IsNullOrEmpty(this.mountpoint) && this.referenceCount > 0)
				{
					this.referenceCount--;

					if (this.referenceCount == 0)
					{
						if (this.DRMContentClose())
						{
							//アンマウント成功
							this.mountpoint = null;
						}
					}
				}
			}

			/// <summary>
			/// DRMコンテンツを開く
			/// </summary>
			protected virtual bool DRMContentOpen()
			{
#if UNITY_PS4
				return PS4DRM.ContentOpen(this.label);
#else
				return false;
#endif
			}

			/// <summary>
			/// DRMコンテンツのマウントポイントを取得する
			/// </summary>
			protected virtual bool DRMContentGetMountPoint(out string mountpoint)
			{
				mountpoint = null;
#if UNITY_PS4
				return PS4DRM.ContentGetMountPoint(this.label, out mountpoint);
#else
				return false;
#endif
			}

			/// <summary>
			/// DRMコンテンツを閉じる
			/// </summary>
			protected virtual bool DRMContentClose()
			{
#if UNITY_PS4
				return PS4DRM.ContentClose(this.mountpoint);
#else
				return false;
#endif
			}
		}

		/// <summary>
		/// PS4用DLCデータ
		/// </summary>
		public class DLCDataPS5 : DLCDataPS4
		{
			/// <summary>
			/// エクストラデータの有無
			/// </summary>
			private bool isPSAC = false;

			/// <summary>
			/// construct
			/// </summary>
			public DLCDataPS5(string entitlementLabel, bool isPSAC)
				: base(entitlementLabel)
			{
				this.isPSAC = isPSAC;
			}

			/// <summary>
			/// 初期化
			/// </summary>
			public override void Initialize()
			{
				if (this.isPSAC)
				{
					//エクストラデータありの追加コンテンツならマウントしてDLC内ファイルパス一覧を取得
					base.Initialize();
				}
			}

			/// <summary>
			/// DRMコンテンツを開く
			/// </summary>
			protected override bool DRMContentOpen()
			{
#if UNITY_PS5
				return PS5DRM.ContentOpen(this.label);
#else
				return false;
#endif
			}

			/// <summary>
			/// DRMコンテンツのマウントポイントを取得する
			/// </summary>
			protected override bool DRMContentGetMountPoint(out string mountpoint)
			{
				mountpoint = null;
#if UNITY_PS5
				return PS5DRM.ContentGetMountPoint(this.label, out mountpoint);
#else
				return false;
#endif
			}

			/// <summary>
			/// DRMコンテンツを閉じる
			/// </summary>
			protected override bool DRMContentClose()
			{
#if UNITY_PS5
				return PS5DRM.ContentClose(this.mountpoint);
#else
				return false;
#endif
			}
		}

		/// <summary>
		/// Steam用DLCデータ
		/// </summary>
		public class DLCDataSteam : IDLCData
		{
			/// <summary>
			/// アセットハッシュリストJsonファイル名
			/// </summary>
			public const string ASSET_HASHLIST_NAME = "assetHashList.json";

			/// <summary>
			/// アセットハッシュリスト
			/// </summary>
			private Dictionary<string, string> assetHashList = new Dictionary<string, string>();

			/// <summary>
			/// construct
			/// </summary>
			public DLCDataSteam(string depotId)
			{
				//例：1002
				this.label = depotId;
				//例：C:/{APP_ROOT}/DLC/1002
				this.mountpoint = $"{Path.GetDirectoryName(Application.dataPath)}/DLC/{this.label}".Replace("\\", "/");
			}

			/// <summary>
			/// 初期化
			/// </summary>
			public override void Initialize()
			{
				if (this.Mount())
				{
					//DLC内ファイルパス一覧取得
					this.files = Directory.GetFiles(this.mountpoint).Select(x => x.Replace("\\", "/").Replace($"{this.mountpoint}/", null)).ToArray();

					//アセットバンドル情報リストのパス　例：C:/{APP_ROOT}/DLC/{depotId}/0123456789abcdef
					var infoListPath = $"{this.mountpoint}/{EncryptUtility.ToHashString(AssetManager.ASSETBUNDLE_INFOLIST_NAME)}";

					//DLC内にアセットバンドル情報リストが入っているなら
					if (File.Exists(infoListPath))
					{
						//読み込み
						var infoListJson = File.ReadAllText(infoListPath);
						//複合化
						infoListJson = EncryptUtility.defaultAes.Decrypt(infoListJson);
						//デシリアライズ
						var infoList = JsonConvert.DeserializeObject<AssetBundleInfo[]>(infoListJson);

						//AssetManagerにマージ
						foreach (var info in infoList)
						{
							if (!AssetManager.Instance.infoList.Exists(x => x.assetBundleName.Equals(info.assetBundleName, StringComparison.OrdinalIgnoreCase)))
							{
								AssetManager.Instance.infoList.Add(info);
							}
						}
					}

					//アセットハッシュリストのパス　例：C:/{APP_ROOT}/DLC/{depotId}/0123456789abcdef
					var assetHashListPath = $"{this.mountpoint}/{EncryptUtility.ToHashString(ASSET_HASHLIST_NAME)}";

					//DLC内にアセットハッシュリストが入っているなら
					if (File.Exists(assetHashListPath))
					{
						//読み込み
						var assetHashListJson = File.ReadAllText(assetHashListPath);
						//複合化
						assetHashListJson = EncryptUtility.defaultAes.Decrypt(assetHashListJson);
						//デシリアライズ
						this.assetHashList = JsonConvert.DeserializeObject<Dictionary<string, string>>(assetHashListJson);
					}

					//マウント解除
					this.Unmount();
				}
			}

			/// <summary>
			/// filePathで始まるfileがあるなら取得する
			/// </summary>
			public override bool GetFileStartsWith(string filePath, out string file)
			{
				file = null;

				foreach (var kv in this.assetHashList)
				{
					if (kv.Value.StartsWith(filePath, StringComparison.OrdinalIgnoreCase))
					{
						file = kv.Key;
						break;
					}
				}

				return !string.IsNullOrEmpty(file);
			}

			/// <summary>
			/// マウント
			/// </summary>
			public override bool Mount()
			{
				return !string.IsNullOrEmpty(this.mountpoint) && Directory.Exists(this.mountpoint);
			}
		}

		/// <summary>
		/// DLCManagerインターフェース
		/// </summary>
		public abstract class IDLCManager
		{
			/// <summary>
			/// DLCデータ一覧
			/// </summary>
			public List<IDLCData> dataList { get; protected set; } = new List<IDLCData>();

			/// <summary>
			/// 初期化
			/// </summary>
			public virtual void Initialize() { }

			/// <summary>
			/// アセットバンドル同期ロード
			/// </summary>
			public abstract AssetBundle LoadAssetBundle(AssetBundleInfo assetBundleInfo);

			/// <summary>
			/// アセットバンドル非同期ロード
			/// </summary>
			public abstract void LoadAsyncAssetBundle(AssetBundleInfo assetBundleInfo, Action<AssetBundle> onLoaded);

			/// <summary>
			/// アセットバンドル破棄時
			/// </summary>
			public abstract void OnUnloadAssetBundle(AssetBundleInfo assetBundleInfo);
		}

		/// <summary>
		/// PS4用DLC管理
		/// </summary>
		public class DLCManagerPS4 : IDLCManager
		{
			/// <summary>
			/// 同時マウント最大数
			/// </summary>
			protected const int MAX_MOUNT_SIZE = 64;

			/// <summary>
			/// 初期化
			/// </summary>
			public override void Initialize()
			{
				if (Instance.initStatus == InitStatus.Success)
				{
					//初期化済み
					return;
				}
#if UNITY_PS4
				var finder = new PS4DRM.DrmContentFinder();
				finder.serviceLabel = 0;

				if (PS4DRM.ContentFinderOpen(ref finder))
				{
					do
					{
						//DLCデータ準備
						var data = new DLCDataPS4(finder.entitlementLabel);
						data.Initialize();

						//リストにデータ追加
						this.dataList.Add(data);
					}
					while (PS4DRM.ContentFinderNext(ref finder));

					PS4DRM.ContentFinderClose(ref finder);
				}

				//初期化完了
				Instance.initStatus = InitStatus.Success;
#endif
			}

			/// <summary>
			/// アセットバンドル同期ロード
			/// </summary>
			public override AssetBundle LoadAssetBundle(AssetBundleInfo assetBundleInfo)
			{
				string assetBundlePath = $"{ASSETBUNDLE_DIRECTORY}/{assetBundleInfo.hashName}";

				for (int i = 0, imax = this.dataList.Count; i < imax; i++)
				{
					var data = this.dataList[i] as DLCDataPS4;

					if (data.files.Contains(assetBundlePath))
					{
						//マウント済みかどうか
						bool isMounted = !string.IsNullOrEmpty(data.mountpoint);

						//マウント
						if (data.Mount())
						{
							//マウント済み or まだマウント数に余裕がある
							if (isMounted || this.dataList.Count(x => !string.IsNullOrEmpty(x.mountpoint)) < MAX_MOUNT_SIZE)
							{
								//ファイルからアセットバンドル同期ロード
								return AssetBundle.LoadFromFile($"{data.mountpoint}/{assetBundlePath}");
							}
							//同時マウント数が限界
							else
							{
								//メモリに確保
								data.fileMemory[assetBundlePath] = File.ReadAllBytes($"{data.mountpoint}/{assetBundlePath}");

								//マウント解除
								data.Unmount();

								//メモリからアセットバンドル同期ロード
								return AssetBundle.LoadFromMemory(data.fileMemory[assetBundlePath]);
							}
						}
					}
				}

				return null;
			}

			/// <summary>
			/// アセットバンドル非同期ロード
			/// </summary>
			public override void LoadAsyncAssetBundle(AssetBundleInfo assetBundleInfo, Action<AssetBundle> onLoaded)
			{
				string assetBundlePath = $"{ASSETBUNDLE_DIRECTORY}/{assetBundleInfo.hashName}";

				for (int i = 0, imax = this.dataList.Count; i < imax; i++)
				{
					var data = this.dataList[i] as DLCDataPS4;

					if (data.files.Contains(assetBundlePath))
					{
						//マウント済みかどうか
						bool isMounted = !string.IsNullOrEmpty(data.mountpoint);

						//マウント
						if (data.Mount())
						{
							AssetBundleCreateRequest request = null;

							//マウント済み or まだマウント数に余裕がある
							if (isMounted || this.dataList.Count(x => !string.IsNullOrEmpty(x.mountpoint)) < MAX_MOUNT_SIZE)
							{
								//ファイルからアセットバンドル非同期ロード
								request = AssetBundle.LoadFromFileAsync($"{data.mountpoint}/{assetBundlePath}");
							}
							//同時マウント数が限界
							else
							{
								//メモリに確保
								data.fileMemory[assetBundlePath] = File.ReadAllBytes($"{data.mountpoint}/{assetBundlePath}");

								//マウント解除
								data.Unmount();

								//メモリからアセットバンドル非同期ロード
								request = AssetBundle.LoadFromMemoryAsync(data.fileMemory[assetBundlePath]);
							}

							request.completed += (_) =>
							{
								//コールバック実行
								onLoaded?.Invoke(request.assetBundle);
							};

							break;
						}
					}
				}
			}

			/// <summary>
			/// アセットバンドル破棄時
			/// </summary>
			public override void OnUnloadAssetBundle(AssetBundleInfo assetBundleInfo)
			{
				string assetBundlePath = $"{ASSETBUNDLE_DIRECTORY}/{assetBundleInfo.hashName}";

				for (int i = 0, imax = this.dataList.Count; i < imax; i++)
				{
					var data = this.dataList[i] as DLCDataPS4;

					if (data.files.Contains(assetBundlePath))
					{
						//（あるなら）アセットバンドルメモリの解放
						data.fileMemory.Remove(assetBundlePath);

						//マウント解除
						data.Unmount();

						break;
					}
				}
			}
		}

		/// <summary>
		/// PS5用DLC管理
		/// </summary>
		public class DLCManagerPS5 : DLCManagerPS4
		{
			/// <summary>
			/// 初期化処理中かどうか
			/// </summary>
			private bool isInitializing = false;

			/// <summary>
			/// 初期化
			/// </summary>
			public override void Initialize()
			{
				if (Instance.initStatus == InitStatus.Success)
				{
					//初期化済み
					return;
				}

				if (this.isInitializing)
				{
					//初期化中
					return;
				}
#if UNITY_PS5
				//PSN初期化
				PSNInitializeManager.Initialize();

				if (!PSNInitializeManager.initResult.Initialized)
				{
					//PSN初期化されてない
					return;
				}

				//初期化開始
				this.isInitializing = true;

				var request = new Entitlements.GetAdditionalContentEntitlementListRequest
				{
					ServiceLabel = 0,
				};

				var requestOp = new AsyncRequest<Entitlements.GetAdditionalContentEntitlementListRequest>(request).ContinueWith((antecedent) =>
				{
					//初期化処理終了
					this.isInitializing = false;

					if (antecedent == null
					|| antecedent.Request == null
					|| antecedent.Request.Result.apiResult != APIResultTypes.Success)
					{
						//エラー
						Instance.initStatus = InitStatus.Error;
						return;
					}

					if (antecedent.Request.Entitlements != null)
					{
						//全エンタイトルメントラベルを総なめ
						foreach (var entitlement in antecedent.Request.Entitlements)
						{
							//DLCデータ準備
							var data = new DLCDataPS5(entitlement.EntitlementLabel, entitlement.PackageType == Entitlements.EntitlementAccessPackageType.PSAC);
							data.Initialize();

							//リストにデータ追加
							this.dataList.Add(data);
						}
					}

					//初期化完了
					Instance.initStatus = InitStatus.Success;
				});

				Entitlements.Schedule(requestOp);
#endif
			}
		}

		/// <summary>
		/// Steam用DLC管理
		/// </summary>
		public class DLCManagerSteam : IDLCManager
		{
			/// <summary>
			/// 初期化
			/// </summary>
			public override void Initialize()
			{
				if (Instance.initStatus == InitStatus.Success)
				{
					//初期化済み
					return;
				}

#if UNITY_STANDALONE_WIN && STEAMWORKS_NET
				//インストール済みDLCのデポIDリスト
				var installedDLCs = new List<string>();

				if (SteamManager.Initialized)
				{
					//DLC総数の取得
					int imax = SteamApps.GetDLCCount();

					for (int i = 0; i < imax; i++)
					{
						//DLCデータの取得
						if (SteamApps.BGetDLCDataByIndex(i, out var pAppID, out var pbAvailable, out var pchName, 128))
						{
							//DLCがインストール済みかチェック
							if (SteamApps.BIsDlcInstalled(pAppID))
							{
								installedDLCs.Add(pAppID.m_AppId.ToString());
							}
						}
					}
				}
				else
				{
#if DEBUG
					//DLCインストール先　例：C:/{APP_ROOT}/DLC
					var dlcRootDir = $"{Path.GetDirectoryName(Application.dataPath)}/DLC".Replace("\\", "/");;

					if (Directory.Exists(dlcRootDir))
					{
						installedDLCs.AddRange(Directory.GetDirectories(dlcRootDir).Select(Path.GetFileName));
					}
#else
					return;
#endif
				}

				for (int i = 0; i < installedDLCs.Count; i++)
				{
					//DLCデータ準備
					var data = new DLCDataSteam(installedDLCs[i]);
					data.Initialize();

					//リストにデータ追加
					this.dataList.Add(data);
				}

				//初期化完了
				Instance.initStatus = InitStatus.Success;
#endif
			}
			
			/// <summary>
			/// アセットバンドル同期ロード
			/// </summary>
			public override AssetBundle LoadAssetBundle(AssetBundleInfo assetBundleInfo)
			{
				string assetBundlePath = $"{assetBundleInfo.hashName}";

				for (int i = 0, imax = this.dataList.Count; i < imax; i++)
				{
					var data = this.dataList[i];

					if (data.files.Contains(assetBundlePath))
					{
						if (data.Mount())
						{
							return AssetBundle.LoadFromFile($"{data.mountpoint}/{assetBundlePath}");
						}
					}
				}

				return null;
			}
			
			/// <summary>
			/// アセットバンドル非同期ロード
			/// </summary>
			public override void LoadAsyncAssetBundle(AssetBundleInfo assetBundleInfo, Action<AssetBundle> onLoaded)
			{
				string assetBundlePath = $"{assetBundleInfo.hashName}";

				for (int i = 0, imax = this.dataList.Count; i < imax; i++)
				{
					var data = this.dataList[i];

					if (data.files.Contains(assetBundlePath))
					{
						if (data.Mount())
						{
							var request = AssetBundle.LoadFromFileAsync($"{data.mountpoint}/{assetBundlePath}");

							request.completed += (_) =>
							{
								onLoaded?.Invoke(request.assetBundle);
							};

							break;
						}
					}
				}
			}
			
			/// <summary>
			/// アセットバンドル破棄時
			/// </summary>
			public override void OnUnloadAssetBundle(AssetBundleInfo assetBundleInfo)
			{
				string assetBundlePath = $"{assetBundleInfo.hashName}";

				for (int i = 0, imax = this.dataList.Count; i < imax; i++)
				{
					var data = this.dataList[i];

					if (data.files.Contains(assetBundlePath))
					{
						data.Unmount();

						break;
					}
				}
			}
		}
	}
}