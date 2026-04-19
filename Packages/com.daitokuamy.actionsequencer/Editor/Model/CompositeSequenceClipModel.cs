using System.Collections.Generic;
using System.Linq;

namespace ActionSequencer.Editor {
    /// <summary>
    /// root clip と include clips を統合した編集用キャッシュ
    /// </summary>
    internal sealed class CompositeSequenceClipModel : SequenceClipModel {
        private readonly List<SequenceClipSectionModel> _sections;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="target">対応する root の SequenceClip</param>
        /// <param name="frameRate">現在のフレームレート</param>
        /// <param name="filterData">イベントフィルタ設定</param>
        /// <param name="sections">保持するセクション一覧</param>
        public CompositeSequenceClipModel(
            SequenceClip target,
            int frameRate,
            SequenceEventFilterData filterData,
            IEnumerable<SequenceClipSectionModel> sections)
            : base(
                target,
                frameRate,
                filterData,
                sections.SelectMany(x => x.TrackModels)) {
            _sections = sections.ToList();
        }

        /// <summary>保持しているセクション一覧</summary>
        public IReadOnlyList<SequenceClipSectionModel> Sections => _sections;
    }
}
