using System.Globalization;

namespace Lab5
{
    public class LSystem
    {
        public string Axiom { get; }
        public double Angle { get; }
        public double StartDirection { get; }

        // Для каждого символа список возможных замен.
        // Если замен несколько, при генерации выбирается случайная.
        public Dictionary<char, List<string>> Rules { get; }

        public LSystem(string axiom, double angle, double startDirection,
                       Dictionary<char, List<string>> rules)
        {
            Axiom = axiom;
            Angle = angle;
            StartDirection = startDirection;
            Rules = rules;
        }

        public static LSystem Load(string path)
        {
            string[] lines = File.ReadAllLines(path);

            string? axiom = null;
            double angle = 0, direction = 0;
            var rules = new Dictionary<char, List<string>>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                // Запись как в лекции: стрелка "→", минус "−" и тире "–"
                line = line.Replace("→", "->").Replace('−', '-').Replace('–', '-');

                int lineNo = i + 1;

                if (axiom == null)
                {
                    
                    string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 3)
                        throw new FormatException(
                            $"Строка {lineNo}: ожидается \"<атом> <угол> <направление>\"");

                    axiom = parts[0];
                    angle = ParseNumber(parts[1], lineNo);
                    direction = ParseNumber(parts[2], lineNo);
                }
                else
                {
                    
                    int arrow = line.IndexOf("->");
                    if (arrow < 0)
                        throw new FormatException(
                            $"Строка {lineNo}: в правиле нет \"->\"");

                    string left = line.Substring(0, arrow).Trim();
                    string right = line.Substring(arrow + 2).Replace(" ", "");

                    if (left.Length != 1)
                        throw new FormatException(
                            $"Строка {lineNo}: слева от \"->\" должен быть ровно один символ");

                    char symbol = left[0];
                    if (!rules.ContainsKey(symbol))
                        rules[symbol] = new List<string>();
                    rules[symbol].Add(right);
                }
            }

            if (axiom == null)
                throw new FormatException("Файл пуст");

