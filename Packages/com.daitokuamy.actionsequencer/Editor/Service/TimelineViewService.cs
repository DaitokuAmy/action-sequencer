using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor {
    /// <summary>
    /// タイムライン表示設定を扱うサービス
    /// </summary>
    internal sealed class TimelineViewService : IDisposable {
        /// <summary>選択対象 fit 時の最小 duration</summary>
        private const float MinFitDuration = 0.25f;

        /// <summary>
        /// タイムライン表示設定の永続化データ
        /// </summary>
        [Serializable]
        private sealed class UserData {
            [SerializeField]
            private float _timeToSize;

            [SerializeField]
            private SequenceEditorModel.TimeMode _timeMode;

            [SerializeField]
            private bool _timeFit;

            /// <summary>保存する TimeToSize 値</summary>
            public float TimeToSize => _timeToSize;
            /// <summary>保存する時間モード</summary>
            public SequenceEditorModel.TimeMode TimeMode => _timeMode;
            /// <summary>保存する time fit 状態</summary>
            public bool TimeFit => _timeFit;

            /// <summary>
            /// シリアライズ用の空コンストラクタ
            /// </summary>
            public UserData() {
            }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="timeToSize">保存する TimeToSize</param>
            /// <param name="timeMode">保存する時間モード</param>
            /// <param name="timeFit">保存する time fit 状態</param>
            public UserData(float timeToSize, SequenceEditorModel.TimeMode timeMode, bool timeFit) {
                _timeToSize = timeToSize;
                _timeMode = timeMode;
                _timeFit = timeFit;
            }
        }

        private readonly SequenceEditorModel _model;
        private readonly SequenceClipRepository _repository;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="model">編集中のモデル</param>
        /// <param name="repository">永続化を担う repository</param>
        public TimelineViewService(SequenceEditorModel model, SequenceClipRepository repository) {
            _model = model;
            _repository = repository;
            LoadUserData();
        }

        /// <summary>表示設定変更時に発火する</summary>
        public event Action SettingsChanged;

        /// <summary>現在の TimeToSize</summary>
        public float TimeToSize => _model.TimeToSize;
        /// <summary>現在の時間モード</summary>
        public SequenceEditorModel.TimeMode CurrentTimeMode => _model.CurrentTimeMode;
        /// <summary>現在の time fit 状態</summary>
        public bool TimeFit => _model.TimeFit;

        /// <summary>
        /// クリップオープン後の表示設定を反映
        /// </summary>
        public void OnClipOpened() {
            var clipModel = _model.ClipModel;
            if (clipModel != null) {
                _model.SetTimeMode(FrameRateToTimeMode(clipModel.FrameRate));
            }

            SaveUserData();
            SettingsChanged?.Invoke();
        }

        /// <summary>
        /// クリップ再読込後の表示設定を反映
        /// </summary>
        public void OnClipReloaded() {
            var clipModel = _model.ClipModel;
            if (clipModel != null) {
                _model.SetTimeMode(FrameRateToTimeMode(clipModel.FrameRate));
            }

            SettingsChanged?.Invoke();
        }

        /// <summary>
        /// TimeToSize を更新
        /// </summary>
        /// <param name="value">設定する TimeToSize</param>
        public void SetTimeToSize(float value) {
            if (!_model.SetTimeToSize(value)) {
                return;
            }

            SaveUserData();
            SettingsChanged?.Invoke();
        }

        /// <summary>
        /// 時間モードを更新
        /// </summary>
        /// <param name="value">設定する時間モード</param>
        public void SetTimeMode(SequenceEditorModel.TimeMode value) {
            if (!_model.SetTimeMode(value)) {
                return;
            }

            if (_model.CurrentClip != null) {
                _repository.SetFrameRate(_model.CurrentClip, value);
                _model.ClipModel?.SetFrameRate(TimeModeToFrameRate(value));
            }

            SaveUserData();
            SettingsChanged?.Invoke();
        }

        /// <summary>
        /// time fit 状態を更新
        /// </summary>
        /// <param name="value">設定する time fit 状態</param>
        public void SetTimeFit(bool value) {
            if (!_model.SetTimeFit(value)) {
                return;
            }

            SaveUserData();
            SettingsChanged?.Invoke();
        }

        /// <summary>
        /// スナップ後の時間を取得
        /// </summary>
        /// <param name="time">基準となる時間</param>
        /// <returns>スナップ後の時間</returns>
        public float GetAbsorptionTime(float time) {
            return _model.GetAbsorptionTime(time);
        }

        /// <summary>
        /// 画面幅に合う TimeToSize を自動設定
        /// </summary>
        /// <param name="contentWidth">表示領域の幅</param>
        /// <returns>値が変更された場合は true</returns>
        public bool SetBestTimeToSize(float contentWidth) {
            var changed = _model.SetBestTimeToSize(contentWidth);
            if (!changed) {
                return false;
            }

            SaveUserData();
            SettingsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 現在の選択対象が収まる TimeToSize を自動設定
        /// </summary>
        /// <param name="selectedTargets">現在の選択対象</param>
        /// <param name="contentWidth">表示領域の幅</param>
        /// <param name="padding">左右に確保する余白</param>
        /// <param name="startTime">選択範囲の開始時間</param>
        /// <returns>選択範囲を算出できた場合は true</returns>
        public bool FitSelection(IReadOnlyList<Object> selectedTargets, float contentWidth, float padding, out float startTime) {
            startTime = 0.0f;
            if (contentWidth <= 0.0f || !TryGetSelectionRange(selectedTargets, out var endTime, out startTime)) {
                return false;
            }

            var fitWidth = Mathf.Max(1.0f, contentWidth - padding * 2.0f);
            var duration = Mathf.Max(endTime - startTime, MinFitDuration);
            var changed = _model.SetTimeToSize(fitWidth / duration);
            if (changed) {
                SaveUserData();
                SettingsChanged?.Invoke();
            }

            return true;
        }

        /// <inheritdoc/>
        public void Dispose() {
            SaveUserData();
        }

        /// <summary>
        /// EditorPrefs から表示設定を読み込む
        /// </summary>
        private void LoadUserData() {
            var key = $"{nameof(SequenceEditorModel)}_UserData";
            var json = EditorPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(json)) {
                return;
            }

            var userData = JsonUtility.FromJson<UserData>(json);
            _model.SetTimeToSize(userData.TimeToSize);
            _model.SetTimeMode(userData.TimeMode);
            _model.SetTimeFit(userData.TimeFit);
        }

        /// <summary>
        /// 現在の表示設定を EditorPrefs へ保存
        /// </summary>
        private void SaveUserData() {
            var key = $"{nameof(SequenceEditorModel)}_UserData";
            var userData = new UserData(
                _model.TimeToSize,
                _model.CurrentTimeMode,
                _model.TimeFit);
            EditorPrefs.SetString(key, JsonUtility.ToJson(userData));
        }

        /// <summary>
        /// フレームレートから時間モードへ変換
        /// </summary>
        /// <param name="frameRate">変換元のフレームレート</param>
        /// <returns>対応する時間モード</returns>
        private SequenceEditorModel.TimeMode FrameRateToTimeMode(int frameRate) {
            return frameRate switch {
                30 => SequenceEditorModel.TimeMode.Frames30,
                60 => SequenceEditorModel.TimeMode.Frames60,
                _ => SequenceEditorModel.TimeMode.Seconds,
            };
        }

        /// <summary>
        /// 時間モードから保存用フレームレートへ変換
        /// </summary>
        /// <param name="timeMode">変換元の時間モード</param>
        /// <returns>対応するフレームレート。秒表示は負値</returns>
        private int TimeModeToFrameRate(SequenceEditorModel.TimeMode timeMode) {
            return timeMode switch {
                SequenceEditorModel.TimeMode.Frames30 => 30,
                SequenceEditorModel.TimeMode.Frames60 => 60,
                _ => -1,
            };
        }

        /// <summary>
        /// 現在の選択対象から表示範囲を算出
        /// </summary>
        /// <param name="selectedTargets">現在の選択対象</param>
        /// <param name="endTime">選択範囲の終了時間</param>
        /// <param name="startTime">選択範囲の開始時間</param>
        /// <returns>算出できた場合は true</returns>
        private bool TryGetSelectionRange(IReadOnlyList<Object> selectedTargets, out float endTime, out float startTime) {
            startTime = 0.0f;
            endTime = 0.0f;

            var clipModel = _model.ClipModel;
            if (clipModel == null || selectedTargets == null || selectedTargets.Count == 0) {
                return false;
            }

            var hasRange = false;
            var minStartTime = float.MaxValue;
            var maxEndTime = float.MinValue;
            foreach (var target in selectedTargets) {
                switch (target) {
                    case SequenceEvent sequenceEvent:
                        var eventModel = clipModel.FindEventModel(sequenceEvent);
                        if (!TryGetEventRange(eventModel, out var eventStartTime, out var eventEndTime)) {
                            continue;
                        }

                        minStartTime = Mathf.Min(minStartTime, eventStartTime);
                        maxEndTime = Mathf.Max(maxEndTime, eventEndTime);
                        hasRange = true;
                        break;

                    case SequenceTrack sequenceTrack:
                        var trackModel = clipModel.FindTrackModel(sequenceTrack);
                        if (!TryGetTrackRange(trackModel, out var trackStartTime, out var trackEndTime)) {
                            continue;
                        }

                        minStartTime = Mathf.Min(minStartTime, trackStartTime);
                        maxEndTime = Mathf.Max(maxEndTime, trackEndTime);
                        hasRange = true;
                        break;
                }
            }

            if (!hasRange) {
                return false;
            }

            startTime = minStartTime;
            endTime = maxEndTime;
            return true;
        }

        /// <summary>
        /// Track の表示範囲を算出
        /// </summary>
        /// <param name="trackModel">対象の TrackModel</param>
        /// <param name="startTime">表示範囲の開始時間</param>
        /// <param name="endTime">表示範囲の終了時間</param>
        /// <returns>算出できた場合は true</returns>
        private bool TryGetTrackRange(SequenceTrackModel trackModel, out float startTime, out float endTime) {
            startTime = 0.0f;
            endTime = 0.0f;

            if (trackModel == null || trackModel.EventModels.Count == 0) {
                return false;
            }

            var ranges = trackModel.EventModels
                .Select(eventModel => TryGetEventRange(eventModel, out var eventStartTime, out var eventEndTime)
                    ? (valid: true, startTime: eventStartTime, endTime: eventEndTime)
                    : (valid: false, startTime: 0.0f, endTime: 0.0f))
                .Where(x => x.valid)
                .ToArray();
            if (ranges.Length == 0) {
                return false;
            }

            startTime = ranges.Min(x => x.startTime);
            endTime = ranges.Max(x => x.endTime);
            return true;
        }

        /// <summary>
        /// Event の表示範囲を算出
        /// </summary>
        /// <param name="eventModel">対象の EventModel</param>
        /// <param name="startTime">表示範囲の開始時間</param>
        /// <param name="endTime">表示範囲の終了時間</param>
        /// <returns>算出できた場合は true</returns>
        private bool TryGetEventRange(SequenceEventModel eventModel, out float startTime, out float endTime) {
            startTime = 0.0f;
            endTime = 0.0f;
            if (eventModel == null) {
                return false;
            }

            startTime = eventModel.GetStartTime();
            endTime = eventModel switch {
                SignalSequenceEventModel signalEventModel => signalEventModel.Time + signalEventModel.ViewDuration,
                _ => eventModel.GetEndTime(),
            };
            endTime = Mathf.Max(startTime, endTime);
            return true;
        }
    }
}
