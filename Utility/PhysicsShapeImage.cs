using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KG
{
	/// <summary>
	/// PhysicsShapeの当たり判定を持ったImage
	/// </summary>
	public class PhysicsShapeImage : Image
	{
		/// <summary>
		/// PhysicsShape
		/// </summary>
		private List<Vector2> physicsShape = new List<Vector2>();

		/// <summary>
		/// 当たり判定
		/// </summary>
		public override bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
		{
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(this.rectTransform, sp, eventCamera, out Vector2 localPoint))
			{
				return false;
			}

			Vector2 p;
			p.x = (localPoint.x / this.rectTransform.rect.width + this.rectTransform.pivot.x - 0.5f) * this.sprite.rect.width / this.sprite.pixelsPerUnit;
			p.y = (localPoint.y / this.rectTransform.rect.height + this.rectTransform.pivot.y - 0.5f) * this.sprite.rect.height / this.sprite.pixelsPerUnit;

			int physicsShapeCount = this.sprite.GetPhysicsShapeCount();

			for (int i = 0; i < physicsShapeCount; i++)
			{
				this.sprite.GetPhysicsShape(i, this.physicsShape);

				bool isInPolygon = false;

				//どれかの多角形の内部にあるか
				//pからx軸の正方向への無限な半直線を考えて、多角形との交差回数によって判定する
				for (int j = 0; j < this.physicsShape.Count; j++)
				{
					int k = (j + 1) % this.physicsShape.Count;
					var a = this.physicsShape[j] - p;
					var b = this.physicsShape[k] - p;

					if (a.y > b.y)
					{
						//swap
						var t = a;
						a = b;
						b = t;
					}

					if (a.y <= 0f && 0f < b.y && (a.x * b.y - a.y * b.x) > 0f)
					{
						isInPolygon = !isInPolygon;
					}
				}

				if (isInPolygon)
				{
					return true;
				}
			}

			return false;
		}
	}
}