using System;
using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// コピーに使用するためのデータ構造
    /// </summary>
    [Serializable]
    internal sealed class CopyData {
        [SerializeField]
        private SequenceEvent[] _sequenceEvents = Array.Empty<SequenceEvent>();

        /// <summary>コピー対象の Event 一覧</summary>
        public SequenceEvent[] SequenceEvents => _sequenceEvents;

        /// <summary>
        /// シリアライズ用の空コンストラクタ
        /// </summary>
        public CopyData() {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sequenceEvents">コピー対象の Event 一覧</param>
        public CopyData(SequenceEvent[] sequenceEvents) {
            _sequenceEvents = sequenceEvents ?? Array.Empty<SequenceEvent>();
        }
    }
}