using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Editor 全体の編集用キャッシュ
    /// </summary>
    internal sealed class SequenceEditorModel {
        /// <summary>
        /// 時間表示モード
        /// </summary>
        public enum TimeMode {
            /// <summary>秒表示</summary>
            Seconds,
            /// <summary>30fps フレーム表示</summary>
            Frames30,
            /// <summary>60fps フレーム表示</summary>
            Frames60,
        }

        private readonly List<Object> _selectedTargets = new();

        private Object _lastSelectedTarget;

        /// <summary>ルートの SequenceClip</summary>
        public SequenceClip RootClip { get; private set; }
        /// <summary>現在選択中の includeClip index</summary>
        public int IncludeClipIndex { get; private set; } = -1;
        /// <summary>現在編集中の SequenceClip</summary>
        public SequenceClip CurrentClip { get; private set; }
        /// <summary>時間 1 秒あたりの表示幅</summary>
        public float TimeToSize { get; private set; } = 200.0f;
        /// <summary>現在の時間表示モード</summary>
        public TimeMode CurrentTimeMode { get; private set; } = TimeMode.Seconds;
        /// <summary>時間スナップを有効にするか</summary>
        public bool TimeFit { get; private set; } = true;
        /// <summary>現在編集中の ClipModel</summary>
        public SequenceClipModel ClipModel { get; private set; }
        /// <summary>現在の選択対象一覧</summary>
        public IReadOnlyList<Object> SelectedTargets => _selectedTargets;

        /// <summary>
        /// 編集対象のクリップ情報を更新
        /// </summary>
        /// <param name="rootClip">ルートの SequenceClip</param>
        /// <param name="includeClipIndex">選択する includeClip index</param>
        /// <param name="currentClip">現在編集中の SequenceClip</param>
        public void SetClipTargets(SequenceClip rootClip, int includeClipIndex, SequenceClip currentClip) {
            RootClip = rootClip;
            IncludeClipIndex = includeClipIndex;
            CurrentClip = currentClip;
        }

        /// <summary>
        /// ClipModel を更新
        /// </summary>
        /// <param name="clipModel">更新後の ClipModel</param>
        /// <returns>値が変化した場合は true</returns>
        public bool SetClipModel(SequenceClipModel clipModel) {
            if (ReferenceEquals(ClipModel, clipModel)) {
                return false;
            }

            ClipModel = clipModel;
            return true;
        }

        /// <summary>
        /// 時間表示幅を更新
        /// </summary>
        /// <param name="value">更新後の幅</param>
        /// <returns>値が変化した場合は true</returns>
        public bool SetTimeToSize(float value) {
            var nextValue = Mathf.Max(40.0f, value);
            if (Mathf.Approximately(TimeToSize, nextValue)) {
                return false;
            }

            TimeToSize = nextValue;
            return true;
        }

        /// <summary>
        /// 時間表示モードを更新
        /// </summary>
        /// <param name="value">更新後の時間表示モード</param>
        /// <returns>値が変化した場合は true</returns>
        public bool SetTimeMode(TimeMode value) {
            if (CurrentTimeMode == value) {
                return false;
            }

            CurrentTimeMode = value;
            return true;
        }

        /// <summary>
        /// 時間スナップ設定を更新
        /// </summary>
        /// <param name="value">更新後の設定</param>
        /// <returns>値が変化した場合は true</returns>
        public bool SetTimeFit(bool value) {
            if (TimeFit == value) {
                return false;
            }

            TimeFit = value;
            return true;
        }

        /// <summary>
        /// 単一選択対象を設定
        /// </summary>
        /// <param name="target">選択対象</param>
        /// <returns>選択状態が変化した場合は true</returns>
        public bool SetSelectedTarget(Object target) {
            if (target == null) {
                return ClearSelection();
            }

            if (_selectedTargets.Count == 1 && _selectedTargets[0] == target && _lastSelectedTarget == target) {
                return false;
            }

            _selectedTargets.Clear();
            _lastSelectedTarget = null;
            return AddSelectedTarget(target);
        }

        /// <summary>
        /// 選択対象を追加
        /// </summary>
        /// <param name="target">追加対象</param>
        /// <returns>選択状態が変化した場合は true</returns>
        public bool AddSelectedTarget(Object target) {
            if (target == null || _selectedTargets.Contains(target)) {
                return false;
            }

            _selectedTargets.Add(target);
            _lastSelectedTarget = target;
            SortSelectedTargets();
            return true;
        }

        /// <summary>
        /// 範囲選択対象を追加
        /// </summary>
        /// <param name="target">範囲終端となる対象</param>
        /// <returns>選択状態が変化した場合は true</returns>
        public bool AddRangeSelectedTarget(Object target) {
            if (target == null || _selectedTargets.Contains(target) || ClipModel == null) {
                return false;
            }

            if (_lastSelectedTarget == null) {
                return SetSelectedTarget(target);
            }

            var startIndex = ClipModel.GetTargetOrder(_lastSelectedTarget);
            var endIndex = ClipModel.GetTargetOrder(target);
            if (startIndex < 0 || endIndex < 0) {
                return false;
            }

            if (startIndex > endIndex) {
                (startIndex, endIndex) = (endIndex, startIndex);
            }

            var nextSelection = new List<Object>();
            for (var i = startIndex; i <= endIndex; i++) {
                var current = ClipModel.GetTargetByOrder(i);
                if (current != null) {
                    nextSelection.Add(current);
                }
            }

            if (_selectedTargets.SequenceEqual(nextSelection)) {
                return false;
            }

            _selectedTargets.Clear();
            _selectedTargets.AddRange(nextSelection);
            _lastSelectedTarget = target;
            SortSelectedTargets();
            return true;
        }

        /// <summary>
        /// 選択対象を解除
        /// </summary>
        /// <param name="target">解除対象</param>
        /// <returns>選択状態が変化した場合は true</returns>
        public bool RemoveSelectedTarget(Object target) {
            if (!_selectedTargets.Remove(target)) {
                return false;
            }

            if (_lastSelectedTarget == target) {
                _lastSelectedTarget = null;
            }

            return true;
        }

        /// <summary>
        /// 選択状態を全解除
        /// </summary>
        /// <returns>選択状態が変化した場合は true</returns>
        public bool ClearSelection() {
            if (_selectedTargets.Count == 0 && _lastSelectedTarget == null) {
                return false;
            }

            _selectedTargets.Clear();
            _lastSelectedTarget = null;
            return true;
        }

        /// <summary>
        /// 選択状態を復元
        /// </summary>
        /// <param name="targets">復元対象一覧</param>
        /// <returns>選択状態が変化した場合は true</returns>
        public bool RestoreSelection(IEnumerable<Object> targets) {
            var nextSelection = new List<Object>();
            Object lastSelectedTarget = null;

            foreach (var target in targets.Distinct()) {
                if (ClipModel?.ContainsTarget(target) == true) {
                    nextSelection.Add(target);
                    lastSelectedTarget = target;
                }
            }

            if (_selectedTargets.SequenceEqual(nextSelection) && _lastSelectedTarget == lastSelectedTarget) {
                return false;
            }

            _selectedTargets.Clear();
            _selectedTargets.AddRange(nextSelection);
            _lastSelectedTarget = lastSelectedTarget;
            SortSelectedTargets();
            return true;
        }

        /// <summary>
        /// 現在設定に基づく吸着後の時間を取得
        /// </summary>
        /// <param name="time">変換前の時間</param>
        /// <returns>吸着後の時間</returns>
        public float GetAbsorptionTime(float time) {
            if (!TimeFit) {
                return time;
            }

            return CurrentTimeMode switch {
                TimeMode.Seconds => Mathf.RoundToInt(time * 40.0f) / 40.0f,
                TimeMode.Frames30 => Mathf.RoundToInt(time * 30.0f) / 30.0f,
                TimeMode.Frames60 => Mathf.RoundToInt(time * 60.0f) / 60.0f,
                _ => time,
            };
        }

        /// <summary>
        /// 現在の内容に最適な時間表示幅を設定
        /// </summary>
        /// <param name="contentWidth">利用可能な表示幅</param>
        /// <returns>値が変化した場合は true</returns>
        public bool SetBestTimeToSize(float contentWidth) {
            if (ClipModel == null || contentWidth <= 0.0f) {
                return false;
            }

            var eventModels = ClipModel.TrackModels
                .SelectMany(x => x.EventModels)
                .ToArray();
            if (eventModels.Length == 0) {
                return false;
            }

            var duration = eventModels.Max(x => x.GetEndTime());
            if (duration <= 0.0f) {
                return false;
            }

            return SetTimeToSize(contentWidth / duration);
        }

        /// <summary>
        /// 選択対象をクリップ内の並び順で整列
        /// </summary>
        private void SortSelectedTargets() {
            if (ClipModel == null) {
                return;
            }

            _selectedTargets.Sort((a, b) => ClipModel.GetTargetOrder(a).CompareTo(ClipModel.GetTargetOrder(b)));
        }
    }
}