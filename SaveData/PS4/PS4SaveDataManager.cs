using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_PS4
using Sony.PS4.SaveData;
using UnityEngine.PS4;
#elif UNITY_PS5
using Unity.SaveData.PS5;
using Unity.SaveData.PS5.Mount;
using Unity.SaveData.PS5.Search;
using Unity.SaveData.PS5.Dialog;
using Unity.SaveData.PS5.Core;
using Unity.SaveData.PS5.Info;
using Unity.SaveData.PS5.Initialization;
using PS4Input = UnityEngine.PS5.PS5Input;
#endif

namespace KG
{
    /// <summary>
    /// PS4/PS5セーブデータマネージャー
    /// </summary>
    public class PS4SaveDataManager : MonoBehaviour
    {
		public enum SearchingSearchSortKey
		{
			DirName = 0,
			UserParam = 1,
			Blocks = 2,
			Time = 3,
			FreeBlocks = 5
		}
		public enum SearchingSearchSortOrder
		{
			Ascending = 0,
			Descending = 1,
		}
		public enum DialogsFocusPos
		{
			ListHead = 0,
			ListTail = 1,
			DataHead = 2,
			DataTail = 3,
			DataLatest = 4,
			DataOldest = 5,
			DirName = 6
		}
		public enum DialogsItemStyle
		{
			DateSizeSubtitle = 0,
			SubtitleDataSize = 1,
			DataSize = 2
		}

#pragma warning disable CS0414
		/// <summary>
		/// 手動セーブデータスロット最大数
		/// </summary>
#if UNITY_PS4 || UNITY_PS5
		[SerializeField, Range(1, Searching.DirNameSearchRequest.DIR_NAME_MAXSIZE)]
		private int maxSaveSlotSize = 5;
#else
		[SerializeField, Range(1, 1024)]
		private int maxSaveSlotSize = 5;
#endif
        /// <summary>
        /// 新規セーブデータ作成時ブロックサイズ
        /// セーブデータのサイズのおおよその目安として、
        /// セーブデータの作成時にアプリケーションが保存するすべてのファイルサイズの
        /// 合計値よりも10%程度大きめの空き容量を確保することを推奨します。
        /// </summary>
#if UNITY_PS4 || UNITY_PS5
		[SerializeField, Range(Mounting.MountRequest.BLOCKS_MIN, Mounting.MountRequest.BLOCKS_MAX)]
		private int newSaveDataBlocks = Mounting.MountRequest.BLOCKS_MIN;
#else
		[SerializeField, Range(96, 32768)]
		private int newSaveDataBlocks = 96;
#endif
        /// <summary>
        /// セーブデータ検索結果ソートキー
        /// </summary>
        [SerializeField]
        private SearchingSearchSortKey m_searchSortKey = SearchingSearchSortKey.DirName;
		
        /// <summary>
        /// ソート順
        /// </summary>
        [SerializeField]
		private SearchingSearchSortOrder m_searchSortOrder = SearchingSearchSortOrder.Ascending;
		
        /// <summary>
        /// セーブデータ一覧表示時フォーカス位置
        /// </summary>
        [SerializeField]
		private DialogsFocusPos m_focusPos = DialogsFocusPos.DataLatest;
		
        /// <summary>
        /// セーブデータ一覧表示スタイル
        /// </summary>
        [SerializeField]
		private DialogsItemStyle m_itemStyle = DialogsItemStyle.SubtitleDataSize;
		
		/// <summary>
		/// 新規作成するセーブデータのデフォルトアイコン
		/// </summary>
		[SerializeField]
		[Tooltip("StreamingAssets以下のパスを入れてください")]
		private string ps4DefaultNewItemIconPath = "PS4SaveIcon.png";

		/// <summary>
		/// 新規作成するセーブデータのデフォルトアイコン
		/// </summary>
		[SerializeField]
		[Tooltip("StreamingAssets以下のパスを入れてください")]
		private string ps5DefaultNewItemIconPath = "PS5SaveIcon.png";
#pragma warning restore CS0414

#if UNITY_PS4 || UNITY_PS5
		/// <summary>
		/// セーブ時マウントモードフラグ
		/// </summary>
		private const Mounting.MountModeFlags SAVE_MOUNT_MODE_FLAGS = Mounting.MountModeFlags.Create2 | Mounting.MountModeFlags.ReadWrite;

        /// <summary>
        /// セーブデータ検索結果ソートキー
        /// </summary>
        private Searching.SearchSortKey searchSortKey => (Searching.SearchSortKey)(int)this.m_searchSortKey;

        /// <summary>
        /// ソート順
        /// </summary>
        private Searching.SearchSortOrder searchSortOrder => (Searching.SearchSortOrder)(int)this.m_searchSortOrder;

        /// <summary>
        /// セーブデータ一覧表示時フォーカス位置
        /// </summary>
        private Dialogs.FocusPos focusPos => (Dialogs.FocusPos)(int)this.m_focusPos;

        /// <summary>
        /// セーブデータ一覧表示スタイル
        /// </summary>
        private Dialogs.ItemStyle itemStyle => (Dialogs.ItemStyle)(int)this.m_itemStyle;

        /// <summary>
        /// 初期化結果
        /// </summary>
        private InitResult initResult;

        /// <summary>
        /// ログイン中ユーザー
        /// </summary>
        private PS4Input.LoggedInUser loggedInUser;

		/// <summary>
		/// セーブデータイベントコールバック
		/// </summary>
		private List<Func<SaveDataCallbackEvent, bool>> onAsyncEventList = new List<Func<SaveDataCallbackEvent, bool>>();

		/// <summary>
		/// 現在のダイアログタイプ
		/// </summary>
		private Dialogs.DialogType dialogType = Dialogs.DialogType.Invalid;

		/// <summary>
		/// 進捗ダイアログID
		/// </summary>
		private int? progressDialogId = null;

        /// <summary>
        /// 閉じたときのコールバック
        /// </summary>
        public Action onClose = null;

		/// <summary>
		/// エラーコード
		/// </summary>
		public int errorCode { get; private set; }

