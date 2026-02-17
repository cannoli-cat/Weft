using UnityEngine;
using UnityEngine.UIElements;

namespace Weft.Demos.RobotDemo.Scripts {
    /// <summary>
    /// A VisualElement that renders the RobotGrid state.
    /// Polls the grid each frame to reflect robot movement.
    /// </summary>
    public class RobotGridView : VisualElement {
        private RobotGrid grid;
        private IVisualElementScheduledItem refreshHandle;

        public void Bind(RobotGrid g) {
            grid = g;
            MarkDirtyRepaint();
        }

        public RobotGridView() {
            generateVisualContent += OnGenerateVisualContent;
            style.flexGrow = 1;
            style.minWidth = 200;
            style.minHeight = 200;

            RegisterCallback<AttachToPanelEvent>(_ => {
                refreshHandle = schedule.Execute(MarkDirtyRepaint).Every(50);
            });

            RegisterCallback<DetachFromPanelEvent>(_ => { refreshHandle?.Pause(); });
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx) {
            if (grid == null) return;

            var painter = ctx.painter2D;
            var w = grid.width;
            var h = grid.height;

            var areaW = resolvedStyle.width;
            var areaH = resolvedStyle.height;
            var cell = Mathf.Min(areaW / w, areaH / h);
            if (cell < 4) cell = 4;

            var offsetX = (areaW - w * cell) * 0.5f;
            var offsetY = (areaH - h * cell) * 0.5f;

            // draw grid background
            painter.fillColor = new Color(0.08f, 0.08f, 0.08f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(offsetX, offsetY));
            painter.LineTo(new Vector2(offsetX + w * cell, offsetY));
            painter.LineTo(new Vector2(offsetX + w * cell, offsetY + h * cell));
            painter.LineTo(new Vector2(offsetX, offsetY + h * cell));
            painter.ClosePath();
            painter.Fill();

            // draw grid lines
            painter.strokeColor = new Color(0.2f, 0.2f, 0.2f);
            painter.lineWidth = 1;
            for (var x = 0; x <= w; x++) {
                painter.BeginPath();
                painter.MoveTo(new Vector2(offsetX + x * cell, offsetY));
                painter.LineTo(new Vector2(offsetX + x * cell, offsetY + h * cell));
                painter.Stroke();
            }

            for (var y = 0; y <= h; y++) {
                painter.BeginPath();
                painter.MoveTo(new Vector2(offsetX, offsetY + y * cell));
                painter.LineTo(new Vector2(offsetX + w * cell, offsetY + y * cell));
                painter.Stroke();
            }

            // draw cells (flip y so 0,0 is bottom-left like math coords)
            for (var gx = 0; gx < w; gx++) {
                for (var gy = 0; gy < h; gy++) {
                    var pos = new Vector2Int(gx, gy);
                    var px = offsetX + gx * cell;
                    var py = offsetY + (h - 1 - gy) * cell; // flip y

                    if (grid.IsWall(pos)) {
                        FillCell(painter, px, py, cell, new Color(0.3f, 0.3f, 0.35f));
                    }
                    else if (grid.IsGem(pos)) {
                        DrawGem(painter, px, py, cell);
                    }
                }
            }

            // draw robot
            var rx = offsetX + grid.RobotPos.x * cell;
            var ry = offsetY + (h - 1 - grid.RobotPos.y) * cell;
            DrawRobot(painter, rx, ry, cell, grid.RobotDir);
        }

        private static void FillCell(Painter2D p, float x, float y, float size, Color color) {
            var inset = 1f;
            p.fillColor = color;
            p.BeginPath();
            p.MoveTo(new Vector2(x + inset, y + inset));
            p.LineTo(new Vector2(x + size - inset, y + inset));
            p.LineTo(new Vector2(x + size - inset, y + size - inset));
            p.LineTo(new Vector2(x + inset, y + size - inset));
            p.ClosePath();
            p.Fill();
        }

        private static void DrawGem(Painter2D p, float x, float y, float size) {
            var cx = x + size * 0.5f;
            var cy = y + size * 0.5f;
            var r = size * 0.2f;

            p.fillColor = new Color(0.2f, 0.9f, 0.4f);
            p.BeginPath();
            p.MoveTo(new Vector2(cx, cy - r)); // top
            p.LineTo(new Vector2(cx + r, cy)); // right
            p.LineTo(new Vector2(cx, cy + r)); // bottom
            p.LineTo(new Vector2(cx - r, cy)); // left
            p.ClosePath();
            p.Fill();
        }

        private static void DrawRobot(Painter2D p, float x, float y, float size, int dir) {
            var cx = x + size * 0.5f;
            var cy = y + size * 0.5f;
            var r = size * 0.3f;

            // body circle
            p.fillColor = new Color(0.3f, 0.6f, 1f);
            p.BeginPath();
            p.Arc(new Vector2(cx, cy), r, 0, 360);
            p.ClosePath();
            p.Fill();

            // direction indicator (small triangle pointing the facing direction)
            // dir: 0=N, 1=E, 2=S, 3=W — but Y is flipped in screen coords
            var arrowLen = r * 0.8f;
            var fwd = dir switch {
                0 => new Vector2(0, -1), // N (up on screen)
                1 => new Vector2(1, 0), // E
                2 => new Vector2(0, 1), // S (down on screen)
                _ => new Vector2(-1, 0) // W
            };
            var perp = new Vector2(-fwd.y, fwd.x);

            var tip = new Vector2(cx, cy) + fwd * arrowLen;
            var baseL = new Vector2(cx, cy) + perp * (r * 0.35f);
            var baseR = new Vector2(cx, cy) - perp * (r * 0.35f);

            p.fillColor = new Color(1f, 1f, 1f, 0.9f);
            p.BeginPath();
            p.MoveTo(tip);
            p.LineTo(baseL);
            p.LineTo(baseR);
            p.ClosePath();
            p.Fill();
        }
    }
}