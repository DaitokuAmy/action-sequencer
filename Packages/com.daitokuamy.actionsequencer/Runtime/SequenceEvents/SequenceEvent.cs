using UnityEngine;

namespace ActionSequencer {
    /// <summary>
    /// SequenceTrackに配置するイベント
    /// </summary>
    public abstract class SequenceEvent : ScriptableObject {
        [Tooltip("表示用ラベル")]
        public string label = "";
        [Tooltip("有効なイベントか")]
        public bool active = true;

        /// <summary>Timeline 上に表示する補助テキスト</summary>
        public virtual string TimelineText => string.Empty;

        /// <summary>
        /// スクリプト更新時処理
        /// </summary>
        private void OnValidate() {
            hideFlags |= HideFlags.HideInHierarchy;
        }
    }
}
