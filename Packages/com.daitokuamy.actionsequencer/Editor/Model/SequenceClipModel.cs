using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceClip の編集用キャッシュ
    /// </summary>
    internal class SequenceClipModel {
        private readonly List<SequenceTrackModel> _trackModels;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="target">対応する SequenceClip</param>
        /// <param name="frameRate">現在のフレームレート</param>
        /// <param name="filterData">イベントフィルタ設定</param>
        /// <param name="trackModels">保持する TrackModel 一覧</param>
        public SequenceClipModel(
            SequenceClip target,
            int frameRate,
            SequenceEventFilterData filterData,
            IEnumerable<SequenceTrackModel> trackModels) {
            Target = target;
            FrameRate = frameRate;
            FilterData = filterData;
            _trackModels = trackModels.ToList();
        }

        /// <summary>対応する SequenceClip</summary>
        public SequenceClip Target { get; }
        /// <summary>現在のフレームレート</summary>
        public int FrameRate { get; private set; }
        /// <summary>イベントフィルタ設定</summary>
        public SequenceEventFilterData FilterData { get; }
        /// <summary>保持している TrackModel 一覧</summary>
        public IReadOnlyList<SequenceTrackModel> TrackModels => _trackModels;

        /// <summary>
        /// フレームレートを更新
        /// </summary>
        /// <param name="frameRate">更新後のフレームレート</param>
        /// <returns>値が変化した場合は true</returns>
        public bool SetFrameRate(int frameRate) {
            if (FrameRate == frameRate) {
                return false;
            }

            FrameRate = frameRate;
            return true;
        }

        /// <summary>
        /// 型の namespace がフィルタ対象か判定
        /// </summary>
        /// <param name="type">判定対象の型</param>
        /// <returns>表示対象なら true</returns>
        public bool FilterNamespace(Type type) {
            if (FilterData == null) {
                return true;
            }

            var currentNamespace = type.Namespace ?? string.Empty;
            if (FilterData.ignoreNamespaces.Length > 0 &&
                FilterData.ignoreNamespaces.Any(ignoreNamespace => currentNamespace == ignoreNamespace)) {
                return false;
            }

            if (FilterData.namespaceFilters.Length <= 0) {
                return true;
            }

            return FilterData.namespaceFilters.Any(namespaceFilter => currentNamespace == namespaceFilter);
        }

        /// <summary>
        /// 表示パスがフィルタ対象か判定
        /// </summary>
        /// <param name="path">判定対象のパス</param>
        /// <returns>表示対象なら true</returns>
        public bool FilterPath(string path) {
            if (FilterData == null) {
                return true;
            }

            if (FilterData.ignorePaths.Length > 0 &&
                FilterData.ignorePaths.Any(ignorePath => !string.IsNullOrEmpty(ignorePath) && path.StartsWith(ignorePath))) {
                return false;
            }

            if (FilterData.pathFilters.Length <= 0) {
                return true;
            }

            return FilterData.pathFilters.Any(pathFilter => !string.IsNullOrEmpty(pathFilter) && path.StartsWith(pathFilter));
        }

        /// <summary>
        /// TrackModel の表示順 index を取得
        /// </summary>
        /// <param name="trackModel">対象の TrackModel</param>
        /// <returns>index</returns>
        public int GetTrackIndex(SequenceTrackModel trackModel) {
            return _trackModels.IndexOf(trackModel);
        }

        /// <summary>
        /// Track に対応する TrackModel を取得
        /// </summary>
        /// <param name="track">対象の Track</param>
        /// <returns>対応する TrackModel</returns>
        public SequenceTrackModel FindTrackModel(SequenceTrack track) {
            return _trackModels.FirstOrDefault(x => x.Target == track);
        }

        /// <summary>
        /// Event に対応する EventModel を取得
        /// </summary>
        /// <param name="sequenceEvent">対象の Event</param>
        /// <returns>対応する EventModel</returns>
        public SequenceEventModel FindEventModel(SequenceEvent sequenceEvent) {
            return _trackModels
                .Select(trackModel => trackModel.FindEventModel(sequenceEvent))
                .FirstOrDefault(eventModel => eventModel != null);
        }

        /// <summary>
        /// 指定ターゲットが保持対象に含まれるか判定
        /// </summary>
        /// <param name="target">判定対象</param>
        /// <returns>保持対象に含まれる場合は true</returns>
        public bool ContainsTarget(Object target) {
            return GetTargetOrder(target) >= 0;
        }

        /// <summary>
        /// ターゲットの表示順を取得
        /// </summary>
        /// <param name="target">対象のターゲット</param>
        /// <returns>表示順 index</returns>
        public int GetTargetOrder(Object target) {
            var index = 0;
            foreach (var trackModel in _trackModels) {
                if (trackModel.Target == target) {
                    return index;
                }

                index++;

                foreach (var eventModel in trackModel.EventModels) {
                    if (eventModel.Target == target) {
                        return index;
                    }

                    index++;
                }
            }

            return -1;
        }

        /// <summary>
        /// 表示順からターゲットを取得
        /// </summary>
        /// <param name="order">表示順 index</param>
        /// <returns>対応するターゲット</returns>
        public Object GetTargetByOrder(int order) {
            if (order < 0) {
                return null;
            }

            var index = 0;
            foreach (var trackModel in _trackModels) {
                if (index == order) {
                    return trackModel.Target;
                }

                index++;

                foreach (var eventModel in trackModel.EventModels) {
                    if (index == order) {
                        return eventModel.Target;
                    }

                    index++;
                }
            }

            return null;
        }
    }
}
