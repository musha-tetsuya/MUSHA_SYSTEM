using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace KG
{
    /// <summary>
    /// サウンド機能
    /// </summary>
    public class SoundManager : SingletonMonoBehaviour<SoundManager>
    {
        /// <summary>
        /// AudioListener
        /// </summary>
        [SerializeField]
        public AudioListener audioListener = null;

        /// <summary>
        /// AudioMixer
        /// </summary>
        [SerializeField]
        private AudioMixer audioMixer = null;

        /// <summary>
        /// 音量変化カーブ
        /// </summary>
        /// <remarks>
        /// -80db～0dbを0f～1fにそのまま割り当てると0.5fでも音が小さくなりすぎてしまうので、カーブに沿って音量調整するためのやつ
        /// </remarks>
        [SerializeField]
        private AnimationCurve volumeCurve = null;

        /// <summary>
        /// BGMトラックリスト
        /// </summary>
        [SerializeField]
        private SoundTrackList bgmTrackList = null;

        /// <summary>
        /// SEトラックリスト
        /// </summary>
        [SerializeField]
        private SoundTrackList seTrackList = null;

        /// <summary>
        /// ボイストラックリスト
        /// </summary>
        [SerializeField]
        private SoundTrackList voiceTrackList = null;

        /// <summary>
        /// 名前管理ボイストラックリスト
        /// </summary>
		private List<VoiceTrackNameData> voiceTrackNameList = new List<VoiceTrackNameData>();

        /// <summary>
        /// マスター音量
        /// </summary>
        private float m_masterVolume = 1f;
        public float masterVolume
        {
            get => this.m_masterVolume;
            set => this.SetVolume("Master Volume", value, ref this.m_masterVolume);
        }

        /// <summary>
        /// BGM音量
        /// </summary>
        private float m_bgmVolume = 1f;
        public float bgmVolume
        {
            get => this.m_bgmVolume;
            set => this.SetVolume("Bgm Volume", value, ref this.m_bgmVolume);
        }

        /// <summary>
        /// SE音量
        /// </summary>
        private float m_seVolume = 1f;
        public float seVolume
        {
            get => this.m_seVolume;
            set => this.SetVolume("Se Volume", value, ref this.m_seVolume);
        }

        /// <summary>
        /// ボイス音量
        /// </summary>
        private float m_voiceVolume = 1f;
        public float voiceVolume
        {
            get => this.m_voiceVolume;
            set => this.SetVolume("Voice Volume", value, ref this.m_voiceVolume);
        }

        /// <summary>
        /// 音量設定
        /// </summary>
        private void SetVolume(string name, float value, ref float volume)
        {
            volume = Mathf.Clamp01(value);
            value = this.volumeCurve.Evaluate(value);
            value = Mathf.Lerp(-80f, 0f, value);
            this.audioMixer.SetFloat(name, value);
        }

        /// <summary>
        /// BGM再生
        /// </summary>
		public SoundTrack PlayBgm(AudioClip bgmClip, float volume = 1f, bool loop = true, float loopStartNormalizedTime = 0f, float loopEndNormalizedTime = 1f, int priority = 0)
		{
			if (bgmClip == null)
			{
				return null;
			}

			this.bgmTrackList.tracks.RemoveAll(x => x == null);

            //既に再生中かどうか検索
            var playingSameTracks = this.bgmTrackList.tracks.FindAll(x => x.isPlaying && x.audioSource.clip == bgmClip);

            if (playingSameTracks.Count > 0)
            {
                //再生中のトラックを返却
                return playingSameTracks[0];
            }
            else
            {
                //空きトラックを取得し再生
                var freeTrack = this.bgmTrackList.GetFreeTrack(bgmClip, 1);
                this.bgmTrackList.Play(freeTrack, bgmClip, volume, loop, loopStartNormalizedTime, loopEndNormalizedTime, priority);
                return freeTrack;
            }
		}

        /// <summary>
        /// BGM再生
        /// </summary>
        public SoundTrack PlayBgm(SoundData bgmData, bool loop = true, int priority = 0)
        {
			return this.PlayBgm(bgmData.audioClip, bgmData.volume, loop, bgmData.loopStartNormalizedTime, bgmData.loopEndNormalizedTime, priority);
        }

        /// <summary>
        /// 全BGM停止
        /// </summary>
        public void StopAllBgm()
        {
            this.bgmTrackList.Stop();
        }

		/// <summary>
		/// SE再生
		/// </summary>
		public SoundTrack PlaySe(AudioClip seClip, float volume = 1f,  bool loop = false, float loopStartNormalizedTime = 0f, float loopEndNormalizedTime = 1f, int priority = 0, int polyphonySize = 2)
		{
			if (seClip == null)
			{
				return null;
			}

			this.seTrackList.tracks.RemoveAll(x => x == null);

			//空きトラックを取得し再生
			var freeTrack = this.seTrackList.GetFreeTrack(seClip, polyphonySize);
			this.seTrackList.Play(freeTrack, seClip, volume, loop, loopStartNormalizedTime, loopEndNormalizedTime, priority);
			return freeTrack;
		}

        /// <summary>
        /// SE再生
        /// </summary>
        public SoundTrack PlaySe(SoundData seData, bool loop = false, int priority = 0, int polyphonySize = 2)
        {
			return this.PlaySe(seData.audioClip, seData.volume, loop, seData.loopStartNormalizedTime, seData.loopEndNormalizedTime, priority, polyphonySize);
        }

        /// <summary>
        /// 全SE停止
        /// </summary>
        public void StopAllSe()
        {
            this.seTrackList.Stop();
        }

		/// <summary>
        /// ボイス再生
        /// </summary>
        /// <remarks>
        /// trackNameでトラックを指定して再生できる。
        /// </remarks>
        public SoundTrack PlayVoice(AudioClip voiceClip, string trackName, float volume = 1f, bool loop = false, float loopStartNormalizedTime = 0f, float loopEndNormalizedTime = 1f, int priority = 0)
        {
			if (voiceClip == null)
			{
				return null;
			}

            this.voiceTrackList.tracks.RemoveAll(x => x == null || x.gameObject == null);

            SoundTrack targetTrack = null;

            //トラック指定がある
            if (!string.IsNullOrEmpty(trackName))
            {
				var data = this.voiceTrackNameList.Find(_ => _.trackName == trackName);

                if (data != null)
                {
                    if (data.track == null || data.track.gameObject == null)
                    {
						//何かしらの理由で名前管理されているトラックがnullってたら管理から外す
						this.voiceTrackNameList.Remove(data);
                    }
                    else
                    {
                        //指定トラックを取得
                        targetTrack = data.track;
                    }
                }
            }

            //指定トラックが見つからなかった
            if (targetTrack == null)
            {
                //空きトラックを取得
                targetTrack = this.voiceTrackList.GetFreeTrack(voiceClip, 1);

				var data = this.voiceTrackNameList.Find(_ => _.track == targetTrack);

				//取得した空きトラックが名前管理されているトラックなら
				if (data != null)
				{
					if (string.IsNullOrEmpty(trackName))
					{
						//トラック指定されてないので名前管理から外す
						this.voiceTrackNameList.Remove(data);
					}
					else
					{
						//指定されたトラック名で管理し直す
						data.trackName = trackName;
					}
				}
				//名前管理されていない空きトラックを名前管理する
				else if (!string.IsNullOrEmpty(trackName))
				{ 
					data = new VoiceTrackNameData();
					data.track = targetTrack;
					data.trackName = trackName;
					this.voiceTrackNameList.Add(data);
				}
            }

            //トラック再生
            this.voiceTrackList.Play(targetTrack, voiceClip, volume, loop, loopStartNormalizedTime, loopEndNormalizedTime, priority);

            return targetTrack;
        }

        /// <summary>
        /// ボイス再生
        /// </summary>
        /// <remarks>
        /// trackNameでトラックを指定して再生できる。
        /// </remarks>
        public SoundTrack PlayVoice(SoundData voiceData, string trackName, bool loop = false, int priority = 0)
        {
            return this.PlayVoice(voiceData.audioClip, trackName, voiceData.volume, loop, voiceData.loopStartNormalizedTime, voiceData.loopEndNormalizedTime, priority);
        }

        /// <summary>
        /// 全ボイス停止
        /// </summary>
        public void StopAllVoice()
        {
            this.voiceTrackList.Stop();
        }

		/// <summary>
		/// ボイストラック名データ
		/// </summary>
		private class VoiceTrackNameData
		{
			/// <summary>
			/// サウンドトラック
			/// </summary>
			public SoundTrack track;

			/// <summary>
			/// トラック名
			/// </summary>
			public string trackName;
		}
    }
}