using System;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// タイムライン表示設定を扱うサービス
    /// </summary>
    internal sealed class TimelineViewService : IDisposable {
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
    }
}