		/// <summary>
		/// エラーしているかどうか
		/// </summary>
		public bool isError => this.errorCode != 0;

		/// <summary>
		/// Start
		/// </summary>
		private void Start()
        {
			Main.OnAsyncEvent += this.OnAsyncEvent;

            try
            {
                var initSettings = new InitSettings { Affinity = ThreadAffinity.Core5 };
                this.initResult = Main.Initialize(initSettings);

                if (this.initResult.Initialized)
                {
                    this.loggedInUser = PS4Input.RefreshUsersDetails(0);
                }
                else
                {
                    Console.Error.WriteLine("********** SaveData not initialized **********");
                }
            }
            catch (Exception e)
            {
                this.ConsoleWriteException(e, "Start");
            }
        }

        /// <summary>
        /// 閉じる
        /// </summary>
        private void Close()
        {
			this.onAsyncEventList.Clear();
            this.onClose?.Invoke();
            this.onClose = null;
        }

		/// <summary>
		/// セーブデータイベントコールバック
		/// </summary>
		private void OnAsyncEvent(SaveDataCallbackEvent callbackEvent)
		{
			Console.WriteLine($"########## OnAsyncEvent : ApiCalled = {callbackEvent.ApiCalled}, RequestId = {callbackEvent.RequestId} ##########");

			//エラーチェック
			this.CheckError(callbackEvent);

			//コールバック発火
			for (int i = 0; i < this.onAsyncEventList.Count; i++)
			{
				bool isFired = this.onAsyncEventList[i].Invoke(callbackEvent);

				if (isFired)
				{
					//発火成功したらリストから除去
					this.onAsyncEventList.RemoveAt(i);
					i--;
				}
			}
		}

		/// <summary>
		/// エラーコードクリア
		/// </summary>
		private void ClearErrorCode()
		{
			this.errorCode = 0;
		}

        /// <summary>
        /// エラーチェック
        /// </summary>
        private void CheckError(SaveDataCallbackEvent callbackEvent)
        {
			if (this.isError)
			{
				//すでにエラー起こしてる
				return;
			}

			var response = callbackEvent.Response;
            var error = new List<string>();

            if (response == null)
            {
                error.Add($"response == null, ApiCalled = {callbackEvent.ApiCalled}");
            }
            else if (response.Locked
			|| response.IsErrorCode
            || response.ReturnCode != ReturnCodes.SUCCESS
            || response.ReturnCodeValue < 0
            || response.Exception != null)
            {
                this.errorCode = response.ReturnCodeValue;

                error.Add($"Locked = {response.Locked}");
                error.Add($"IsErrorCode = {response.IsErrorCode}");
                error.Add($"ReturnCode = {response.ReturnCode}");
                error.Add($"ReturnCodeValue = {response.ReturnCodeValue}");
                error.Add($"{response.ConvertReturnCodeToString(callbackEvent.ApiCalled)}");

                if (response.Exception != null)
                {
                    error.Add($"{response.Exception.Message}");
                    
                    if (response.Exception is SaveDataException)
                    {
                        error.Add($"{(response.Exception as SaveDataException).ExtendedMessage}");
                    }
                }
            }

            if (error.Count > 0)
            {
				if (!this.isError)
				{
					//不明なエラー
					this.errorCode = unchecked((int)0x80B8000E);
				}

                Console.Error.WriteLine($"PS4SaveDataDialog.IsError ==========>");

                if (response != null)
                {
                    Console.Error.WriteLine($"ResponseType = {response.GetType()}");
                }

                for (int i = 0; i < error.Count; i++)
                {
                    Console.Error.WriteLine(error[i]);
                }

                Console.Error.WriteLine($"<========== PS4SaveDataDialog.IsError");
            }
        }

        /// <summary>
        /// エラー出力
        /// </summary>
        private void ConsoleWriteException(Exception e, string place)
        {
            Console.Error.WriteLine($"PS4SaveDataDialog.{place} catch Exception ==========>");
            Console.Error.WriteLine($"{e.Message}");
            if (e is SaveDataException)
            {
				this.errorCode = (e as SaveDataException).SceErrorCode;
                Console.Error.WriteLine($"{(e as SaveDataException).ExtendedMessage}");
            }
            Console.Error.WriteLine($"<========== PS4SaveDataDialog.{place} catch Exception");

			if (!this.isError)
			{
				this.errorCode = unchecked((int)0x80B8000E);
			}
        }

		/// <summary>
		/// 指定スロットタイプのセーブデータが存在するか
		/// </summary>
		public void CheckExistsSlotType(SaveSlotSearch slotSearch, Action<bool> callback)
		{
			this.ClearErrorCode();
			this.dialogType = Dialogs.DialogType.Load;
			StartCoroutine(this.CheckExistsSlotTypeCoroutine(slotSearch, callback));
		}

		/// <summary>
		/// SaveSlotParams -> SaveDataParams
		/// </summary>
		private static SaveDataParams ToSaveDataParams(SaveSlotParams param) => new SaveDataParams
		{
			Title = param.title,
			SubTitle = param.subTitle,
			Detail = param.detail,
			UserParam = param.userParam,
		};

		/// <summary>
		/// SaveDataParams -> SaveSlotParams
		/// </summary>
		private static SaveSlotParams ToSaveSlotParams(SaveDataParams param) => new SaveSlotParams
		{
			title = param.Title,
			subTitle = param.SubTitle,
			detail = param.Detail,
			userParam = param.UserParam,
		};

		/// <summary>
		/// デフォルトセーブ処理（全スロットタイプ表示されてしまうやつ）
		/// </summary>
		public void DefaultSave(ISaveData saveData, SaveSlotParams param)
		{
			this.ClearErrorCode();
			this.dialogType = Dialogs.DialogType.Save;

			var newItem = this.GetNewItem(param.newItemTitle, param.newItemRawPNG);

			var saveDataParams = ToSaveDataParams(param);

			StartCoroutine(this.DefaultSaveCoroutine(saveData, newItem, saveDataParams));
		}

