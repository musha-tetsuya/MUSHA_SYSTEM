#if UNITY_PS4 || UNITY_PS5
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
#if UNITY_PS4
using Sony.PS4.SaveData;
#elif UNITY_PS5
using Unity.SaveData.PS5.Info;
using Unity.SaveData.PS5.Mount;
#endif

namespace KG
{
	/// <summary>
	/// PS4ファイルオペレーション
	/// </summary>
	public static class PS4FileOps
    {
        /// <summary>
        /// ファイル名
        /// </summary>
        private const string FILENAME = "SaveData.dat";

        /// <summary>
        /// ファイル書き込みリクエスト
        /// </summary>
        public class FileWriteRequest : FileOps.FileOperationRequest
        {
            /// <summary>
            /// セーブデータインターフェース
            /// </summary>
            public ISaveData saveData = null;

			/// <summary>
			/// 進捗更新時コールバック
			/// </summary>
			public Action<float> onUpdateProgress = null;

            /// <summary>
            /// ファイル書き込み処理
            /// </summary>
            public override void DoFileOperations(Mounting.MountPoint mp, FileOps.FileOperationResponse response)
            {
				var sw = new Stopwatch();
				sw.Start();

				FileStream fs = null;
				{
					var _response = response as FileWriteResponse;

					if (_response.process == null)
					{
						//先頭から書き込む
						fs = File.Open($"{mp.PathName.Data}/{FILENAME}", FileMode.Create, FileAccess.Write);
						this.saveData.SetFileStream(fs);
						_response.process = this.saveData.Save();
					}
					else
					{
						//続きから書き込む
						fs = File.OpenWrite($"{mp.PathName.Data}/{FILENAME}");
						fs.Seek(0, SeekOrigin.End);
						this.saveData.SetFileStream(fs);
					}

					//TRC R4098 : 15秒以内に書き込み可能モードでマウントしたディレクトリをアンマウントする必要があるので14秒経過したら中断する
					while (sw.Elapsed.TotalSeconds < 14f && _response.process.MoveNext())
					{
						response.UpdateProgress(this.saveData.Progress);

						this.onUpdateProgress?.Invoke(this.saveData.Progress);
					}
				}
				fs.Dispose();
            }
        }

        /// <summary>
        /// ファイル書き込みレスポンス
        /// </summary>
        public class FileWriteResponse :　FileOps.FileOperationResponse
        {
			/// <summary>
			/// ファイル書き込み処理
			/// </summary>
			public IEnumerator process = null;
        }

        /// <summary>
        /// ファイル読み込みリクエスト
        /// </summary>
        public class FileReadRequest : FileOps.FileOperationRequest
        {
			/// <summary>
			/// セーブデータインターフェース
			/// </summary>
			public ISaveData saveData = null;

			/// <summary>
			/// 進捗更新時コールバック
			/// </summary>
			public Action<float> onUpdateProgress = null;

            /// <summary>
            /// ファイル読み込み処理
            /// </summary>
            public override void DoFileOperations(Mounting.MountPoint mp, FileOps.FileOperationResponse response)
            {
				using (var fs = File.OpenRead($"{mp.PathName.Data}/{FILENAME}"))
				{
					this.saveData.SetFileStream(fs);

					var coroutine = this.saveData.Load();

					while (coroutine.MoveNext())
					{
						response.UpdateProgress(this.saveData.Progress);

						this.onUpdateProgress?.Invoke(this.saveData.Progress);
					}
				}
            }
        }

        /// <summary>
        /// ファイル読み込みレスポンス
        /// </summary>
        public class FileReadResponse : FileOps.FileOperationResponse
        {

        }
    }
}
#endif