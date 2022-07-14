using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace KG
{
    /// <summary>
    /// ゲーム中共通オブジェクト
    /// </summary>
    public class GameSystem : SingletonMonoBehaviour<GameSystem>
    {
        /// <summary>
        /// 次に開くシーン名
        /// </summary>
        [SerializeField]
        private string nextSceneName = null;

        /// <summary>
        /// オーバーレイキャンバス
        /// </summary>
        [SerializeField]
        public Canvas overlayCanvas = null;

        /// <summary>
        /// レイヤー名リスト
        /// </summary>
        [SerializeField, HideInInspector]
        private List<string> layerNames = null;

        /// <summary>
        /// 準備完了フラグ
        /// </summary>
        [NonSerialized]
        public bool isReady = true;

        /// <summary>
        /// タッチブロック
        /// </summary>
        public Image touchBlock { get; private set; }

        /// <summary>
        /// Awake
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            DontDestroyOnLoad(this.gameObject);

            //レイヤー作成
            for (int i = 0, imax = this.layerNames.Count; i < imax; i++)
            {
                var rectTransform = this.GetOverlayCanvasLayer(this.layerNames[i]) as RectTransform;
                if (rectTransform == null)
                {
                    var layer = new GameObject(this.layerNames[i], typeof(RectTransform));
                    rectTransform = layer.transform as RectTransform;
                    rectTransform.SetParent(this.overlayCanvas.transform);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.pivot = Vector2.one * 0.5f;
                    rectTransform.localPosition = Vector3.zero;
                    rectTransform.localScale = Vector3.one;
                }
                else
                {
                    rectTransform.SetAsLastSibling();
                }
            }

            //タッチブロック作成
            this.touchBlock = this.GetOverlayCanvasLayer("TouchBlock").gameObject.AddComponent<Image>();
            this.touchBlock.color = Color.clear;
            this.touchBlock.enabled = false;
        }

        /// <summary>
        /// Start
        /// </summary>
        private IEnumerator Start()
        {
            //準備完了を待つ
            yield return new WaitUntil(() => this.isReady);

#if UNITY_EDITOR
            if (EditorPrefs.HasKey(NEXT_SCENE_NAME_KEY))
            {
                //次のシーンへ遷移
                var op = SceneManager.LoadSceneAsync(EditorPrefs.GetString(NEXT_SCENE_NAME_KEY));
                yield return op;
                yield break;
            }
#endif
            if (!string.IsNullOrEmpty(this.nextSceneName))
            {
                //次のシーンへ遷移
                var op = SceneManager.LoadSceneAsync(this.nextSceneName);
                if (op != null)
                {
                    yield return op;
                    yield break;
                }
            }

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                //BuildSettingsの番号が最も若いシーンに遷移
                if (i != SceneManager.GetActiveScene().buildIndex)
                {
                    var op = SceneManager.LoadSceneAsync(i);
                    yield return op;
                    yield break;
                }
            }
        }

        /// <summary>
        /// オーバーレイキャンバス内レイヤーの取得
        /// </summary>
        public Transform GetOverlayCanvasLayer(string layerName)
        {
            return this.overlayCanvas.transform.Find(layerName);
        }

#if UNITY_EDITOR
        /// <summary>
        /// GameSystem経由後に開くシーン名のEditorPrefsキー
        /// </summary>
        private static readonly string NEXT_SCENE_NAME_KEY = typeof(GameSystem).FullName + ".nextSceneName";

        /// <summary>
        /// 再生開始シーンをセットする
        /// </summary>
        [InitializeOnLoadMethod]
        private static void SetPlayModeStartScene()
        {
            EditorSceneManager.activeSceneChangedInEditMode += (closeScene, openedScene) =>
            {
                EditorSceneManager.playModeStartScene = null;

                //開いたシーンがEditorBuildSettingsに含まれていない場合、GameSystemを経由したくないのでスルーする
                if (!EditorBuildSettings.scenes.Any(x => x.path == openedScene.path))
                {
                    return;
                }

                //EditorBuildSettings.scenesに一つもシーンが登録されていない場合、ゲーム開始シーンを取得出来ないのでスルーする
                var gameStartScene = EditorBuildSettings.scenes.FirstOrDefault();
                if (gameStartScene == null)
                {
                    return;
                }

                //開いたシーンがゲーム開始シーンの場合、次のシーンはデフォルトなのでEditorPrefsをリセットする
                if (gameStartScene.path == openedScene.path)
                {
                    EditorPrefs.DeleteKey(NEXT_SCENE_NAME_KEY);
                    return;
                }

                //再生開始シーンをゲーム開始シーンに設定
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(gameStartScene.path);

                //経由後に開くシーンとして、今開いたシーンの名前を保存
                EditorPrefs.SetString(NEXT_SCENE_NAME_KEY, openedScene.name);

                Debug.LogFormat("Set playModeStartScene = {0}", openedScene.name);
            };
        }

        /// <summary>
        /// カスタムインスペクター
        /// </summary>
        [CustomEditor(typeof(GameSystem))]
        private class CustomInspector : Editor
        {
            /// <summary>
            /// レイヤー名リスト
            /// </summary>
            private SimpleReorderableList layerNames = null;

            /// <summary>
            /// OnEnable
            /// </summary>
            private void OnEnable()
            {
                this.layerNames = new SimpleReorderableList(this.serializedObject.FindProperty("layerNames"));
            }

            /// <summary>
            /// OnInspectorGUI
            /// </summary>
            public override void OnInspectorGUI()
            {
                this.serializedObject.Update();

                base.OnInspectorGUI();

                GUILayout.Space(10);

                this.layerNames.DoLayoutList();

                this.serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}