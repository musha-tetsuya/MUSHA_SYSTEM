using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

namespace KG
{
	/// <summary>
	/// スプライトアトラスキャッシュ
	/// </summary>
	[Serializable]
	public class SpriteAtlasCache
	{
		/// <summary>
		/// アトラス
		/// </summary>
		[SerializeField]
		public SpriteAtlas atlas = null;

		/// <summary>
		/// スプライトキャッシュ
		/// </summary>
		private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

		/// <summary>
		/// スプライト取得
		/// </summary>
		public virtual Sprite GetSprite(string spriteName)
		{
			if (!this.spriteCache.TryGetValue(spriteName, out Sprite sprite))
			{
				sprite = this.spriteCache[spriteName] = this.atlas.GetSprite(spriteName);
			}
			return sprite;
		}
	}
}