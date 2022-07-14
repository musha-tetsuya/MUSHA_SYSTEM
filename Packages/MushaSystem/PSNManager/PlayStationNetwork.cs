using UnityEngine;
using System;
using System.Collections;
#if UNITY_PS4
using PlatformInput = UnityEngine.PS4.PS4Input;
#elif UNITY_PS5
using PlatformInput = UnityEngine.PS5.PS5Input;
using Unity.PSN.PS5;
using Unity.PSN.PS5.Initialization;
using Unity.PSN.PS5.Users;
using Unity.PSN.PS5.Aysnc;
using Unity.PSN.PS5.UDS;
#endif

#pragma warning disable CS0162  /// 到達不能コードのwarningを抑制

namespace KG
{
	/// <summary>
	/// PlayStation Network処理
	/// </summary>
	public class PlayStationNetwork
	{
		private bool init = false;

		// アクティビティ用定義
		public const string ACTIVITY_COMPLETE   = "completed";  // 成功
		public const string ACTIVITY_FAILED     = "failed";     // 失敗
		public const string ACTIVITY_ABANDONED  = "abandoned";  // 放棄

#if UNITY_PS4 || UNITY_PS5
		/// ログイン済みユーザー
		private PlatformInput.LoggedInUser loggedInUser;
#endif
#if UNITY_PS5
		private bool    psnRegisted = false;    /// PSN初期化済みか
		private bool    udsInit     = false;    /// UDSの初期化済みか

#endif

		/// <summary>
		/// 初期化処理
		/// </summary>
		public void Init()
		{
			if(init)
				return;

#if UNITY_EDITOR
			return;
#endif
#if UNITY_PS4
			/// 最初のユーザーを使用
			loggedInUser = PlatformInput.RefreshUsersDetails(0); 
			/// NPライブラリ初期化
			InitNP();
#endif
#if UNITY_PS5
			psnRegisted = false;
			udsInit = false;

			loggedInUser = PlatformInput.RefreshUsersDetails(0);

			InitPSN();
			InitUserSystem();
			InitUniversalSystem();
#endif

			init = true;

			UnityEngine.Debug.Log("Trophy system initialized.");
		}

		/// <summary>
		/// 更新処理
		/// </summary>
		public void Update()
		{
#if UNITY_EDITOR
			return;
#endif
#if UNITY_PS4
			Sony.NP.Main.Update();
#endif
#if UNITY_PS5
			Unity.PSN.PS5.Main.Update();
#endif
		}

		/// <summary>
		/// トロフィーのアンロック
		/// </summary>
		/// <param name="a_trophyId">トロフィーID</param>
		public void UnlockTrophy(int _trophyId)
		{
#if UNITY_EDITOR
			return;
#endif
#if UNITY_PS4
			try
			{
				Sony.NP.Trophies.UnlockTrophyRequest request = new Sony.NP.Trophies.UnlockTrophyRequest();
				Sony.NP.Core.EmptyResponse response = new Sony.NP.Core.EmptyResponse();

				request.TrophyId = _trophyId;
				request.UserId = loggedInUser.userId;

				int requestId = Sony.NP.Trophies.UnlockTrophy(request, response);
			}
			catch (Sony.NP.NpToolkitException e)
			{
				Debug.Log("Exception : " + e.ExtendedMessage);
			}
#endif
#if UNITY_PS5
			if(!psnRegisted)
				return;
			if(!udsInit)
				return;
#if true
			/// トロフィー用のイベント名、プロパティ等をデフォルトで使用するならこちらで大丈夫
			UniversalDataSystem.UnlockTrophyRequest request = new UniversalDataSystem.UnlockTrophyRequest()
			{
				TrophyId = _trophyId,
				UserId = loggedInUser.userId,
			};

			var getTrophyOp = new AsyncRequest<UniversalDataSystem.UnlockTrophyRequest>(request).ContinueWith((antecedent) =>
			{
				/// Trophy unlocked for the given user (antecedent.Request.TrophyId)
				UnityEngine.Debug.Log("Unlock Trophy : ID = " + _trophyId);
			});

			UniversalDataSystem.Schedule(getTrophyOp);
#else
			/// トロフィー用のイベント名、プロパティ等をカスタムで使用するならこちら
			string EventName = "_UnlockTrophy";
			string property = "_trophy_id";

			UniversalDataSystem.UDSEvent myEvent = new UniversalDataSystem.UDSEvent();

			myEvent.Create(EventName);

			UniversalDataSystem.EventProperty prop = new UniversalDataSystem.EventProperty(property, (Int32)_trophyId);

			myEvent.Properties.Set(prop);

			UniversalDataSystem.PostEventRequest request = new UniversalDataSystem.PostEventRequest();

			request.UserId = loggedInUser.userId;
			request.EventData = myEvent;

			var getTrophyOp = new AsyncRequest<UniversalDataSystem.PostEventRequest>(request).ContinueWith((antecedent) =>
			{
				if (CheckAysncRequestOK(antecedent))
				{
					UnityEngine.Debug.Log("Trophy " + _trophyId + " unlocked");
				}
			});

			UniversalDataSystem.Schedule(getTrophyOp);
#endif
#endif
		}


#if UNITY_PS5
		/// <summary>
		/// PlayStationNetwork初期化
		/// </summary>
		private void InitPSN()
		{
#if true
			// PSNInitializeManagerが存在するならこっち
			PSNInitializeManager.Initialize();
#else
			// 存在しないなら手動で初期化
			InitResult initResult;

			try {
				initResult = Unity.PSN.PS5.Main.Initialize();

				if(initResult.Initialized == true) {
					// Initialization succeeded
					UnityEngine.Debug.Log("PSN Initialized ");
					UnityEngine.Debug.Log("Plugin SDK Version : " + initResult.SceSDKVersion.ToString());

				} else {
					// Initialization failed
					UnityEngine.Debug.Log("PSN not initialized ");
				}
			} catch(PSNException e) {
				// Exception - See e.ExtendedMessage for more info
				UnityEngine.Debug.Log("Exception During Initialization : " + e.ExtendedMessage);
			}
#endif
		}

