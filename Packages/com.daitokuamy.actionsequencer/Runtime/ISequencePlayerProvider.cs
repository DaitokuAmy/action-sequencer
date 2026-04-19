using UnityEngine;

namespace ActionSequencer {
    /// <summary>
    /// SequencePlayerを提供するためのProvider(Editor用)
    /// </summary>
    public interface ISequencePlayerProvider {
        // 制御対象のSequencePlayer
        SequencePlayer SequencePlayer { get; }
    }
}
