using UnityEngine;
using System;

namespace KG
{
	/// <summary>
	/// PlayStation Networkマネージャ
	/// </summary>
	public class PlayStationNetworkManager : SingletonMonoBehaviour<PlayStationNetworkManager>
	{
		private PlayStationNetwork psn;
		private GameIntent gameIntent;
		public Action<GameIntent> onGameIntentNotification = null;

		/// <summary>
		/// Awake
		/// </summary>
		protected override void Awake()
		{
			base.Awake();
			
			this.psn = new PlayStationNetwork();
			this.gameIntent = new GameIntent();
			this.gameIntent.onGameIntentNotification = this.OnGameIntentNotification;
		}

		/// <summary>
		/// 開始処理
		/// </summary>
		public void Start()
		{
			psn.Init();
		}

		/// <summary>
		/// 更新処理
		/// </summary>
		public void Update()
		{
			psn.Update();
		}

		/// <summary>
		/// GameIntent通知時
		/// </summary>
		private void OnGameIntentNotification(GameIntent _gameIntent)
		{
			this.onGameIntentNotification?.Invoke(_gameIntent);
		}

		//------------------------------------------------------------------------------------
		// 基本情報
		//------------------------------------------------------------------------------------
		// ・UDS Management Toolの確認方法
		// DevNetログイン後、PlayStation5を選択し、上部メニューから「タイトル」>「タイトル・プロダクト」を選択
		// 自身のタイトルを選択（※もしタイトルが無い場合は、権限がないので管理者に問い合わせてください）
		// Data Platform > Universal Data System (UDS) のサービスが登録済みであることを確認
		// 「NP サービスラベル」の列の右端の「-」アイコンをクリックし、表示されるメニューから
		// 「UDS Management Tool」を選択する、これでUDS Management Toolのページが開く

		//------------------------------------------------------------------------------------
		// トロフィー
		//------------------------------------------------------------------------------------
		// ・基本的な使い方
		// UnlockTrophy()を使ってトロフィーをアンロックできます。
		// _trophyIdに指定するIDは、
		// UDSの「Features」>「Trophies」のページに表示される任意（プラチナ以外）のトロフィーデータの
		// 「Trophy ID」を指定してください

		/// <summary>
		/// トロフィーのアンロック
		/// </summary>
		/// <param name="a_trophyId">トロフィーID</param>
		public void UnlockTrophy(int _trophyId)
		{
			psn.UnlockTrophy(_trophyId);
		}

		//------------------------------------------------------------------------------------
		// アクティビティ
		//------------------------------------------------------------------------------------
		// ・基本的な使い方
		// UDSの「Define Data」>「Objects」でアクティビティ/タスク/サブタスクの情報を確認する
		// 
		// アクティビティ/タスクに対応するシーンに遷移したら
		// ActivityAvailabilityChange() を呼んでアクティビティ/タスクを有効化し
		// ActivityStart() を呼んでアクティビティ/タスクを開始する
		// 該当アクティビティ/タスクが完了したら
		// ActivityEnd() を呼んでアクティビティ/タスクを終了させてください
		// 該当アクティビティがもう実行不可になった（次のステージに進み、前のステージはもうプレイできないような場合）は、
		// ActivityAvailabilityChange() で該当アクティビティを無効化しておいてください。
		// （※そうでないとコントロールセンターに完了したアクティビティが表示されてしまいシーンジャンプできてしまいます。）
		// 
		// メニューから別シーンに移動する際などで、ゲームインテントで元のシーンに復帰させたくない場合は
		// 都度ActivityAvailabilityChange()で該当アクティビティを無効化しておいてください
		// 元のシーンに戻ってきたら、再度ActivityAvailabilityChange()で該当アクティビティを有効化してください
		// なお、この際 ActivityStart() を呼ぶかどうかは復帰の仕方によって決めてください。
		// 該当アクティビティが最初からやり直し、のようなタイプならActivityStart()を呼んでください。
		// （該当アクティビティ内の進行状況がリセットされます）
		// 該当アクティビティが、前回までの進行状況から再開、のようなタイプなら ActivityStart() は呼ばないでください。
		//
		// セーブデータをロードして、進行状況が変わった場合は、以下の手順でアクティビティを再設定してください
		// ActivityAvailabilityChange() を呼んで現在有効な全アクティビティ/タスクを一旦無効化する
		// ActivityTerminate() を呼んで全アクティビティ/タスクの進行状況をリセットする
		// ActivityResume() を呼んで、現在進行中のアクティビティの再設定を行う
		// 必要に応じて ActivityAvailabilityChange() を呼んでアクティビティ/タスクを有効化する

