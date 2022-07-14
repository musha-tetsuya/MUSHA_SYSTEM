#if UNITY_PS5
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.PSN.PS5;
using Unity.PSN.PS5.Initialization;
using UnityEngine;

namespace KG
{
	/// <summary>
	/// PSN初期化管理
	/// </summary>
	public static class PSNInitializeManager
	{
		/// <summary>
		/// 初期化結果
		/// </summary>
		public static InitResult initResult { get; private set; }

		/// <summary>
		/// 初期化
		/// </summary>
		public static void Initialize()
		{
			if (initResult.Initialized)
			{
				//初期化済み
				return;
			}

			try
			{
				initResult = Main.Initialize();
			}
			catch (Exception e)
			{
				Debug.LogError($"########### PSNInitializeManager Error ##########\n{e.Message}");
			}
		}
	}
}
#endif