        /// <summary>
        /// セーブ
        /// </summary>
        public void Save(ISaveData saveData, SaveSlotParams param)
        {
			this.ClearErrorCode();
			this.dialogType = Dialogs.DialogType.Save;

            var newItem = this.GetNewItem(param.newItemTitle, param.newItemRawPNG);

			StartCoroutine(this.SaveCoroutine(saveData, newItem, param));
        }
		
        /// <summary>
        /// オートセーブ
        /// </summary>
        public void AutoSave(ISaveData saveData, SaveSlotParams param, SaveSlotType targetSlotType, int slotNo = 0)
        {
			this.ClearErrorCode();
			this.dialogType = Dialogs.DialogType.Save;

            var newItem = this.GetNewItem(param.newItemTitle, param.newItemRawPNG);

            StartCoroutine(this.AutoSaveCoroutine(saveData, targetSlotType, slotNo, newItem, param));
        }

		/// <summary>
		/// デフォルトロード処理（全スロットタイプ表示されてしまうやつ）
		/// </summary>
		public void DefaultLoad(ISaveData saveData)
		{
			this.ClearErrorCode();
			this.dialogType = Dialogs.DialogType.Load;

			StartCoroutine(this.DefaultLoadCoroutine(saveData));
		}

		/// <summary>
        /// ロード
        /// </summary>
        public void Load(ISaveData saveData, SaveSlotSearch slotSearch)
        {
			this.ClearErrorCode();
			this.dialogType = Dialogs.DialogType.Load;

			StartCoroutine(this.LoadCoroutine(saveData, slotSearch));
        }

		/// <summary>
		/// オートロード
		/// </summary>
		public void AutoLoad(ISaveData saveData, SaveSlotType targetSlotType, int slotNo = 0)
		{
			this.ClearErrorCode();
			this.dialogType = Dialogs.DialogType.Load;

			StartCoroutine(this.AutoLoadCoroutine(saveData, targetSlotType, slotNo));
		}

		/// <summary>
		/// 指定スロットタイプのセーブデータが存在するか
		/// </summary>
		private IEnumerator CheckExistsSlotTypeCoroutine(SaveSlotSearch slotSearch, Action<bool> callback)
		{
			yield return new WaitUntil(() => this.initResult.Initialized);

			bool isExists = false;

			var coroutine = this.DirNameSearch();
			yield return coroutine;

			if (!this.isError)
			{
				var searchResponse = coroutine.Current as Searching.DirNameSearchResponse;

				if (searchResponse.SaveDataItems != null)
				{
					isExists = searchResponse.SaveDataItems.Any(item => slotSearch.IsMatch(item.DirName.Data, ToSaveSlotParams(item.Params)));
				}
			}

			callback?.Invoke(isExists);
			this.Close();
		}

		/// <summary>
        /// デフォルトセーブコルーチン
        /// </summary>
        private IEnumerator DefaultSaveCoroutine(ISaveData saveData, Dialogs.NewItem newItem, SaveDataParams saveDataParams)
        {
            //初期化完了待ち
            yield return new WaitUntil(() => this.initResult.Initialized);

            //セーブデータディレクトリ検索
            var coroutine = this.DirNameSearch();
            yield return coroutine;

			if (this.isError)
			{
				//エラー
				this.Close();
				yield break;
			}

            var searchResponse = coroutine.Current as Searching.DirNameSearchResponse;
            
            var newDirName = this.GetNewDirName(SaveSlotType.UserSlot, searchResponse.SaveDataItems);

            yield return SaveDataDialogProcess.StartSaveDialogProcess(
                userId: this.loggedInUser.userId,
                newItem: newItem,
                newDirName: newDirName,
                newSaveDataBlocks: (ulong)this.newSaveDataBlocks,
                saveDataParams: saveDataParams,
                fileRequest: new PS4FileOps.FileWriteRequest
                {
                    saveData = saveData,
                },
                fileResponse: new PS4FileOps.FileWriteResponse(),
#if UNITY_PS4
				allowNewCB: _ => (_.SaveDataItems?.Length ?? 0) < this.maxSaveSlotSize,
#endif
				backup: false
			);

            //終了
            this.Close();
        }

        /// <summary>
        /// セーブコルーチン
        /// </summary>
        private IEnumerator SaveCoroutine(ISaveData saveData, Dialogs.NewItem newItem, SaveSlotParams saveSlotParams)
        {
            //初期化完了待ち
            yield return new WaitUntil(() => this.initResult.Initialized);

            //セーブデータディレクトリの検索
            var coroutine = this.DirNameSearch();
            yield return coroutine;

			if (this.isError)
            {
                //エラー
                this.Close();
                yield break;
            }

            var searchResponse = coroutine.Current as Searching.DirNameSearchResponse;

			//ユーザーセーブスロットを検索
			var searchSlotName = SaveSlotType.UserSlot.ToString();
			var userSaveDataItems = (searchResponse.SaveDataItems == null)
				? new Searching.SearchSaveDataItem[0]
				: searchResponse.SaveDataItems.Where(x => x.DirName.Data.StartsWith(searchSlotName)).ToArray();

            //一覧に表示するスロットの決定
            DirName[] dirNames = null;

			//セーブスロット最大数に達しているかどうか
			bool isMaxSaveSlot = false;

            if (userSaveDataItems.Length > 0)
            {
                dirNames = userSaveDataItems.Select(x => x.DirName).ToArray();

                if (dirNames.Length >= this.maxSaveSlotSize)
                {
                    //これ以上セーブデータ新規作成出来ない
					isMaxSaveSlot = true;
                }
            }

            //セーブデータ一覧表示
			coroutine = this.OpenSaveListDialog(dirNames, isMaxSaveSlot ? null : newItem);
            yield return coroutine;

			if (this.isError)
			{
                //エラー
                this.Close();
                yield break;
			}

            var saveListDialogResponse = coroutine.Current as Dialogs.OpenDialogResponse;

            if (saveListDialogResponse.Result.CallResult != Dialogs.DialogCallResults.OK)
            {
                //キャンセル
                this.Close();
                yield break;
            }

            //マウントするディレクトリ
            var mountDir = saveListDialogResponse.Result.DirName;

            //セーブデータ新規作成
            if (mountDir.IsEmpty)
            {
                mountDir = this.GetNewDirName(SaveSlotType.UserSlot, userSaveDataItems);
            }
            //セーブデータ上書き
            else
            {
                //上書き確認ダイアログ表示
				coroutine = this.OpenOverwriteDialog(mountDir);
                yield return coroutine;

				if (this.isError)
				{
					//エラー
					this.Close();
					yield break;
				}

				var overwriteDialogResponse = coroutine.Current as Dialogs.OpenDialogResponse;

                if (overwriteDialogResponse.Result.ButtonId != Dialogs.DialogButtonIds.Yes)
                {
                    //キャンセル
                    this.Close();
                    yield break;
                }
            }

            //セーブ進捗ダイアログ表示
            yield return this.OpenProgressDialog(null, newItem);
			
			if (this.isError)
			{
				//エラー
				this.Close();
				yield break;
			}

			//セーブ処理
            coroutine = this.SaveProcess(
                mountDir: mountDir,
                saveData: saveData,
                onUpdateProgress: (progress) => Dialogs.ProgressBarSetValue((uint)(progress * 100)),
                newItem: newItem,
                saveSlotParams: saveSlotParams
            );
            yield return coroutine;

			//セーブ進捗ダイアログ閉じる
			yield return this.CloseSaveProgressDialog(false);

            //終了
            this.Close();
        }

