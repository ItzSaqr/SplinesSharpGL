using NurbsEditor.Geometry;
using NurbsEditor.Rendering;

using SharpGL;

using System;
using System.Drawing;
using System.Windows.Forms;

namespace NurbsEditor
{
    public class MainForm : Form
    {
        // ==========================================
        // OpenGL / данные
        // ==========================================

        private readonly OpenGLControl glControl;

        private readonly NurbsCurve curve = new NurbsCurve();

        private readonly Camera2D camera = new Camera2D();

        // ==========================================
        // UI
        // ==========================================

        private readonly NumericUpDown degreeBox;
        private readonly ComboBox modeBox;

        private readonly Label infoLabel;
        private readonly Label selectedPointLabel;

        private readonly NumericUpDown weightBox;

        private readonly Button deleteSelectedButton;
        private readonly Button clearButton;

        // ==========================================
        // Выбор и перетаскивание
        // ==========================================

        private int selectedPointIndex = -1;

        private bool pointDragging = false;
        private bool cameraDragging = false;

        private int lastMouseX;
        private int lastMouseY;

        // Радиус захвата точки мышью в пикселях.
        private const double PickRadiusPixels = 14.0;

        // ==========================================
        // Конструктор
        // ==========================================

        public MainForm()
        {
            Text = "NURBS Shape Studio";

            Width = 1200;
            Height = 750;

            StartPosition = FormStartPosition.CenterScreen;

            KeyPreview = true;

            KeyDown += MainForm_KeyDown;

            // ======================================
            // Главное разделение окна
            // ======================================

            SplitContainer split = new SplitContainer();

            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Vertical;
            split.FixedPanel = FixedPanel.Panel2;

            Controls.Add(split);

            // Теперь у split уже есть размер
            split.Panel1MinSize = 300;
            split.Panel2MinSize = 250;

            int desiredRightWidth = 280;

            int desiredDistance = split.Width - desiredRightWidth;

            int minDistance = split.Panel1MinSize;

            int maxDistance = split.Width - split.Panel2MinSize;

            split.SplitterDistance = Math.Max(
            minDistance, Math.Min(
            desiredDistance, maxDistance));

            // ======================================
            // OpenGLControl
            // ======================================

            glControl = new OpenGLControl();

            ((System.ComponentModel.ISupportInitialize)
            glControl).BeginInit();

            glControl.Dock = DockStyle.Fill;

            glControl.OpenGLVersion = SharpGL.Version.OpenGLVersion.OpenGL2_1;

            glControl.RenderContextType = RenderContextType.DIBSection;

            glControl.FrameRate = 60;

            glControl.DrawFPS = false;

            glControl.OpenGLInitialized += GlControl_OpenGLInitialized;

            glControl.OpenGLDraw += GlControl_OpenGLDraw;

            glControl.Resized += GlControl_Resized;

            glControl.MouseDown += GlControl_MouseDown;

            glControl.MouseUp += GlControl_MouseUp;

            glControl.MouseMove += GlControl_MouseMove;

            glControl.MouseWheel += GlControl_MouseWheel;

            ((System.ComponentModel.ISupportInitialize)
            glControl).EndInit();

            split.Panel1.Controls.Add(
            glControl);

            // ======================================
            // Правая панель
            // ======================================

            Panel rightPanel = split.Panel2;

            rightPanel.Padding = new Padding(12);

            rightPanel.AutoScroll = true;

            // --------------------------------------
            // Заголовок
            // --------------------------------------

            Label titleLabel = new Label();

            titleLabel.Text = "NURBS Shape Studio";

            titleLabel.Font = new Font(
            Font.FontFamily, 12, FontStyle.Bold);

            titleLabel.AutoSize = true;

            titleLabel.Left = 12;
            titleLabel.Top = 15;

            rightPanel.Controls.Add(
            titleLabel);

            // --------------------------------------
            // Степень
            // --------------------------------------

            Label degreeLabel = new Label();

            degreeLabel.Text = "Степень NURBS:";

            degreeLabel.AutoSize = true;

            degreeLabel.Left = 12;
            degreeLabel.Top = 60;

            rightPanel.Controls.Add(
            degreeLabel);

            degreeBox = new NumericUpDown();

            degreeBox.Left = 12;
            degreeBox.Top = 82;

            degreeBox.Width = 210;

            degreeBox.Minimum = 1;
            degreeBox.Maximum = 10;

            degreeBox.Value = 3;

            degreeBox.ValueChanged += DegreeBox_ValueChanged;

            rightPanel.Controls.Add(
            degreeBox);

            // --------------------------------------
            // Режим
            // --------------------------------------

            Label modeLabel = new Label();

            modeLabel.Text = "Режим:";

            modeLabel.AutoSize = true;

            modeLabel.Left = 12;
            modeLabel.Top = 125;

            rightPanel.Controls.Add(
            modeLabel);

            modeBox = new ComboBox();

            modeBox.Left = 12;
            modeBox.Top = 147;

            modeBox.Width = 210;

            modeBox.DropDownStyle = ComboBoxStyle.DropDownList;

            modeBox.Items.Add("Закрытый");

            modeBox.Items.Add("Замкнутый / периодический");

            modeBox.SelectedIndex = 0;

            modeBox.SelectedIndexChanged += ModeBox_SelectedIndexChanged;

            rightPanel.Controls.Add(
            modeBox);

            // --------------------------------------
            // Выбранная точка
            // --------------------------------------

            Label selectionHeader = new Label();

            selectionHeader.Text = "Выбранная точка:";

            selectionHeader.AutoSize = true;

            selectionHeader.Font = new Font(
            Font.FontFamily, 9, FontStyle.Bold);

            selectionHeader.Left = 12;
            selectionHeader.Top = 195;

            rightPanel.Controls.Add(
            selectionHeader);

            selectedPointLabel = new Label();

            selectedPointLabel.Left = 12;
            selectedPointLabel.Top = 220;

            selectedPointLabel.Width = 220;
            selectedPointLabel.Height = 55;

            selectedPointLabel.Text = "нет";

            rightPanel.Controls.Add(
            selectedPointLabel);

            // --------------------------------------
            // Вес
            // --------------------------------------

            Label weightLabel = new Label();

            weightLabel.Text = "Вес выбранной точки:";

            weightLabel.AutoSize = true;

            weightLabel.Left = 12;
            weightLabel.Top = 280;

            rightPanel.Controls.Add(
            weightLabel);

            weightBox = new NumericUpDown();

            weightBox.Left = 12;
            weightBox.Top = 302;

            weightBox.Width = 210;

            weightBox.DecimalPlaces = 2;

            weightBox.Minimum = 1;
            weightBox.Maximum = 1000;

            weightBox.Increment = 10;

            // Здесь используем деление на 100:
            // 100 -> вес 1.00
            weightBox.Value = 100;

            weightBox.Enabled = false;

            weightBox.ValueChanged += WeightBox_ValueChanged;

            rightPanel.Controls.Add(
            weightBox);

            // --------------------------------------
            // Удалить выбранную
            // --------------------------------------

            deleteSelectedButton = new Button();

            deleteSelectedButton.Text = "Удалить выбранную";

            deleteSelectedButton.Left = 12;
            deleteSelectedButton.Top = 350;

            deleteSelectedButton.Width = 210;
            deleteSelectedButton.Height = 32;

            deleteSelectedButton.Enabled = false;

            deleteSelectedButton.Click += DeleteSelectedButton_Click;

            rightPanel.Controls.Add(
            deleteSelectedButton);

            // --------------------------------------
            // Очистить
            // --------------------------------------

            clearButton = new Button();

            clearButton.Text = "Удалить все точки";

            clearButton.Left = 12;
            clearButton.Top = 390;

            clearButton.Width = 210;
            clearButton.Height = 32;

            clearButton.Click += ClearButton_Click;

            rightPanel.Controls.Add(
            clearButton);

            // --------------------------------------
            // Info
            // --------------------------------------

            Label infoHeader = new Label();

            infoHeader.Text = "Info";

            infoHeader.AutoSize = true;

            infoHeader.Font = new Font(
            Font.FontFamily, 9, FontStyle.Bold);

            infoHeader.Left = 12;
            infoHeader.Top = 445;

            rightPanel.Controls.Add(
            infoHeader);

            infoLabel = new Label();

            infoLabel.Left = 12;
            infoLabel.Top = 470;

            infoLabel.Width = 220;
            infoLabel.Height = 190;

            infoLabel.BorderStyle = BorderStyle.FixedSingle;

            infoLabel.Padding = new Padding(8);

            infoLabel.BackColor = SystemColors.ControlLightLight;

            infoLabel.ForeColor = Color.Black;

            rightPanel.Controls.Add(
            infoLabel);

            UpdateInfo();
        }

