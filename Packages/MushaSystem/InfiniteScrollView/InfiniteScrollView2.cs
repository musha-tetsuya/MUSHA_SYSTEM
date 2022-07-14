using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KG
{
	/// <summary>
	/// 無限スクロールビュー
	/// </summary>
	[RequireComponent(typeof(ScrollRect))]
	public class InfiniteScrollView2 : MonoBehaviour
	{
		/// <summary>
		/// フォーカスタイプ
		/// </summary>
		public enum FocusType
		{
			Nearest,//TopかBottomの近い方に合わせる
			Top,	//Topに合わせる
			Bottom,	//Bottomに合わせる
		}

		/// <summary>
		/// ScrollRect
		/// </summary>
		[SerializeField]
		public ScrollRect scrollRect = null;

		/// <summary>
		/// 余白
		/// </summary>
		[SerializeField]
		private RectOffset padding = null;

		/// <summary>
		/// 要素間スペース
		/// </summary>
		[SerializeField]
		private float spacing = 0f;

		/// <summary>
		/// 要素整列タイプ
		/// </summary>
		[SerializeField]
		private TextAnchor childAlignment = TextAnchor.UpperLeft;

		/// <summary>
		/// Instantiate追加数
		/// </summary>
		[SerializeField]
		private int addInstantiateSize = 2;

		/// <summary>
		/// 初期化されたかどうか
		/// </summary>
		private bool isInitilaized = false;

		/// <summary>
		/// 要素プレハブ
		/// </summary>
		private RectTransform elementPrefab = null;

		/// <summary>
		/// 要素数
		/// </summary>
		public int maxElementSize { get; private set; }

		/// <summary>
		/// 各要素の現在のIndex
		/// </summary>
		private Dictionary<RectTransform, int> elementIndex = new Dictionary<RectTransform, int>();

		/// <summary>
		/// 前回のスクロールバー値
		/// </summary>
		private float prevScrollbarValue = 0f;

		/// <summary>
		/// 要素Index更新時コールバック
		/// </summary>
		private Action<RectTransform, int> onUpdateElement = null;

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="elementPrefab">要素のプレハブ</param>
		/// <param name="maxElementSize">要素数</param>
		/// <param name="onUpdateElement">要素Index更新時コールバック</param>
		public void Initialize(RectTransform elementPrefab, int maxElementSize, Action<RectTransform, int> onUpdateElement = null)
		{
			Debug.Assert(this.scrollRect.content.GetComponent<LayoutGroup>() == null || !this.scrollRect.content.GetComponent<LayoutGroup>().enabled, "ContentからLayoutGroupを外して下さい。");
			Debug.Assert(this.scrollRect.content.GetComponent<ContentSizeFitter>() == null || !this.scrollRect.content.GetComponent<ContentSizeFitter>().enabled, "ContentからContentSizeFitterを外して下さい。");

			if (!this.isInitilaized)
			{
				//スクロール時コールバックの登録
				this.scrollRect.onValueChanged.AddListener(_ => this.OnScroll());
				this.isInitilaized = true;
			}

			//Instantiateが必要かどうか
			bool isNeedInstantiate = false;

			//必要生成数
			int instantiateSize = 0;

			//要素プレハブが変更された
			if (elementPrefab != this.elementPrefab)
			{
				//Instantiate必要
				isNeedInstantiate = true;

				//Content内クリア
				foreach (Transform child in this.scrollRect.content)
				{
					Destroy(child.gameObject);
				}
			}
			//要素プレハブに変更無し
			else
			{
				//既に生成されている要素を再利用する
				instantiateSize = this.scrollRect.content.childCount;
			}

			this.elementPrefab = elementPrefab;
			this.maxElementSize = maxElementSize;
			this.elementIndex.Clear();
			this.onUpdateElement = onUpdateElement;

			var elementAnchorPivot = Vector2.zero;
			var elementAnchoredPosition = this.elementPrefab.anchoredPosition;

			//横スクロールの場合
			if (this.scrollRect.horizontal)
			{
				Debug.Assert(this.scrollRect.horizontalScrollbarVisibility != ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport, "スクロールバーのVisiblityはPermanentかAutoHideにして下さい");

				if (isNeedInstantiate)
				{
					//Viewportの幅、要素の幅、要素間スペースから必要生成数を計算
					instantiateSize = Mathf.CeilToInt((this.scrollRect.viewport.rect.width + this.spacing) / (this.elementPrefab.rect.width + this.spacing)) + this.addInstantiateSize;
				}

				if (this.maxElementSize > 0)
				{
					//Contentの幅を決定 (全要素の幅) + (全要素間スペース) + (左右余白)
					var contentSize = this.scrollRect.content.sizeDelta;
					contentSize.x = (this.elementPrefab.rect.width * this.maxElementSize)
								  + (this.spacing * (this.maxElementSize - 1))
								  + (this.padding.left + this.padding.right);
					this.scrollRect.content.sizeDelta = contentSize;
				}
				else
				{
					this.scrollRect.content.sizeDelta = new Vector2(0f, this.scrollRect.content.sizeDelta.y);
				}

				//要素のAnchorとPivotの決定
				elementAnchorPivot.x = 0f;

				switch (this.childAlignment)
				{
					case TextAnchor.MiddleLeft:
						elementAnchorPivot.y = 0.5f;
						break;
					case TextAnchor.LowerLeft:
						elementAnchoredPosition.y += this.padding.bottom;
						break;
					default:
						elementAnchorPivot.y = 1f;
						elementAnchoredPosition.y -= this.padding.top;
						break;
				}

				//スクロールバー値リセット
				if (this.scrollRect.horizontalScrollbar != null)
				{
					this.scrollRect.horizontalScrollbar.value = 0f;
					this.prevScrollbarValue = 0f;
				}
			}
			//縦スクロールの場合
			else if (this.scrollRect.vertical)
			{
				Debug.Assert(this.scrollRect.verticalScrollbarVisibility != ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport, "スクロールバーのVisiblityはPermanentかAutoHideにして下さい");

				if (isNeedInstantiate)
				{
					//Viewportの高さ、要素の高さ、要素間スペースから必要生成数を計算
					instantiateSize = Mathf.CeilToInt((this.scrollRect.viewport.rect.height + this.spacing) / (this.elementPrefab.rect.height + this.spacing)) + this.addInstantiateSize;
				}

				if (this.maxElementSize > 0)
				{
					//Contentの高さを決定 (全要素の高さ) + (全要素間スペース) + (上下余白)
					var contentSize = this.scrollRect.content.sizeDelta;
					contentSize.y = (this.elementPrefab.rect.height * this.maxElementSize)
								  + (this.spacing * (this.maxElementSize - 1))
								  + (this.padding.top + this.padding.bottom);
					this.scrollRect.content.sizeDelta = contentSize;
				}
				else
				{
					this.scrollRect.content.sizeDelta = new Vector2(this.scrollRect.content.sizeDelta.x, 0f);
				}

				//要素のAnchorとPivotを決定
				elementAnchorPivot.y = 1f;

				switch (this.childAlignment)
				{
					case TextAnchor.UpperCenter:
						elementAnchorPivot.x = 0.5f;
						break;
					case TextAnchor.UpperRight:
						elementAnchorPivot.x = 1f;
						elementAnchoredPosition.x -= this.padding.right;
						break;
					default:
						elementAnchorPivot.x = 0f;
						elementAnchoredPosition.x += this.padding.left;
						break;
				}

				//スクロールバー値リセット
				if (this.scrollRect.verticalScrollbar != null)
				{
					this.scrollRect.verticalScrollbar.value = 1f;
					this.prevScrollbarValue = 1f;
				}
			}

			//必要な数だけ要素を生成
			for (int i = 0; i < instantiateSize; i++)
			{
				RectTransform element = null;

				if (isNeedInstantiate)
				{
					element = Instantiate(this.elementPrefab, this.scrollRect.content);
				}
				else
				{
					element = this.scrollRect.content.GetChild(i) as RectTransform;
				}

				if (this.scrollRect.horizontal)
				{
					//要素位置の計算 (左余白) + (スペース + 要素幅) * Index
					elementAnchoredPosition.x = this.padding.left + (this.spacing + element.rect.width) * i;
				}
				else if (this.scrollRect.vertical)
				{
					//要素位置の計算 (上余白) + (スペース + 要素幅) * Index
					elementAnchoredPosition.y = this.padding.top + (this.spacing + element.rect.height) * i;
					elementAnchoredPosition.y *= -1;//縦スクロールの場合、Indexが増えるほどマイナス座標になる
				}

				//要素位置を設定
				element.anchorMin =
				element.anchorMax =
				element.pivot = elementAnchorPivot;
				element.anchoredPosition = elementAnchoredPosition;

				//要素のIndexを決定
				this.elementIndex[element] = i;

				//全要素数内のIndexなら表示ON
				element.gameObject.SetActive(i < this.maxElementSize);

				//表示ONのものだけIndex更新通知
				if (element.gameObject.activeSelf)
				{
					this.onUpdateElement?.Invoke(element, i);
				}
			}
		}

		/// <summary>
		/// スクロールされたとき
		/// </summary>
		private void OnScroll()
		{
			var velocity = this.scrollRect.velocity;
			var updateTargets = new List<RectTransform>();

			//横スクロールの場合
			if (this.scrollRect.horizontal)
			{
				if (this.scrollRect.horizontalScrollbar != null)
				{
					//スクロールバーを操作してスクロールした場合velocityがゼロになるので、スクロールバー値の差分をvelocityとする
					velocity.x = this.prevScrollbarValue - this.scrollRect.horizontalScrollbar.value;
					this.prevScrollbarValue = this.scrollRect.horizontalScrollbar.value;
				}

				//スクロールを進ませた場合
				if (velocity.x < 0f)
				{
					while (true)
					{
						//先頭の要素
						var firstElement = this.scrollRect.content.GetChild(0) as RectTransform;
						//先頭要素の矩形の最大x値
						var firstElementRectMaxX = this.scrollRect.content.anchoredPosition.x + firstElement.anchoredPosition.x + firstElement.rect.width;

						//先頭要素がViewport外に出た場合
						if (firstElementRectMaxX < 0f)
						{
							//末尾の要素
							var lastElement = this.scrollRect.content.GetChild(this.scrollRect.content.childCount - 1) as RectTransform;

							//先頭要素のIndex更新
							var newFirstElementIndex = this.elementIndex[firstElement] = this.elementIndex[lastElement] + 1;
							firstElement.SetAsLastSibling();

							//先頭要素の新規位置 = 末尾要素位置 + 末尾要素幅 + 要素間スペース
							var newFirstElementPosition = firstElement.anchoredPosition;
							newFirstElementPosition.x = lastElement.anchoredPosition.x + lastElement.rect.width + this.spacing;
							firstElement.anchoredPosition = newFirstElementPosition;
							
							//全要素数内のIndexなら表示ON
							firstElement.gameObject.SetActive(0 <= newFirstElementIndex && newFirstElementIndex < this.maxElementSize);

							//表示ONのものだけIndex更新通知
							if (firstElement.gameObject.activeSelf && !updateTargets.Contains(firstElement))
							{
								updateTargets.Add(firstElement);
							}
						}
						else
						{
							//更新する要素は無いので終了
							break;
						}
					}
				}
				//スクロールを戻した場合
				else if (velocity.x > 0f)
				{
					while (true)
					{
						//末尾の要素
						var lastElement = this.scrollRect.content.GetChild(this.scrollRect.content.childCount - 1) as RectTransform;
						//末尾要素の矩形の最小x値
						var lastElementRectMinX = this.scrollRect.content.anchoredPosition.x + lastElement.anchoredPosition.x;

						//末尾要素がViewport外に出た場合
						if (this.scrollRect.viewport.rect.width < lastElementRectMinX)
						{
							//先頭の要素
							var firstElement = this.scrollRect.content.GetChild(0) as RectTransform;

							//末尾要素のIndex更新
							var newLastElementIndex = this.elementIndex[lastElement] = this.elementIndex[firstElement] - 1;
							lastElement.SetAsFirstSibling();

							//末尾要素の新規位置 = 先頭要素位置 - 末尾要素幅 - 要素間スペース
							var newLastElementPosition = lastElement.anchoredPosition;
							newLastElementPosition.x = firstElement.anchoredPosition.x - lastElement.rect.width - this.spacing;
							lastElement.anchoredPosition = newLastElementPosition;

							//全要素数内のIndexなら表示ON
							lastElement.gameObject.SetActive(0 <= newLastElementIndex && newLastElementIndex < this.maxElementSize);

							//表示ONのものだけIndex更新通知
							if (lastElement.gameObject.activeSelf && !updateTargets.Contains(lastElement))
							{
								updateTargets.Add(lastElement);
							}
						}
						else
						{
							//更新する要素は無いので終了
							break;
						}
					}
				}
			}
			//縦スクロールの場合
			else if (this.scrollRect.vertical)
			{
				if (this.scrollRect.verticalScrollbar != null)
				{
					//スクロールバーを操作してスクロールした場合velocityがゼロになるので、スクロールバー値の差分をvelocityとする
					velocity.y = this.prevScrollbarValue - this.scrollRect.verticalScrollbar.value;
					this.prevScrollbarValue = this.scrollRect.verticalScrollbar.value;
				}

				//スクロールを進ませた場合
				if (velocity.y > 0f)
				{
					while (true)
					{
						//先頭の要素
						var firstElement = this.scrollRect.content.GetChild(0) as RectTransform;
						//先頭要素の矩形の最大y値
						var firstElementRectMaxY = -this.scrollRect.content.anchoredPosition.y - firstElement.anchoredPosition.y + firstElement.rect.height;

						//先頭要素がViewport外に出た場合
						if (firstElementRectMaxY < 0f)
						{
							//末尾の要素
							var lastElement = this.scrollRect.content.GetChild(this.scrollRect.content.childCount - 1) as RectTransform;

							//先頭要素のIndex更新
							var newFirstElementIndex = this.elementIndex[firstElement] = this.elementIndex[lastElement] + 1;
							firstElement.SetAsLastSibling();

							//先頭要素の新規位置 = 末尾要素位置 + 末尾要素高さ + 要素間スペース
							var newFirstElementPosition = firstElement.anchoredPosition;
							newFirstElementPosition.y = lastElement.anchoredPosition.y - lastElement.rect.height - this.spacing;
							firstElement.anchoredPosition = newFirstElementPosition;
							
							//全要素数内のIndexなら表示ON
							firstElement.gameObject.SetActive(0 <= newFirstElementIndex && newFirstElementIndex < this.maxElementSize);

							//表示ONのものだけIndex更新通知
							if (firstElement.gameObject.activeSelf && !updateTargets.Contains(firstElement))
							{
								updateTargets.Add(firstElement);
							}
						}
						else
						{
							//更新する要素は無いので終了
							break;
						}
					}
				}
				//スクロールを戻した場合
				else if (velocity.y < 0f)
				{
					while (true)
					{
						//末尾の要素
						var lastElement = this.scrollRect.content.GetChild(this.scrollRect.content.childCount - 1) as RectTransform;
						//末尾要素の矩形の最小y値
						var lastElementRectMinY = -this.scrollRect.content.anchoredPosition.y - lastElement.anchoredPosition.y;

						//末尾要素がViewport外に出た場合
						if (this.scrollRect.viewport.rect.height < lastElementRectMinY)
						{
							//先頭の要素
							var firstElement = this.scrollRect.content.GetChild(0) as RectTransform;

							//末尾要素のIndex更新
							var newLastElementIndex = this.elementIndex[lastElement] = this.elementIndex[firstElement] - 1;
							lastElement.SetAsFirstSibling();

							//末尾要素の新規位置 = 先頭要素位置 - 末尾要素高さ - 要素間スペース
							var newLastElementPosition = lastElement.anchoredPosition;
							newLastElementPosition.y = firstElement.anchoredPosition.y + lastElement.rect.height + this.spacing;
							lastElement.anchoredPosition = newLastElementPosition;

							//全要素数内のIndexなら表示ON
							lastElement.gameObject.SetActive(0 <= newLastElementIndex && newLastElementIndex < this.maxElementSize);

							//表示ONのものだけIndex更新通知
							if (lastElement.gameObject.activeSelf && !updateTargets.Contains(lastElement))
							{
								updateTargets.Add(lastElement);
							}
						}
						else
						{
							//更新する要素は無いので終了
							break;
						}
					}
				}
			}

			//まとめてIndex更新通知
			for (int i = 0, imax = updateTargets.Count; i < imax; i++)
			{
				if (updateTargets[i].gameObject.activeSelf)
				{
					this.onUpdateElement?.Invoke(updateTargets[i], this.elementIndex[updateTargets[i]]);
				}
			}
		}

		/// <summary>
		/// 指定Index要素の更新処理を走らせる
		/// </summary>
		public void UpdateElement(int index)
		{
			foreach (var obj in this.elementIndex)
			{
				if (obj.Value == index)
				{
					this.onUpdateElement?.Invoke(obj.Key, index);
				}
			}
		}

		/// <summary>
		/// 全要素の更新処理を走らせる
		/// </summary>
		public void UpdateElements()
		{
			foreach (Transform child in this.scrollRect.content)
			{
				var element = child as RectTransform;
				if (element.gameObject.activeSelf)
				{
					this.onUpdateElement?.Invoke(element, this.elementIndex[element]);
				}
			}
		}

		/// <summary>
		/// Viewportが映しているコンテンツ内の範囲を取得
		/// </summary>
		private void GetViewportRange(out float min, out float max)
		{
			min = 0f;
			max = 0f;

			if (this.scrollRect.horizontal)
			{
				min = this.padding.left - this.scrollRect.content.anchoredPosition.x;
				max = this.scrollRect.viewport.rect.width - this.padding.right - this.scrollRect.content.anchoredPosition.x;
			}
			else if (this.scrollRect.vertical)
			{
				min = this.scrollRect.content.anchoredPosition.y + this.padding.top;
				max = this.scrollRect.content.anchoredPosition.y + this.scrollRect.viewport.rect.height - this.padding.bottom;
			}
		}

		/// <summary>
		/// 指定Index要素のコンテンツ内位置範囲
		/// </summary>
		private void GetElementPositionRange(int index, out float min, out float max, RectOffset elementPadding = null)
		{
			min = 0f;
			max = 0f;

			if (this.scrollRect.content.childCount > 0)
			{
				var element = this.scrollRect.content.GetChild(0) as RectTransform;

				if (this.scrollRect.horizontal)
				{
					min = this.padding.left + (this.spacing + element.rect.width) * index;
					max = min + element.rect.width;

					if (elementPadding != null)
					{
						min -= elementPadding.left;
						max += elementPadding.right;
					}
				}
				else if (this.scrollRect.vertical)
				{
					min = (this.padding.top + (this.spacing + element.rect.height) * index);
					max = min + element.rect.height;

					if (elementPadding != null)
					{
						min -= elementPadding.top;
						max += elementPadding.bottom;
					}
				}
			}
		}

		/// <summary>
		/// 指定Index要素がViewportに収まっているかどうか
		/// </summary>
		public bool IsInViewport(int index, RectOffset elementPadding = null)
		{
			if (index < 0 || this.maxElementSize <= index)
			{
				return false;
			}

			//Viewportが映している範囲を取得
			this.GetViewportRange(out float viewportMin, out float viewportMax);
			//要素の位置範囲を取得
			this.GetElementPositionRange(index, out float elementPosMin, out float elementPosMax, elementPadding);
			//要素がViewport内に収まっているかどうか
			return viewportMin <= elementPosMin && elementPosMax <= viewportMax;
		}

		/// <summary>
		/// 指定Index要素にフォーカスする
		/// </summary>
		public void SetFocus(int index, FocusType focusType = FocusType.Nearest, RectOffset elementPadding = null)
		{
			if (index < 0 || this.maxElementSize <= index)
			{
				return;
			}

			//スクロールを止める
			this.scrollRect.velocity = Vector2.zero;

			//Viewportが映している範囲を取得
			this.GetViewportRange(out float viewportMin, out float viewportMax);

			//要素の位置範囲を取得
			this.GetElementPositionRange(index, out float elementPosMin, out float elementPosMax, elementPadding);

			//範囲差分
			var distanceMin = viewportMin - elementPosMin;
			var distanceMax = elementPosMax - viewportMax;

			//範囲差分だけコンテンツ位置を調整して要素をViewport内に収める
			var contentPosition = this.scrollRect.content.anchoredPosition;

			if (this.scrollRect.horizontal)
			{
				switch (focusType)
				{
					case FocusType.Nearest:
						contentPosition.x += Mathf.Abs(distanceMin) < Mathf.Abs(distanceMax) ? distanceMin : -distanceMax;
						break;
					case FocusType.Top:
						contentPosition.x += distanceMin;
						break;
					case FocusType.Bottom:
						contentPosition.x -= distanceMax;
						break;
				}
			}
			else if (this.scrollRect.vertical)
			{
				switch (focusType)
				{
					case FocusType.Nearest:
						contentPosition.y += Mathf.Abs(distanceMin) < Mathf.Abs(distanceMax) ? -distanceMin : distanceMax;
						break;
					case FocusType.Top:
						contentPosition.y -= distanceMin;
						break;
					case FocusType.Bottom:
						contentPosition.y += distanceMax;
						break;
				}
			}

			this.scrollRect.content.anchoredPosition = contentPosition;
		}

		/// <summary>
		/// 指定要素の現在のIndexを取得
		/// </summary>
		public int GetIndex(RectTransform element)
		{
			return this.elementIndex[element];
		}

		/// <summary>
		/// 制御
		/// </summary>
		public class Controller
		{
			/// <summary>
			/// スクロールビュー
			/// </summary>
			private InfiniteScrollView2 scrollView = null;

			/// <summary>
			/// ループするかどうか
			/// </summary>
			private bool isLoop = false;

			/// <summary>
			/// ループする際に必要な間隔
			/// </summary>
			public float loopInterval = 0f;

			/// <summary>
			/// 前回移動を行った時間
			/// </summary>
			private float prevMoveTime = 0f;

			/// <summary>
			/// フォーカス中Index
			/// </summary>
			public int focusedIndex = 0;

			/// <summary>
			/// Index移動成功時コールバック
			/// </summary>
			public Action onMove = null;

			/// <summary>
			/// construct
			/// </summary>
			public Controller(InfiniteScrollView2 scrollView, bool isLoop, float loopInterval = 0f)
			{
				this.scrollView = scrollView;
				this.isLoop = isLoop;
				this.loopInterval = loopInterval;
			}

			/// <summary>
			/// Index移動
			/// </summary>
			public bool Move(int moveIndex)
			{
				if (this.scrollView.maxElementSize == 0)
				{
					return false;
				}

				int beforeIndex = this.focusedIndex;

				if (this.isLoop)
				{
					int nextIndex = this.focusedIndex + moveIndex;

					this.focusedIndex = (int)Mathf.Repeat(nextIndex, this.scrollView.maxElementSize);

					if (nextIndex < 0 || this.scrollView.maxElementSize <= nextIndex)
					{
						if (Time.realtimeSinceStartup - this.prevMoveTime < this.loopInterval)
						{
							//ループが必要だがループ間隔を満たしていない場合はループ不可
							this.focusedIndex = beforeIndex;
						}
					}
				}
				else
				{
					this.focusedIndex = Mathf.Clamp(this.focusedIndex + moveIndex, 0, this.scrollView.maxElementSize - 1);
				}

				this.prevMoveTime = Time.realtimeSinceStartup;

				if (this.focusedIndex == beforeIndex)
				{
					return false;
				}

				this.scrollView.UpdateElement(beforeIndex);
				this.scrollView.UpdateElement(this.focusedIndex);

				if (!this.scrollView.IsInViewport(this.focusedIndex))
				{
					this.scrollView.SetFocus(this.focusedIndex);
				}

				this.onMove?.Invoke();

				return true;
			}

			/// <summary>
			/// 次のIndexへ移動
			/// </summary>
			public bool Next()
			{
				return this.Move(1);
			}

			/// <summary>
			/// 前のIndexへ移動
			/// </summary>
			public bool Prev()
			{
				return this.Move(-1);
			}

			/// <summary>
			/// ページ移動
			/// </summary>
			public void MovePage(int moveIndex)
			{
				if (this.scrollView.maxElementSize == 0)
				{
					return;
				}

				//1ページ内要素数
				int pageElementSize = Mathf.Abs(moveIndex);

				//総ページサイズ
				int totalPageSize = Mathf.CeilToInt((float)this.scrollView.maxElementSize / pageElementSize);

				//移動前のIndex
				int beforeIndex = this.focusedIndex;

				//Index移動
				this.focusedIndex = Mathf.Clamp(this.focusedIndex + moveIndex, 0, this.scrollView.maxElementSize - 1);

				//複数ページなら
				if (totalPageSize > 1)
				{
					//フォーカス要素が所属するページのIndex
					int pageIndex = this.focusedIndex / pageElementSize;

					if (pageIndex < totalPageSize - 1)
					{
						//所属するページの先頭要素をトップに合わせるようフォーカス
						this.scrollView.SetFocus(pageIndex * pageElementSize, FocusType.Top);
					}
					else
					{
						//最終要素をボトムに合わせるようフォーカス
						this.scrollView.SetFocus(this.scrollView.maxElementSize - 1, FocusType.Bottom);
					}
				}

				if (this.focusedIndex != beforeIndex)
				{
					//Index移動したので要素更新
					this.scrollView.UpdateElement(beforeIndex);
					this.scrollView.UpdateElement(this.focusedIndex);
					this.onMove?.Invoke();
				}
			}

			/// <summary>
			/// 次のページへ移動
			/// </summary>
			public void NextPage()
			{
				this.MovePage(this.scrollView.scrollRect.content.childCount - this.scrollView.addInstantiateSize);
			}

			/// <summary>
			/// 前のページへ移動
			/// </summary>
			public void PrevPage()
			{
				this.MovePage((this.scrollView.scrollRect.content.childCount - this.scrollView.addInstantiateSize) * -1);
			}
		}
	}
}
