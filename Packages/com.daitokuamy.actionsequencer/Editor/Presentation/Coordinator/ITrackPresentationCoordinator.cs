using System;
using System.Collections.Generic;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Track Presentation を管理する Coordinator
    /// </summary>
    internal interface ITrackPresentationCoordinator : IDisposable {
        /// <summary>現在の TrackPresentation 一覧</summary>
        IReadOnlyList<TrackPresentationContext> TrackPresentations { get; }

        /// <summary>
        /// Track Presentation を再構築
        /// </summary>
        /// <param name="editorModel">編集中のモデル</param>
        /// <param name="trackModels">表示対象の TrackModel 一覧</param>
        void Rebuild(SequenceEditorModel editorModel, IReadOnlyList<SequenceTrackModel> trackModels);

        /// <summary>
        /// Track Presentation を全破棄
        /// </summary>
        void Clear();
    }
}
