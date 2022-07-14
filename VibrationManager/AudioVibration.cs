using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections.Generic;

namespace KG
{
	/// <summary>
	/// オーディオ振動クラス
	/// </summary>
	public class AudioVibration
	{
		/// <summary>
		/// 振動有効・無効フラグ
		/// </summary>
		public bool Enable;

		/// <summary>
		/// 作成済みフラグ
		/// </summary>
		protected bool create;

		/// <summary>
		/// パッドID、複数パッドを使う場合は適切なIDを設定してください
		/// </summary>
		protected int padID;

		public class AudioObject
		{
			public AudioSource Source;
			public bool CreateFlag;
#if UNITY_PS5
			public AudioSource.GamepadSpeakerOutputType Type;
#endif
			public float LoopStartTime;
			public float LoopEndTime;

			public AudioObject()
			{
				CreateFlag = false;
#if UNITY_PS5
				Type = 0;
#endif
				LoopStartTime = 0.0f;
				LoopEndTime = 0.0f;
			}
		};

		List<AudioObject> m_sourceList;
		GameObject m_GameObject;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public AudioVibration()
		{
			Enable = true;
			create = false;
			padID = 0;
		}

		/// <summary>
		/// 作成
		/// </summary>
		/// <param name="obj">ゲームオブジェクト</param>
		/// <param name="padID">パッドID</param>
		public void Create(GameObject obj, int a_padID = 0)
		{
			m_GameObject = obj;
			padID = a_padID;

			create = true;
			m_sourceList = new List<AudioObject>();
		}

		/// <summary>
		/// 振動開始
		/// </summary>
		/// <param name="clip">振動波形のオーディオクリップ</param>
		/// <param name="startTime">再生位置</param>
		/// <param name="volume">ボリューム 0.0f ～ 1.0f</param>
		/// <param name="loop">ループ</param>
		/// <param name="loopStartTime">ループ開始時間[s]</param>
		/// <param name="loopEndTime">ループ終了時間[s]</param>
		/// <param name="pitch">ピッチ 0.0f ～ 1.0f</param>
		/// <param name="pan">パン -1.0f ～ 1.0f</param>
		public void Start(AudioClip clip, float startTime, float volume = 1.0f, bool loop = false, float loopStartTime = 0.0f, float loopEndTime = 0.0f, float pitch = 1.0f, float pan = 0.0f)
		{
			AudioSource source = m_GameObject.AddComponent<AudioSource>();
			source.clip = clip;
			source.loop = loop;
			source.volume = volume;
			source.pitch = pitch;
			source.panStereo = pan;
			source.time = startTime;

			Start(source, loopStartTime, loopEndTime, true);
		}

		/// <summary>
		/// 振動開始（オーディオソース指定）
		/// </summary>
		/// <param name="source">オーディオソース</param>
		/// <param name="loopStartTime">ループ開始時間[s]</param>
		/// <param name="loopEndTime">ループ終了時間[s]</param>
		public void Start(AudioSource source, float loopStartTime = 0.0f, float loopEndTime = 0.0f)
		{
			Start(source, loopStartTime, loopEndTime, false);
		}

		/// <summary>
		/// 振動開始（内部実行処理）
		/// </summary>
		/// <param name="source">オーディオソース</param>
		/// <param name="loopStartTime">ループ開始時間[s]</param>
		/// <param name="loopEndTime">ループ終了時間[s]</param>
		/// <param name="create_flag">生成フラグ</param>
		private void Start(AudioSource source, float loopStartTime, float loopEndTime, bool create_flag)
		{
#if UNITY_PS5 && !UNITY_EDITOR
			if(!Enable)
				return;
			if(!create)
				return;
			if(!AudioSource.GamepadSpeakerSupportsOutputType(AudioSource.GamepadSpeakerOutputType.Vibration))
				return;

			AudioObject audio = new AudioObject();

			audio.Type = source.gamepadSpeakerOutputType;
			audio.CreateFlag = create_flag;
			audio.Source = source;
			audio.LoopStartTime = loopStartTime;
			audio.LoopEndTime = loopEndTime;

			audio.Source.gamepadSpeakerOutputType = AudioSource.GamepadSpeakerOutputType.Vibration;

			audio.Source.PlayOnGamepad(padID);

			// 再生中リストに追加
			m_sourceList.Add(audio);
#endif
		}

		/// <summary>
		/// 振動停止（全て）
		/// </summary>
		public void StopAll()
		{
			if(!create)
				return;

			// 全てのコンポーネントを破棄する
			int count = m_sourceList.Count;

			for(int ii = count-1;ii >= 0;ii--) {
				// 停止してから破棄
				m_sourceList[ii].Source.Stop();
				Destroy(m_sourceList, ii);
			}
		}

		/// <summary>
		/// 振動停止（指定オーディオクリップ）
		/// </summary>
		/// <param name="clip">停止するオーディオクリップ</param>
		public void Stop(AudioClip clip)
		{
			if(!create)
				return;

			// 指定のオーディオクリップを破棄する
			int count = m_sourceList.Count;

			for(int ii = count-1;ii >= 0;ii--) {
				if(m_sourceList[ii].Source.clip != clip)
					continue;

				// 停止してから破棄
				m_sourceList[ii].Source.Stop();
				Destroy(m_sourceList, ii);
			}
		}

		/// <summary>
		/// 振動停止（指定オーディオソース）
		/// </summary>
		/// <param name="clip">停止するオーディオソース</param>
		public void Stop(AudioSource source)
		{
			if(!create)
				return;

			// 指定のオーディオソースを破棄する
			int count = m_sourceList.Count;

			for(int ii = count-1;ii >= 0;ii--) {
				if(m_sourceList[ii].Source != source)
					continue;

				// 停止してから破棄
				m_sourceList[ii].Source.Stop();
				Destroy(m_sourceList, ii);
			}
		}

		public void Update()
		{
			if(!create)
				return;

			float time = Time.deltaTime;
			int count = m_sourceList.Count;

			for(int ii = count-1;ii >= 0;ii--) {
				AudioSource source = m_sourceList[ii].Source;
				if(source.isPlaying) {
					// 再生中ならループ判定
					if(source.loop && (m_sourceList[ii].LoopEndTime > 0.0f) && (source.time >= m_sourceList[ii].LoopEndTime)) {
						//ループ処理
						source.time = m_sourceList[ii].LoopStartTime + (source.time - m_sourceList[ii].LoopEndTime);
					}
				} else {
					// 再生が終了したらコンポーネントを破棄する
					Destroy(m_sourceList, ii);
				}
			}
		}

		/// <summary>
		/// オーディオオブジェクトの破棄
		/// </summary>
		/// <param name="list">オーディオリスト</param>
		/// <param name="index">オーディオオブジェクトインデックス</param>
		private void Destroy(List<AudioObject> list, int index)
		{
#if UNITY_PS5
			// 出力タイプを元に戻す
			list[index].Source.gamepadSpeakerOutputType = list[index].Type;
#endif
			// オーディオソースを内部で生成している場合は、ここで破棄する
			if(list[index].CreateFlag) {
				UnityEngine.Object.Destroy(list[index].Source);
			}

			list[index] = null;
			list.RemoveAt(index);
		}
	}
}