        /// <summary>
        /// オートセーブコルーチン
        /// </summary>
        private IEnumerator AutoSaveCoroutine(ISaveData saveData, SaveSlotType targetSlotType, int slotNo, Dialogs.NewItem newItem, SaveSlotParams saveSlotParams)
        {
            //初期化完了待ち
            yield return new WaitUntil(() => this.initResult.Initialized);

            var dirName = new DirName
            {
                Data = GetSlotName(targetSlotType, slotNo),
            };

            //セーブ処理
            var coroutine = this.SaveProcess(
                mountDir: dirName,
                saveData: saveData,
                onUpdateProgress: null,
                newItem: newItem,
                saveSlotParams: saveSlotParams
            );
            yield return coroutine;

            //終了
            this.Close();
        }
		
        /// <summary>
        /// デフォルトロードコルーチン
        /// </summary>
        private IEnumerator DefaultLoadCoroutine(ISaveData saveData)
        {
            //初期化完了待ち
            yield return new WaitUntil(() => this.initResult.Initialized);

            yield return SaveDataDialogProcess.StartLoadDialogProcess(
                userId: this.loggedInUser.userId,
                fileRequest: new PS4FileOps.FileReadRequest
				{
					saveData = saveData,
				},
                fileResponse: new PS4FileOps.FileReadResponse()
            );
            
            //終了
            this.Close();
        }

        /// <summary>
        /// ロードコルーチン
        /// </summary>
        private IEnumerator LoadCoroutine(ISaveData saveData, SaveSlotSearch slotSearch)
        {
            //初期化完了待ち
            yield return new WaitUntil(() => this.initResult.Initialized);
 
			//セーブデータディレクトリの検索
            var coroutine = this.DirNameSearch();
            yield return coroutine;

			if (this.isError)
            {
                //エラー
                this.Close();
                yield break;
            }

            var searchResponse = coroutine.Current as Searching.DirNameSearchResponse;

			//対象スロットタイプのセーブデータを検索
			var saveDataItems = (searchResponse.SaveDataItems == null)
				? new Searching.SearchSaveDataItem[0]
				: searchResponse.SaveDataItems.Where(x => slotSearch.IsMatch(x.DirName.Data, ToSaveSlotParams(x.Params))).ToArray();

			if (saveDataItems.Length == 0)
			{
				//セーブデータが無いことを通知
				yield return this.OpenNoDataErrorDialog();

				//終了
				this.Close();
				yield break;
			}

            //セーブデータ一覧表示
			coroutine = this.OpenSaveListDialog(saveDataItems.Select(x => x.DirName).ToArray(), null);
            yield return coroutine;

			if (this.isError)
			{
                //エラー
                this.Close();
                yield break;
			}

            var loadListDialogResponse = coroutine.Current as Dialogs.OpenDialogResponse;

            if (loadListDialogResponse.Result.CallResult != Dialogs.DialogCallResults.OK)
            {
                //キャンセル
                this.Close();
                yield break;
            }

            //進捗ダイアログ表示
            yield return this.OpenProgressDialog(new Dialogs.Items { DirNames = new DirName[] { loadListDialogResponse.Result.DirName } }, null);

			if (this.isError)
			{
				//エラー
				this.Close();
				yield break;
			}

			//ロード処理
			yield return this.LoadProcess(loadListDialogResponse.Result.DirName, saveData, (progress) => Dialogs.ProgressBarSetValue((uint)(progress * 100)));

			//進捗ダイアログ閉じる
			yield return this.CloseSaveProgressDialog(false);
            
            //終了
            this.Close();
        }

		/// <summary>
		/// オートロードコルーチン
		/// </summary>
		private IEnumerator AutoLoadCoroutine(ISaveData saveData, SaveSlotType targetSlotType, int slotNo = 0)
		{
            //初期化完了待ち
            yield return new WaitUntil(() => this.initResult.Initialized);

            var dirName = new DirName
            {
                Data = GetSlotName(targetSlotType, slotNo),
            };

			//ロード処理
			yield return this.LoadProcess(dirName, saveData, null);

            //終了
            this.Close();
		}