        // ==========================================
        // OpenGL initialization
        // ==========================================

        private void GlControl_OpenGLInitialized(
        object sender, EventArgs e)
        {
            OpenGL gl = glControl.OpenGL;

            gl.ClearColor(
            0.08f, 0.09f, 0.11f, 1.0f);

            gl.Disable(
            OpenGL.GL_DEPTH_TEST);

            gl.Disable(
            OpenGL.GL_LIGHTING);

            gl.Disable(
            OpenGL.GL_TEXTURE_2D);
        }

        private void GlControl_Resized(
        object sender, EventArgs e)
        {
            SetupProjection();
        }

        // ==========================================
        // Проекция
        // ==========================================

        private void SetupProjection()
        {
            OpenGL gl = glControl.OpenGL;

            int width = Math.Max(
            glControl.Width, 1);

            int height = Math.Max(
            glControl.Height, 1);

            gl.Viewport(
            0, 0, width, height);

            gl.MatrixMode(
            OpenGL.GL_PROJECTION);

            gl.LoadIdentity();

            double halfWidth = width / (2.0 * camera.Zoom);

            double halfHeight = height / (2.0 * camera.Zoom);

            gl.Ortho(
            camera.X - halfWidth, camera.X + halfWidth,

            camera.Y - halfHeight, camera.Y + halfHeight,

            -1.0, 1.0);

            gl.MatrixMode(
            OpenGL.GL_MODELVIEW);

            gl.LoadIdentity();
        }

