using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor.VisualElements
{
    /// <summary>
    /// RulerView
    /// </summary>
    public class RulerView : ImmediateModeElement
    {
        public new class UxmlFactory : UxmlFactory<RulerView, UxmlTraits> {}

        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int Cull = Shader.PropertyToID("_Cull");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        
        private static Material _material;
        private List<Label> _labelPool = new List<Label>();
        private List<Label> _usedLabels = new List<Label>();

        private float _memorySize = 10.0f;
        private int _thickCycle = 5;

        private static Material Material
        {
            get
            {
                if (_material != null)
                {
                    return _material;
                }

                _material = new Material(Shader.Find("Hidden/Internal-Colored"));
                _material.hideFlags = HideFlags.HideAndDontSave;
                _material.SetInt(SrcBlend, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _material.SetInt(DstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _material.SetInt(Cull, (int)UnityEngine.Rendering.CullMode.Off);
                _material.SetInt(ZWrite, 0);
                return _material;
            }
        }

        // Thickメモリに記述するラベルの取得
        public event Func<int, string> OnGetThickLabel;

        public Color LineColor { get; set; } = Color.gray;
        public Color ThickLineColor { get; set; } = Color.gray;
        public float LineWidth { get; set; } = 1;
        public float ThickLineWidth { get; set; } = 1;
        public float LineHeightRate { get; set; } = 0.25f;
        public float ThickLineHeightRate { get; set; } = 0.75f;
        public VisualElement MaskElement { get; set; }
        public float MemorySize
        {
            get => _memorySize;
            set
            {
                _memorySize = Mathf.Max(2, value);
                SetupLabels(layout);
            }
        }
        public int ThickCycle
        {
            get => _thickCycle;
            set
            {
                _thickCycle = Mathf.Max(1, value);
                SetupLabels(layout);
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public RulerView()
        {
            pickingMode = PickingMode.Ignore;
            style.position = new StyleEnum<Position>(Position.Absolute);

            this.StretchToParentSize();
            
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        /// <summary>
        /// ラベル表記のリフレッシュ
        /// </summary>
        public void RefreshLabels()
        {
            SetupLabels(layout);
        }

        /// <summary>
        /// 描画処理
        /// </summary>
        protected override void ImmediateRepaint()
        {
            Material.SetPass(0);

            var totalRect = layout;
            var clipRect = MaskElement != null ? this.WorldToLocal(MaskElement.worldBound) : totalRect;
            var lineOffset = MemorySize;

            clipRect.xMin = Mathf.Max(clipRect.xMin, totalRect.xMin);
            clipRect.xMax = Mathf.Min(clipRect.xMax, totalRect.xMax);
            
            for (var i = 0;; i++)
            {
                var thick = i % ThickCycle == 0;
                var rect = totalRect;
                rect.width = thick ? ThickLineWidth : LineWidth;
                rect.x = i * lineOffset;
                
                // 範囲外
                if (rect.xMax <= clipRect.xMin)
                {
                    continue;
                }
                if (rect.xMin >= clipRect.xMax)
                {
                    break;
                }
                
                // Clip
                rect.xMin = Mathf.Clamp(rect.xMin, clipRect.xMin, clipRect.xMax);
                rect.xMax = Mathf.Clamp(rect.xMax, clipRect.xMin, clipRect.xMax);
                
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

            for (var i = 0; i < _usedLabels.Count; i++)
            {
                var label = _usedLabels[i];
                // 範囲外のLabelは非表示
                var labelRect = label.layout;
                label.visible = labelRect.xMax > clipRect.xMin && labelRect.xMin < clipRect.xMax;
            }
        }

        /// <summary>
        /// CustomStyle変更時
        /// </summary>
        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            
        }

        /// <summary>
        /// 形状変化通知
        /// </summary>
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Labelを生成
            SetupLabels(evt.newRect);
        }

        /// <summary>
        /// メモリラベルの再構築
        /// </summary>
        private void SetupLabels(Rect totalRect)
        {
            var width = totalRect.width;
            
            ReturnLabels();
            var labelCount = (int)(width / MemorySize / ThickCycle) + 1;
            var labelUnitOffset = MemorySize * ThickCycle;
            for (var i = 0; i < labelCount; i++)
            {
                var label = GetOrCreateLabel();
                label.style.left = i * labelUnitOffset;
                label.text = OnGetThickLabel != null ? OnGetThickLabel(i) : "";
            }
        }

        /// <summary>
        /// Labelの取得(なければ生成)
        /// </summary>
        private Label GetOrCreateLabel()
        {
            // Poolがあればそれを取得
            if (_labelPool.Count > 0)
            {
                var lastIndex = _labelPool.Count - 1;
                var label = _labelPool[lastIndex];
                label.visible = true;
                _labelPool.RemoveAt(lastIndex);
                _usedLabels.Add(label);
                Add(label);
                return label;
            }
            // Poolになければ生成
            else
            {
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
        private void ReturnLabel(Label label)
        {
            if (!_usedLabels.Remove(label))
            {
                return;
            }

            label.visible = false;
            Remove(label);
            _labelPool.Add(label);
        }

        /// <summary>
        /// 全てのラベルをPoolに戻す
        /// </summary>
        private void ReturnLabels()
        {
            var labels = _usedLabels.ToArray();
            foreach (var label in labels)
            {
                ReturnLabel(label);
            }
        }
    }
}