        /// <summary>
        /// 新規作成するセーブデータの見た目取得
        /// </summary>
        private Dialogs.NewItem GetNewItem(string title, byte[] rawPNG)
        {
#if UNITY_PS4
			string iconPath = this.ps4DefaultNewItemIconPath;
#elif UNITY_PS5
			string iconPath = this.ps5DefaultNewItemIconPath;
#endif
			var newItem = new Dialogs.NewItem
            {
                Title = title,
            };

            if (rawPNG != null)
            {
                newItem.RawPNG = rawPNG;
            }
			else if (!string.IsNullOrEmpty(iconPath))
            {
                newItem.IconPath = $"/app0/Media/StreamingAssets/{iconPath}";
            }

			return newItem;
        }

		/// <summary>
		/// セーブスロット名の取得
		/// </summary>
		private static string GetSlotName(SaveSlotType slotType, int slotNo)
		{
			return $"{slotType}{slotNo:0000}";
		}

        /// <summary>
        /// 新規作成するセーブデータディレクトリ名の取得
        /// </summary>
        private DirName GetNewDirName(SaveSlotType slotType, Searching.SearchSaveDataItem[] saveDataItems)
        {
            int slotNo = 0;

            if (saveDataItems != null)
            {
                while (saveDataItems.Any(x => x.DirName.Data == $"{slotType}{slotNo:0000}"))
                {
                    slotNo++;
                }
            }

            return new DirName { Data = GetSlotName(slotType, slotNo) };
        }

		/// <summary>
		/// セーブデータリクエスト呼び出し
		/// </summary>
		private IEnumerator CallSaveDataRequest<RequestType, ResponseType>(RequestType request,	ResponseType response, Func<RequestType, ResponseType, int> func, bool openErrorDialog = true)
			where RequestType : RequestBase
			where ResponseType : ResponseBase
		{
			request.UserId = this.loggedInUser.userId;

			if (request is Dialogs.OpenDialogRequest)
			{
				(request as Dialogs.OpenDialogRequest).DispType = this.dialogType;
			}

			var wait = new WaitWhile(() => response.Locked);

			bool isAlreadyError = this.isError;

			try
			{
				Console.WriteLine($"########## Start CallSaveDataRequest RequestType={typeof(RequestType)}, ResponseType={typeof(ResponseType)} ##########");
				func(request, response);
			}
			catch (Exception e)
			{
				this.ConsoleWriteException(e, "CallSaveDataRequest");
				wait = null;
			}

			yield return wait;

			Console.WriteLine($"########## End CallSaveDataRequest RequestType={typeof(RequestType)}, ResponseType={typeof(ResponseType)} ##########");

			if (openErrorDialog && !isAlreadyError && this.isError)
			{
				yield return this.OpenErrorDialog(response);
			}

			yield return response;
		}

        /// <summary>
        /// エラーダイアログを開く
        /// </summary>
        private IEnumerator OpenErrorDialog(ResponseBase response)
        {
			//進捗ダイアログ開いてたら閉じる
			yield return this.CloseSaveProgressDialog(true);

			//セーブデータ破損
            if ((uint)this.errorCode == (uint)ReturnCodes.SAVE_DATA_ERROR_BROKEN)
            {
				if (response is Mounting.MountResponse)
				{
					//TRC R4096 : セーブデータ破損通知
					yield return this.OpenBrokenErrorDialog((response as Mounting.MountResponse).MountPoint.DirName);
				}
            }
			//空きスペース不足
            else if ((uint)this.errorCode == (uint)ReturnCodes.DATA_ERROR_NO_SPACE_FS)
            {
				if (response is Mounting.MountResponse)
				{
					//TRC R4099 : 空きスペース不足通知
					yield return this.OpenNoSpaceErrorDialog((response as Mounting.MountResponse).RequiredBlocks);
				}
            }
			//その他
            else
            {
				//エラーコード通知
				yield return this.OpenErrorCodeDialog();
            }
        }

		/// <summary>
		/// セーブデータ破損通知
		/// </summary>
		private IEnumerator OpenBrokenErrorDialog(DirName dirName)
		{
			var request = new Dialogs.OpenDialogRequest
			{
				Mode = Dialogs.DialogMode.SystemMsg,
				SystemMessage = new Dialogs.SystemMessageParam
				{
					SysMsgType = Dialogs.SystemMessageType.Corrupted,
				},
				Items = new Dialogs.Items
				{
					DirNames = new DirName[] { dirName },
				},
			};

			var response = new Dialogs.OpenDialogResponse();

			yield return this.CallSaveDataRequest(request, response, Dialogs.OpenDialog);
		}

		/// <summary>
		/// 空きスペース不足通知
		/// </summary>
		private IEnumerator OpenNoSpaceErrorDialog(ulong requiredBlocks)
		{
			var request = new Dialogs.OpenDialogRequest
			{
				Mode = Dialogs.DialogMode.SystemMsg,
				SystemMessage = new Dialogs.SystemMessageParam
				{
					SysMsgType = Dialogs.SystemMessageType.NoSpaceContinuable,
					Value = requiredBlocks,
				},
			};

			var response = new Dialogs.OpenDialogResponse();

			yield return this.CallSaveDataRequest(request, response, Dialogs.OpenDialog);
		}

		/// <summary>
		/// セーブデータが無いエラー通知
		/// </summary>
		private IEnumerator OpenNoDataErrorDialog()
		{
			var request = new Dialogs.OpenDialogRequest
			{
				Mode = Dialogs.DialogMode.SystemMsg,
				SystemMessage = new Dialogs.SystemMessageParam
				{
					SysMsgType = Dialogs.SystemMessageType.NoData,
				},
			};

			var response = new Dialogs.OpenDialogResponse();

			yield return this.CallSaveDataRequest(request, response, Dialogs.OpenDialog);
		}

		/// <summary>
		/// エラーコード通知
		/// </summary>
		private IEnumerator OpenErrorCodeDialog()
		{
			var request = new Dialogs.OpenDialogRequest
			{
				Mode = Dialogs.DialogMode.ErrorCode,
				ErrorCode = new Dialogs.ErrorCodeParam
				{
					ErrorCode = this.errorCode
				},
			};

			var response = new Dialogs.OpenDialogResponse();

			yield return this.CallSaveDataRequest(request, response, Dialogs.OpenDialog);
		}

