namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceのGUI用定義
    /// </summary>
    public static class SequenceEditorGUI
    {
        // GUI描画時に使用するTimeMode
        public static SequenceEditorModel.TimeMode TimeMode { get; set; } = SequenceEditorModel.TimeMode.Seconds;
    }
}