		/// <summary>
		/// <para>アクティビティ/タスクの開始</para>
		/// <para>※このコマンドを実行してもコントロールセンターにアクティビティは表示されません</para>
		/// <para>コントロールセンターに表示させる場合は、ActivityAvailabilityChangeを呼んでください</para>
		/// <para>サブタスクは開始を呼ぶ必要はありません（呼んでもエラーにはらないが、意味はない）</para>
		/// </summary>
		/// <param name="_activityId">Object ID（アクティビティ時）/ ActivityId（タスク/サブタスク時）</param>
		public void ActivityStart(string _activityId)
		{
#if UNITY_PS5 && !UNITY_EDITOR
			psn.ActivityStart(_activityId);
#endif
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
#if UNITY_PS5 && !UNITY_EDITOR
			psn.ActivityEnd(_activityId, _outcome);
#endif
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
#if UNITY_PS5 && !UNITY_EDITOR
			psn.ActivityAvailabilityChange(_availableActivities, _unavailableActivities);
#endif
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
#if UNITY_PS5 && !UNITY_EDITOR
			psn.ActivityResume(_activityId, _inProgressActivities, _completedActivities);
#endif
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
#if UNITY_PS5 && !UNITY_EDITOR
			psn.ActivityTerminate();
#endif
		}

		//------------------------------------------------------------------------------------
		// ゲームインテント
		//------------------------------------------------------------------------------------
		// ・基本的な使い方
		// 可能な限り毎フレーム ExistsIntent() で通知があるかチェックする
		// 通知があったら、GetIntentType() でインテントタイプをチェックする
		// （シングルプレイヤーゲームなら KG.GameIntent.EType.eActivity だけチェックすればよい）
		// インテントタイプチェック後、GetIntentActivity()で GameIntent.SActivity 構造体を取得し
		// activityId メンバを、登録済みActivityIDと比較する
		// 登録済みActivityIDは、DevNet上のUDS Management Tool で確認できる。
		// 一致したアクティビティのシーンへ遷移する
		// （※画面遷移中などのクリティカルタイミングだった場合は、画面遷移が完了してから、
		// 改めてアクティビティのシーンへ遷移してください（「ASTRO's PLAYROOM」等はそうしています）
		// また、以下の条件を満たす場合はユーザー入力を受け付けたり、またはシーン遷移しなくてよいようです

		// 以下、DevNetのTRC R5303 より抜粋
		//-------------------------------------------------------------------------------------------------
		// ユーザーをセッションやアクティビティーに遷移させる際に、
		// タイトル画面やメニューの選択などの不必要な画面ははさまないようにしてください。
		// ただし、以下のような状況ではユーザー入力を受け付けるようにしてもかまいません。
		//
		// ・セーブデータを選択する
		// ・PlayStation™Networkへサインインする
		// ・EULAやその他の法律に関するテキストを表示するまたは同意させる
		// ・契約上表示が義務付けられている画面を起動時に表示する（例：ライセンスを受けたミドルウェアの画面）
		//
		// アプリケーションのあるエリアに入るのに条件や制約がある場合や特定のゲームプレイ上の理由で遷移ができない場合は、
		// ユーザーを直接セッションやアクティビティーに遷移させなくてもかまいません。
		// 例えば関連したエリアやセクションでゲームプレイをするのに以下のような条件や制約がある場合です。
		//
		// ・チュートリアルを完了した後にのみプレイできる
		// ・特定のファイルをダウンロードした後にのみプレイできる
		//-------------------------------------------------------------------------------------------------

		// 以下補足
		//
		// UDSの画面上部の「Define Data」>「Objects」のページに表示される
		// 「Object ID」がアクティビティIDです
		// この文字列が GameIntent.SActivity.activityId に設定されています。

		/// <summary>
		/// 通知が存在するか
		/// </summary>
		/// <returns>true / false</returns>
		public bool ExistsIntent()
		{
			return gameIntent.ExistsNotification();
		}

