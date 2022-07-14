using UnityEngine;
using UnityEngine.Serialization;
using System;

namespace KG
{
	/// <summary>
	/// 振動クラスマネージャ
	/// </summary>
	public class VibrationManager : SingletonMonoBehaviour<VibrationManager>
	{
		/// <summary>
		/// パッド数、複数人で遊ぶゲームは遊ぶ最大人数を設定してください
		/// </summary>
		readonly int PAD_NUMS = 1;

		/// <summary>
		/// オーディオ振動クラス
		/// </summary>
		KG.AudioVibration[] audioVibration;

		void Start()
		{
			audioVibration = new KG.AudioVibration[PAD_NUMS];
			for(int ii = 0;ii < PAD_NUMS;ii++) {
				audioVibration[ii] = new KG.AudioVibration();
				audioVibration[ii].Create(gameObject, ii);
			}
		}

		/// <summary>
		/// オーディオ振動開始
		/// </summary>
		/// <param name="clip">振動に使用するオーディオクリップ</param>
		/// <param name="startTime">再生位置</param>
		/// <param name="volume">ボリューム 0.0f ～ 1.0f</param>
		/// <param name="loop">ループ</param>
		/// <param name="loopStartTime">ループ開始時間[s]</param>
		/// <param name="loopEndTime">ループ終了時間[s]</param>
		/// <param name="pitch">ピッチ 0.0f ～ 1.0f</param>
		/// <param name="pan">パン -1.0f ～ 1.0f</param>
		/// <param name="padID">パッドID（0～n）</param>
		public void AudioVibrationStart(AudioClip clip, float startTime = 0.0f, float volume = 1.0f, bool loop = false, float loopStartTime = 0.0f, float loopEndTime = 0.0f, float pitch = 1.0f, float pan = 0.0f, int padID = 0)
		{
			audioVibration[padID].Start(clip, startTime, volume, loop, loopStartTime, loopEndTime, pitch, pan);
		}

		/// <summary>
		/// オーディオ振動開始
		/// </summary>
		/// <param name="souce">振動に使用するオーディオソース</param>
		/// <param name="loopStartTime">ループ開始時間[s]</param>
		/// <param name="loopEndTime">ループ終了時間[s]</param>
		/// <param name="padID">パッドID（0～n）</param>
		public void AudioVibrationStart(AudioSource souce, float loopStartTime = 0.0f, float loopEndTime = 0.0f, int padID = 0)
		{
			audioVibration[padID].Start(souce, loopStartTime, loopEndTime);
		}

		/// <summary>
		/// 振動停止
		/// </summary>
		/// <param name="padID">パッドID（0～n）</param>
		public void VibrationStop(int padID = 0)
		{
			audioVibration[padID].StopAll();
		}

		public void Update()
		{
			for(int ii = 0;ii < PAD_NUMS;ii++) {
				audioVibration[ii].Update();
			}
		}
	}
}