            return new LSystem(axiom, angle, direction, rules);
        }

        // Защита от зависания: строка растёт экспоненциально
        public const int MaxLength = 5_000_000;

        public string Generate(int iterations, Random random)
        {
            string current = Axiom;

            for (int step = 0; step < iterations; step++)
            {
                var next = new System.Text.StringBuilder();

                foreach (char c in current)
                {
                    if (Rules.TryGetValue(c, out List<string>? replacements))
                    {
                        int index = random.Next(replacements.Count);
                        next.Append(replacements[index]);
                    }
                    else
                    {
                        next.Append(c);
                    }

                    if (next.Length > MaxLength)
                        throw new InvalidOperationException(
                            $"На итерации {step + 1} строка длиннее {MaxLength} символов. " +
                            "Уменьшите число итераций.");
                }

                current = next.ToString();
            }

            return current;
        }

        private static double ParseNumber(string s, int lineNo)
        {
            s = s.Replace(',', '.').TrimEnd('°', 'º');
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                throw new FormatException($"Строка {lineNo}: \"{s}\" не является числом");
            return value;
        }
    }


    // Отрезок, который нарисовала черепаха.
    // Distance — сколько шагов прошла черепаха от основания до начала этого отрезка
    // (вдоль своей ветки, без учёта возвратов по "]")
    public readonly record struct TurtleSegment(double X1, double Y1, double X2, double Y2, int Distance);

    public static class Turtle
    {
        private readonly record struct State(double X, double Y, double Direction, int Distance);

        // Координаты математические: ось Y вверх, длина шага 1.
        // В экранные координаты их переведёт масштабирование.
        // angleJitter — случайное отклонение поворота в пределах ±angleJitter градусов.
        // Применяется только у ветвей (внутри скобок), ствол остаётся ровным.
        public static List<TurtleSegment> Walk(string commands, double angle, double startDirection,
                                               double angleJitter, Random random)
        {
            var segments = new List<TurtleSegment>();
            var stack = new Stack<State>();

            double x = 0, y = 0;
            double direction = startDirection;
            int distance = 0;

            double Turn()
            {
                if (stack.Count == 0)
                    return angle;
                return angle + (random.NextDouble() * 2 - 1) * angleJitter;
            }

            foreach (char c in commands)
            {
                switch (c)
                {
                    case 'F':
                        double rad = direction * Math.PI / 180;
                        double nx = x + Math.Cos(rad);
                        double ny = y + Math.Sin(rad);
                        segments.Add(new TurtleSegment(x, y, nx, ny, distance));
                        x = nx;
                        y = ny;
                        distance++;
                        break;

                    case '+':
                        direction -= Turn(); // по часовой стрелке, как в лекции
                        break;

                    case '-':
                        direction += Turn(); // против часовой стрелки
                        break;

                    case '[':
                        stack.Push(new State(x, y, direction, distance));
                        break;

                    case ']':
                        if (stack.Count == 0)
                            throw new InvalidOperationException("Лишняя закрывающая скобка \"]\"");

                        State saved = stack.Pop();
                        x = saved.X;
                        y = saved.Y;
                        direction = saved.Direction;
                        distance = saved.Distance;
                        break;

                    // Остальные символы (X, Y...) нужны только для правил
                }
            }

            return segments;
        }
    }

    public partial class MainForm
    {
        
        private readonly FlowLayoutPanel t1Toolbar = new FlowLayoutPanel();
        private readonly Button t1BtnOpen = new Button();
        private readonly Label t1LblFile = new Label();
        private readonly NumericUpDown t1NumIterations = new NumericUpDown();
        private readonly Button t1BtnDraw = new Button();
        private readonly NumericUpDown t1NumJitter = new NumericUpDown();
        private readonly CheckBox t1ChkTree = new CheckBox();
        private readonly PictureBox t1Canvas = new PictureBox();

        
        private string? t1FilePath;
        private LSystem? t1System;
        private List<TurtleSegment>? t1Segments;
        private readonly Random t1Random = new Random();

        private void InitTask1()
        {
            // Кнопка выбора файла
            t1BtnOpen.Text = "Открыть файл...";
            t1BtnOpen.AutoSize = true;
            t1BtnOpen.Click += T1BtnOpen_Click;

            // Подпись с именем выбранного файла
            t1LblFile.Text = "файл не выбран";
            t1LblFile.AutoSize = true;
            t1LblFile.Margin = new Padding(3, 8, 15, 3);

            // Число итераций
            var lblIter = new Label();
            lblIter.Text = "Итераций:";
            lblIter.AutoSize = true;
            lblIter.Margin = new Padding(3, 8, 3, 3);

            t1NumIterations.Minimum = 0;
            t1NumIterations.Maximum = 12;
            t1NumIterations.Value = 4;
            t1NumIterations.Width = 60;

            // Случайное отклонение угла поворота
            var lblJitter = new Label();
            lblJitter.Text = "Разброс угла ±°:";
            lblJitter.AutoSize = true;
            lblJitter.Margin = new Padding(15, 8, 3, 3);

            t1NumJitter.Minimum = 0;
            t1NumJitter.Maximum = 45;
            t1NumJitter.Value = 0;
            t1NumJitter.Width = 60;

            // Режим дерева (1.б): толщина и цвет зависят от расстояния до основания
            t1ChkTree.Text = "Дерево (1.б)";
            t1ChkTree.AutoSize = true;
            t1ChkTree.Margin = new Padding(15, 6, 3, 3);
            t1ChkTree.CheckedChanged += (s, e) => t1Canvas.Invalidate();

            // Кнопка построения
            t1BtnDraw.Text = "Построить";
            t1BtnDraw.AutoSize = true;
            t1BtnDraw.Click += T1BtnDraw_Click;

            // Панель инструментов сверху
            t1Toolbar.Dock = DockStyle.Top;
            t1Toolbar.AutoSize = true;
            t1Toolbar.Padding = new Padding(5);
            t1Toolbar.Controls.Add(t1BtnOpen);
            t1Toolbar.Controls.Add(t1LblFile);
            t1Toolbar.Controls.Add(lblIter);
            t1Toolbar.Controls.Add(t1NumIterations);
            t1Toolbar.Controls.Add(lblJitter);
            t1Toolbar.Controls.Add(t1NumJitter);
            t1Toolbar.Controls.Add(t1ChkTree);
            t1Toolbar.Controls.Add(t1BtnDraw);

            // Холст для рисования занимает всё остальное место
            t1Canvas.Dock = DockStyle.Fill;
            t1Canvas.BackColor = Color.White;
            t1Canvas.Paint += T1Canvas_Paint;
            t1Canvas.Resize += (s, e) => t1Canvas.Invalidate();

            // Порядок важен: сначала Fill, потом Top,
            // иначе холст залезет под панель инструментов
            tabTask1.Controls.Add(t1Canvas);
            tabTask1.Controls.Add(t1Toolbar);
        }

        private void T1BtnOpen_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            dialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";

            string examples = Path.Combine(AppContext.BaseDirectory, "LSystems");
            if (Directory.Exists(examples))
                dialog.InitialDirectory = examples;
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                t1System = LSystem.Load(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка в файле",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            t1FilePath = dialog.FileName;
            t1LblFile.Text = Path.GetFileName(t1FilePath);
            t1Segments = null;
            t1Canvas.Invalidate();
        }

        private void T1BtnDraw_Click(object? sender, EventArgs e)
        {
            if (t1System == null)
                return;

            try
            {
                string commands = t1System.Generate((int)t1NumIterations.Value, t1Random);
                t1Segments = Turtle.Walk(commands, t1System.Angle, t1System.StartDirection,
                                         (double)t1NumJitter.Value, t1Random);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Ошибка построения",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            t1Canvas.Invalidate();
        }

        private void T1Canvas_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);

            if (t1System == null)
            {
                T1DrawHint(g, "Откройте файл с описанием L-системы");
                return;
            }

            if (t1Segments == null || t1Segments.Count == 0)
            {
                T1DrawHint(g, "Нажмите \"Построить\"");
                return;
            }

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            T1DrawSegments(g, t1Segments);

            g.DrawString($"Отрезков: {t1Segments.Count}", Font, Brushes.Gray, 5, 5);
        }

        private void T1DrawHint(Graphics g, string hint)
        {
            SizeF size = g.MeasureString(hint, Font);
            g.DrawString(hint, Font, Brushes.Gray,
                (t1Canvas.Width - size.Width) / 2,
                (t1Canvas.Height - size.Height) / 2);
        }

        private void T1DrawSegments(Graphics g, List<TurtleSegment> segments)
        {
            
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            foreach (var s in segments)
            {
                minX = Math.Min(minX, Math.Min(s.X1, s.X2));
                maxX = Math.Max(maxX, Math.Max(s.X1, s.X2));
                minY = Math.Min(minY, Math.Min(s.Y1, s.Y2));
                maxY = Math.Max(maxY, Math.Max(s.Y1, s.Y2));
            }

            double width = maxX - minX;
            double height = maxY - minY;

            
            const int margin = 20;
            double availW = t1Canvas.Width - 2 * margin;
            double availH = t1Canvas.Height - 2 * margin;
            if (availW <= 0 || availH <= 0)
                return;

            
            double scaleX = width > 0 ? availW / width : double.MaxValue;
            double scaleY = height > 0 ? availH / height : double.MaxValue;
            double scale = Math.Min(scaleX, scaleY);

            
            double offsetX = (t1Canvas.Width - width * scale) / 2;
            double offsetY = (t1Canvas.Height - height * scale) / 2;

            PointF ToScreen(double x, double y)
            {
                float sx = (float)(offsetX + (x - minX) * scale);
                float sy = (float)(t1Canvas.Height - offsetY - (y - minY) * scale);
                return new PointF(sx, sy);
            }

            
            if (!t1ChkTree.Checked)
            {
                using var pen = new Pen(Color.Black, 1);
                foreach (var s in segments)
                    g.DrawLine(pen, ToScreen(s.X1, s.Y1), ToScreen(s.X2, s.Y2));
                return;
            }

            // Режим дерева: заранее готовим перья для T1Levels ступеней
            // от основания (коричневое, толстое) до кончиков веток (зелёное, тонкое)
            Pen[] pens = new Pen[T1Levels];
            for (int i = 0; i < T1Levels; i++)
            {
                double t = (double)i / (T1Levels - 1);
                pens[i] = new Pen(T1Lerp(T1TrunkColor, T1LeafColor, t), (float)T1Lerp(T1TrunkWidth, T1LeafWidth, t));
                pens[i].StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pens[i].EndCap = System.Drawing.Drawing2D.LineCap.Round;
            }

            int maxDistance = segments.Max(s => s.Distance);
            foreach (var s in segments)
            {
                // t = 0 у основания, t = 1 у самого дальнего от основания отрезка
                double t = maxDistance == 0 ? 0 : (double)s.Distance / maxDistance;
                int level = (int)Math.Round(t * (T1Levels - 1));
                g.DrawLine(pens[level], ToScreen(s.X1, s.Y1), ToScreen(s.X2, s.Y2));
            }

            foreach (Pen p in pens)
                p.Dispose();
        }

        private static readonly Color T1TrunkColor = Color.FromArgb(101, 67, 33);
        private static readonly Color T1LeafColor = Color.FromArgb(34, 160, 34);
        private const double T1TrunkWidth = 10;
        private const double T1LeafWidth = 1;
        private const int T1Levels = 32;

        // Линейная интерполяция: при t = 0 получаем a, при t = 1 получаем b
        private static double T1Lerp(double a, double b, double t) => a + (b - a) * t;

        private static Color T1Lerp(Color a, Color b, double t) => Color.FromArgb(
            (int)T1Lerp(a.R, b.R, t),
            (int)T1Lerp(a.G, b.G, t),
            (int)T1Lerp(a.B, b.B, t));
    }
}
