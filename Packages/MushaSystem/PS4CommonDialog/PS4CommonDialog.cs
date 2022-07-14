using System;
#if UNITY_PS4
using Sony.PS4.Dialog;
using PlayStationInput = UnityEngine.PS4.PS4Input;
#elif UNITY_PS5
using Sony.PS5.Dialog;
using PlayStationInput = UnityEngine.PS5.PS5Input;
#endif

namespace KG
{
	/// <summary>
	/// PS4/PS5用CommonDialog
	/// </summary>
	public class PS4CommonDialog : SingletonMonoBehaviour<PS4CommonDialog>
	{
		/// <summary>
		/// Editorかどうか
		/// </summary>
#if UNITY_EDITOR
		private bool isEditor => true;
#else
		private bool isEditor => false;
#endif

		/// <summary>
		/// IMEダイアログが閉じたときのコールバック
		/// </summary>
		private Action<bool, string> onCloseImeDialog = null;

		/// <summary>
		/// Start
		/// </summary>
		private void Start()
		{
			if (this.isEditor)
			{
				return;
			}

#if UNITY_PS4 || UNITY_PS5
			//IMEダイアログ結果受信のコールバックを設定
			Ime.OnGotIMEDialogResult += (_) =>
			{
				var result = Ime.GetResult();
				this.onCloseImeDialog?.Invoke(result.result == Ime.EnumImeDialogResult.RESULT_OK, result.text);
				this.onCloseImeDialog = null;
			};

			//Main初期化
			Main.Initialise();
#endif
		}

		/// <summary>
		/// Update
		/// </summary>
		private void Update()
		{
			if (this.isEditor)
			{
				return;
			}

#if UNITY_PS4 || UNITY_PS5
			//毎フレームUpdate
			Main.Update();
#endif
		}

		/// <summary>
		/// IMEダイアログを開く
		/// </summary>
		public void OpenIMEDialog(IMEDialogParam param, Action<bool, string> onCloseImeDialog = null)
		{
			if (this.isEditor)
			{
				return;
			}

#if UNITY_PS4 || UNITY_PS5
			this.onCloseImeDialog = onCloseImeDialog;

			var loggedInUser = PlayStationInput.GetUsersDetails(0);

			param.imeParam.userId = (uint)loggedInUser.userId;

			Ime.Open(param.imeParam, param.imeExtendedParam);
#endif
		}

		/// <summary>
		/// IMEダイアログ用パラメータ
		/// </summary>
		public class IMEDialogParam
		{
#if UNITY_PS4 || UNITY_PS5
			/// <summary>
			/// 基本パラメータ
			/// </summary>
			public Ime.SceImeDialogParam imeParam = new Ime.SceImeDialogParam { maxTextLength = 2048 };

			/// <summary>
			/// 拡張パラメータ
			/// </summary>
			public Ime.SceImeParamExtended imeExtendedParam = new Ime.SceImeParamExtended();
#endif
			/// <summary>
			/// タイトル設定
			/// </summary>
			public void SetTitle(string title)
			{
#if UNITY_PS4 || UNITY_PS5
				this.imeParam.title = title;
#endif
			}
			
			/// <summary>
			/// 初期テキストの設定
			/// </summary>
			public void SetStartingText(string startingText)
			{
#if UNITY_PS4 || UNITY_PS5
				this.imeParam.inputTextBuffer = startingText;
#endif
			}
		}
	}
}