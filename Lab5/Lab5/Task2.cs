using System.Drawing.Drawing2D;

namespace Lab5
{
    public static class MidpointDisplacement
    {
        public static List<List<PointF>> Build(
            double leftHeight,
            double rightHeight,
            int iterations,
            double roughness,
            Random random)
        {
            var steps = new List<List<PointF>>();
            var current = new List<PointF>
            {
                new(0f, (float)leftHeight),
                new(1f, (float)rightHeight)
            };

            steps.Add(current);

            double amplitude = 1;

            for (int level = 0; level < iterations; level++)
            {
                var next = new List<PointF>(current.Count * 2 - 1);

                for (int i = 0; i < current.Count - 1; i++)
                {
                    PointF left = current[i];
                    PointF right = current[i + 1];
                    float x = (left.X + right.X) / 2f;
                    float y = (float)((left.Y + right.Y) / 2.0
                                      + (random.NextDouble() * 2.0 - 1.0) * roughness * amplitude);

                    next.Add(left);
                    next.Add(new PointF(x, y));
                }

                next.Add(current[^1]);
                steps.Add(next);
                current = next;
                amplitude *= 0.5;
            }

            return steps;
        }
    }

    public partial class MainForm
    {
        private readonly FlowLayoutPanel t2Toolbar = new();
        private readonly NumericUpDown t2NumIterations = new();
        private readonly NumericUpDown t2NumRoughness = new();
        private readonly NumericUpDown t2NumLeft = new();
        private readonly NumericUpDown t2NumRight = new();
        private readonly Button t2BtnBuild = new();
        private readonly PictureBox t2Canvas = new();

        private readonly List<List<PointF>> t2Steps = new();
        private readonly Random t2Random = new();

        private double t2MinHeight;
        private double t2MaxHeight;

        private void InitTask2()
        {
            var lblIterations = new Label
            {
                Text = "Итераций:",
                AutoSize = true,
                Margin = new Padding(3, 8, 3, 3)
            };

            t2NumIterations.Minimum = 0;
            t2NumIterations.Maximum = 12;
            t2NumIterations.Value = 7;
            t2NumIterations.Width = 60;

            var lblRoughness = new Label
            {
                Text = "Шероховатость:",
                AutoSize = true,
                Margin = new Padding(15, 8, 3, 3)
            };

            t2NumRoughness.DecimalPlaces = 2;
            t2NumRoughness.Increment = 0.05m;
            t2NumRoughness.Minimum = 0m;
            t2NumRoughness.Maximum = 1m;
            t2NumRoughness.Value = 0.55m;
            t2NumRoughness.Width = 60;

            var lblLeft = new Label
            {
                Text = "Высота слева:",
                AutoSize = true,
                Margin = new Padding(15, 8, 3, 3)
            };

            t2NumLeft.DecimalPlaces = 2;
            t2NumLeft.Increment = 0.05m;
            t2NumLeft.Minimum = 0m;
            t2NumLeft.Maximum = 1m;
            t2NumLeft.Value = 0.45m;
            t2NumLeft.Width = 60;

            var lblRight = new Label
            {
                Text = "Высота справа:",
                AutoSize = true,
                Margin = new Padding(15, 8, 3, 3)
            };

            t2NumRight.DecimalPlaces = 2;
            t2NumRight.Increment = 0.05m;
            t2NumRight.Minimum = 0m;
            t2NumRight.Maximum = 1m;
            t2NumRight.Value = 0.45m;
            t2NumRight.Width = 60;

            t2BtnBuild.Text = "Построить";
            t2BtnBuild.AutoSize = true;
            t2BtnBuild.Margin = new Padding(15, 3, 3, 3);
            t2BtnBuild.Click += (s, e) => T2Build();

            t2Toolbar.Dock = DockStyle.Top;
            t2Toolbar.AutoSize = true;
            t2Toolbar.WrapContents = false;
            t2Toolbar.Padding = new Padding(5);
            t2Toolbar.Controls.Add(lblIterations);
            t2Toolbar.Controls.Add(t2NumIterations);
            t2Toolbar.Controls.Add(lblRoughness);
            t2Toolbar.Controls.Add(t2NumRoughness);
            t2Toolbar.Controls.Add(lblLeft);
            t2Toolbar.Controls.Add(t2NumLeft);
            t2Toolbar.Controls.Add(lblRight);
            t2Toolbar.Controls.Add(t2NumRight);
            t2Toolbar.Controls.Add(t2BtnBuild);

            t2Canvas.Dock = DockStyle.Fill;
            t2Canvas.BackColor = Color.White;
            t2Canvas.Paint += T2Canvas_Paint;
            t2Canvas.Resize += (s, e) => t2Canvas.Invalidate();

            tabTask2.Controls.Add(t2Canvas);
            tabTask2.Controls.Add(t2Toolbar);

            T2Build();
        }

