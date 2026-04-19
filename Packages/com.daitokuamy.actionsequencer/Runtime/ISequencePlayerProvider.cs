namespace ActionSequencer {
    /// <summary>
    /// SequencePlayerを提供するためのProvider(Editor用)
    /// </summary>
    public interface ISequencePlayerProvider {
        /// <summary>制御対象のSequencePlayer</summary>
        IReadOnlySequencePlayer SequencePlayer { get; }
    }
}