        /// <summary>
        /// セーブデータディレクトリ検索
        /// </summary>
        private IEnumerator DirNameSearch()
        {
            var request = new Searching.DirNameSearchRequest
            {
                MaxDirNameCount = Searching.DirNameSearchRequest.DIR_NAME_MAXSIZE,
                Key = this.searchSortKey,
                Order = this.searchSortOrder,
                IncludeParams = true,
                IncludeBlockInfo = true,
            };

            var response = new Searching.DirNameSearchResponse();

			/*
            if (!string.IsNullOrEmpty(slotName))
            {
                //検索対象を絞る
                request.DirName = new DirName
                {
                    Data = $"{slotName}%",
                };
            }
			*/

			yield return this.CallSaveDataRequest(request, response, Searching.DirNameSearch);
			yield return response;
        }

		/// <summary>
		/// セーブデータ一覧表示
		/// </summary>
		private IEnumerator OpenSaveListDialog(DirName[] dirNames, Dialogs.NewItem newItem)
		{
			var request = new Dialogs.OpenDialogRequest
			{
				Mode = Dialogs.DialogMode.List,
				Items = new Dialogs.Items
				{
					FocusPos = this.focusPos,
					ItemStyle = this.itemStyle,
					DirNames = dirNames,
				},
				NewItem = newItem,
			};

			var response = new Dialogs.OpenDialogResponse();

			yield return this.CallSaveDataRequest(request, response, Dialogs.OpenDialog);
            yield return response;
		}

		/// <summary>
		/// 上書き確認ダイアログ表示
		/// </summary>
		private IEnumerator OpenOverwriteDialog(DirName mountDir)
		{
            var request = new Dialogs.OpenDialogRequest
			{
				Mode = Dialogs.DialogMode.SystemMsg,
				SystemMessage = new Dialogs.SystemMessageParam
				{
					SysMsgType = Dialogs.SystemMessageType.Overwrite,
				},
				Items = new Dialogs.Items
				{
					DirNames = new DirName[] { mountDir }
				},
			};

			var response = new Dialogs.OpenDialogResponse();

			yield return this.CallSaveDataRequest(request, response, Dialogs.OpenDialog);
			yield return response;
		}

        /// <summary>
        /// 進捗ダイアログ表示
        /// </summary>
        private IEnumerator OpenProgressDialog(Dialogs.Items items, Dialogs.NewItem newItem)
        {
            var request = new Dialogs.OpenDialogRequest
            {
                UserId = this.loggedInUser.userId,
                Mode = Dialogs.DialogMode.ProgressBar,
				DispType = this.dialogType,
                ProgressBar = new Dialogs.ProgressBarParam
                {
                    BarType = Dialogs.ProgressBarType.Percentage,
                    SysMsgType = Dialogs.ProgressSystemMessageType.Progress,
                },
				Items = items,
                NewItem = newItem,
            };

            var response = new Dialogs.OpenDialogResponse();

			bool isBusy = true;

			var wait = new WaitWhile(() => isBusy || !Dialogs.DialogIsReadyToDisplay());

			bool isAlreadyError = this.isError;

			this.onAsyncEventList.Add((_) =>
			{
				if (_.ApiCalled == FunctionTypes.NotificationDialogOpened && _.RequestId == this.progressDialogId)
				{
					isBusy = false;
					return true;
				}
				return false;
			});

            try
            { 
                Console.WriteLine("########## Start OpenProgressDialog ##########");
				this.progressDialogId = Dialogs.OpenDialog(request, response);
            }
            catch (Exception e)
            {
                this.ConsoleWriteException(e, "OpenProgressDialogCoroutine");
                wait = null;
            }

			yield return wait;

            Console.WriteLine("########## End OpenProgressDialog ##########");

			if (!isAlreadyError && this.isError)
			{
				yield return this.OpenErrorDialog(response);
			}
        }

		/// <summary>
		/// 進捗ダイアログを閉じる
		/// </summary>
		private IEnumerator CloseSaveProgressDialog(bool isWait)
		{
			//進捗ダイアログが開いているなら
			if (this.progressDialogId.HasValue)
			{
				//閉じるのを待つ場合
				if (isWait)
				{
					this.onAsyncEventList.Add((_) =>
					{
						if (_.ApiCalled == FunctionTypes.NotificationDialogClosed && _.RequestId == this.progressDialogId)
						{
							this.progressDialogId = null;
							return true;
						}
						return false;
					});

					Dialogs.Close(new Dialogs.CloseParam());

					while (this.progressDialogId.HasValue)
					{
						yield return null;
					}
				}
				//閉じるのを待たない場合
				else
				{
					Dialogs.Close(new Dialogs.CloseParam());

					this.progressDialogId = null;
				}
			}
		}

        /// <summary>
        /// マウント
        /// </summary>
        private IEnumerator Mount(DirName dirName, Mounting.MountModeFlags mountMode)
        {
            var request = new Mounting.MountRequest
            {
                DirName = dirName,
                Blocks = (ulong)this.newSaveDataBlocks,
                MountMode = mountMode,
            };

            var response = new Mounting.MountResponse();

			yield return this.CallSaveDataRequest(request, response, Mounting.Mount);
			yield return response;
        }

        /// <summary>
        /// アンマウント
        /// </summary>
        private IEnumerator Unmount(Mounting.MountPointName mountPointName)
        {
            var request = new Mounting.UnmountRequest
            {
                MountPointName = mountPointName,
            };

            var response = new EmptyResponse();

			yield return this.CallSaveDataRequest(request, response, Mounting.Unmount, false);
        }

        /// <summary>
        /// ファイル書き込み
        /// </summary>
        private IEnumerator FileWrite(Mounting.MountPointName mountPointName, ISaveData saveData, Action<float> onUpdateProgress, IEnumerator writeProcess)
        {
            var request = new PS4FileOps.FileWriteRequest
            {
                MountPointName = mountPointName,
                saveData = saveData,
				onUpdateProgress = onUpdateProgress,
            };
                
            var response = new PS4FileOps.FileWriteResponse
			{
				process = writeProcess,
			};

			yield return this.CallSaveDataRequest(request, response, FileOps.CustomFileOp, false);
			yield return response;
        }

