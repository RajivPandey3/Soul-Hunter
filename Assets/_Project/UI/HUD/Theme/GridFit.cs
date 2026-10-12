using UnityEngine;
using UnityEngine.UI;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>Sizes a grid's cells from its viewport: all Count cards fit in the grid's columns, 0.72 as tall as wide at most.</summary>
    public sealed class GridFit : MonoBehaviour
    {
        public GridLayoutGroup Grid;
        public RectTransform Viewport;
        public int Count;
        [Tooltip("Cells are at least this share of their width tall (0 = whatever height fits the viewport).")]
        public float MinHeightRatio = 0.45f;
        [Tooltip("Responsive (owner, 2026-10-10): the narrowest a cell may be, in canvas units. When set, the column count " +
                 "follows the viewport's width (fewer columns in portrait), between 1 and MaxColumns. 0 keeps the grid's own count.")]
        public float MinCellWidth;
        public int MaxColumns = 8;
        private Vector2 _lastSize;

        private void LateUpdate()
        {
            if (Grid == null || Viewport == null || Count <= 0) return;
            Vector2 size = Viewport.rect.size;
            if (size == _lastSize || size.x <= 0f || size.y <= 0f) return;
            _lastSize = size;
            if (MinCellWidth > 0f)
            {
                Grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                Grid.constraintCount = UiScreen.ColumnsFor(size.x, Mathf.Max(MinCellWidth, UiScreen.MinTouchUnits), Grid.spacing.x, MaxColumns);
            }
            int columns = Mathf.Max(1, Grid.constraintCount);
            int rows = Mathf.CeilToInt(Count / (float)columns);
            float width = (size.x - Grid.spacing.x * (columns - 1)) / columns;
            float height = Mathf.Min(width * 0.72f, (size.y - Grid.spacing.y * (rows - 1)) / rows);
            // A cell is never under the touch target (UiScreen.MinTouchUnits is 0 without touch).
            Grid.cellSize = new Vector2(Mathf.Floor(width), Mathf.Floor(Mathf.Max(height, width * MinHeightRatio, UiScreen.MinTouchUnits)));
        }
    }
}
