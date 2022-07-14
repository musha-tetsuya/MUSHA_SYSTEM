using UnityEngine;

namespace KG
{
	/// <summary>
	/// UVスクロール
	/// </summary>
	public class SharedUVScroll : MonoBehaviour
	{
		/// <summary>
		/// マテリアル
		/// </summary>
		[SerializeField]
		private Material material = null;

		/// <summary>
		/// スクロール速度
		/// </summary>
		[SerializeField]
		private Vector2 scrollSpeed = Vector2.zero;

		/// <summary>
		/// 元のテクスチャオフセット値
		/// </summary>
		private Vector2 mainTextureOffset = Vector2.zero;

		/// <summary>
		/// Reset
		/// </summary>
		private void Reset()
		{
			var renderer = this.GetComponent<Renderer>();
			if (renderer)
			{
				this.material = renderer.sharedMaterial;
			}
		}

		/// <summary>
		/// OnDestroy
		/// </summary>
		private void OnDestroy()
		{
			if (this.material != null)
			{
				//変更前の値に戻す
				this.material.mainTextureOffset = this.mainTextureOffset;
			}
		}

		/// <summary>
		/// Awake
		/// </summary>
		private void Awake()
		{
			if (this.material != null)
			{
				//変更前の値を保持しておく
				this.mainTextureOffset = this.material.mainTextureOffset;
			}
		}

		/// <summary>
		/// Update
		/// </summary>
		private void Update()
		{
			if (this.material != null)
			{
				var offset = this.material.mainTextureOffset;
				offset.x = Mathf.Repeat(offset.x + this.scrollSpeed.x * Time.deltaTime, 1f);
				offset.y = Mathf.Repeat(offset.y + this.scrollSpeed.y * Time.deltaTime, 1f);
				this.material.mainTextureOffset = offset;
			}
		}
	}
}