using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KG
{
    /// <summary>
    /// サウンドトラック
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SoundTrack : MonoBehaviour
    {
        /// <summary>
        /// AudioSource
        /// </summary>
        [SerializeField]
        public AudioSource audioSource = null;

        /// <summary>
        /// 再生中かどうか
        /// </summary>
        public bool isPlaying { get; private set; }

        /// <summary>
        /// 一時停止中かどうか
        /// </summary>
        public bool isPause { get; private set; }

        /// <summary>
        /// プライオリティ
        /// </summary>
        public int priority { get; private set; }

        /// <summary>
        /// ループ開始位置
        /// </summary>
        private float loopStartTime = 0f;

        /// <summary>
        /// ループ終了位置
        /// </summary>
        private float loopEndTime = 0f;

        /// <summary>
        /// 現在位置
        /// </summary>
        private float time = 0f;

        /// <summary>
        /// 停止時コールバック
        /// </summary>
        public Action<SoundTrack> onStop = null;

        /// <summary>
        /// Update
        /// </summary>
        private void Update()
        {
            //再生中
            if (this.isPlaying && !this.isPause)
            {
                if (this.audioSource.isPlaying)
                {
                    if (this.audioSource.loop)
                    {
                        //現在位置がループ開始位置よりも手前になった場合
                        if (this.audioSource.time < this.loopStartTime && this.audioSource.time < this.time)
                        {
                            //ループ処理
                            this.audioSource.time = this.loopStartTime + this.audioSource.time;
                        }
                        //現在位置がループ終了位置を超えた場合
                        else if (this.audioSource.time >= this.loopEndTime)
                        {
                            //ループ処理
                            this.audioSource.time = this.loopStartTime + (this.audioSource.time - this.loopEndTime);
                        }
                    }

                    //現在位置更新
                    this.time = this.audioSource.time;
                }
                else
                {
                    //停止
                    this.Stop();
                }
            }
        }

		/// <summary>
		/// 初期化
		/// </summary>
		public void Init(AudioClip audioClip, float volume, bool loop, float loopStartNormalizedTime, float loopEndNormalizedTime, int priority)
		{
			this.audioSource.clip = audioClip;
			this.audioSource.volume = volume;
			this.audioSource.loop = loop;
			this.priority = priority;
			this.loopStartTime = this.audioSource.clip.length * loopStartNormalizedTime;
			this.loopEndTime = this.audioSource.clip.length * loopEndNormalizedTime;
			this.Set3dSettings(0f);
		}

		/// <summary>
		/// 3Dサウンド設定
		/// </summary>
		public void Set3dSettings(float spatialBlend = 1f, float minDistance = 0f, float maxDistance = 100f)
		{
			this.audioSource.spatialBlend = spatialBlend;
			this.audioSource.minDistance = minDistance;
			this.audioSource.maxDistance = maxDistance;
		}

        /// <summary>
        /// 再生
        /// </summary>
        public void Play()
        {
            if (!this.isPlaying || this.isPause)
            {
                this.isPlaying = true;
                this.isPause = false;
                this.audioSource.Play();
                this.audioSource.time = this.time;
            }
        }

        /// <summary>
        /// 一時停止（再開する場合はPlay()を呼ぶ）
        /// </summary>
        public void Pause()
        {
            if (this.isPlaying && !this.isPause)
            {
                this.isPause = true;
                this.time = this.audioSource.time;
                this.audioSource.Stop();
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            if (this.isPlaying)
            {
                this.isPlaying = false;
                this.isPause = false;
                this.time = 0f;
				if (this.audioSource != null) { this.audioSource?.Stop(); }
                this.onStop?.Invoke(this);
                this.onStop = null;
            }
        }

        /// <summary>
        /// フェードイン
        /// </summary>
        public void FadeIn(float fadeTime, Action onFinished = null)
        {
            StartCoroutine(this.Fade(0f, this.audioSource.volume, fadeTime, onFinished));
        }

        /// <summary>
        /// フェードアウト
        /// </summary>
        public void FadeOut(float fadeTime, Action onFinished = null)
        {
            StartCoroutine(this.Fade(this.audioSource.volume, 0f, fadeTime, onFinished));
        }

        /// <summary>
        /// フェード処理
        /// </summary>
        private IEnumerator Fade(float startVolume, float endVolume, float fadeTime, Action onFinished = null)
        {
            float time = 0f;

            while (time < fadeTime)
            {
                if (this.isPlaying && !this.isPause)
                {
                    this.audioSource.volume = Mathf.Lerp(startVolume, endVolume, time / fadeTime);
                    time += Time.deltaTime;
                }
                yield return null;
            }

            this.audioSource.volume = endVolume;

            onFinished?.Invoke();
        }
    }
}