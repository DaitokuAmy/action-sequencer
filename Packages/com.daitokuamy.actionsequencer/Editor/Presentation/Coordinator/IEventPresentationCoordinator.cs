using System;
using System.Collections.Generic;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Event Presentation を管理する Coordinator
    /// </summary>
    internal interface IEventPresentationCoordinator : IDisposable {
        /// <summary>
        /// Event Presentation を再構築
        /// </summary>
        /// <param name="editorModel">編集中のモデル</param>
        /// <param name="trackModels">表示対象の TrackModel 一覧</param>
        /// <param name="trackPresentations">追加先となる TrackPresentation 一覧</param>
        void Rebuild(
            SequenceEditorModel editorModel,
            IReadOnlyList<SequenceTrackModel> trackModels,
            IReadOnlyList<TrackPresentationContext> trackPresentations);

        /// <summary>
        /// Event Presentation を全破棄
        /// </summary>
        void Clear();
    }
}
