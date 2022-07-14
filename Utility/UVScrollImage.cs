using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KG
{
	/// <summary>
	/// UVスクロール
	/// </summary>
	[RequireComponent(typeof(Image))]
	public class UVScrollImage : MonoBehaviour
	{
		/// <summary>
		/// Renderer
		/// </summary>
		[SerializeField]
		private Image m_image = null;

		/// <summary>
		/// スクロール速度
		/// </summary>
		[SerializeField]
		private Vector2 scrollSpeed = Vector2.zero;

		/// <summary>
		/// マテリアル
		/// </summary>
		private Material material = null;

		/// <summary>
		/// Reset
		/// </summary>
		private void Reset()
		{
			this.m_image = GetComponent<Image>();
		}

		/// <summary>
		/// Awake
		/// </summary>
		private void Awake()
		{
			if (this.m_image && this.m_image.material != null)
			{
				this.material = Instantiate(this.m_image.material);
			}
		}

		/// <summary>
		/// Update
		/// </summary>
		private void Update()
		{
			if (this.material)
			{
				var offset = this.material.mainTextureOffset;
				offset.x = Mathf.Repeat(offset.x + this.scrollSpeed.x * Time.deltaTime, 1f);
				offset.y = Mathf.Repeat(offset.y + this.scrollSpeed.y * Time.deltaTime, 1f);
				this.material.mainTextureOffset = offset;
			}
		}

#if UNITY_EDITOR
		/// <summary>
		/// カスタムインスペクター
		/// </summary>
		[CustomEditor(typeof(UVScroll))]
		private class MyInspector : Editor
		{
			/// <summary>
			/// OnInspectorGUI
			/// </summary>
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				var t = this.target as UVScrollImage;

				EditorGUI.BeginDisabledGroup(true);

				if (Application.isPlaying)
				{
					EditorGUILayout.ObjectField("Material", t.material, typeof(Material), false);
				}

				EditorGUI.EndDisabledGroup();
			}
		}
#endif
	}
}