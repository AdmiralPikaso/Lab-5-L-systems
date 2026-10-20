using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Drawing2D;

namespace Lab5
{
    public static class BezierSpline
    {
        public const int SamplesPerSegment = 32;

        public static List<PointF[]> Build(IReadOnlyList<PointF> points)
        {
            var segments = new List<PointF[]>();
            int n = points.Count;
            if (n < 2)
                return segments;

            PointF At(int i) => points[Math.Clamp(i, 0, n - 1)];

            for (int i = 0; i < n - 1; i++)
            {
                PointF p0 = At(i - 1);
                PointF p1 = At(i);
                PointF p2 = At(i + 1);
                PointF p3 = At(i + 2);

                segments.Add(new[]
                {
                    p1,
                    new PointF(p1.X + (p2.X - p0.X) / 6f, p1.Y + (p2.Y - p0.Y) / 6f),
                    new PointF(p2.X - (p3.X - p1.X) / 6f, p2.Y - (p3.Y - p1.Y) / 6f),
                    p2
                });
            }

            return segments;
        }

        public static PointF Evaluate(PointF[] control, double t)
        {
            double u = 1.0 - t;
            double uu = u * u, tt = t * t;
            double uuu = uu * u, ttt = tt * t;

            return new PointF(
                (float)(uuu * control[0].X + 3 * uu * t * control[1].X +
                        3 * u * tt * control[2].X + ttt * control[3].X),
                (float)(uuu * control[0].Y + 3 * uu * t * control[1].Y +
                        3 * u * tt * control[2].Y + ttt * control[3].Y));
        }
    }

    public partial class MainForm
    {
        private readonly FlowLayoutPanel t3Toolbar = new();
        private readonly Label t3LblHint = new();
        private readonly Button t3BtnDelete = new();
        private readonly PictureBox t3Canvas = new();

        private readonly List<PointF> t3Points = new();
        private int t3Selected = -1;
        private bool t3Dragging;
        private PointF t3GrabOffset;

        private float t3Scale = 1f;
        private float t3OffsetX;
        private float t3OffsetY;

        private const int t3HitRadius = 8;

        private void InitTask3()
        {
            t3LblHint.Text = "Клик — добавить точку, перетаскивание — переместить, ПКМ — удалить, Del — удалить выбранную";
            t3LblHint.AutoSize = true;
            t3LblHint.Margin = new Padding(3, 8, 15, 3);

            t3BtnDelete.Text = "Удалить выбранную";
            t3BtnDelete.AutoSize = true;
            t3BtnDelete.Margin = new Padding(3, 3, 3, 3);
            t3BtnDelete.Click += (s, e) => T3DeleteSelected();

            t3Toolbar.Dock = DockStyle.Top;
            t3Toolbar.AutoSize = true;
            t3Toolbar.WrapContents = false;
            t3Toolbar.Padding = new Padding(5);
            t3Toolbar.Controls.Add(t3LblHint);
            t3Toolbar.Controls.Add(t3BtnDelete);

            t3Canvas.Dock = DockStyle.Fill;
            t3Canvas.BackColor = Color.White;
            t3Canvas.TabStop = true;
            t3Canvas.Paint += T3Canvas_Paint;
            t3Canvas.Resize += (s, e) => t3Canvas.Invalidate();
            t3Canvas.MouseDown += T3Canvas_MouseDown;
            t3Canvas.MouseMove += T3Canvas_MouseMove;
            t3Canvas.MouseUp += (s, e) => t3Dragging = false;
            t3Canvas.KeyDown += T3Canvas_KeyDown;

            t3Points.AddRange(new[]
            {
                new PointF(0f, 0f),
                new PointF(1f, 0.8f),
                new PointF(2f, -0.4f),
                new PointF(3f, 0.6f),
                new PointF(4f, 0f)
            });

            tabTask3.Controls.Add(t3Canvas);
            tabTask3.Controls.Add(t3Toolbar);
        }

