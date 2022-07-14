using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KG
{
	/// <summary>
	/// UVスクロール
	/// </summary>
	[RequireComponent(typeof(Renderer))]
	public class UVScroll : MonoBehaviour
	{
		/// <summary>
		/// Renderer
		/// </summary>
		[SerializeField]
		private Renderer m_renderer = null;

		/// <summary>
		/// マテリアルIndex
		/// </summary>
		[SerializeField]
		private int materialIndex = 0;

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
			this.m_renderer = GetComponent<Renderer>();
		}

		/// <summary>
		/// Awake
		/// </summary>
		private void Awake()
		{
			if (this.m_renderer && this.m_renderer.materials != null && this.materialIndex < this.m_renderer.materials.Length)
			{
				this.material = this.m_renderer.materials[this.materialIndex];
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

		/// <summary>
		/// OnDestroy
		/// </summary>
		private void OnDestroy()
		{
			if (this.material != null)
			{
				Destroy(this.material);
				this.material = null;
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

				var t = this.target as UVScroll;

				EditorGUI.BeginDisabledGroup(true);

				if (Application.isPlaying)
				{
					EditorGUILayout.ObjectField("Material", t.material, typeof(Material), false);
				}
				else
				{
					Material material = null;

					if (t.m_renderer && t.m_renderer.sharedMaterials != null && t.materialIndex < t.m_renderer.sharedMaterials.Length)
					{
						material = t.m_renderer.sharedMaterials[t.materialIndex];
					}

					EditorGUILayout.ObjectField("Material", material, typeof(Material), false);
				}

				EditorGUI.EndDisabledGroup();
			}
		}
#endif
	}
}