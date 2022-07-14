using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KG
{
	/// <summary>
	/// マスターデータマネージャ
	/// </summary>
	[Serializable]
	public struct AssetPathData
	{
#if UNITY_EDITOR
		/// <summary>
		/// フォルダ
		/// </summary>
		public DefaultAsset folder;
#endif
		/// <summary>
		/// フォルダパス
		/// </summary>
		public string path;

		/// <summary>
		/// フォルダ内ファイル一覧
		/// </summary>
		public string[] files;

#if UNITY_EDITOR
		/// <summary>
		/// カスタムPropertyDrawer
		/// </summary>
		[CustomPropertyDrawer(typeof(AssetPathData))]
		private class MyPropertyDrawer : PropertyDrawer
		{
			/// <summary>
			/// 高さ取得
			/// </summary>
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				//各プロパティ
				//var folder = property.FindPropertyRelative("folder");
				//var path = property.FindPropertyRelative("path");
				var files = property.FindPropertyRelative("files");

				float height = 0f;

				//folder
				height += EditorGUIUtility.singleLineHeight;

				if (property.isExpanded)
				{
					//path
					height += EditorGUIUtility.standardVerticalSpacing;
					height += EditorGUIUtility.singleLineHeight;

					//files
					int size = Mathf.Max(1, files.arraySize);
					height += EditorGUIUtility.standardVerticalSpacing * size;
					height += EditorGUIUtility.singleLineHeight * size;
				}

				return height;
			}

			/// <summary>
			/// OnGUI
			/// </summary>
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				//各プロパティ
				var folder = property.FindPropertyRelative("folder");
				var path = property.FindPropertyRelative("path");
				var files = property.FindPropertyRelative("files");

				//折り畳み表示
				position.height = EditorGUIUtility.singleLineHeight;
				property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);

				//folder表示
				var folderPosition = position;
				folderPosition.width -= 60;
				EditorGUI.PropertyField(folderPosition, folder, label);

				//Refleshボタン表示
				var buttonPosition = folderPosition;
				buttonPosition.x = folderPosition.xMax;
				buttonPosition.width = 60;
				if (GUI.Button(buttonPosition, "Reflesh"))
				{
					if (folder.objectReferenceValue == null)
					{
						path.stringValue = null;
						files.ClearArray();
					}
					else
					{
						//folderパス取得
						path.stringValue = AssetDatabase.GetAssetPath(folder.objectReferenceValue);
						
						//folder内ファイル一覧取得（サブフォルダは除く）
						var fileNames = AssetDatabase
							.FindAssets("", new string[] { path.stringValue })
							.Select(AssetDatabase.GUIDToAssetPath)
							.Where(x => !AssetDatabase.IsValidFolder(x))
							.Select(Path.GetFileName)
							.ToArray();
						
						//ファイル数決定
						files.arraySize = fileNames.Length;
						
						//ファイル名取得
						for (int i = 0; i < files.arraySize; i++)
						{
							files.GetArrayElementAtIndex(i).stringValue = fileNames[i];
						}
					}
				}

				//折り畳みが開かれている時
				if (property.isExpanded)
				{
					position.x += 12f;
					position.width -= 12f;

					//folderパス表示
					position.y += EditorGUIUtility.standardVerticalSpacing;
					position.y += EditorGUIUtility.singleLineHeight;
					EditorGUI.TextField(position, path.name, path.stringValue);

					if (files.arraySize == 0)
					{
						position.y += EditorGUIUtility.standardVerticalSpacing;
						position.y += EditorGUIUtility.singleLineHeight;
						EditorGUI.TextField(position, $"{files.name} = empty", null);
					}
					else
					{
						//folder内ファイル名一覧表示
						for (int i = 0; i < files.arraySize; i++)
						{
							position.y += EditorGUIUtility.standardVerticalSpacing;
							position.y += EditorGUIUtility.singleLineHeight;
							EditorGUI.TextField(position, $"{files.name} {i}", files.GetArrayElementAtIndex(i).stringValue);
						}
					}
				}
			}
		}
#endif
	}
}