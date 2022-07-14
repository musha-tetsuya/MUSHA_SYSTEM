using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace KG
{
    /// <summary>
    /// サウンドトラックリスト
    /// </summary>
    public class SoundTrackList : MonoBehaviour
    {
        /// <summary>
        /// サウンドトラックプレハブ
        /// </summary>
        [SerializeField]
        private SoundTrack soundTrackPrefab = null;

        /// <summary>
        /// AudioMixerGroup
        /// </summary>
        [SerializeField]
        private AudioMixerGroup audioMixerGroup = null;

        /// <summary>
        /// 最大トラック数
        /// </summary>
        [SerializeField]
        private int maxTrackSize = 2;

        /// <summary>
        /// トラックリスト
        /// </summary>
        public List<SoundTrack> tracks { get; private set; } = new List<SoundTrack>();

		/// <summary>
        /// 空きトラックの取得
        /// </summary>
		public SoundTrack GetFreeTrack(AudioClip audioClip, int polyphonySize)
		{
			SoundTrack freeTrack = null;

			//既に発声中の同データトラックの検索
			var playingSameTracks = this.tracks.FindAll(x => x.isPlaying && x.audioSource.clip == audioClip);

			if (playingSameTracks.Count >= polyphonySize)
			{
				//既に同時発音数以上発声していたら、発声中トラックのうち一番古いトラックを利用する
				freeTrack = playingSameTracks[0];
			}
			else
			{
				//空いてるトラックを探す
				freeTrack = this.tracks.Find(x => !x.isPlaying);

				//空いてるトラックがなかった
				if (freeTrack == null)
				{
					if (this.tracks.Count < this.maxTrackSize)
					{
						//まだ余裕があるのでトラックを追加
						freeTrack = Instantiate(this.soundTrackPrefab, this.transform, false);
						freeTrack.audioSource.outputAudioMixerGroup = this.audioMixerGroup;
					}
					else
					{
						//プライオリティが低いトラックを利用する
						freeTrack = this.tracks[0];
					}
				}
			}

			return freeTrack;
		}

		/// <summary>
		/// 再生
		/// </summary>
		public void Play(SoundTrack targetTrack, AudioClip audioClip, float volume, bool loop, float loopStartNormalizedTime, float loopEndNormalizedTime, int priority)
		{
			//トラック準備
			targetTrack.Stop();
			targetTrack.Init(audioClip, volume, loop, loopStartNormalizedTime, loopEndNormalizedTime, priority);

			//再生
			this.Play(targetTrack);
		}

		/// <summary>
		/// 再生
		/// </summary>
		private void Play(SoundTrack targetTrack)
		{
			//プライオリティ順にトラックリストをソート
            this.tracks.Remove(targetTrack);
            this.tracks.Add(targetTrack);
            this.tracks.Sort((a, b) => a.priority - b.priority);

            //トラック再生
            targetTrack.gameObject.SetActive(true);
            targetTrack.Play();
            targetTrack.onStop += (track) =>
            {
				if (track != null)
				{
					track.gameObject.SetActive(false);
					track.transform.SetParent(this.transform);
				}
            };
		}

		/// <summary>
		/// 停止
		/// </summary>
		public void Stop()
        {
            for (int i = 0, imax = this.tracks.Count; i < imax; i++)
            {
                this.tracks[i]?.Stop();
            }
        }
    }
}