        private void T2Build()
        {
            t2Steps.Clear();
            t2Steps.AddRange(MidpointDisplacement.Build(
                (double)t2NumLeft.Value,
                (double)t2NumRight.Value,
                (int)t2NumIterations.Value,
                (double)t2NumRoughness.Value,
                t2Random));

            t2MinHeight = double.MaxValue;
            t2MaxHeight = double.MinValue;

            foreach (List<PointF> step in t2Steps)
            {
                foreach (PointF point in step)
                {
                    t2MinHeight = Math.Min(t2MinHeight, point.Y);
                    t2MaxHeight = Math.Max(t2MaxHeight, point.Y);
                }
            }

            t2Canvas.Invalidate();
        }

        private void T2Canvas_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);

            if (t2Canvas.Width <= 0 || t2Canvas.Height <= 0)
                return;

            if (t2Steps.Count == 0)
            {
                T2DrawHint(g, "Нажмите \"Построить\"");
                return;
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;

            int count = t2Steps.Count;
            int columns = count switch
            {
                <= 1 => 1,
                <= 4 => 2,
                <= 9 => 3,
                _ => 4
            };
            int rows = (count + columns - 1) / columns;

            const float margin = 10f;
            const float gap = 8f;
            float tileWidth = (t2Canvas.Width - margin * 2 - gap * (columns - 1)) / columns;
            float tileHeight = (t2Canvas.Height - margin * 2 - gap * (rows - 1)) / rows;

            if (tileWidth <= 0 || tileHeight <= 0)
                return;

            for (int i = 0; i < count; i++)
            {
                int row = i / columns;
                int column = i % columns;
                var tile = new RectangleF(
                    margin + column * (tileWidth + gap),
                    margin + row * (tileHeight + gap),
                    tileWidth,
                    tileHeight);

                T2DrawStep(g, i, tile);
            }
        }

        private void T2DrawStep(Graphics g, int index, RectangleF tile)
        {
            using var border = new Pen(Color.FromArgb(214, 219, 226));
            g.DrawRectangle(border, tile.X, tile.Y, tile.Width - 1, tile.Height - 1);

            string caption = $"Шаг {index}, точек: {t2Steps[index].Count}";
            g.DrawString(caption, Font, Brushes.DimGray, tile.X + 8, tile.Y + 6);

            var plot = new RectangleF(
                tile.X + 10,
                tile.Y + 30,
                tile.Width - 20,
                tile.Height - 40);

            if (plot.Width <= 0 || plot.Height <= 0)
                return;

            PointF[] line = T2ToScreen(t2Steps[index], plot);
            var area = new PointF[line.Length + 2];
            line.CopyTo(area, 0);
            area[^2] = new PointF(plot.Right, plot.Bottom);
            area[^1] = new PointF(plot.Left, plot.Bottom);

            using (var fill = new SolidBrush(Color.FromArgb(216, 226, 232)))
            {
                g.FillPolygon(fill, area);
            }

            using var outline = new Pen(Color.FromArgb(54, 79, 94), 1.6f);
            outline.LineJoin = LineJoin.Round;
            g.DrawLines(outline, line);
        }

        private PointF[] T2ToScreen(List<PointF> points, RectangleF plot)
        {
            double span = t2MaxHeight - t2MinHeight;
            bool flat = span < 1e-9;
            const float pad = 0.05f;

            var result = new PointF[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                PointF point = points[i];
                float normalized = flat ? 0.5f : (float)((point.Y - t2MinHeight) / span);

                result[i] = new PointF(
                    plot.Left + point.X * plot.Width,
                    plot.Bottom - (pad + normalized * (1 - 2 * pad)) * plot.Height);
            }

            return result;
        }

        private void T2DrawHint(Graphics g, string text)
        {
            SizeF size = g.MeasureString(text, Font);
            g.DrawString(
                text,
                Font,
                Brushes.Gray,
                (t2Canvas.Width - size.Width) / 2,
                (t2Canvas.Height - size.Height) / 2);
        }
    }
}