		/// <summary>
		/// <para>Universal Data System初期化</para>
		/// <para>PS5のトロフィーはUDSシステムを利用して獲得します</para>
		/// </summary>
		private void InitUniversalSystem()
		{
			UniversalDataSystem.StartSystemRequest request = new UniversalDataSystem.StartSystemRequest();

			request.PoolSize = 256 * 1024;

			var requestOp = new AsyncRequest<UniversalDataSystem.StartSystemRequest>(request).ContinueWith((antecedent) =>
			{
				if (CheckAysncRequestOK(antecedent))
				{
					UnityEngine.Debug.Log("System Started");
					udsInit = true;
				}
			});

			UniversalDataSystem.Schedule(requestOp);
		}

		/// <summary>
		/// User System初期化
		/// </summary>
		private void InitUserSystem()
		{
			UserSystem.AddUserRequest request = new UserSystem.AddUserRequest() { UserId = loggedInUser.userId };

			var requestOp = new AsyncRequest<UserSystem.AddUserRequest>(request).ContinueWith((antecedent) =>
			{
				// エラーチェック
				if (CheckAysncRequestOK(antecedent))
				{
					UnityEngine.Debug.Log("User Initalised");
					psnRegisted = true;
				}
			});

			UserSystem.Schedule(requestOp);
		}

		/// <summary>
		/// <para>Universal Data System解放</para>
		/// <para>※基本的に呼ぶ必要はありません</para>
		/// </summary>
		public void ReleaseUniversalSystem()
		{
			UniversalDataSystem.StopSystemRequest request = new UniversalDataSystem.StopSystemRequest();

			var requestOp = new AsyncRequest<UniversalDataSystem.StopSystemRequest>(request).ContinueWith((antecedent) =>
			{
				if (CheckAysncRequestOK(antecedent))
				{
//                    OnScreenLog.Add("System Stopped");
                }
			});

			UniversalDataSystem.Schedule(requestOp);
		}

		/// <summary>
		/// <para>アクティビティ/タスクの開始</para>
		/// <para>※このコマンドを実行してもコントロールセンターにアクティビティは表示されません</para>
		/// <para>コントロールセンターに表示させる場合は、ActivityAvailabilityChangeを呼んでください</para>
		/// <para>サブタスクは開始を呼ぶ必要はありません（呼んでもエラーにはらないが、意味はない）</para>
		/// </summary>
		/// <param name="_activityId">Object ID（アクティビティ時）/ ActivityId（タスク/サブタスク時）</param>
		public void ActivityStart(string _activityId)
		{
			UniversalDataSystem.UDSEvent myEvent = new UniversalDataSystem.UDSEvent();

			myEvent.Create("activityStart");
			myEvent.Properties.Set("activityId", _activityId);

			PostEvent(myEvent);
		}

