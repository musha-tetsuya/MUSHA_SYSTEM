using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KG
{
    /// <summary>
    /// サウンドデータ
    /// </summary>
    [CreateAssetMenu(menuName = "KG/ScriptableObject/SoundData")]
    public class SoundData : ScriptableObject
    {
        /// <summary>
        /// オーディオクリップ
        /// </summary>
        [SerializeField]
        public AudioClip audioClip = null;

        /// <summary>
        /// 音量
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        public float volume = 1f;

        /// <summary>
        /// ループ開始正規位置
        /// </summary>
        [SerializeField, HideInInspector]
        public float loopStartNormalizedTime = 0f;

        /// <summary>
        /// ループ終了正規位置
        /// </summary>
        [SerializeField, HideInInspector]
        public float loopEndNormalizedTime = 1f;

#if UNITY_EDITOR
        /// <summary>
        /// サウンドデータ作成
        /// </summary>
        [MenuItem("CONTEXT/AudioClip/Create Sound Data")]
        private static void CreateSoundData(MenuCommand menuCommand)
        {
            var clip = menuCommand.context as AudioClip;
            var path = Path.ChangeExtension(AssetDatabase.GetAssetPath(clip), "asset");
            var data = ScriptableObject.CreateInstance<SoundData>();
            data.audioClip = clip;
            AssetDatabase.CreateAsset(data, path);
        }

        /// <summary>
        /// カスタムインスペクター
        /// </summary>
        [CustomEditor(typeof(SoundData))]
        private class MyInspector : Editor
        {
            /// <summary>
            /// target
            /// </summary>
            private new SoundData target => base.target as SoundData;

            /// <summary>
            /// OnInspectorGUI
            /// </summary>
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                this.serializedObject.Update();

                if (this.target.audioClip != null)
                {
                    var _loopStartNormalizedTime = this.target.loopStartNormalizedTime;
                    var _loopEndNormalizedTime = this.target.loopEndNormalizedTime;

                    float length = this.target.audioClip.length;
                    this.target.loopStartNormalizedTime = EditorGUILayout.Slider("ループ開始位置", length * this.target.loopStartNormalizedTime, 0f, length) / length;
                    this.target.loopEndNormalizedTime = EditorGUILayout.Slider("ループ終了位置", length * this.target.loopEndNormalizedTime, 0f, length) / length;

                    if (this.target.loopStartNormalizedTime != _loopStartNormalizedTime
                    ||  this.target.loopEndNormalizedTime != _loopEndNormalizedTime)
                    {
                        EditorUtility.SetDirty(this.target);
                    }
                }

                this.serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}