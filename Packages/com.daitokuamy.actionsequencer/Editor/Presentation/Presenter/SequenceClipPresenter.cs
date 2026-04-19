using ActionSequencer.Editor.Utils;
using ActionSequencer.Editor.VisualElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceClip 用 Presenter
    /// </summary>
    internal sealed class SequenceClipPresenter : System.IDisposable {
        private readonly SequenceEditorModel _editorModel;
        private readonly TimelineViewService _timelineService;
        private readonly ScrollView _trackLabelListView;
        private readonly ScrollView _trackScrollView;
        private readonly VisualElement _rulerArea;
        private readonly SequenceTrackListView _trackListView;
        private readonly RulerView _headerRulerView;
        private readonly System.Collections.Generic.List<System.IDisposable> _disposables = new();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="trackLabelListView">Track ラベル一覧の ScrollView</param>
        /// <param name="trackScrollView">Track 一覧の ScrollView</param>
        /// <param name="rulerArea">ルーラー表示領域</param>
        /// <param name="trackListView">Track 一覧の View</param>
        /// <param name="editorModel">編集中のモデル</param>
        /// <param name="timelineService">タイムライン設定を扱うサービス</param>
        public SequenceClipPresenter(
            ScrollView trackLabelListView,
            ScrollView trackScrollView,
            VisualElement rulerArea,
            SequenceTrackListView trackListView,
            SequenceEditorModel editorModel,
            TimelineViewService timelineService) {
            _editorModel = editorModel;
            _timelineService = timelineService;
            _trackLabelListView = trackLabelListView;
            _trackScrollView = trackScrollView;
            _rulerArea = rulerArea;
            _trackListView = trackListView;
            _headerRulerView = _rulerArea.Q<RulerView>();

            _trackListView.SetRulerMask(_rulerArea);
            if (_headerRulerView != null) {
                _headerRulerView.MaskElement = _rulerArea;
                _headerRulerView.OnGetThickLabel += OnGetThickLabel;
            }

            _timelineService.SettingsChanged += OnTimelineSettingsChanged;

            AddDisposable(new ActionDisposable(() => _timelineService.SettingsChanged -= OnTimelineSettingsChanged));

            _trackLabelListView.verticalScroller.valueChanged += OnTrackLabelScrollChanged;
            _trackScrollView.verticalScroller.valueChanged += OnTrackScrollChanged;
            _trackScrollView.horizontalScroller.valueChanged += OnHorizontalScrollChanged;

            AddDisposable(new ActionDisposable(() => _trackLabelListView.verticalScroller.valueChanged -= OnTrackLabelScrollChanged));
            AddDisposable(new ActionDisposable(() => _trackScrollView.verticalScroller.valueChanged -= OnTrackScrollChanged));
            AddDisposable(new ActionDisposable(() => _trackScrollView.horizontalScroller.valueChanged -= OnHorizontalScrollChanged));
            if (_headerRulerView != null) {
                AddDisposable(new ActionDisposable(() => _headerRulerView.OnGetThickLabel -= OnGetThickLabel));
            }

            void ApplyListPadding() {
                var viewport = _trackListView.parent.parent;
                var totalWidth = viewport.layout.width - 4.0f;
                var baseWidth = _trackListView.contentRect.width;
                var padding = Mathf.Max(200.0f, totalWidth - baseWidth);
                _trackListView.style.paddingRight = padding;
            }

            AddChangedCallback<GeometryChangedEvent>(_trackListView.parent.parent, _ => ApplyListPadding());
            AddChangedCallback<GeometryChangedEvent>(_trackListView.contentContainer, _ => ApplyListPadding());
            AddChangedCallback<GeometryChangedEvent>(_trackListView, _ => SetRulerWidth(_trackListView.layout.width));
            AddChangedCallback<WheelEvent>(_rulerArea, OnRulerWheel);

            RefreshRuler();
        }

        /// <summary>
        /// 対応するラベル一覧 View
        /// </summary>
        public ScrollView View => _trackLabelListView;

        /// <summary>
        /// 終了処理
        /// </summary>
        public void Dispose() {
            foreach (var disposable in _disposables) {
                disposable.Dispose();
            }

            _disposables.Clear();
        }

        /// <summary>
        /// Disposable を登録
        /// </summary>
        /// <param name="disposable">登録対象</param>
        private void AddDisposable(System.IDisposable disposable) {
            _disposables.Add(disposable);
        }

        /// <summary>
        /// VisualElement の callback を登録
        /// </summary>
        /// <typeparam name="TEvent">監視するイベント型</typeparam>
        /// <param name="element">監視対象</param>
        /// <param name="callback">受信時の処理</param>
        private void AddChangedCallback<TEvent>(VisualElement element, EventCallback<TEvent> callback)
            where TEvent : EventBase<TEvent>, new() {
            element.RegisterCallback(callback);
            AddDisposable(new ActionDisposable(() => element.UnregisterCallback(callback)));
        }

        /// <summary>
        /// ラベル側の縦スクロールを Track 側へ同期
        /// </summary>
        /// <param name="value">同期後のスクロール量</param>
        private void OnTrackLabelScrollChanged(float value) {
            _trackScrollView.verticalScroller.value = value;
        }

        /// <summary>
        /// Track 側の縦スクロールをラベル側へ同期
        /// </summary>
        /// <param name="value">同期後のスクロール量</param>
        private void OnTrackScrollChanged(float value) {
            _trackLabelListView.verticalScroller.value = value;
        }

        /// <summary>
        /// 横スクロール量に応じてルーラー位置を更新
        /// </summary>
        /// <param name="value">現在の横スクロール量</param>
        private void OnHorizontalScrollChanged(float value) {
            _trackListView.SetRulerOffset(value);
            if (_headerRulerView != null) {
                _headerRulerView.style.translate = new Translate(-value, 0.0f);
            }
        }

        /// <summary>
        /// ルーラー上のホイール操作で時間表示幅を変更
        /// </summary>
        /// <param name="evt">ホイールイベント</param>
        private void OnRulerWheel(WheelEvent evt) {
            _timelineService.SetTimeToSize(_timelineService.TimeToSize - evt.delta.y * 8.0f);
        }

        /// <summary>
        /// Thick ラベル表示文字列を取得
        /// </summary>
        /// <param name="thickIndex">表示対象のメモリ index</param>
        /// <returns>表示するラベル文字列</returns>
        private string OnGetThickLabel(int thickIndex) {
            if (thickIndex % 2 != 0) {
                return string.Empty;
            }

            var thickSeconds = SequenceEditorUtility.GetThickSeconds(_editorModel.CurrentTimeMode);
            var seconds = thickIndex * thickSeconds;
            return _editorModel.CurrentTimeMode switch {
                SequenceEditorModel.TimeMode.Seconds => $"{seconds:0.0}",
                SequenceEditorModel.TimeMode.Frames30 => $"{seconds * 30.0f:0}",
                SequenceEditorModel.TimeMode.Frames60 => $"{seconds * 60.0f:0}",
                _ => string.Empty,
            };
        }

        /// <summary>
        /// タイムライン表示設定変更時にルーラー表示を更新
        /// </summary>
        private void OnTimelineSettingsChanged() {
            RefreshRuler();
        }

        /// <summary>
        /// ルーラー表示設定をモデルへ同期
        /// </summary>
        private void RefreshRuler() {
            RefreshRulerView(_trackListView.RulerView);
            RefreshRulerView(_headerRulerView);
        }

        /// <summary>
        /// ルーラー幅を更新
        /// </summary>
        /// <param name="width">表示幅</param>
        private void SetRulerWidth(float width) {
            _trackListView.SetRulerWidth(width);
            if (_headerRulerView != null) {
                _headerRulerView.style.width = width;
            }
        }

        /// <summary>
        /// 指定ルーラーへ表示設定を反映
        /// </summary>
        /// <param name="rulerView">反映対象のルーラー</param>
        private void RefreshRulerView(RulerView rulerView) {
            if (rulerView == null) {
                return;
            }

            rulerView.MemoryCycles = SequenceEditorUtility.GetMemoryCycles(_editorModel.CurrentTimeMode);
            rulerView.TickCycle = SequenceEditorUtility.GetTickCycle(_editorModel.CurrentTimeMode);
            rulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel);
            rulerView.RefreshLabels();
        }
    }
}