		/// <summary>
		/// <para>アクティビティ/タスク/サブタスクの終了</para>
		/// <para>例えば別シーンに移動しても、後で同じ個所に戻ってこれる場合は呼ばなくていいらしい</para>
		/// <para>サブタスクを終了させる場合は、それ以前に必ずその親タスクが開始されている必要があります</para>
		/// </summary>
		/// <param name="_activityId">Object ID（アクティビティ時）/ ActivityId（タスク/サブタスク時）</param>
		/// <param name="_outcome">completed:完了、failed:失敗、abandoned:放棄</param>
		public void ActivityEnd(string _activityId, string _outcome)
		{
			UniversalDataSystem.UDSEvent myEvent = new UniversalDataSystem.UDSEvent();

			myEvent.Create("activityEnd");
			myEvent.Properties.Set("activityId", _activityId);
			myEvent.Properties.Set("outcome", _outcome);

			PostEvent(myEvent);
		}

		/// <summary>
		/// <para>アクティビティ/タスクのプレイ可/不可設定変更</para>
		/// <para>この関数でプレイ可に設定したアクティビティ/タスクが</para>
		/// <para>コントロースセンターに表示されるようになります</para>
		/// <para>不可に設定すれば、コントロースセンターに表示されなくなります</para>
		/// <para>※タスクも明示的にプレイ可に設定する必要があります（サブタスクは多分不要）</para>
		/// <para>不可に設定したアクティビティはコントロールセンターに絶対表示されませんが</para>
		/// <para>プレイ可に設定したアクティビティが必ずコントロールセンターに表示されるとは限りません</para>
		/// <para>どのアクティビティを表示するかはシステムソフトウェアが様々なパラメータをもとに決定します</para>
		/// <para>サブタスクに対しては本関数での操作は不要です（呼んでもエラーにはならないが、意味はない）</para>
		/// </summary>
		/// <param name="_availableActivities">プレイ可能になるアクティビティ/タスクの配列、変更が無い場合はnull</param>
		/// <param name="_unavailableActivities">プレイ不可になるアクティビティ/タスクの配列、変更が無い場合はnull</param>
		public void ActivityAvailabilityChange(string[] _availableActivities, string[] _unavailableActivities)
		{
			string[] availableArray = null;
			string[] unavailableArray = null;

			if(_availableActivities != null) {
				availableArray = _availableActivities;
			} else {
				availableArray = new string[] { };
			}

			if(_unavailableActivities != null) {
				unavailableArray = _unavailableActivities;
			} else {
				unavailableArray = new string[] { };
			}

			UniversalDataSystem.EventPropertyArray availableProps = new UniversalDataSystem.EventPropertyArray(UniversalDataSystem.PropertyType.String);
			UniversalDataSystem.EventPropertyArray unavailableProps = new UniversalDataSystem.EventPropertyArray(UniversalDataSystem.PropertyType.String);

			availableProps.CopyValues(availableArray);
			unavailableProps.CopyValues(unavailableArray);

			UniversalDataSystem.UDSEvent myEvent = new UniversalDataSystem.UDSEvent();

			myEvent.Create("activityAvailabilityChange");
			myEvent.Properties.Set("mode", "delta");
			myEvent.Properties.Set("availableActivities", availableProps);
			myEvent.Properties.Set("unavailableActivities", unavailableProps);

			PostEvent(myEvent);
		}

