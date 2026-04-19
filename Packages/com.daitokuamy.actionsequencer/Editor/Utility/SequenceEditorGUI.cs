namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceのGUI用定義
    /// </summary>
    internal static class SequenceEditorGUI {
        /// <summary>GUI 描画時に使用する時間モード</summary>
        public static SequenceEditorModel.TimeMode TimeMode { get; set; } = SequenceEditorModel.TimeMode.Seconds;
    }
}