		/// <summary>
		/// <para>通知をクリアする</para>
		/// <para>通知に対する処理を行った後に呼んでください</para>
		/// </summary>
		public void ClearIntent()
		{
			gameIntent.ClearNotification();
		}

		/// <summary>
		/// <para>インテントタイプ取得</para>
		/// <para>通知の存在を確認したら、この関数でインテントの種類を確認してください</para>
		/// </summary>
		/// <returns>インテントタイプ</returns>
		public GameIntent.EType GetIntentType()
		{
			return gameIntent.GetIntentType();
		}

		/// <summary>
		/// アクティビティ情報構造体取得
		/// </summary>
		/// <returns>アクティビティ情報構造体</returns>
		public GameIntent.SActivity GetIntentActivity()
		{
			return gameIntent.GetActivity();
		}

		/// <summary>
		/// セッション情報構造体取得（マルチプレイヤー用）
		/// </summary>
		/// <returns>セッション情報構造体</returns>
		public GameIntent.SJoinSession GetIntentJoinSession()
		{
			return gameIntent.GetJoinSession();
		}

		/// <summary>
		/// マルチプレイヤーアクティビティ情報構造体取得（マルチプレイヤー用）
		/// </summary>
		/// <returns>マルチプレイヤーアクティビティ情報構造体</returns>
		public GameIntent.SMultiplayerActivity GetIntentMultiplayerActivity()
		{
			return gameIntent.GetMultiplayerActivity();
		}


		//---------------------------------------------------------------------------------
		//---------------------------------------------------------------------------------
		// アクティビティ、ゲームインテントサンプル
		//---------------------------------------------------------------------------------
		//---------------------------------------------------------------------------------
#if false
		// アクティビティ/タスク/サブタスクの文字列リスト
		string[] activitys	= new string[]{"area_001", "area_002", "sub_story_001", "training" };
		string[] tasks		= new string[]{"stage_001", "stage_002" };
		string[] sub_tasks	= new string[]{"enemy_001_001", "enemy_001_002", "enemy_002_001", "enemy_002_002" };

		//---------------------------------------------------------------------------------
		// アクティビティ系関数リスト
		//---------------------------------------------------------------------------------
		// 有効・無効切り替え
//		KG.PlayStationNetworkManager.Instance.ActivityAvailabilityChange(availableActivities, unavailableActivities);
		// 開始
//		KG.PlayStationNetworkManager.Instance.ActivityStart(activityId);
		// 終了
//		KG.PlayStationNetworkManager.Instance.ActivityEnd(activityId, KG.PlayStationNetwork.ACTIVITY_COMPLITE);
		// 進捗復元（ロード時などに使用）
//		KG.PlayStationNetworkManager.Instance.ActivityResume(activityId, inProgressActivities, completedActivities);
		// 全クリア
//		KG.PlayStationNetworkManager.Instance.ActivityTerminate();

		//---------------------------------------------------------------------------------
		// アクティビティサンプル
		//---------------------------------------------------------------------------------
		if(InputManager.GetKeyDown(InputManager.EButton.eCircle))
		{
			// レジューム、過去の進行状態の復元
			KG.PlayStationNetworkManager.Instance.ActivityResume(activitys[0], tasks, sub_tasks);
			// 全アクティビティを有効化
			KG.PlayStationNetworkManager.Instance.ActivityAvailabilityChange(activitys, null);
			// 全タスクを有効化
			KG.PlayStationNetworkManager.Instance.ActivityAvailabilityChange(tasks, null);
			// 指定タスクを開始
//			KG.PlayStationNetworkManager.Instance.ActivityStart(tasks[0]);
		}
		if(InputManager.GetKeyDown(InputManager.EButton.eCross))
		{
			// 指定サブタスクを終了
			KG.PlayStationNetworkManager.Instance.ActivityEnd(sub_tasks[0], KG.PlayStationNetwork.ACTIVITY_COMPLITE);
		}
		if(InputManager.GetKeyDown(InputManager.EButton.eTriangle))
		{
			// 指定タスクを終了
			KG.PlayStationNetworkManager.Instance.ActivityEnd(tasks[1], KG.PlayStationNetwork.ACTIVITY_COMPLITE);
		}
		if(InputManager.GetKeyDown(InputManager.EButton.eSquare))
		{
			// 全アクティビティ/タスク/サブタスクを無効化
			KG.PlayStationNetworkManager.Instance.ActivityAvailabilityChange(null, sub_tasks);
			KG.PlayStationNetworkManager.Instance.ActivityAvailabilityChange(null, tasks);
			KG.PlayStationNetworkManager.Instance.ActivityAvailabilityChange(null, activitys);
			// 全アクティビティ/タスクの進捗をクリア
			KG.PlayStationNetworkManager.Instance.ActivityTerminate();
		}

