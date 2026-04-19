using System.Collections.Generic;
using System.Linq;

namespace ActionSequencer.Editor {
    /// <summary>
    /// 1 つの SequenceClip セクションを表す編集用キャッシュ
    /// </summary>
    internal sealed class SequenceClipSectionModel {
        private readonly List<SequenceTrackModel> _trackModels;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="target">対応する SequenceClip</param>
        /// <param name="displayName">表示名</param>
        /// <param name="trackModels">保持する TrackModel 一覧</param>
        public SequenceClipSectionModel(
            SequenceClip target,
            string displayName,
            IEnumerable<SequenceTrackModel> trackModels) {
            Target = target;
            DisplayName = displayName;
            _trackModels = trackModels.ToList();
        }

        /// <summary>対応する SequenceClip</summary>
        public SequenceClip Target { get; }
        /// <summary>セクション表示名</summary>
        public string DisplayName { get; }
        /// <summary>保持している TrackModel 一覧</summary>
        public IReadOnlyList<SequenceTrackModel> TrackModels => _trackModels;
    }
}
