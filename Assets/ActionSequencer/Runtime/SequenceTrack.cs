using System;
using UnityEngine;

namespace ActionSequencer
{
    /// <summary>
    /// SequenceEventを配置するためのTrack
    /// </summary>
    public sealed class SequenceTrack : ScriptableObject
    {
        [Tooltip("表示名")]
        public string label = "";
        [Tooltip("配置されたイベントリスト")]
        public SequenceEvent[] sequenceEvents = Array.Empty<SequenceEvent>();

        private void OnValidate()
        {
            hideFlags |= HideFlags.HideInHierarchy;
        }
    }
}
