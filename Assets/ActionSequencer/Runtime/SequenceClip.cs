using System;
using UnityEngine;

namespace ActionSequencer
{
    /// <summary>
    /// Sequence再生用アセット
    /// </summary>
    [CreateAssetMenu(fileName = "sequence_clip.asset", menuName = "Sequence Tools/Clip")]
    public sealed class SequenceClip : ScriptableObject
    {
        [Tooltip("Trackリスト")]
        public SequenceTrack[] tracks = Array.Empty<SequenceTrack>();
        [Tooltip("フレームレート")]
        public int frameRate = 30;
    }
}