		/// <summary>
		/// ファイル読み込み
		/// </summary>
		private IEnumerator FileRead(Mounting.MountPointName mountPointName, ISaveData saveData, Action<float> onUpdateProgress)
		{
            var request = new PS4FileOps.FileReadRequest
            {
                MountPointName = mountPointName,
                saveData = saveData,
				onUpdateProgress = onUpdateProgress,
            };
                
            var response = new PS4FileOps.FileReadResponse();

			yield return this.CallSaveDataRequest(request, response, FileOps.CustomFileOp, false);
		}

        /// <summary>
        /// アイコン保存
        /// </summary>
        private IEnumerator SaveIcon(Mounting.MountPointName mountPointName, Dialogs.NewItem newItem)
        {
            var request = new Mounting.SaveIconRequest
            {
                MountPointName = mountPointName,
                IconPath = newItem.IconPath,
                RawPNG = newItem.RawPNG,
            };

            var response = new EmptyResponse();

			yield return this.CallSaveDataRequest(request, response, Mounting.SaveIcon, false);
        }

        /// <summary>
        /// マウントパラメータの保存
        /// </summary>
        private IEnumerator SetMountParams(Mounting.MountPointName mountPointName, SaveDataParams saveDataParams)
        {
            var request = new Mounting.SetMountParamsRequest
            {
                MountPointName = mountPointName,
                Params = saveDataParams,
            };

            var response = new EmptyResponse();

			yield return this.CallSaveDataRequest(request, response, Mounting.SetMountParams, false);
        }

        /// <summary>
        /// セーブ処理
        /// </summary>
        private IEnumerator SaveProcess(
            DirName mountDir,
            ISaveData saveData,
            Action<float> onUpdateProgress,
            Dialogs.NewItem newItem,
            SaveSlotParams saveSlotParams)
        {
			//スロット番号の抽出
			if (int.TryParse(System.Text.RegularExpressions.Regex.Replace(mountDir.Data, @"[^0-9]", ""), out int slotNo))
			{
				saveSlotParams.OnSetSlotNo(slotNo);
			}

			//セーブデータパラメータ変換
			var saveDataParams = ToSaveDataParams(saveSlotParams);

			//ファイル書き込みの中断データ
			IEnumerator writeProcess = null;

			//マウント中に行う処理
			//・ファイル書き込み
			//・アイコン保存
			//・マウントパラメータ保存
			var processList = new List<Func<Mounting.MountPointName, IEnumerator>>
			{
				(x) => this.FileWrite(x, saveData, onUpdateProgress, writeProcess),
				(x) => this.SaveIcon(x, newItem),
				(x) => this.SetMountParams(x, saveDataParams),
			};

			while (processList.Count > 0)
			{
				//セーブデータのマウント
				var coroutine = this.Mount(mountDir, SAVE_MOUNT_MODE_FLAGS);
				yield return coroutine;

				if (this.isError)
				{
					//エラー
					break;
				}

				var mountPointName = (coroutine.Current as Mounting.MountResponse).MountPoint.PathName;

				while (processList.Count > 0)
				{
					coroutine = processList[0].Invoke(mountPointName);
					yield return coroutine;

					if (this.isError)
					{
						//エラーしたのでアンマウント処理へ
						//エラーダイアログを表示する前にアンマウントしなければいけないので、
						//アンマウント後にエラーダイアログを表示する
						processList.Clear();
						break;
					}
					
					if (coroutine.Current is PS4FileOps.FileWriteResponse)
					{
						var fileWriteResponse = coroutine.Current as PS4FileOps.FileWriteResponse;
						if (fileWriteResponse.Progress < 1f)
						{
							//時間内に書き込み終わんなかったので中断して一旦アンマウント
							writeProcess = fileWriteResponse.process;
							break;
						}
					}
					
					//正常終了
					processList.RemoveAt(0);
				}

				//セーブデータのアンマウント
				yield return this.Unmount(mountPointName);
			}

			if (this.isError
			&& (uint)this.errorCode != (uint)ReturnCodes.SAVE_DATA_ERROR_BROKEN
			&& (uint)this.errorCode != (uint)ReturnCodes.DATA_ERROR_NO_SPACE_FS)
			{
				//マウント中に起きたエラーの通知
				yield return this.OpenErrorCodeDialog();
			}
        }

		/// <summary>
		/// ロード処理
		/// </summary>
		private IEnumerator LoadProcess(DirName mountDir, ISaveData saveData, Action<float> onUpdateProgress)
		{
			//マウント
			var coroutine = this.Mount(mountDir, Mounting.MountModeFlags.ReadOnly);
			yield return coroutine;

			if (this.isError)
			{
				//エラー
				yield break;
			}

			var mountPointName = (coroutine.Current as Mounting.MountResponse).MountPoint.PathName;

			//ファイル読み込み
			yield return this.FileRead(mountPointName, saveData, onUpdateProgress);

			//アンマウント
			yield return this.Unmount(mountPointName);

			if (this.isError)
			{
				//マウント中に起きたエラーの通知
				yield return this.OpenErrorCodeDialog();
			}
		}

#if UNITY_PS5
		/// <summary>
		/// 指定スロットタイプのセーブデータが存在するか
		/// </summary>
		public void CheckExistsSlotTypePS4(SaveSlotSearch slotSearch, string titleId, Action<bool> callback)
		{
			this.ClearErrorCode();
			this.dialogType = Dialogs.DialogType.Load;
			StartCoroutine(this.CheckExistsSlotTypePS4Coroutine(slotSearch, titleId, callback));
		}

