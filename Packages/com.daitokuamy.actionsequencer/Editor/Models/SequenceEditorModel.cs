using System.Collections.Generic;
using System.Linq;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceEditor用のModel
    /// </summary>
    internal class SequenceEditorModel : Model {
        // 時間モード
        public enum TimeMode {
            Seconds,
            Frames30,
            Frames60,
        }

        // 保存用ユーザーデータ
        private class UserData {
            public float timeToSize;
            public TimeMode timeMode;
            public bool timeFit;
        }

        private List<Object> _selectedTargets = new List<Object>();

        public Subject<Object[]> ChangedSelectedTargetsSubject { get; } = new Subject<Object[]>();

        public Subject<Object, SequenceEventManipulator.DragType> EventDragStartSubject { get; } =
            new Subject<Object, SequenceEventManipulator.DragType>();

        public Subject<Object, SequenceEventManipulator.DragInfo> EventDraggingSubject { get; } =
            new Subject<Object, SequenceEventManipulator.DragInfo>();

        public Subject<SequenceClipModel> ChangeClipModelSubject { get; } = new Subject<SequenceClipModel>();

        public Object[] SelectedTargets => _selectedTargets.ToArray();
        public VisualElement RootElement { get; private set; }
        public SequenceClipModel ClipModel { get; private set; }

        public ReactiveProperty<float> TimeToSize { get; private set; } =
            new ReactiveProperty<float>(200.0f, x => Mathf.Max(40.0f, x));

        public ReactiveProperty<TimeMode> CurrentTimeMode { get; private set; } =
            new ReactiveProperty<TimeMode>(TimeMode.Seconds);

        public ReactiveProperty<bool> TimeFit { get; private set; } = new ReactiveProperty<bool>(true);

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceEditorModel(VisualElement rootElement) {
            RootElement = rootElement;
            LoadUserData();

            // ユーザーデータ関連の更新を常に保存
            TimeToSize.Subscribe(_ => SaveUserData());
            CurrentTimeMode.Subscribe(_ => SaveUserData());
            TimeFit.Subscribe(_ => SaveUserData());

            // TimeModeとFrameRateの同期
            AddDisposable(CurrentTimeMode
                .Subscribe(x => {
                    if (ClipModel != null) {
                        ClipModel.SetFrameRate(x);
                    }
                }));
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public override void Dispose() {
            base.Dispose();
            SaveUserData();
            SetSequenceClip(null);
        }

        /// <summary>
        /// SequenceClipの設定
        /// </summary>
        public SequenceClipModel SetSequenceClip(SequenceClip clip) {
            RemoveSelectedTargets();

            if (ClipModel != null) {
                ClipModel.Dispose();
                ClipModel = null;
            }

            if (clip != null) {
                ClipModel = new SequenceClipModel(clip);

                // TimeModeをFrameRateに反映
                CurrentTimeMode.Value = ClipModel.GetTimeMode();
            }

            ChangeClipModelSubject.Invoke(ClipModel);

            return ClipModel;
        }

        /// <summary>
        /// 選択対象の変更
        /// </summary>
        public void SetSelectedTarget(Object target) {
            _selectedTargets.Clear();
            AddSelectedTarget(target);
        }

        /// <summary>
        /// 選択対象の追加
        /// </summary>
        public void AddSelectedTarget(Object target) {
            if (_selectedTargets.Contains(target)) {
                return;
            }

            _selectedTargets.Add(target);
            ChangedSelectedTargetsSubject.Invoke(SelectedTargets);
        }

        /// <summary>
        /// 選択対象の削除
        /// </summary>
        public void RemoveSelectedTarget(Object target) {
            if (!_selectedTargets.Remove(target)) {
                return;
            }

            ChangedSelectedTargetsSubject.Invoke(SelectedTargets);
        }

        /// <summary>
        /// 全選択対象の削除
        /// </summary>
        public void RemoveSelectedTargets() {
            _selectedTargets.Clear();
            ChangedSelectedTargetsSubject.Invoke(SelectedTargets);
        }

        /// <summary>
        /// Drag開始
        /// </summary>
        public void StartDragEvent(Object target, SequenceEventManipulator.DragType dragType) {
            EventDragStartSubject.Invoke(target, dragType);
        }

        /// <summary>
        /// Drag終了
        /// </summary>
        public void DraggingEvent(Object target, SequenceEventManipulator.DragInfo info) {
            EventDraggingSubject.Invoke(target, info);
        }

        /// <summary>
        /// 吸着させた場合の時間を取得
        /// </summary>
        public float GetAbsorptionTime(float time) {
            if (!TimeFit.Value) {
                return time;
            }

            switch (CurrentTimeMode.Value) {
                case TimeMode.Seconds:
                    return Mathf.RoundToInt(time * 40) / 40.0f;
                case TimeMode.Frames30:
                    return Mathf.RoundToInt(time * 30) / 30.0f;
                case TimeMode.Frames60:
                    return Mathf.RoundToInt(time * 60) / 60.0f;
            }

            return time;
        }

        /// <summary>
        /// ちょうど良いTimeToSizeを設定
        /// </summary>
        public bool SetBestTimeToSize(float contentWidth) {
            if (ClipModel == null || contentWidth <= 0.0f) {
                return false;
            }

            var eventModels = ClipModel.TrackModels
                .SelectMany(x => x.EventModels)
                .ToArray();

            if (!eventModels.Any()) {
                return false;
            }

            var duration = eventModels.Max(x => {
                if (x is SignalSequenceEventModel signalEventModel) {
                    return signalEventModel.Time;
                }

                if (x is RangeSequenceEventModel rangeEventModel) {
                    return rangeEventModel.ExitTime;
                }

                return 0.0f;
            });

            if (duration <= 0.0f) {
                return false;
            }

            // 幅に合うようにTimeToSizeを設定
            TimeToSize.Value = contentWidth / duration;
            return true;
        }

        /// <summary>
        /// ユーザーデータの読み込み
        /// </summary>
        private void LoadUserData() {
            var key = $"{nameof(SequenceEditorModel)}_UserData";
            var json = EditorPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json)) {
                return;
            }

            var userData = JsonUtility.FromJson<UserData>(json);
            TimeToSize.Value = userData.timeToSize;
            CurrentTimeMode.Value = userData.timeMode;
            TimeFit.Value = userData.timeFit;
        }

        /// <summary>
        /// ユーザーデータの保存
        /// </summary>
        private void SaveUserData() {
            var key = $"{nameof(SequenceEditorModel)}_UserData";
            var userData = new UserData {
                timeToSize = TimeToSize.Value,
                timeMode = CurrentTimeMode.Value,
                timeFit = TimeFit.Value
            };
            EditorPrefs.SetString(key, JsonUtility.ToJson(userData));
        }
    }
}