		/// <summary>
		/// <para>アクティビティの再設定</para>
		/// <para>セーブデータをロードした場合に、</para>
		/// <para>そのセーブデータに応じたアクティビティを再設定する場合に使用します</para>
		/// <para>※activityStartでアクティビティを開始し、終了していないすべてのアクティビティに対して設定する必要があります</para>
		/// </summary>
		/// <param name="_activityId">進行中のアクティビティのObject ID</param>
		/// <param name="_inProgressActivities">進行中のタスクのアクティビティIDの配列、無いならnull</param>
		/// <param name="_completedActivities">完了しているタスク/サブタスクのアクティビティIDの配列、無いならnull</param>
		public void ActivityResume(string _activityId, string[] _inProgressActivities, string[] _completedActivities)
		{
			string[] inProgressArray = null;
			string[] completedArray = null;

			if(_inProgressActivities != null) {
				inProgressArray = _inProgressActivities;
			} else {
				inProgressArray = new string[] { };
			}

			if(_completedActivities != null) {
				completedArray = _completedActivities;
			} else {
				completedArray = new string[] { };
			}

			UniversalDataSystem.EventPropertyArray inProgressProps = new UniversalDataSystem.EventPropertyArray(UniversalDataSystem.PropertyType.String);
			UniversalDataSystem.EventPropertyArray completedProps = new UniversalDataSystem.EventPropertyArray(UniversalDataSystem.PropertyType.String);

			inProgressProps.CopyValues(inProgressArray);
			completedProps.CopyValues(completedArray);

			UniversalDataSystem.UDSEvent myEvent = new UniversalDataSystem.UDSEvent();

			myEvent.Create("activityResume");
			myEvent.Properties.Set("activityId", _activityId);
			myEvent.Properties.Set("inProgressActivities", inProgressProps);
			myEvent.Properties.Set("completedActivities", completedProps);

			PostEvent(myEvent);
		}

		/// <summary>
		/// <para>アクティビティ/タスクのクリア</para>
		/// <para>アクティビティ/タスクの達成・未達成・進捗状況をクリアします</para>
		/// <para>利用可/不可状態のクリアは行いません</para>
		/// <para>セーブデータをロードした場合にこの関数を呼んでから</para>
		/// <para>ActivityResumeで各アクティビティ/タスクの再設定を行ってください</para>
		/// </summary>
		public void ActivityTerminate()
		{
			UniversalDataSystem.UDSEvent myEvent = new UniversalDataSystem.UDSEvent();

			myEvent.Create("activityTerminate");

			PostEvent(myEvent);
		}

		/// <summary>
		/// UDSイベントのPOST
		/// </summary>
		/// <param name="_udsEvent"></param>
		private void PostEvent(UniversalDataSystem.UDSEvent _udsEvent)
		{
			UniversalDataSystem.PostEventRequest request = new UniversalDataSystem.PostEventRequest();

			request.UserId = loggedInUser.userId;
			request.CalculateEstimatedSize = true;
			request.EventData = _udsEvent;

			var requestOp = new AsyncRequest<UniversalDataSystem.PostEventRequest>(request).ContinueWith((antecedent) =>
			{
				if (CheckAysncRequestOK(antecedent))
				{
//                    OnScreenLog.Add("Event sent - Estimated size = " + antecedent.Request.EstimatedSize);
                }
				else
				{
//                    OnScreenLog.AddError("Event send error");
                }
			});

			UniversalDataSystem.Schedule(requestOp);

			UniversalDataSystem.EventDebugStringRequest stringRequest = new UniversalDataSystem.EventDebugStringRequest();

			stringRequest.UserId = loggedInUser.userId;
			stringRequest.EventData = _udsEvent;

			var secondRequestOp = new AsyncRequest<UniversalDataSystem.EventDebugStringRequest>(stringRequest).ContinueWith((antecedent) =>
			{
				if (CheckAysncRequestOK(antecedent))
				{
//                    OnScreenLog.Add(antecedent.Request.Output, true);
                }
				else
				{
//                    OnScreenLog.AddError("Event string error");
                }
			});

			UniversalDataSystem.Schedule(secondRequestOp);
		}

		/// <summary>
		/// リクエスト結果がOKだったか取得
		/// </summary>
		/// <typeparam name="R">リクエストクラスの型</typeparam>
		/// <param name="_request">リクエスト</param>
		/// <returns>OKだったか</returns>
		private static bool CheckRequestOK<R>(R _request) where R : Request
		{
			if(_request == null) {
				UnityEngine.Debug.LogError("Request is null");
				return false;
			}

			if(_request.Result.apiResult == APIResultTypes.Success) {
				return true;
			}

			//		SonyNpMain.OutputApiResult(request.Result);

			return false;
		}

