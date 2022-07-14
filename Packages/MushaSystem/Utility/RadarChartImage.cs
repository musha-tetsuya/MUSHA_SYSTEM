using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KG
{
	/// <summary>
	/// レーダーチャートイメージ
	/// </summary>
	public class RadarChartImage : Graphic
	{
		/// <summary>
		/// 頂点
		/// </summary>
		[SerializeField, Range(0f, 1f)]
		public float[] vert = null;

		/// <summary>
		/// OnPopulateMesh
		/// </summary>
		protected override void OnPopulateMesh(VertexHelper toFill)
		{
			toFill.Clear();

			//最低3点は必要
			if (this.vert == null || this.vert.Length < 3)
			{
				return;
			}

			//中心点
			var v = UIVertex.simpleVert;
			v.color = this.color;
			v.position = Vector3.zero;
			toFill.AddVert(v);

			var sizeX = this.rectTransform.sizeDelta.x * 0.5f;
			var sizeY = this.rectTransform.sizeDelta.y * 0.5f;

			for (int i = 0; i < this.vert.Length; i++)
			{
				//中心以外の点
				var angleZ = 360f / this.vert.Length * i;
				var rotation = Quaternion.Euler(0f, 0f, angleZ);
				var position = rotation * Vector3.up * this.vert[i];
				position.x *= sizeX;
				position.y *= sizeY;
				v.position = position;
				toFill.AddVert(v);

				int idx0 = 0;
				int idx1 = i + 1;
				int idx2 = (idx1 < this.vert.Length) ? idx1 + 1 : 1;
				toFill.AddTriangle(idx0, idx1, idx2);
			}
		}
	}
}