		/// <summary>
		/// 指定スロットタイプのセーブデータが存在するか
		/// </summary>
		private IEnumerator CheckExistsSlotTypePS4Coroutine(SaveSlotSearch slotSearch, string titleId, Action<bool> callback)
		{
			yield return new WaitUntil(() => this.initResult.Initialized);

			bool isExists = false;

			var coroutine = this.DirNameSearchPS4(titleId);
			yield return coroutine;

			if (!this.isError)
			{
				var searchResponse = coroutine.Current as Searching.DirNameSearchResponse;

				if (searchResponse.SaveDataItems != null)
				{
					isExists = searchResponse.SaveDataItems.Any(item => slotSearch.IsMatch(item.DirName.Data, ToSaveSlotParams(item.Params)));
				}
			}

			callback?.Invoke(isExists);
			this.Close();
		}

		/// <summary>
        /// セーブデータディレクトリ検索
        /// </summary>
        private IEnumerator DirNameSearchPS4(string titleId)
        {
            var request = new Searching.DirNameSearchRequest
            {
                MaxDirNameCount = Searching.DirNameSearchRequest.DIR_NAME_MAXSIZE,
                Key = this.searchSortKey,
                Order = this.searchSortOrder,
                IncludeParams = true,
                IncludeBlockInfo = true,
				SearchPS4 = true,
				TitleId = new TitleId { Data = titleId },
            };

            var response = new Searching.DirNameSearchResponse();

			/*
            if (!string.IsNullOrEmpty(slotName))
            {
                //検索対象を絞る
                request.DirName = new DirName
                {
                    Data = $"{slotName}%",
                };
            }
			*/

			yield return this.CallSaveDataRequest(request, response, Searching.DirNameSearch);
			yield return response;
        }

		/// <summary>
        /// ロード
        /// </summary>
		/// <param name="fingerprint">from ps4 package generator tool. project settings->package->passcode fingerprint</param>
        public void LoadPS4(ISaveData saveData, SaveSlotSearch slotSearch, string titleId, string fingerprint, Action<object[], Action<int>> openSaveListCallback)
        {
			this.ClearErrorCode();
			this.dialogType = Dialogs.DialogType.Load;

			StartCoroutine(this.LoadPS4Coroutine(saveData, slotSearch, titleId, fingerprint, openSaveListCallback));
        }

        /// <summary>
        /// ロードコルーチン
        /// </summary>
		/// <param name="fingerprint">from ps4 package generator tool. project settings->package->passcode fingerprint</param>
        private IEnumerator LoadPS4Coroutine(ISaveData saveData, SaveSlotSearch slotSearch, string titleId, string fingerprint, Action<object[], Action<int>> openSaveListCallback)
        {
            //初期化完了待ち
            yield return new WaitUntil(() => this.initResult.Initialized);
 
			//セーブデータディレクトリの検索
            var coroutine = this.DirNameSearchPS4(titleId);
            yield return coroutine;

			if (this.isError)
            {
                //エラー
                this.Close();
                yield break;
            }

            var searchResponse = coroutine.Current as Searching.DirNameSearchResponse;

			//対象スロットタイプのセーブデータを検索
			var saveDataItems = (searchResponse.SaveDataItems == null)
				? new Searching.SearchSaveDataItem[0]
				: searchResponse.SaveDataItems.Where(x => slotSearch.IsMatch(x.DirName.Data, ToSaveSlotParams(x.Params))).ToArray();

			if (saveDataItems.Length == 0)
			{
				//セーブデータが無いことを通知
				yield return this.OpenNoDataErrorDialog();

				//終了
				this.Close();
				yield break;
			}

            //セーブデータ一覧表示
			bool isBusy = true;
			int selectedIndex = 0;

			openSaveListCallback(saveDataItems, i =>
			{
				isBusy = false;
				selectedIndex = i;
			});

            yield return new WaitWhile(() => isBusy);

			if (this.isError)
			{
                //エラー
                this.Close();
                yield break;
			}

            if (selectedIndex < 0)
            {
                //キャンセル
                this.Close();
                yield break;
            }

            //進捗ダイアログ表示
            yield return this.OpenProgressDialog(new Dialogs.Items { DirNames = new DirName[] { saveDataItems[selectedIndex].DirName } }, null);

			if (this.isError)
			{
				//エラー
				this.Close();
				yield break;
			}

			//ロード処理
			yield return this.LoadProcessPS4(saveDataItems[selectedIndex].DirName, titleId, fingerprint, saveData, (progress) => Dialogs.ProgressBarSetValue((uint)(progress * 100)));

			//進捗ダイアログ閉じる
			yield return this.CloseSaveProgressDialog(false);
            
            //終了
            this.Close();
        }

		/// <summary>
		/// ロード処理
		/// </summary>
		/// <param name="fingerprint">from ps4 package generator tool. project settings->package->passcode fingerprint</param>
		private IEnumerator LoadProcessPS4(DirName mountDir, string titleId, string fingerprint, ISaveData saveData, Action<float> onUpdateProgress)
		{
			//マウント
			var coroutine = this.MountPS4(mountDir, titleId, fingerprint);
			yield return coroutine;

			if (this.isError)
			{
				//エラー
				yield break;
			}

			var mountPointName = (coroutine.Current as Mounting.MountResponse).MountPoint.PathName;

			//ファイル読み込み
			yield return this.FileRead(mountPointName, saveData, onUpdateProgress);

			//アンマウント
			yield return this.Unmount(mountPointName);

			if (this.isError)
			{
				//マウント中に起きたエラーの通知
				yield return this.OpenErrorCodeDialog();
			}
		}

        /// <summary>
        /// マウント
        /// </summary>
		/// <param name="fingerprint">from ps4 package generator tool. project settings->package->passcode fingerprint</param>
        private IEnumerator MountPS4(DirName dirName, string titleId, string fingerprint)
        {
            var request = new Mounting.MountPS4Request
            {
                DirName = dirName,
				TitleId = new TitleId { Data = titleId },
				Fingerprint = new Fingerprint { Data = fingerprint },// from ps4 package generator tool. project settings->package->passcode fingerprint
            };

            var response = new Mounting.MountResponse();

			yield return this.CallSaveDataRequest(request, response, Mounting.MountPS4);
			yield return response;
        }
#endif

#endif
	}
}