        private void T3Canvas_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                T3DeleteSelected();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                t3Selected = -1;
                t3Canvas.Invalidate();
                e.Handled = true;
            }
        }

        private void T3Canvas_MouseDown(object? sender, MouseEventArgs e)
        {
            t3Canvas.Focus();

            int hit = T3HitTest(e.Location);

            if (e.Button == MouseButtons.Left)
            {
                if (hit >= 0)
                {
                    t3Selected = hit;
                    t3Dragging = true;
                    PointF world = T3ScreenToWorld(e.Location);
                    t3GrabOffset = new PointF(
                        t3Points[hit].X - world.X,
                        t3Points[hit].Y - world.Y);
                }
                else
                {
                    T3AddPoint(e.Location);
                }
            }
            else if (e.Button == MouseButtons.Right && hit >= 0)
            {
                T3DeletePoint(hit);
            }

            t3Canvas.Invalidate();
        }

        private void T3Canvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (t3Dragging && t3Selected >= 0 && t3Selected < t3Points.Count)
            {
                PointF world = T3ScreenToWorld(e.Location);
                t3Points[t3Selected] = new PointF(world.X + t3GrabOffset.X, world.Y + t3GrabOffset.Y);
                t3Canvas.Invalidate();
                return;
            }

            Cursor = T3HitTest(e.Location) >= 0 ? Cursors.SizeAll : Cursors.Default;
        }

        private void T3AddPoint(Point location)
        {
            PointF world = T3ScreenToWorld(location);

            if (t3Points.Count < 2)
            {
                t3Points.Add(world);
                t3Selected = t3Points.Count - 1;
                return;
            }

            int bestSegment = 0;
            double bestDistance = double.MaxValue;

            for (int i = 0; i < t3Points.Count - 1; i++)
            {
                double d = T3DistanceToSegment(world, t3Points[i], t3Points[i + 1]);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    bestSegment = i;
                }
            }

            t3Points.Insert(bestSegment + 1, world);
            t3Selected = bestSegment + 1;
        }

        private void T3DeleteSelected()
        {
            if (t3Selected >= 0)
                T3DeletePoint(t3Selected);
            t3Canvas.Invalidate();
        }

        private void T3DeletePoint(int index)
        {
            if (t3Points.Count <= 2 || index < 0 || index >= t3Points.Count)
                return;

            t3Points.RemoveAt(index);

            if (t3Selected >= t3Points.Count)
                t3Selected = t3Points.Count - 1;
        }

        private int T3HitTest(Point location)
        {
            for (int i = 0; i < t3Points.Count; i++)
            {
                PointF screen = T3PointToScreen(t3Points[i]);
                double dx = screen.X - location.X;
                double dy = screen.Y - location.Y;
                if (dx * dx + dy * dy <= t3HitRadius * t3HitRadius)
                    return i;
            }

            return -1;
        }

        private static double T3DistanceToSegment(PointF p, PointF a, PointF b)
        {
            double abx = b.X - a.X, aby = b.Y - a.Y;
            double apx = p.X - a.X, apy = p.Y - a.Y;
            double lengthSq = abx * abx + aby * aby;

            double t = lengthSq < 1e-12
                ? 0
                : Math.Clamp((apx * abx + apy * aby) / lengthSq, 0, 1);

            double cx = a.X + abx * t - p.X;
            double cy = a.Y + aby * t - p.Y;
            return Math.Sqrt(cx * cx + cy * cy);
        }

        private void T3UpdateTransform()
        {
            if (t3Canvas.Width <= 0 || t3Canvas.Height <= 0)
                return;

            if (t3Points.Count == 0)
            {
                t3Scale = 1f;
                t3OffsetX = t3Canvas.Width / 2f;
                t3OffsetY = t3Canvas.Height / 2f;
                return;
            }

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            foreach (PointF p in t3Points)
            {
                minX = Math.Min(minX, p.X);
                maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y);
                maxY = Math.Max(maxY, p.Y);
            }

            double width = Math.Max(maxX - minX, 1e-9);
            double height = Math.Max(maxY - minY, 1e-9);

            const int margin = 50;
            double availW = t3Canvas.Width - 2 * margin;
            double availH = t3Canvas.Height - 2 * margin;
            if (availW <= 0 || availH <= 0)
                return;

            double scale = Math.Min(availW / width, availH / height);

            t3Scale = (float)scale;
            t3OffsetX = (float)((t3Canvas.Width - width * scale) / 2.0 - minX * scale);
            t3OffsetY = (float)((t3Canvas.Height - height * scale) / 2.0 - minY * scale);
        }

        private PointF T3PointToScreen(PointF p)
        {
            return new PointF(
                t3OffsetX + p.X * t3Scale,
                t3Canvas.Height - t3OffsetY - p.Y * t3Scale);
        }

        private PointF T3ScreenToWorld(Point p)
        {
            return new PointF(
                (p.X - t3OffsetX) / t3Scale,
                (t3Canvas.Height - t3OffsetY - p.Y) / t3Scale);
        }

        private void T3Canvas_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);

            if (!t3Dragging)
                T3UpdateTransform();

            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (t3Points.Count == 0)
            {
                T3DrawHint(g, "Кликните на холсте, чтобы добавить опорные точки");
                return;
            }

            PointF[] screen = new PointF[t3Points.Count];
            for (int i = 0; i < t3Points.Count; i++)
                screen[i] = T3PointToScreen(t3Points[i]);

            if (screen.Length >= 2)
            {
                using var polygonPen = new Pen(Color.FromArgb(178, 196, 214))
                {
                    DashStyle = DashStyle.Dash
                };
                g.DrawLines(polygonPen, screen);
            }

            List<PointF[]> segments = BezierSpline.Build(t3Points);
            if (segments.Count > 0)
            {
                using var path = new GraphicsPath();
                foreach (PointF[] segment in segments)
                {
                    var samples = new PointF[BezierSpline.SamplesPerSegment + 1];
                    for (int i = 0; i <= BezierSpline.SamplesPerSegment; i++)
                    {
                        double t = (double)i / BezierSpline.SamplesPerSegment;
                        samples[i] = T3PointToScreen(BezierSpline.Evaluate(segment, t));
                    }
                    path.AddLines(samples);
                }

                using var curvePen = new Pen(Color.FromArgb(30, 90, 160), 2.5f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                g.DrawPath(curvePen, path);
            }

            for (int i = 0; i < screen.Length; i++)
            {
                bool selected = i == t3Selected;
                float r = selected ? 7f : 5f;
                PointF c = screen[i];

                using var fill = new SolidBrush(selected ? Color.OrangeRed : Color.White);
                using var outline = new Pen(selected ? Color.DarkRed : Color.FromArgb(30, 90, 160), 2f);

                g.FillEllipse(fill, c.X - r, c.Y - r, r * 2, r * 2);
                g.DrawEllipse(outline, c.X - r, c.Y - r, r * 2, r * 2);
            }

            g.DrawString($"Точек: {t3Points.Count}", Font, Brushes.Gray, 5, 5);
        }

        private void T3DrawHint(Graphics g, string text)
        {
            SizeF size = g.MeasureString(text, Font);
            g.DrawString(
                text,
                Font,
                Brushes.Gray,
                (t3Canvas.Width - size.Width) / 2,
                (t3Canvas.Height - size.Height) / 2);
        }
    }
}