		/// <summary>
		/// 非同期リクエスト結果がOKだったか取得
		/// </summary>
		/// <typeparam name="R">リクエストクラスの型</typeparam>
		/// <param name="_asyncRequest">リクエスト</param>
		/// <returns>OKだったか</returns>
		private static bool CheckAysncRequestOK<R>(AsyncRequest<R> _asyncRequest) where R : Request
		{
			if(_asyncRequest == null) {
				UnityEngine.Debug.LogError("AsyncRequest is null");
				return false;
			}

			return CheckRequestOK<R>(_asyncRequest.Request);
		}
#endif

#if UNITY_PS4
		/// <summary>
		/// NPToolkit2初期化
		/// </summary>
		private void InitNP()
		{
			/// NPToolkit2の非同期処理
			Sony.NP.Main.OnAsyncEvent += Main_OnAsyncEvent;
			Sony.NP.InitToolkit init = new Sony.NP.InitToolkit();

			/// R4208規定
			/// ネットワーク機能を使用している場合は言語別に年齢制限を設定する必要があります。
			/// デフォルト年齢制限
			init.contentRestrictions.DefaultAgeRestriction = 0;
			init.contentRestrictions.ApplyContentRestriction = false;

			/// リージョン別年齢制限
	//        Sony.NP.AgeRestriction[] ageRestrictions = new Sony.NP.AgeRestriction[1];
	//        ageRestrictions[0] = new Sony.NP.AgeRestriction(10, new Sony.NP.Core.CountryCode("us"));
	//        init.contentRestrictions.AgeRestrictions = ageRestrictions;

			/// CPUアフィニティマスク設定
			/// 指定例）Sony.NP.Affinity.Core2 | Sony.NP.Affinity.Core4
	//		init.threadSettings.affinity = Sony.NP.Affinity.Core2 | Sony.NP.Affinity.Core4;
			init.threadSettings.affinity = Sony.NP.Affinity.AllCores;	// Core2～Core5
			init.SetPushNotificationsFlags(Sony.NP.PushNotificationsFlags.None);

			try
			{
				Sony.NP.Main.Initialize(init);
				RegisterTrophyPack();
			}
			catch (Sony.NP.NpToolkitException e)
			{
				Debug.Log("Error initializing the NPToolkit2 : " + e.ExtendedMessage);
			}
		}

		/// <summary>
		/// トロフィーパック登録
		/// </summary>
		private void RegisterTrophyPack()
		{
			try
			{
				Sony.NP.Core.EmptyResponse response = new Sony.NP.Core.EmptyResponse();
				Sony.NP.Trophies.RegisterTrophyPackRequest request = new Sony.NP.Trophies.RegisterTrophyPackRequest();

				request.UserId = loggedInUser.userId;

				int requestId = Sony.NP.Trophies.RegisterTrophyPack(request, response);
			}
			catch (Sony.NP.NpToolkitException e)
			{
				Debug.Log("Exception : " + e.ExtendedMessage);
			}
		}

		/// <summary>
		/// <para>Sony.NP系の非同期イベントコールバック処理</para>
		/// <para>使用するにはNpToolkit2が初期化済みである必要があります</para>
		/// </summary>
		/// <param name="callbackEvent">コールバックイベント</param>
		private void Main_OnAsyncEvent(Sony.NP.NpCallbackEvent callbackEvent)
		{
			//Print some useful info on the current event: 
	//        Debug.Log("Event: Service = (" + callbackEvent.Service + ") : API Called = (" + callbackEvent.ApiCalled + ") : Request Id = (" + callbackEvent.NpRequestId + ") : Calling User Id = (" + callbackEvent.UserId + ")");
			HandleAsyncEvent(callbackEvent);
		}

		/// <summary>
		/// リクエスト応答管理
		/// </summary>
		/// <param name="callbackEvent">コールバックイベント</param>
		private void HandleAsyncEvent(Sony.NP.NpCallbackEvent callbackEvent)
		{
			try
			{
				if (callbackEvent.Response != null)
				{
					if (callbackEvent.Response.ReturnCodeValue < 0)
					{
						Debug.LogError("Response : " + callbackEvent.Response.ConvertReturnCodeToString(callbackEvent.ApiCalled));
					}
					else
					{
						/// トロフィーアンロックコールバック
						if(callbackEvent.ApiCalled == Sony.NP.FunctionTypes.TrophyUnlock)
						{
	//                        Debug.Log("Trophy Unlock : " + callbackEvent.Response.ConvertReturnCodeToString(callbackEvent.ApiCalled));
						}
					}
				}
			}
			catch ( Sony.NP.NpToolkitException e)
			{
				Debug.Log("Main_OnAsyncEvent Exception = " + e.ExtendedMessage);
			}
		}
#endif
	}
}