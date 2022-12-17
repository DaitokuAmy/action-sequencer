using UnityEngine;

namespace ActionSequencer
{
    /// <summary>
    /// SequenceTrackに配置するイベント
    /// </summary>
    public abstract class SequenceEvent : ScriptableObject {
        [Tooltip("表示用ラベル")]
        public string label = "";
        [Tooltip("有効なイベントか")]
        public bool active = true;

        private void OnValidate()
        {
            hideFlags |= HideFlags.HideInHierarchy;
        }
    }
}
