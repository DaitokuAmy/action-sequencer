using System.Collections.Generic;
using System.Linq;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceTrack の編集用キャッシュ
    /// </summary>
    internal sealed class SequenceTrackModel {
        private readonly List<SequenceEventModel> _eventModels;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="target">対応する Track</param>
        /// <param name="label">表示ラベル</param>
        /// <param name="eventModels">保持する EventModel 一覧</param>
        public SequenceTrackModel(SequenceTrack target, string label, IEnumerable<SequenceEventModel> eventModels) {
            Target = target;
            Label = label;
            _eventModels = eventModels.ToList();

            foreach (var eventModel in _eventModels) {
                eventModel.TrackModel = this;
            }
        }

        /// <summary>対応する Track</summary>
        public SequenceTrack Target { get; }
        /// <summary>表示ラベル</summary>
        public string Label { get; private set; }
        /// <summary>Foldout 状態</summary>
        public bool Foldout { get; private set; } = true;
        /// <summary>保持している EventModel 一覧</summary>
        public IReadOnlyList<SequenceEventModel> EventModels => _eventModels;

        /// <summary>
        /// ラベルを更新
        /// </summary>
        /// <param name="label">更新後のラベル</param>
        /// <returns>値が変化した場合は true</returns>
        public bool SetLabel(string label) {
            if (Label == label) {
                return false;
            }

            Label = label;
            return true;
        }

        /// <summary>
        /// Foldout 状態を更新
        /// </summary>
        /// <param name="foldout">更新後の状態</param>
        /// <returns>値が変化した場合は true</returns>
        public bool SetFoldout(bool foldout) {
            if (Foldout == foldout) {
                return false;
            }

            Foldout = foldout;
            return true;
        }

        /// <summary>
        /// EventModel の index を取得
        /// </summary>
        /// <param name="eventModel">対象の EventModel</param>
        /// <returns>index</returns>
        public int GetEventIndex(SequenceEventModel eventModel) {
            return _eventModels.IndexOf(eventModel);
        }

        /// <summary>
        /// Event に対応する EventModel を取得
        /// </summary>
        /// <param name="sequenceEvent">対象の Event</param>
        /// <returns>対応する EventModel</returns>
        public SequenceEventModel FindEventModel(SequenceEvent sequenceEvent) {
            return _eventModels.FirstOrDefault(x => x.Target == sequenceEvent);
        }
    }
}