using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;

namespace KG
{
    /// <summary>
    /// セーブデータインターフェース
    /// </summary>
    public interface ISaveData
    {
        /// <summary>
        /// セーブ/ロード進捗 0f～1f
        /// </summary>
        float Progress { get; }

		/// <summary>
		/// 読み書き対象ファイルセット
		/// </summary>
		void SetFileStream(FileStream fs);

		/// <summary>
		/// 暗号化・複合化用AESオブジェクトセット
		/// </summary>
		void SetAesManaged(AesManaged aes);

        /// <summary>
        /// セーブ
        /// </summary>
        IEnumerator Save();

        /// <summary>
        /// ロード
        /// </summary>
        IEnumerator Load();
    }

    /// <summary>
    /// サンプルセーブデータオペレーション
    /// </summary>
    public class SampleSaveDataOperation<T> : ISaveData
    {
        /// <summary>
        /// セーブデータ実体
        /// </summary>
        public T data;

        /// <summary>
        /// セーブ/ロード進捗 0f～1f
        /// </summary>
        private float progress = 0f;

		/// <summary>
		/// 読み書き対象ファイル
		/// </summary>
		private FileStream fs = null;

		/// <summary>
		/// 暗号化・複合化用AESオブジェクト
		/// </summary>
		private AesManaged aes = null;

        /// <summary>
        /// 読み書き速度 byte
        /// </summary>
        private int speed = 1024 * 10;

        /// <summary>
        /// construct
        /// </summary>
        public SampleSaveDataOperation(int speed = 1024 * 10)
        {
            this.speed = speed;
        }

        /// <summary>
        /// セーブ/ロード進捗 0f～1f
        /// </summary>
        float ISaveData.Progress => this.progress;

		/// <summary>
		/// 読み書き対象ファイルセット
		/// </summary>
		void ISaveData.SetFileStream(FileStream fs)
		{
			this.fs = fs;
		}

		/// <summary>
		/// 暗号化・複合化用AESオブジェクトセット
		/// </summary>
		void ISaveData.SetAesManaged(AesManaged aes)
		{
			this.aes = aes;
		}

        /// <summary>
        /// セーブ
        /// </summary>
        IEnumerator ISaveData.Save()
        {
            this.progress = 0f;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
			string json = Newtonsoft.Json.JsonConvert.SerializeObject(this.data, Newtonsoft.Json.Formatting.Indented);
#else
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(this.data);
#endif

			if (this.aes != null)
			{
				//暗号化
				json = this.aes.Encrypt(json);
			}
                
            int byteSize = Encoding.UTF8.GetByteCount(json);

            int writeSpeed = Mathf.CeilToInt((float)byteSize / this.speed);

            writeSpeed = Mathf.CeilToInt(json.Length / writeSpeed);

            int pos = 0;

            while (pos < json.Length)
            {
                int writeSize = Mathf.Min(json.Length - pos, writeSpeed);
				
				byte[] bytes = Encoding.UTF8.GetBytes(json.Substring(pos, writeSize));

                this.fs.Write(bytes, 0, bytes.Length);

                pos += writeSize;

                this.progress = (float)pos / json.Length;

                yield return null;
            }

            this.progress = 1f;

			this.OnFinishedSave();
		}

		/// <summary>
		/// セーブ終了時
		/// </summary>
		protected virtual void OnFinishedSave() { }

        /// <summary>
        /// ロード
        /// </summary>
        IEnumerator ISaveData.Load()
        {
            this.progress = 0f;

            byte[] buffer = new byte[this.fs.Length];
                
            int pos = 0;

            while (pos < buffer.Length)
            {
                int readSize = Mathf.Min(buffer.Length - pos, this.speed);

                this.fs.Read(buffer, pos, readSize);

                pos += readSize;

                this.progress = (float)pos / buffer.Length;

                yield return null;
            }

            string json = Encoding.UTF8.GetString(buffer);

			if (this.aes != null)
			{
				//複合化
				json = this.aes.Decrypt(json);
			}
                
            this.data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);

            this.progress = 1f;

			this.OnFinishedLoad();
		}

		/// <summary>
		/// ロード終了時
		/// </summary>
		protected virtual void OnFinishedLoad() { }
    }
}