		//---------------------------------------------------------------------------------
		// ゲームインテント（ゲーム内通知）処理サンプル
		//---------------------------------------------------------------------------------
		// 通知取得
		KG.PlayStationNetworkManager psn = KG.PlayStationNetworkManager.Instance;
		if(psn.ExistsIntent()) {
			// 通知がある
			if(psn.GetIntentType() == KG.GameIntent.EType.eActivity) {
				// アクティビティである
				// アクティビティID取得
				activityId = psn.GetIntentActivity().activityId;
			}
			// 通知は受け取ったらクリアする
			psn.ClearIntent();
		}

		// 通知があるならシーン切り替え判定
		if(activityId != null) {
			// 対応する該当シーンに移動する
			// 即座に移動できない場合は移動できるまで待ってから移動処理を行う
			// その間に新しいアクティビティが発生した場合は、
			// 古いアクティビティは無視して新しいアクティビティを処理する（ドキュメントより）

			// アクティビティの無効化を行っても、コントロールセンターへの反映に時間がかかるため（体感5秒～10秒くらい）
			// その間に無効化されたはずのアクティビティを選択され、通知が飛んでくる場合がある
			// その場合はゲーム内ではじくなりなんなり例外対処をする必要がある
			// もし致命的なことになったらタイトルに戻してしまうのもアリらしい（ASTRO's PLAYROOMではそうしてた）
			// タイミングの問題で、無効化しているはずのアクティビティへの通知が来たら基本無視でよいと思われる

			// 以下、DevNetでのドキュメントより抜粋
			//--------------------------------------------------------------------------------
			// ゲームインテントイベントが発生した場合、アプリケーションは速やかにそのゲームインテントの情報を受信してください。
			// 受信した情報には、ゲームインテントのタイプやタイプに応じたプロパティが含まれます。
			// なお、受信前にさらにゲームインテントイベントが発生した場合など、
			// より古いゲームインテント情報の受信に失敗する場合があります。失敗は無視して、
			// 新しく発生したゲームインテントの情報を受信するようにしてください。
			//--------------------------------------------------------------------------------
			if(!sceneChange) {
				for(int ii = 0;ii < activitys.Length;ii++) {
					if(activityId == activitys[ii]) {
						if(ii == nowSceneId) {
							// 同一シーンには移動しない
							// してもいいだろうけどASTRO's PLAYROOMでやってなかったのでここでもやってない
							activityId = null;
							break;
						} else {
							nextSceneId = ii;
							sceneChange = true;
							sceneChangeState = 0;
							break;
						}
					}
				}
			}
		}

		// シーン切り替え
		// フェードアウト -> シーン切り替え -> フェードイン
		if(sceneChange) {
			switch(sceneChangeState)
			{
			case 0:
				FadeManager.FadeOut(0.5f);
				sceneChangeState++;
				break;
			case 1:
				if(FadeManager.IsFade()) break;

				sceneChangeState++;
				break;
			case 2:
				// シーン切り替え
				// 必要であればここでアクティビティ関係の処理もおこなう
				for(int ii = 0;ii < activitys.Length;ii++) {
					if(ii == nextSceneId) {
						backGround[ii].enabled = true;
					} else {
						backGround[ii].enabled = false;
					}
				}
				sleepEndFlag = false;
				StartCoroutine(sleep(1.0f));	// シーン切り替え演出用に1秒待つ
				sceneChangeState++;
				break;
			case 3:
				if(!sleepEndFlag) break;

				FadeManager.FadeIn(0.5f);
				sceneChangeState++;
				break;
			case 4:
				if(FadeManager.IsFade()) break;

				sceneChangeState++;
				break;
			case 5:
				sceneChange = false;
				sceneChangeState = 0;
				activityId = null;
				nowSceneId = nextSceneId;
				break;
			}
		}
#endif
	}
}
