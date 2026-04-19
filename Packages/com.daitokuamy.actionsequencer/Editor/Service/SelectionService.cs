using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor {
    /// <summary>
    /// 選択状態を扱うサービス
    /// </summary>
    internal sealed class SelectionService {
        private readonly SequenceEditorModel _model;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="model">編集中のモデル</param>
        public SelectionService(SequenceEditorModel model) {
            _model = model;
        }

        /// <summary>選択状態が変化したときに発火する</summary>
        public event Action SelectionChanged;

        /// <summary>現在の選択対象一覧</summary>
        public IReadOnlyList<Object> SelectedTargets => _model.SelectedTargets;

        /// <summary>
        /// 単一選択対象を設定
        /// </summary>
        /// <param name="target">選択対象</param>
        public void SetSelectedTarget(Object target) {
            if (_model.SetSelectedTarget(target)) {
                SelectionChanged?.Invoke();
            }
        }

        /// <summary>
        /// 選択対象を追加
        /// </summary>
        /// <param name="target">追加対象</param>
        public void AddSelectedTarget(Object target) {
            if (_model.AddSelectedTarget(target)) {
                SelectionChanged?.Invoke();
            }
        }

        /// <summary>
        /// 範囲選択対象を追加
        /// </summary>
        /// <param name="target">範囲終端となる対象</param>
        public void AddRangeSelectedTarget(Object target) {
            if (_model.AddRangeSelectedTarget(target)) {
                SelectionChanged?.Invoke();
            }
        }

        /// <summary>
        /// 選択対象を解除
        /// </summary>
        /// <param name="target">解除対象</param>
        public void RemoveSelectedTarget(Object target) {
            if (_model.RemoveSelectedTarget(target)) {
                SelectionChanged?.Invoke();
            }
        }

        /// <summary>
        /// 選択状態を全解除
        /// </summary>
        public void ClearSelection() {
            if (_model.ClearSelection()) {
                SelectionChanged?.Invoke();
            }
        }

        /// <summary>
        /// 選択状態を復元
        /// </summary>
        /// <param name="targets">復元対象一覧</param>
        public void RestoreSelection(IEnumerable<Object> targets) {
            if (_model.RestoreSelection(targets)) {
                SelectionChanged?.Invoke();
            }
        }
    }
}