        // ==========================================
        // Главная отрисовка
        // ==========================================

        private void GlControl_OpenGLDraw(
        object sender, RenderEventArgs args)
        {
            OpenGL gl = glControl.OpenGL;

            gl.Clear(
            OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            SetupProjection();

            DrawGrid(gl);

            DrawAxes(gl);

            DrawControlPolygon(gl);

            DrawCurve(gl);

            DrawControlPoints(gl);

            gl.Flush();
        }

        // ==========================================
        // Сетка
        // ==========================================

        private void DrawGrid(
        OpenGL gl)
        {
            int width = Math.Max(
            glControl.Width, 1);

            int height = Math.Max(
            glControl.Height, 1);

            double halfWidth = width / (2.0 * camera.Zoom);

            double halfHeight = height / (2.0 * camera.Zoom);

            double left = camera.X - halfWidth;

            double right = camera.X + halfWidth;

            double bottom = camera.Y - halfHeight;

            double top = camera.Y + halfHeight;

            double gridStep = CalculateGridStep();

            gl.Color(
            0.22, 0.22, 0.25);

            gl.LineWidth(1);

            gl.Begin(
            OpenGL.GL_LINES);

            double startX = Math.Floor(
            left / gridStep) * gridStep;

            for (
            double x = startX;
            x <= right;
            x += gridStep)
            {
                gl.Vertex(
                x, bottom, 0.0);

                gl.Vertex(
                x, top, 0.0);
            }

            double startY = Math.Floor(
            bottom / gridStep) * gridStep;

            for (
            double y = startY;
            y <= top;
            y += gridStep)
            {
                gl.Vertex(
                left, y, 0.0);

                gl.Vertex(
                right, y, 0.0);
            }

            gl.End();
        }

        private double CalculateGridStep()
        {
            double worldPerPixel = 1.0 / camera.Zoom;

            double target = worldPerPixel * 80.0;

            double power = Math.Pow(
            10, Math.Floor(
            Math.Log10(
            target)));

            double normalized = target / power;

            double step;

            if (normalized < 2.0)
            {
                step = 1.0;
            }
            else if (normalized < 5.0)
            {
                step = 2.0;
            }
            else
            {
                step = 5.0;
            }

            return step * power;
        }

        // ==========================================
        // Оси
        // ==========================================

