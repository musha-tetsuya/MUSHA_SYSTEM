using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace KG
{
	/// <summary>
	/// 暗号化ユーティリティ
	/// </summary>
	public static class EncryptUtility
	{
		/// <summary>
		/// ハッシュ化用アルゴリズム
		/// </summary>
		private readonly static SHA256CryptoServiceProvider algorithm = new SHA256CryptoServiceProvider();

		/// <summary>
		/// デフォルトAES
		/// </summary>
		public readonly static AesManaged defaultAes = CreateAes("PHC9?X$HlX6@J8FL", "o~ELE(EiODNl8UvM|~O/?icHK'4aE?ZO");

		/// <summary>
		/// 文字列ハッシュ化
		/// </summary>
		public static string ToHashString(string text)
		{
			var bytes = Encoding.UTF8.GetBytes(text);
			bytes = algorithm.ComputeHash(bytes);
			return BitConverter.ToString(bytes).Replace("-", null).ToLower();
		}

		/// <summary>
		/// 暗号化・複合化用AESオブジェクトの生成
		/// </summary>
		/// <param name="IV">ランダムな半角16文字</param>
		/// <param name="Key">ランダムな半角32文字</param>
		public static AesManaged CreateAes(string IV, string Key)
		{
			return new AesManaged
			{
				KeySize = 256,
				BlockSize = 128,
				Mode = CipherMode.CBC,
				IV = Encoding.UTF8.GetBytes(IV),
				Key = Encoding.UTF8.GetBytes(Key),
				Padding = PaddingMode.PKCS7,
			};
		}

		/// <summary>
		/// 暗号化
		/// </summary>
		public static string Encrypt(this AesManaged aes, string text)
		{
			var bytes = Encoding.UTF8.GetBytes(text);
			var encryptValue = aes.CreateEncryptor().TransformFinalBlock(bytes, 0, bytes.Length);
			return Convert.ToBase64String(encryptValue);
		}

		/// <summary>
		/// 複合化
		/// </summary>
		public static string Decrypt(this AesManaged aes, string text)
		{
			try
			{
				var bytes = Convert.FromBase64String(text);
				var decryptValue = aes.CreateDecryptor().TransformFinalBlock(bytes, 0, bytes.Length);
				return Encoding.UTF8.GetString(decryptValue);
			}
			catch
			{
				return text;
			}
		}
	}
}
