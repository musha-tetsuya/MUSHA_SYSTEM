using System;
#if UNITY_PS5 || UNITY_PS4
using Unity.PSN.PS5.GameIntent;
#endif

namespace KG
{
	/// <summary>
	/// ゲームインテント
	/// </summary>
	public class GameIntent
	{
		/// <summary>
		/// インテントタイプ
		/// </summary>
		public enum EType
		{
			eNone = -1,

			eActivity,              // アクティビティ
			eJoinSession,           // セッションへの参加（マルチプレイヤー用）
			eMultiplayerActivity,   // マルチプレイヤーアクティビティ（マルチプレイヤー用）
		}

		/// <summary>
		/// アクティビティ情報用構造体
		/// </summary>
		public struct SActivity
		{
			/// <summary>
			/// アクティビティID（UDS上ではObject IDと表記されてます）
			/// </summary>
			public string activityId;
		}

		/// <summary>
		/// セッション情報用構造体（マルチプレイヤー用）
		/// </summary>
		public struct SJoinSession
		{
			public string playerSessionId;
			public int memberType;
		}

		/// <summary>
		/// マルチプレイヤーアクティビティ情報用構造体（マルチプレイヤー用）
		/// </summary>
		public struct SMultiplayerActivity
		{
			public string activityId;
			public string playerSessionId;
		}

		/// <summary>
		/// 通知が存在するかフラグ
		/// </summary>
		private bool notification;

		/// <summary>
		/// インテントタイプ
		/// </summary>
		private EType type;

		/// <summary>
		/// 通知が行われたユーザーID
		/// </summary>
		private int userId;

		private SActivity activity;
		private SJoinSession joinSession;
		private SMultiplayerActivity multiplayerActivity;
		public Action<GameIntent> onGameIntentNotification = null;

		/// <summary>
		/// 通知が存在するか
		/// </summary>
		/// <returns>true / false</returns>
		public bool ExistsNotification()
		{
			return notification;
		}

		/// <summary>
		/// <para>通知をクリアする</para>
		/// <para>通知に対する処理を行った後に呼んでください</para>
		/// </summary>
		public void ClearNotification()
		{
			notification = false;
		}

		/// <summary>
		/// <para>インテントタイプ取得</para>
		/// <para>通知の存在を確認したら、この関数でインテントの種類を確認してください</para>
		/// </summary>
		/// <returns>インテントタイプ</returns>
		public EType GetIntentType()
		{
			return type;
		}

		/// <summary>
		/// 通知が発生したユーザーID取得
		/// </summary>
		/// <returns></returns>
		public int GetUserId()
		{
			return userId;
		}

		/// <summary>
		/// アクティビティ情報構造体取得
		/// </summary>
		/// <returns>アクティビティ情報構造体</returns>
		public SActivity GetActivity()
		{
			return activity;
		}

		/// <summary>
		/// セッション情報構造体取得（マルチプレイヤー用）
		/// </summary>
		/// <returns>セッション情報構造体</returns>
		public SJoinSession GetJoinSession()
		{
			return joinSession;
		}

		/// <summary>
		/// マルチプレイヤーアクティビティ情報構造体取得（マルチプレイヤー用）
		/// </summary>
		/// <returns>マルチプレイヤーアクティビティ情報構造体</returns>
		public SMultiplayerActivity GetMultiplayerActivity()
		{
			return multiplayerActivity;
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public GameIntent()
		{
			userId = 0;
			notification = false;
			type = EType.eNone;
			activity = new SActivity();
			joinSession = new SJoinSession();
			multiplayerActivity = new SMultiplayerActivity();

#if UNITY_PS5 || UNITY_PS4
			GameIntentSystem.OnGameIntentNotification += OnGameIntentNotification;
#endif
		}

#if UNITY_PS5 || UNITY_PS4
		/// <summary>
		/// ゲームインテントコールバック処理
		/// </summary>
		/// <param name="_gameIntent">通知内容が格納されたゲームインテントオブジェクト</param>
		private void OnGameIntentNotification(GameIntentSystem.GameIntent _gameIntent)
		{
			userId = _gameIntent.UserId;

			if(_gameIntent is GameIntentSystem.LaunchActivity) {
				// アクティビティ
				GameIntentSystem.LaunchActivity act = _gameIntent as GameIntentSystem.LaunchActivity;
				activity.activityId = act.ActivityId;
				type = EType.eActivity;
			} else if(_gameIntent is GameIntentSystem.JoinSession) {
				// セッション参加
				GameIntentSystem.JoinSession js = _gameIntent as GameIntentSystem.JoinSession;
				joinSession.playerSessionId = js.PlayerSessionId;
				joinSession.memberType = (int)js.MemberType;
				type = EType.eJoinSession;
			} else if(_gameIntent is GameIntentSystem.LaunchMultiplayerActivity) {
				// マルチプレイヤーアクティビティ
				GameIntentSystem.LaunchMultiplayerActivity act = _gameIntent as GameIntentSystem.LaunchMultiplayerActivity;
				multiplayerActivity.activityId = act.ActivityId;
				multiplayerActivity.playerSessionId = act.PlayerSessionId;
				type = EType.eMultiplayerActivity;
			}

			notification = true;
			
			this.onGameIntentNotification?.Invoke(this);
		}
#endif
	}
}
