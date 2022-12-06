using UnityEngine;

namespace ActionSequencer
{
    /// <summary>
    /// SequenceControllerを提供するためのProvider(Editor用)
    /// </summary>
    public interface ISequenceControllerProvider
    {
        // 制御対象のSequenceController
        SequenceController SequenceController { get; }
    }
}