        private void DrawAxes(
        OpenGL gl)
        {
            int width = Math.Max(
            glControl.Width, 1);

            int height = Math.Max(
            glControl.Height, 1);

            double halfWidth = width / (2.0 * camera.Zoom);

            double halfHeight = height / (2.0 * camera.Zoom);

            gl.LineWidth(2.0f);

            gl.Color(
            0.70, 0.70, 0.70);

            gl.Begin(
            OpenGL.GL_LINES);

            // Ось X
            gl.Vertex(
            camera.X - halfWidth, 0.0, 0.0);

            gl.Vertex(
            camera.X + halfWidth, 0.0, 0.0);

            // Ось Y
            gl.Vertex(
            0.0, camera.Y - halfHeight, 0.0);

            gl.Vertex(
            0.0, camera.Y + halfHeight, 0.0);

            gl.End();
        }

        // ==========================================
        // Управляющий многоугольник
        // ==========================================

        private void DrawControlPolygon(
        OpenGL gl)
        {
            if (curve.Points.Count < 2)
            {
                return;
            }

            gl.Color(
            0.55, 0.55, 0.60);

            gl.LineWidth(1.5f);

            gl.Begin(
            OpenGL.GL_LINE_STRIP);

            foreach (
            ControlPoint point in curve.Points)
            {
                gl.Vertex(
                point.X, point.Y, 0.0);
            }

            gl.End();

            // Для периодического режима
            // показываем визуальное замыкание
            // управляющего многоугольника.
            if (curve.Mode == NurbsMode.Periodic && curve.Points.Count >= 2)
            {
                ControlPoint first = curve.Points[0];

                ControlPoint last = curve.Points[
                curve.Points.Count - 1];

                gl.Begin(
                OpenGL.GL_LINES);

                gl.Vertex(
                last.X, last.Y, 0.0);

                gl.Vertex(
                first.X, first.Y, 0.0);

                gl.End();
            }
        }

        // ==========================================
        // Контрольные точки
        // ==========================================

        private void DrawControlPoints(
        OpenGL gl)
        {
            if (curve.Points.Count == 0)
            {
                return;
            }

            gl.PointSize(12.0f);

            gl.Begin(
            OpenGL.GL_POINTS);

            for (
            int i = 0;
            i < curve.Points.Count;
            i++)
            {
                ControlPoint point = curve.Points[i];

                if (i == selectedPointIndex)
                {
                    // Выбранная точка
                    gl.Color(
                    1.0, 0.15, 0.15);
                }
                else
                {
                    // Обычная точка
                    gl.Color(
                    1.0, 0.55, 0.10);
                }

                gl.Vertex(
                point.X, point.Y, 0.0);
            }

            gl.End();
        }

        // ==========================================
        // NURBS
        // ==========================================

        private void DrawCurve(
        OpenGL gl)
        {
            if (!curve.CanBuild)
            {
                return;
            }

            var curvePoints = curve.BuildCurve(
            600);

            if (curvePoints.Count == 0)
            {
                return;
            }

            gl.Color(
            0.05, 0.85, 1.00);

            gl.LineWidth(
            3.0f);

            gl.Begin(
            OpenGL.GL_LINE_STRIP);

            foreach (
            PointF point in curvePoints)
            {
                gl.Vertex(
                point.X, point.Y, 0.0);
            }

            gl.End();
        }

        // ==========================================
        // Поиск точки под мышью
        // ==========================================

