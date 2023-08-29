using System;

namespace ActionSequencer.Editor {
    /// <summary>
    /// コピーに使用するためのデータ構造
    /// </summary>
    [Serializable]
    internal class CopyData {
        public SequenceEvent[] sequenceEvents;
    }
}
