using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Preview 設定を扱うサービス
    /// </summary>
    internal sealed class PreviewService {
        private readonly SequenceClipRepository _repository;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="repository">永続化境界</param>
        public PreviewService(SequenceClipRepository repository) {
            _repository = repository;
        }

        /// <summary>
        /// 現在の表示対象に対応する Preview 設定を取得
        /// </summary>
        /// <param name="currentClip">現在編集中の SequenceClip</param>
        /// <param name="rootClip">ルートの SequenceClip</param>
        /// <returns>表示する AnimationClip とオフセット時間</returns>
        public (AnimationClip, float) LoadPreviewData(SequenceClip currentClip, SequenceClip rootClip) {
            var previewData = _repository.LoadPreviewData(currentClip);
            if (currentClip != rootClip && previewData.Item1 == null) {
                return _repository.LoadPreviewData(rootClip);
            }

            return previewData;
        }

        /// <summary>
        /// Preview 設定を保存
        /// </summary>
        /// <param name="sequenceClip">保存先の SequenceClip</param>
        /// <param name="animationClip">保存する AnimationClip</param>
        /// <param name="offsetTime">保存するオフセット時間</param>
        public void SavePreviewData(SequenceClip sequenceClip, AnimationClip animationClip, float offsetTime) {
            _repository.SavePreviewData(sequenceClip, animationClip, offsetTime);
        }
    }
}
