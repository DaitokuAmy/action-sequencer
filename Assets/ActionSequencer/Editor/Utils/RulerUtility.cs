namespace ActionSequencer.Editor.Utils
{
    /// <summary>
    /// ルーラー用のユーティリティ
    /// </summary>
    public static class RulerUtility
    {
        /// <summary>
        /// 現在のルーラーメモリサイズ計算
        /// </summary>
        public static float CalcMemorySize(SequenceEditorModel editorModel)
        {
            var timeMode = editorModel.CurrentTimeMode.Value;
            return editorModel.TimeToSize.Value * GetThickSeconds(timeMode) / GetThickCycle(timeMode);
        }

        /// <summary>
        /// 1 Thickで何秒を表すか取得
        /// </summary>
        public static float GetThickSeconds(SequenceEditorModel.TimeMode timeMode)
        {
            switch (timeMode)
            {
                case SequenceEditorModel.TimeMode.Seconds:
                    return 0.5f;
                case SequenceEditorModel.TimeMode.Frames30:
                    return 0.5f;
                case SequenceEditorModel.TimeMode.Frames60:
                    return 0.5f;
            }
            return 1.0f;
        }

        /// <summary>
        /// ThickCycleの取得
        /// </summary>
        public static int GetThickCycle(SequenceEditorModel.TimeMode timeMode)
        {
            switch (timeMode)
            {
                case SequenceEditorModel.TimeMode.Seconds:
                    return 10;
                case SequenceEditorModel.TimeMode.Frames30:
                    return 15;
                case SequenceEditorModel.TimeMode.Frames60:
                    return 15;
            }
            return 10;
        }
    }
}