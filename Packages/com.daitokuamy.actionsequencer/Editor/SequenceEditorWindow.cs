using System;
using System.Collections.Generic;
using System.Linq;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ActionSequencer.Editor.VisualElements;
using UnityEditor.Callbacks;
using ObjectField = UnityEditor.UIElements.ObjectField;
using ToolbarMenu = UnityEditor.UIElements.ToolbarMenu;
using ToolbarToggle = UnityEditor.UIElements.ToolbarToggle;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Sequence編集用Window
    /// </summary>
    public class SequenceEditorWindow : EditorWindow {
        // .metaのUserDataに保存される情報
        [Serializable]
        private class UserData {
            public string Guid = "";
            public long LocalId = 0L;
            public float OffsetTime = 0.0f;
        }
        
        // リセット対策用SequenceClipキャッシュ
        [SerializeField]
        private SequenceClip _escapedClip;

        // Editor用のModel
        private SequenceEditorModel _editorModel;
        // TrackのPresenterリスト
        private SequenceClipPresenter _sequenceClipPresenter;
        // ルーラー表示用View
        private RulerView _rulerView;
        // Preview用View
        private AnimationClipView _previewView;
        // シークバー表示用View
        private VisualElement _seekbarView;
        // スクロールオフセット値
        private float _trackScrollOffsetX;
        // OnDisableで廃棄されるDisposablesのリスト
        private List<IDisposable> _disposables = new List<IDisposable>();
        // 再生主
        private GameObject _controllerProviderOwner;
        private ISequenceControllerProvider _controllerProvider;

        /// <summary>
        /// Windowを開く処理
        /// </summary>
        public static void Open(SequenceClip clip) {
            var window =
                CreateWindow<SequenceEditorWindow>(ObjectNames.NicifyVariableName(nameof(SequenceEditorWindow)));
            window.Setup(clip);
        }

        [MenuItem("Window/Sequence Tools/Sequence Editor Window")]
        private static void Open() {
            var window = GetWindow<SequenceEditorWindow>(ObjectNames.NicifyVariableName(nameof(SequenceEditorWindow)));
            window.Setup(null);
        }

        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line) {
            var asset = EditorUtility.InstanceIDToObject(instanceID);
            if (asset is SequenceClip clip) {
                Open(clip);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Setup(SequenceClip clip, bool force = false) {
            _escapedClip = clip;

            if (!force && _editorModel.ClipModel?.Target == clip) {
                return;
            }

            // SequenceClipのクリーンアップ
            CleanSequenceClipAsset(_escapedClip);

            // Clipの設定
            _editorModel.SetSequenceClip(clip);
            
            // Previewの読み込み
            (var animClip, var offsetTime) = LoadUserPreviewClip(_escapedClip); 
            _previewView.ChangeTarget(animClip);
            _previewView.ChangeOffsetTime(offsetTime);

            // Windowのタイトル変更
            titleContent =
                new GUIContent(clip != null ? clip.name : ObjectNames.NicifyVariableName(nameof(SequenceEditorWindow)));

            // Viewの整理
            var root = rootVisualElement;
            var trackLabelList = root.Q<VisualElement>("TrackLabelList");
            var trackList = root.Q<SequenceTrackListView>("TrackList");
            trackLabelList.Clear();
            trackList.Clear();

            // ObjectField初期化
            var objectField = root.Q<ObjectField>("TargetObjectField");
            objectField.value = clip;

            // Presenterの削除
            if (_sequenceClipPresenter != null) {
                _sequenceClipPresenter.Dispose();
                _sequenceClipPresenter = null;
            }

            // Presenterの生成
            if (_editorModel.ClipModel != null) {
                _sequenceClipPresenter =
                    new SequenceClipPresenter(_editorModel.ClipModel, trackLabelList, trackList, _editorModel);
            }
        }

        /// <summary>
        /// ViewDataKeyの設定
        /// </summary>
        private void SetViewDataKey(VisualElement element) {
            element.viewDataKey = $"{nameof(SequenceEditorWindow)}_{nameof(element.name)}";
        }

        /// <summary>
        /// ControllerProviderを更新
        /// </summary>
        private void UpdateControllerProvider() {
            if (!Application.isPlaying) {
                _controllerProviderOwner = null;
                _controllerProvider = null;
                return;
            }

            var activeObject = Selection.activeGameObject;
            if (activeObject != null && activeObject != _controllerProviderOwner) {
                var provider = activeObject.GetComponentInParent<ISequenceControllerProvider>();
                if (provider == null) {
                    return;
                }

                _controllerProviderOwner = activeObject;
                _controllerProvider = provider;
            }
        }

        /// <summary>
        /// アクティブ時処理
        /// </summary>
        private void OnEnable() {
            // EditorModel生成
            _editorModel = new SequenceEditorModel(rootVisualElement);

            // Xml, Style読み込み
            var root = rootVisualElement;
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.daitokuamy.actionsequencer/Editor/Layouts/sequence_editor_window.uxml");
            uxml.CloneTree(root);
            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Packages/com.daitokuamy.actionsequencer/Editor/Layouts/sequence_editor_window.uss");
            root.styleSheets.Add(styleSheet);

            // キー入力受け取れるように設定
            root.focusable = true;
            root.pickingMode = PickingMode.Position;

            // Scroll位置の同期
            var trackLabelList = root.Q<ScrollView>("TrackLabelList");
            var trackScrollView = root.Q<ScrollView>("TrackScrollView");
            var trackList = root.Q<SequenceTrackListView>("TrackList");
            trackLabelList.verticalScroller.valueChanged += x => { trackScrollView.verticalScroller.value = x; };
            trackScrollView.verticalScroller.valueChanged += x => { trackLabelList.verticalScroller.value = x; };

            // Timeline用Rulerの初期化
            var rulerArea = root.Q<VisualElement>("TrackRulerArea");
            _rulerView = root.Q<RulerView>("RulerView");
            trackList.RegisterCallback<GeometryChangedEvent>(evt => {
                _rulerView.style.width = trackList.layout.width;
            });
            trackScrollView.horizontalScroller.valueChanged += x => {
                var pos = _rulerView.transform.position;
                pos.x = -x;
                _rulerView.transform.position = pos;
                _trackScrollOffsetX = x;
            };
            rulerArea.RegisterCallback<WheelEvent>(evt => {
                // WheelによってTimeToSize変更
                _editorModel.TimeToSize.Value -= evt.delta.y * 8;
            });
            _rulerView.OnGetThickLabel += thickIndex => {
                if (thickIndex % 2 != 0) {
                    return "";
                }

                var thickSeconds = SequenceEditorUtility.GetThickSeconds(_editorModel.CurrentTimeMode.Value);
                var seconds = thickIndex * thickSeconds;
                switch (_editorModel.CurrentTimeMode.Value) {
                    case SequenceEditorModel.TimeMode.Seconds:
                        return $"{seconds:0.0}";
                    case SequenceEditorModel.TimeMode.Frames30:
                        return $"{seconds * 30:0}";
                    case SequenceEditorModel.TimeMode.Frames60:
                        return $"{seconds * 60:0}";
                }

                return "";
            };
            _rulerView.MaskElement = rulerArea;
            _disposables.Add(_editorModel.TimeToSize
                .Subscribe(_ => { _rulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel); }));
            _disposables.Add(_editorModel.CurrentTimeMode
                .Subscribe(timeMode => {
                    _rulerView.MemoryCycles = SequenceEditorUtility.GetMemoryCycles(timeMode);
                    _rulerView.TickCycle = SequenceEditorUtility.GetTickCycle(timeMode);
                    _rulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel);
                    _rulerView.RefreshLabels();
                }));

            // CreateMenu
            var createMenu = root.Q<ToolbarMenu>("CreateMenu");
            createMenu.SetEnabled(_editorModel.ClipModel != null);
            _disposables.Add(_editorModel.ChangeClipModelSubject
                .Subscribe(model => { createMenu.SetEnabled(model != null); }));

            // Trackの生成
            createMenu.menu.AppendAction("Track", _ => {
                if (_editorModel.ClipModel != null) {
                    _editorModel.ClipModel.AddTrack();
                }
            });

            // Play/Pause
            var playPauseToggle = root.Q<ToolbarToggle>("PlayPauseToggle");
            playPauseToggle.RegisterValueChangedCallback(evt => {
                // Play中
                if (evt.newValue) {
                    playPauseToggle.RemoveFromClassList("play_icon");
                    playPauseToggle.AddToClassList("pause_icon");
                }
                // Pause中
                else {
                    playPauseToggle.RemoveFromClassList("pause_icon");
                    playPauseToggle.AddToClassList("play_icon");
                }
            });
            // todo:取り合えず無効
            playPauseToggle.style.display = DisplayStyle.None;

            // ObjectField
            var objectField = root.Q<ObjectField>("TargetObjectField");
            objectField.RegisterValueChangedCallback(evt => { Setup(evt.newValue as SequenceClip); });

            // RulerMode
            var rulerMode = root.Q<DropdownField>("RulerMode");
            rulerMode.choices = new List<string>(Enum.GetNames(typeof(SequenceEditorModel.TimeMode)));
            rulerMode.RegisterValueChangedCallback(evt => {
                _editorModel.CurrentTimeMode.Value = (SequenceEditorModel.TimeMode)rulerMode.index;
            });
            _disposables.Add(_editorModel.CurrentTimeMode
                .Subscribe(timeMode => { rulerMode.value = timeMode.ToString(); }));

            // TimeFitToggle
            var timeFitToggle = root.Q<ToolbarToggle>("TimeFitToggle");
            timeFitToggle.RegisterValueChangedCallback(evt => { _editorModel.TimeFit.Value = evt.newValue; });
            _disposables.Add(_editorModel.TimeFit
                .Subscribe(timeFit => { timeFitToggle.value = timeFit; }));

            // Refresh
            var refreshButton = root.Q<Button>("RefreshButton");
            refreshButton.clicked += () => { Setup(_escapedClip, true); };

            // InspectorView
            var inspector = root.Q<InspectorView>("Inspector");
            _disposables.Add(_editorModel.CurrentTimeMode
                .Subscribe(timeMode => inspector.TimeMode = timeMode));
            _disposables.Add(_editorModel.ChangedSelectedTargetsSubject
                .Subscribe(inspector.SetTarget));

            // PreviewView
            _previewView = root.Q<AnimationClipView>();
            _previewView.OnChangedClipEvent += OnChangedPreviewClip;
            _previewView.OnChangedOffsetTimeEvent += OnChangedPreviewOffsetTime;

            // Seekbar
            _seekbarView = root.Q<VisualElement>("TrackSeekbar");

            // Sizeのフィット
            var trackArea = root.Q<VisualElement>("TrackArea");
            root.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.F) {
                    if (_editorModel.SetBestTimeToSize(trackArea.layout.width - 20.0f)) {
                        trackScrollView.horizontalScroller.value = 0.0f;
                    }
                }
            });

            // ViewDataKey
            var splitViews = root.Query<SplitView>().ToList();
            foreach (var element in splitViews) {
                SetViewDataKey(element);
            }

            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            // 初期化
            Setup(_escapedClip, true);
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        private void OnDisable() {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            
            _previewView.OnChangedClipEvent -= OnChangedPreviewClip;
            _previewView.OnChangedOffsetTimeEvent -= OnChangedPreviewOffsetTime;

            foreach (var disposable in _disposables) {
                disposable.Dispose();
            }

            _disposables.Clear();
            _editorModel?.Dispose();
            _editorModel = null;
        }

        /// <summary>
        /// GUI描画処理
        /// </summary>
        private void OnGUI() {
            if (_editorModel?.ClipModel == null) {
                return;
            }

            UpdateControllerProvider();

            float GetSeekTime(SequenceClip clip) {
                var result = -1.0f;

                // SequenceControllerがあれば優先させる
                var sequenceController = _controllerProvider?.SequenceController;
                result = sequenceController?.GetSequenceTime(clip) ?? -1.0f;
                if (result >= 0.0f) {
                    return result;
                }

                // 無ければPreviewに設定された物を採用
                result = _previewView.IsValid ? _previewView.CurrentTime : -1.0f;
                return result;
            }

            // Seekbarの調整
            if (_seekbarView != null) {
                var time = GetSeekTime(_editorModel.ClipModel.Target as SequenceClip);
                _seekbarView.visible = time >= 0.0f;
                if (_seekbarView.visible) {
                    // 時間をSeekbarに反映
                    var left = time * _editorModel.TimeToSize.Value - _trackScrollOffsetX;
                    _seekbarView.style.marginLeft = left;

                    // ちょっと強引だけどleftが負の数なら非表示
                    if (left < 0.0f) {
                        _seekbarView.visible = false;
                    }
                }
            }
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        private void Update() {
            if (_controllerProvider != null) {
                Repaint();
            }
        }

        /// <summary>
        /// Undo/Redo通知
        /// </summary>
        private void OnUndoRedoPerformed() {
            // 本来はプロパティの変更を監視したいが、監視ができないので強引だが開き直す
            Setup(_escapedClip, true);
        }

        /// <summary>
        /// SequenceClipの中身をクリーンする
        /// </summary>
        private void CleanSequenceClipAsset(SequenceClip clip) {
            if (clip == null) {
                return;
            }

            var cleanAsset = false;
            var serializedObj = new SerializedObject(clip);
            serializedObj.Update();
            var tracksProp = serializedObj.FindProperty("tracks");
            for (var i = 0; i < tracksProp.arraySize; i++) {
                var trackProp = tracksProp.GetArrayElementAtIndex(i);
                if (trackProp.objectReferenceValue == null) {
                    tracksProp.DeleteArrayElementAtIndex(i);
                    i--;
                    cleanAsset = true;
                    continue;
                }

                var trackObj = new SerializedObject(trackProp.objectReferenceValue);
                trackObj.Update();
                var sequenceEventsProp = trackObj.FindProperty("sequenceEvents");
                for (var j = 0; j < sequenceEventsProp.arraySize; j++) {
                    var sequenceEventProp = sequenceEventsProp.GetArrayElementAtIndex(j);
                    if (sequenceEventProp.objectReferenceValue == null) {
                        sequenceEventsProp.DeleteArrayElementAtIndex(j);
                        j--;
                        cleanAsset = true;
                    }
                }

                trackObj.ApplyModifiedPropertiesWithoutUndo();
            }

            serializedObj.ApplyModifiedPropertiesWithoutUndo();

            // 破損ファイルがあった場合、SubAssetsをクリーンアップする
            if (cleanAsset) {
                RemoveMissingSubAssets(clip);
            }
        }

        /// <summary>
        /// PreviewClip変更通知
        /// </summary>
        private void OnChangedPreviewClip(AnimationClip clip) {
            if (_editorModel?.ClipModel?.Target is SequenceClip sequenceClip) {
                SaveUserPreviewClip(sequenceClip, clip, _previewView.OffsetTime);
            }
        }

        /// <summary>
        /// PreviewOffsetTime変更通知
        /// </summary>
        private void OnChangedPreviewOffsetTime(float offsetTime) {
            if (_editorModel?.ClipModel?.Target is SequenceClip sequenceClip) {
                SaveUserPreviewClip(sequenceClip, _previewView.CurrentClip, offsetTime);
            }
        }

        /// <summary>
        /// Preview用のClipをユーザーデータとして保存
        /// </summary>
        private void SaveUserPreviewClip(SequenceClip sequenceClip, AnimationClip animationClip, float offsetTime) {
            if (sequenceClip == null) {
                return;
            }

            var path = AssetDatabase.GetAssetPath(sequenceClip);
            var importer = AssetImporter.GetAtPath(path);

            var userData = new UserData();
            if (animationClip != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(animationClip.GetInstanceID(), out var guid,
                    out long localId)) {
                userData.Guid = guid;
                userData.LocalId = localId;
            }
            userData.OffsetTime = offsetTime;
            importer.userData = JsonUtility.ToJson(userData, true);
            
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Preview用のClipを読み込み
        /// </summary>
        private (AnimationClip, float) LoadUserPreviewClip(SequenceClip sequenceClip) {
            if (sequenceClip == null) {
                return (null, 0.0f);
            }

            var path = AssetDatabase.GetAssetPath(sequenceClip);
            var importer = AssetImporter.GetAtPath(path);
            if (string.IsNullOrEmpty(importer.userData)) {
                return (null, 0.0f);
            }

            var userData = new UserData();
            try {
                JsonUtility.FromJsonOverwrite(importer.userData, userData);
            }
            catch {
                SaveUserPreviewClip(sequenceClip, null, 0.0f);
                return (null, 0.0f);
            }
            
            if (string.IsNullOrEmpty(userData.Guid)) {
                return (null, 0.0f);
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(userData.Guid));
            var targetAnimationClip = assets.FirstOrDefault(x => {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(x, out var _, out long id)) {
                    return id == userData.LocalId;
                }

                return false;
            }) as AnimationClip;
            return (targetAnimationClip, userData.OffsetTime);
        }

        /// <summary>
        /// MissingしたSubAssetsを削除する
        /// </summary>
        private void RemoveMissingSubAssets(UnityEngine.Object targetAsset) {
            // 退避用のInstanceの生成
            var targetName = targetAsset.name;
            var newInstance = CreateInstance(targetAsset.GetType());
            EditorUtility.CopySerialized(targetAsset, newInstance);

            var oldPath = AssetDatabase.GetAssetPath(targetAsset);
            var newPath = oldPath.Replace(".asset", "CLONE.asset");
            AssetDatabase.CreateAsset(newInstance, newPath);
            AssetDatabase.ImportAsset(newPath);

            // SubAssetsをクローンした物の子に退避
            var assets = AssetDatabase.LoadAllAssetsAtPath(oldPath);
            for (var i = 0; i < assets.Length; i++) {
                // 破損した物
                if (assets[i] == null) {
                    continue;
                }

                // MainAsset
                if (assets[i] == targetAsset) {
                    continue;
                }

                // サブアセットを移動させる
                AssetDatabase.RemoveObjectFromAsset(assets[i]);
                AssetDatabase.AddObjectToAsset(assets[i], newInstance);
            }

            // 名前を直す
            newInstance.name = targetName;

            EditorUtility.SetDirty(newInstance);
            AssetDatabase.SaveAssets();

            AssetDatabase.ImportAsset(oldPath);
            AssetDatabase.ImportAsset(newPath);

            // metaを残しつつ、新しいインスタンスに差し替える
            var directoryName = System.IO.Path.GetDirectoryName(Application.dataPath) ?? "";
            var globalOldPath = System.IO.Path.Combine(directoryName, oldPath);
            var globalNewPath = System.IO.Path.Combine(directoryName, newPath);

            System.IO.File.Delete(globalOldPath);
            System.IO.File.Delete(globalNewPath + ".meta");
            System.IO.File.Move(globalNewPath, globalOldPath);

            AssetDatabase.Refresh();
        }
    }
}