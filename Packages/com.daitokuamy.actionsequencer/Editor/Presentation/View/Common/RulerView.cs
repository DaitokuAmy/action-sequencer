using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor.VisualElements {
    /// <summary>
    /// RulerView
    /// </summary>
    [UxmlElement]
    public sealed partial class RulerView : ImmediateModeElement {
        private static readonly int s_srcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int s_dstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int s_cull = Shader.PropertyToID("_Cull");
        private static readonly int s_zWrite = Shader.PropertyToID("_ZWrite");

        private static Material s_material;
        private readonly List<Label> _labelPool = new();
        private readonly List<Label> _usedLabels = new();

        private float _memorySize = 10.0f;
        private int[] _memoryCycles = new[] { 5 };
        private int _tickCycle = 5;
        private bool _showLabels = true;

        private static Material Material {
            get {
                if (s_material != null) {
                    return s_material;
                }

                s_material = new Material(Shader.Find("Hidden/Internal-Colored"));
                s_material.hideFlags = HideFlags.HideAndDontSave;
                s_material.SetInt(s_srcBlend, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                s_material.SetInt(s_dstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                s_material.SetInt(s_cull, (int)UnityEngine.Rendering.CullMode.Off);
                s_material.SetInt(s_zWrite, 0);
                return s_material;
            }
        }

        /// <summary>Thick メモリに表示するラベルを取得する</summary>
        public event Func<int, string> OnGetThickLabel;

        /// <summary>通常ラインの色</summary>
        public Color LineColor { get; set; } = Color.gray;
        /// <summary>Thick ラインの色</summary>
        public Color ThickLineColor { get; set; } = Color.gray;
        /// <summary>通常ラインの幅</summary>
        public float LineWidth { get; set; } = 1;
        /// <summary>Thick ラインの幅</summary>
        public float ThickLineWidth { get; set; } = 1;
        /// <summary>通常ラインの高さ比率</summary>
        public float LineHeightRate { get; set; } = 0.25f;
        /// <summary>Thick ラインの高さ比率</summary>
        public float ThickLineHeightRate { get; set; } = 0.75f;
        /// <summary>描画範囲のマスク要素</summary>
        public VisualElement MaskElement { get; set; }
        /// <summary>ラベルを表示する場合は true</summary>
        public bool ShowLabels {
            get => _showLabels;
            set {
                if (_showLabels == value) {
                    return;
                }

                _showLabels = value;
                SetupLabels(layout);
            }
        }

        /// <summary>メモリの描画間隔</summary>
        public float MemorySize {
            get => _memorySize;
            set {
                _memorySize = Mathf.Max(0, value);
                SetupLabels(layout);
            }
        }
        /// <summary>メモリを間引く際に使用する表示サイクル</summary>
        public int[] MemoryCycles {
            get => _memoryCycles;
            set {
                _memoryCycles = value.Select(x => Mathf.Max(1, x)).ToArray();
                SetupLabels(layout);
            }
        }
        /// <summary>Thick メモリの表示サイクル</summary>
        public int TickCycle {
            get => _tickCycle;
            set {
                _tickCycle = Mathf.Max(1, value);
                SetupLabels(layout);
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public RulerView() {
            pickingMode = PickingMode.Ignore;
            style.position = new StyleEnum<Position>(Position.Absolute);

            this.StretchToParentSize();

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        /// <summary>
        /// ラベル表記のリフレッシュ
        /// </summary>
        public void RefreshLabels() {
            SetupLabels(layout);
        }

        /// <summary>
        /// 描画処理
        /// </summary>
        protected override void ImmediateRepaint() {
            Material.SetPass(0);

            var totalRect = layout;
            var clipRect = MaskElement != null ? this.WorldToLocal(MaskElement.worldBound) : totalRect;
            var lineOffset = MemorySize;

            clipRect.xMin = Mathf.Max(clipRect.xMin, totalRect.xMin);
            clipRect.xMax = Mathf.Min(clipRect.xMax, totalRect.xMax);
            clipRect.yMin = Mathf.Max(clipRect.yMin, totalRect.yMin);
            clipRect.yMax = Mathf.Max(clipRect.yMax, totalRect.yMax);

            // MemoryCycleを適切に変更
            var minLineOffset = LineWidth * 5;
            var memoryCycle = 1;
            if (lineOffset < minLineOffset) {
                memoryCycle = MemoryCycles[0];
                for (var i = MemoryCycles.Length - 1; i > 0; i--) {
                    if (lineOffset * MemoryCycles[i] >= minLineOffset) {
                        memoryCycle = MemoryCycles[i];
                        break;
                    }
                }
            }

            int GetMemoryLevel(int index) {
                if (index % TickCycle == 0) {
                    return 0;
                }

                if (index % memoryCycle == 0) {
                    return 1;
                }

                return -1;
            }

            for (var i = 0; ; i++) {
                // 間引き
                var memoryLevel = GetMemoryLevel(i);
                if (memoryLevel < 0) {
                    continue;
                }

                var thick = memoryLevel == 0;
                var rect = totalRect;
                rect.width = thick ? ThickLineWidth : LineWidth;
                rect.x = i * lineOffset;

                // 範囲外
                if (rect.xMax <= clipRect.xMin) {
                    continue;
                }

                if (rect.xMin >= clipRect.xMax) {
                    break;
                }

                // Clip
                rect.xMin = Mathf.Clamp(rect.xMin, clipRect.xMin, clipRect.xMax);
                rect.xMax = Mathf.Clamp(rect.xMax, clipRect.xMin, clipRect.xMax);
                rect.yMin = Mathf.Clamp(rect.yMin, clipRect.yMin, clipRect.yMax);
                rect.yMax = Mathf.Clamp(rect.yMax, clipRect.yMin, clipRect.yMax);

                // ThickStyle
                var color = thick ? ThickLineColor : LineColor;
                rect.yMin += rect.height * (1 - (thick ? ThickLineHeightRate : LineHeightRate));

                GL.Begin(GL.QUADS);
                GL.Color(color);
                GL.Vertex(new Vector3(rect.x, rect.y));
                GL.Vertex(new Vector3(rect.xMax, rect.y));
                GL.Vertex(new Vector3(rect.xMax, rect.yMax));
                GL.Vertex(new Vector3(rect.x, rect.yMax));
                GL.End();
            }

            for (var i = 0; i < _usedLabels.Count; i++) {
                var label = _usedLabels[i];
                // 範囲外のLabelは非表示
                var labelRect = label.layout;
                label.visible = labelRect.xMax > clipRect.xMin && labelRect.xMin < clipRect.xMax;
            }
        }

        /// <summary>
        /// CustomStyle変更時
        /// </summary>
        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt) {
        }

        /// <summary>
        /// 形状変化通知
        /// </summary>
        private void OnGeometryChanged(GeometryChangedEvent evt) {
            // Labelを生成
            SetupLabels(evt.newRect);
        }

        /// <summary>
        /// メモリラベルの再構築
        /// </summary>
        private void SetupLabels(Rect totalRect) {
            if (!ShowLabels) {
                ReturnLabels();
                return;
            }

            var width = totalRect.width;

            ReturnLabels();
            var thickCycle = TickCycle;
            var labelCount = (int)(width / MemorySize / thickCycle) + 1;
            var labelUnitOffset = MemorySize * thickCycle;
            for (var i = 0; i < labelCount; i++) {
                var label = GetOrCreateLabel();
                label.style.left = i * labelUnitOffset;
                label.text = OnGetThickLabel != null ? OnGetThickLabel(i) : "";
            }
        }

        /// <summary>
        /// Labelの取得(なければ生成)
        /// </summary>
        private Label GetOrCreateLabel() {
            // Poolがあればそれを取得
            if (_labelPool.Count > 0) {
                var lastIndex = _labelPool.Count - 1;
                var label = _labelPool[lastIndex];
                label.visible = true;
                _labelPool.RemoveAt(lastIndex);
                _usedLabels.Add(label);
                Add(label);
                return label;
            }
            // Poolになければ生成
            else {
                var label = new Label();
                label.style.position = Position.Absolute;
                label.AddToClassList("ruler_label");
                Add(label);
                _usedLabels.Add(label);
                return label;
            }
        }

        /// <summary>
        /// ラベルをPoolに戻す
        /// </summary>
        private void ReturnLabel(Label label) {
            if (!_usedLabels.Remove(label)) {
                return;
            }

            label.visible = false;
            Remove(label);
            _labelPool.Add(label);
        }

        /// <summary>
        /// 全てのラベルをPoolに戻す
        /// </summary>
        private void ReturnLabels() {
            var labels = _usedLabels.ToArray();
            foreach (var label in labels) {
                ReturnLabel(label);
            }
        }
    }
}
