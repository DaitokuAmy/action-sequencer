using System;
using System.Collections.Generic;
using System.Linq;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceEditor用のModel
    /// </summary>
    public class SequenceClipModel : SerializedObjectModel {
        private SerializedProperty _tracks;
        private SerializedProperty _frameRate;
        private List<SequenceTrackModel> _trackModels = new List<SequenceTrackModel>();

        public Subject<SequenceTrackModel> AddedTrackModelSubject { get; } = new Subject<SequenceTrackModel>();
        public Subject<SequenceTrackModel> RemovedTrackModelSubject { get; } = new Subject<SequenceTrackModel>();
        public Subject<SequenceEventModel> AddedEventModelSubject { get; } = new Subject<SequenceEventModel>();
        public Subject<SequenceEventModel> RemovedEventModelSubject { get; } = new Subject<SequenceEventModel>();

        public IReadOnlyList<SequenceTrackModel> TrackModels => _trackModels;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceClipModel(SequenceClip target)
            : base(target) {
            _tracks = SerializedObject.FindProperty("tracks");
            _frameRate = SerializedObject.FindProperty("frameRate");

            RefreshTracks();
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public override void Dispose() {
            base.Dispose();
            ClearTrackModels();
        }

        /// <summary>
        /// Trackの状態を再構築
        /// </summary>
        public void RefreshTracks() {
            ClearTrackModels();

            for (var i = 0; i < _tracks.arraySize; i++) {
                var sequenceTrack = _tracks.GetArrayElementAtIndex(i).objectReferenceValue as SequenceTrack;
                if (sequenceTrack != null) {
                    var model = new SequenceTrackModel(sequenceTrack);
                    _trackModels.Add(model);
                    AddedTrackModelSubject.Invoke(model);
                }
            }
        }

        /// <summary>
        /// TimeModeを元にフレームレートを設定
        /// </summary>
        public void SetFrameRate(SequenceEditorModel.TimeMode timeMode) {
            SerializedObject.Update();
            switch (timeMode) {
                case SequenceEditorModel.TimeMode.Seconds:
                    _frameRate.intValue = -1;
                    break;
                case SequenceEditorModel.TimeMode.Frames30:
                    _frameRate.intValue = 30;
                    break;
                case SequenceEditorModel.TimeMode.Frames60:
                    _frameRate.intValue = 60;
                    break;
            }

            SerializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// フレームレートを元にTimeModeを取得
        /// </summary>
        public SequenceEditorModel.TimeMode GetTimeMode() {
            if (_frameRate.intValue < 0) {
                return SequenceEditorModel.TimeMode.Seconds;
            }

            if (_frameRate.intValue == 30) {
                return SequenceEditorModel.TimeMode.Frames30;
            }

            if (_frameRate.intValue == 60) {
                return SequenceEditorModel.TimeMode.Frames60;
            }

            return SequenceEditorModel.TimeMode.Seconds;
        }

        /// <summary>
        /// トラックの追加
        /// </summary>
        public SequenceTrackModel AddTrack() {
            var track = ScriptableObject.CreateInstance<SequenceTrack>();
            track.name = nameof(SequenceTrack);
            AssetDatabase.AddObjectToAsset(track, Target);

            // 要素に追加
            SerializedObject.Update();
            _tracks.arraySize++;
            _tracks.GetArrayElementAtIndex(_tracks.arraySize - 1).objectReferenceValue = track;
            SerializedObject.ApplyModifiedProperties();
            var trackModel = new SequenceTrackModel(track);
            trackModel.Label = "Track";
            _trackModels.Add(trackModel);
            AddedTrackModelSubject.Invoke(trackModel);

            // Eventの追加を監視
            AddDisposable(trackModel.AddedSignalEventModelSubject
                .Subscribe(x => AddedEventModelSubject.Invoke(x)));
            AddDisposable(trackModel.AddedRangeEventModelSubject
                .Subscribe(x => AddedEventModelSubject.Invoke(x)));

            return trackModel;
        }

        /// <summary>
        /// Trackの削除
        /// </summary>
        public void RemoveTrack(SequenceTrack track) {
            var model = _trackModels.FirstOrDefault(x => x.Target == track);
            if (model == null) {
                return;
            }

            // Trackの除外
            SerializedObject.Update();
            for (var i = _tracks.arraySize - 1; i >= 0; i--) {
                var element = _tracks.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != model.Target) {
                    continue;
                }

                _tracks.DeleteArrayElementAtIndex(i);
            }

            SerializedObject.ApplyModifiedProperties();
            _trackModels.Remove(model);

            // Trackの削除
            Undo.DestroyObjectImmediate(model.Target);

            // 通知
            RemovedTrackModelSubject.Invoke(model);
            model.RemoveEvents();
            model.Dispose();
        }

        /// <summary>
        /// 保持しているTrackを全て削除
        /// </summary>
        public void RemoveTracks() {
            var tracks = _trackModels.Select(x => (SequenceTrack)x.Target).ToArray();
            foreach (var track in tracks) {
                RemoveTrack(track);
            }
        }

        /// <summary>
        /// TrackModelの削除(SerializedObjectからは消えない)
        /// </summary>
        private void ClearTrackModels() {
            foreach (var model in _trackModels) {
                RemovedTrackModelSubject.Invoke(model);
                model.Dispose();
            }

            _trackModels.Clear();
        }
    }
}