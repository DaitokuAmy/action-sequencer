using System.Collections.Generic;
using System.Linq;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceEvent 用の PresentationCoordinator
    /// </summary>
    internal sealed class SequenceEventPresentationCoordinator : IEventPresentationCoordinator {
        private readonly SequenceEventPresentationFactory _factory;
        private readonly List<EventPresentationContext> _eventPresentations = new();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="factory">Event Presentation 生成用 Factory</param>
        public SequenceEventPresentationCoordinator(SequenceEventPresentationFactory factory) {
            _factory = factory;
        }

        /// <inheritdoc/>
        public void Rebuild(
            SequenceEditorModel editorModel,
            IReadOnlyList<SequenceTrackModel> trackModels,
            IReadOnlyList<TrackPresentationContext> trackPresentations) {
            Clear();

            if (editorModel == null || trackModels == null || trackPresentations == null) {
                return;
            }

            var trackPresentationMap = trackPresentations.ToDictionary(x => x.Target);
            foreach (var trackModel in trackModels) {
                if (!trackPresentationMap.TryGetValue(trackModel.Target, out var trackPresentation)) {
                    continue;
                }

                trackPresentation.UpdateModel(trackModel);

                foreach (var eventModel in trackModel.EventModels) {
                    _eventPresentations.Add(_factory.CreateEventPresentation(editorModel, trackPresentation, eventModel));
                }

                trackPresentation.RefreshTrackArea();
            }
        }

        /// <inheritdoc/>
        public void Clear() {
            foreach (var eventPresentation in _eventPresentations) {
                eventPresentation.Dispose();
            }

            _eventPresentations.Clear();
        }

        /// <inheritdoc/>
        public void Dispose() {
            Clear();
        }
    }
}
