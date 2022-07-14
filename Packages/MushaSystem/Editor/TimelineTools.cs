using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;

namespace KG
{
	/// <summary>
	/// Timeline ツール
	/// </summary>
	public static class TimelineTools
	{
		[MenuItem("KG/TimelineTools/Unusedを消去")]
		private static void DeleteUnusedSceneBindings()
		{
			var prefabPaths = new List<string>();

			//選択中オブジェクト
			var objs = Selection.objects;

			foreach (var obj in objs)
			{
				var path = AssetDatabase.GetAssetPath(obj);

				//フォルダなら
				if (AssetDatabase.IsValidFolder(path))
				{
					//フォルダ内のGameObjectを検索
					var guids = AssetDatabase.FindAssets("t:GameObject", new[] { path });

					foreach (var guid in guids)
					{
						path = AssetDatabase.GUIDToAssetPath(guid);

						if (path.EndsWith(".prefab"))
						{
							//プレハブのパスをリストに追加
							prefabPaths.Add(path);
						}
					}
				}
				else if (path.EndsWith(".prefab"))
				{
					//プレハブのパスをリストに追加
					prefabPaths.Add(path);
				}
			}

			for (int i = 0; i < prefabPaths.Count; i++)
			{
				EditorUtility.DisplayProgressBar("Delete Unused SceneBindings", $"{i} / {prefabPaths.Count} : {Path.GetFileName(prefabPaths[i])}", (float)i / prefabPaths.Count);

				try
				{
					var gobj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);

					// 現在Playableに設定されてるTimelineAssetを取得する
					var pd = gobj.GetComponent<PlayableDirector>();

					if (pd)
					{
						var path = AssetDatabase.GetAssetPath(pd.playableAsset);

						// シリアライズ化
						var so = new SerializedObject(pd);
						so.Update();

						// m_SceneBindingsの変数を取得
						var prop = so.FindProperty("m_SceneBindings");

						//除去処理が行われたかどうか
						var isDelete = false;

						for (int j = 0; j < prop.arraySize; j++)
						{
							// m_SceneBindingsの配列のj番目のkeyのプロパティを取得
							var key = prop.GetArrayElementAtIndex(j).FindPropertyRelative("key");
							// keyの参照しているオブジェクトのインスタンスIDを取得
							var tmp = key.objectReferenceInstanceIDValue;
							// インスタンスIDを元にアセットのパスを取得する
							var propPath = AssetDatabase.GetAssetPath(tmp);

							// 現在シーンに設定されてるTimelineAssetと比較して一致しなければ配列から消す
							if (path != propPath)
							{
								isDelete = true;
								prop.DeleteArrayElementAtIndex(j);
								j--;
							}
						}

						so.ApplyModifiedProperties();

						//除去処理が行われたなら
						if (isDelete)
						{
							//保存
							AssetDatabase.SaveAssets();

							//除去処理が行われたプレハブをログ通知
							Debug.Log($"Delete Unused SceneBindings => {prefabPaths[i]}");
						}
					}
				}
				catch (System.Exception e)
				{
					Debug.Log($"<color=red>{e}\nMessage={e.Message}\nStackTrace={e.StackTrace}\nSource={e.Source}</color>");
				}
			}

			EditorUtility.ClearProgressBar();

			Debug.Log("完了");
		}
	}
}