        private int FindPointAt(
        int mouseX, int mouseY)
        {
            int bestIndex = -1;

            double bestDistance = PickRadiusPixels;

            for (
            int i = 0;
            i < curve.Points.Count;
            i++)
            {
                ControlPoint p = curve.Points[i];

                double screenX = WorldToScreenX(
                p.X);

                double screenY = WorldToScreenY(
                p.Y);

                double dx = mouseX - screenX;

                double dy = mouseY - screenY;

                double distance = Math.Sqrt(
                dx * dx + dy * dy);

                if (distance <= bestDistance)
                {
                    bestDistance = distance;

                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private double WorldToScreenX(
        double worldX)
        {
            return (worldX - camera.X) * camera.Zoom + glControl.Width / 2.0;
        }

        private double WorldToScreenY(
        double worldY)
        {
            return
            glControl.Height / 2.0 - (worldY - camera.Y) * camera.Zoom;
        }

        // ==========================================
        // MouseDown
        // ==========================================

        private void GlControl_MouseDown(
        object sender, MouseEventArgs e)
        {
            glControl.Focus();

            lastMouseX = e.X;
            lastMouseY = e.Y;

            // --------------------------------------
            // ПКМ -> камера
            // --------------------------------------

            if (e.Button == MouseButtons.Right)
            {
                cameraDragging = true;

                return;
            }

            // --------------------------------------
            // ЛКМ
            // --------------------------------------

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            // Проверяем, кликнули ли
            // по существующей точке.
            int pointIndex = FindPointAt(
            e.X, e.Y);

            if (pointIndex >= 0)
            {
                // Выбираем существующую точку.
                selectedPointIndex = pointIndex;

                pointDragging = true;

                UpdateSelectedPointUI();

                glControl.Refresh();

                return;
            }

            // --------------------------------------
            // Если точки под мышью нет ->
            // создаём новую
            // --------------------------------------

            double x = camera.ScreenToWorldX(
            e.X, glControl.Width);

            double y = camera.ScreenToWorldY(
            e.Y, glControl.Height);

            curve.Points.Add(
            new ControlPoint(
            x, y, 1.0));

            selectedPointIndex = curve.Points.Count - 1;

            pointDragging = true;

            UpdateSelectedPointUI();

            UpdateInfo();

            glControl.Refresh();
        }

        // ==========================================
        // MouseMove
        // ==========================================

        private void GlControl_MouseMove(
        object sender, MouseEventArgs e)
        {
            // --------------------------------------
            // Перетаскивание контрольной точки
            // --------------------------------------

            if (pointDragging && selectedPointIndex >= 0 && selectedPointIndex < curve.Points.Count)
            {
                double x = camera.ScreenToWorldX(
                e.X, glControl.Width);

                double y = camera.ScreenToWorldY(
                e.Y, glControl.Height);

                ControlPoint point = curve.Points[
                selectedPointIndex];

                point.X = x;
                point.Y = y;

                UpdateSelectedPointUI();

                UpdateInfo();

                glControl.Refresh();

                return;
            }

            // --------------------------------------
            // Перемещение камеры
            // --------------------------------------

            if (cameraDragging)
            {
                int dx = e.X - lastMouseX;

                int dy = e.Y - lastMouseY;

                camera.X -= dx / camera.Zoom;

                camera.Y += dy / camera.Zoom;

                lastMouseX = e.X;

                lastMouseY = e.Y;

                glControl.Refresh();
            }
        }

        // ==========================================
        // MouseUp
        // ==========================================

        private void GlControl_MouseUp(
        object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                pointDragging = false;
            }

            if (e.Button == MouseButtons.Right)
            {
                cameraDragging = false;
            }
        }

        // ==========================================
        // Zoom
        // ==========================================

        private void GlControl_MouseWheel(
        object sender, MouseEventArgs e)
        {
            // Сохраняем мировую координату,
            // которая находится под курсором.
            double oldWorldX = camera.ScreenToWorldX(
            e.X, glControl.Width);

            double oldWorldY = camera.ScreenToWorldY(
            e.Y, glControl.Height);

            if (e.Delta > 0)
            {
                camera.Zoom *= 1.15;
            }
            else
            {
                camera.Zoom /= 1.15;
            }

            camera.Zoom = Math.Max(
            5.0, Math.Min(
            1000.0, camera.Zoom));

            // Новый world под мышью
            double newWorldX = camera.ScreenToWorldX(
            e.X, glControl.Width);

            double newWorldY = camera.ScreenToWorldY(
            e.Y, glControl.Height);

            // Масштабирование относительно курсора.
            camera.X += oldWorldX - newWorldX;

            camera.Y += oldWorldY - newWorldY;

            UpdateInfo();

            glControl.Refresh();
        }

        // ==========================================
        // Степень
        // ==========================================

        private void DegreeBox_ValueChanged(
        object sender, EventArgs e)
        {
            curve.Degree = (int)degreeBox.Value;

            UpdateInfo();

            glControl.Refresh();
        }

        // ==========================================
        // Режим
        // ==========================================

        private void ModeBox_SelectedIndexChanged(
        object sender, EventArgs e)
        {
            curve.Mode = modeBox.SelectedIndex == 0 ? NurbsMode.Closed : NurbsMode.Periodic;

            UpdateInfo();

            glControl.Refresh();
        }

        // ==========================================
        // Вес точки
        // ==========================================

        private void WeightBox_ValueChanged(
        object sender, EventArgs e)
        {
            if (selectedPointIndex < 0 || selectedPointIndex >= curve.Points.Count)
            {
                return;
            }

            double weight = (double)weightBox.Value / 100.0;

            curve.Points[
            selectedPointIndex].Weight = weight;

            UpdateSelectedPointUI();

            UpdateInfo();

            glControl.Refresh();
        }

        // ==========================================
        // Delete выбранной точки
        // ==========================================

        private void DeleteSelectedPoint()
        {
            if (selectedPointIndex < 0 || selectedPointIndex >= curve.Points.Count)
            {
                return;
            }

            curve.Points.RemoveAt(
            selectedPointIndex);

            selectedPointIndex = -1;

            pointDragging = false;

            UpdateSelectedPointUI();

            UpdateInfo();

            glControl.Refresh();
        }

        private void DeleteSelectedButton_Click(
        object sender, EventArgs e)
        {
            DeleteSelectedPoint();
        }

        // ==========================================
        // Очистка
        // ==========================================

        private void ClearButton_Click(
        object sender, EventArgs e)
        {
            curve.Points.Clear();

            selectedPointIndex = -1;

            pointDragging = false;

            UpdateSelectedPointUI();

            UpdateInfo();

            glControl.Refresh();
        }

        // ==========================================
        // Клавиатура
        // ==========================================

        private void MainForm_KeyDown(
        object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                DeleteSelectedPoint();

                e.Handled = true;

                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                selectedPointIndex = -1;

                pointDragging = false;

                UpdateSelectedPointUI();

                UpdateInfo();

                glControl.Refresh();
            }
        }

        // ==========================================
        // UI выбранной точки
        // ==========================================

        private void UpdateSelectedPointUI()
        {
            bool hasSelection = selectedPointIndex >= 0 && selectedPointIndex < curve.Points.Count;

            deleteSelectedButton.Enabled = hasSelection;

            weightBox.Enabled = hasSelection;

            if (!hasSelection)
            {
                selectedPointLabel.Text = "нет";

                return;
            }

            ControlPoint p = curve.Points[
            selectedPointIndex];

            selectedPointLabel.Text = "P" + selectedPointIndex + "\nX = " + p.X.ToString("0.000") + "    Y = " + p.Y.ToString("0.000");

            decimal weightValue = (decimal)(
            p.Weight * 100.0);

            if (weightValue < weightBox.Minimum)
            {
                weightValue = weightBox.Minimum;
            }

            if (weightValue > weightBox.Maximum)
            {
                weightValue = weightBox.Maximum;
            }

            // Не вызываем ValueChanged,
            // если значение и так совпадает.
            if (weightBox.Value != weightValue)
            {
                weightBox.Value = weightValue;
            }
        }

        // ==========================================
        // Info
        // ==========================================

        private void UpdateInfo()
        {
            string status;

            if (curve.CanBuild)
            {
                status = "Сплайн построен";
            }
            else
            {
                status = "Недостаточно точек.\n" + "Нужно минимум: " + (curve.Degree + 1);
            }

            string selected;

            if (selectedPointIndex >= 0 && selectedPointIndex < curve.Points.Count)
            {
                selected = "P" + selectedPointIndex;
            }
            else
            {
                selected = "нет";
            }

            infoLabel.Text = "ЛКМ пусто — добавить\n" + "ЛКМ точка — выбрать\n" + "ЛКМ + drag — двигать\n" + "ПКМ + drag — камера\n" + "Колесо — масштаб\n" + "Delete — удалить точку\n" + "Esc — снять выбор\n\n" +

            "Точек: " + curve.Points.Count + "\n" +

            "Степень: " + curve.Degree + "\n" +

            "Выбрана: " + selected + "\n" +

            "Zoom: " + camera.Zoom.ToString("0.0") + "\n\n" +

            status;
        }
    }
}