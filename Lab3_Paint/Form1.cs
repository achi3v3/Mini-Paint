using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.MonthCalendar;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Lab3_Paint
{
    /// <summary>
    /// Главная форма приложения Paint.
    /// Реализует простой графический редактор с различными инструментами и функциями.
    /// </summary>
    public partial class Form1: Form
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса Form1.
        /// Настраивает главную форму, инициализирует холст для рисования и конфигурирует элементы интерфейса.
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            // Инициализация холста для рисования с размером по умолчанию
            map = new Bitmap(1924, 768);
            using (Graphics g = Graphics.FromImage(map))
            {
                g.Clear(Color.White);
            }

            // Настройка PictureBox для рисования
            pictureBox1.Image = map;
            pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
            pictureBox1.Dock = DockStyle.None;
            pictureBox1.Location = new Point(toolStrip1.Width, menuStrip1.Height);

            // Настройка панели прокрутки
            Panel scrollPanel = new Panel();
            scrollPanel.Dock = DockStyle.Fill;
            scrollPanel.AutoScroll = true;
            scrollPanel.Controls.Add(pictureBox1);

            // Настройка панелей инструментов
            toolStrip1.Dock = DockStyle.Left;
            menuStrip1.Dock = DockStyle.Top;

            // Добавление элементов управления в правильном порядке
            this.Controls.Add(menuStrip1);
            this.Controls.Add(toolStrip1);
            this.Controls.Add(scrollPanel);

            // Инициализация элементов интерфейса
            Paste.Enabled = false;
            Cut.Enabled = false;
            Copy.Enabled = false;
            Brush.BackColor = Color.LightBlue;
            Undo.Enabled = false;
            Redo.Enabled = false;

            // Настройка инструмента ластика
            erase.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            erase.StartCap = System.Drawing.Drawing2D.LineCap.Round;

            // Добавление обработчика изменения размера формы
            this.Resize += Form1_Resize;
        }

        /// <summary>
        /// Обрабатывает события изменения размера формы для поддержания правильной компоновки и прокрутки.
        /// </summary>
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (map == null) return;

            // Обновление размеров PictureBox
            pictureBox1.Width = map.Width;
            pictureBox1.Height = map.Height;

            // Обновление позиции PictureBox
            pictureBox1.Location = new Point(toolStrip1.Width, menuStrip1.Height);

            // Обновление размеров для прокрутки
            this.AutoScrollMinSize = new Size(pictureBox1.Width + toolStrip1.Width, 
                                            pictureBox1.Height + menuStrip1.Height);
        }

        /// <summary>
        /// Обрабатывает события изменения размера PictureBox и обновляет холст для рисования.
        /// </summary>
        private void pictureBox1_Resize(object sender, EventArgs e)
        {
            if (map == null) return;

            // Проверка и удаление предыдущего состояния при изменении размера
            if (size_change)
                RemoveState();

            size_change = true;
            SaveState();

            // Сброс на инструмент кисти при изменении размера
            using_light(index, 1);
            index = 1;

            // Сохранение предыдущего изображения и создание нового холста
            Bitmap prev_map = map;
            map = new Bitmap(pictureBox1.Width, pictureBox1.Height, prev_map.PixelFormat);

            // Копирование содержимого предыдущего холста на новый
            using (Graphics g = Graphics.FromImage(map))
            {
                g.Clear(Color.White);
                if (prev_map != null) {
                    g.DrawImage(prev_map, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));
                }
            }

            // Обновление отображения
            pictureBox1.Image = map;
            pictureBox1.Location = new Point(toolStrip1.Width, menuStrip1.Height);
            this.AutoScrollMinSize = new Size(pictureBox1.Width + toolStrip1.Width, 
                                            pictureBox1.Height + menuStrip1.Height);
            pictureBox1.Invalidate();
        }

        // Основной холст для рисования
        private Bitmap map;

        // Поля для работы с выделением
        private Rectangle selectionRect = new Rectangle(0, 0, 0, 0);
        private Bitmap selectedArea;
        private bool isSelecting = false;
        private Point selectionStart;
        private bool isMoving = false;
        private Point moveStart;
        private bool firstMove;

        // Координаты и состояние рисования
        private int putx, puty;
        private int sx, sy, x, y;
        private Point click;
        private Color TargetColor;
        private Bitmap cutBuffer;
        private bool hasSelection = false;
        private RichTextBox textBox = new RichTextBox();
        private bool writing = false;

        /// <summary>
        /// Вспомогательный класс для управления точками рисования.
        /// Поддерживает циклический буфер точек для операций рисования.
        /// </summary>
        private class ArrayPoints
        {
            private int index = 0;
            private Point[] points;      

            /// <summary>
            /// Инициализирует новый экземпляр ArrayPoints с указанным размером.
            /// </summary>
            /// <param name="size">Размер массива точек</param>
            public ArrayPoints(int size)
            {
                if (size <= 0)
                {
                    size = 2; // Минимум 2 точки для рисования линии
                }
                points = new Point[size];
            }

            /// <summary>
            /// Добавляет точку в массив.
            /// </summary>
            public void SetPoint(int x, int y)
            {
                if (index >= points.Length)
                {
                    index = 0;
                }

                points[index] = new Point(x, y); 
                index++; 
            }

            /// <summary>
            /// Сбрасывает индекс массива точек.
            /// </summary>
            public void ResetPoints()
            {
                index = 0; 
            }

            /// <summary>
            /// Получает текущее количество точек в массиве.
            /// </summary>
            public int GetCountPoints()
            {
                return index;
            }

            /// <summary>
            /// Получает массив точек.
            /// </summary>
            public Point[] GetPoints()
            {
                return points;
            }
        }

        // Переменные состояния рисования
        private bool isMouse = false;
        private ArrayPoints arrayPoints = new ArrayPoints(2);
        
        // Инструменты рисования
        private Pen pen = new Pen(System.Drawing.Color.Black, 3f);
        private Pen erase = new Pen(Color.White, 3f);

        /// <summary>
        /// Сохраняет текущее изображение в файл.
        /// </summary>
        /// <param name="image">Битмап для сохранения</param>
        public void SaveImage(Bitmap image)
        {
            if (image == null)
            {
                MessageBox.Show("Нет изображения для сохранения");
                return;
            }

            // Настройка диалога сохранения
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "JPEG Image|*.jpg|PNG Image|*.png|BMP Image|*.bmp";
            saveDialog.Title = "Сохранить изображение";
            saveDialog.FileName = "image";

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveDialog.FileName;
                string extension = Path.GetExtension(filePath).ToLower();

                try
                {
                    // Сохранение в выбранном формате
                    switch (extension)
                    {
                        case ".jpg":
                        case ".jpeg":
                            image.Save(filePath, ImageFormat.Jpeg);
                            break;

                        case ".png":
                            image.Save(filePath, ImageFormat.Png);
                            break;

                        case ".bmp":
                            image.Save(filePath, ImageFormat.Bmp);
                            break;
                    }

                    // Сброс состояния после сохранения
                    check = false;
                    undoStack = new Stack<Bitmap>();
                    redoStack = new Stack<Bitmap>();
                    Undo.Enabled = false;
                    Redo.Enabled = false;

                    // Вывод информации о сохранении
                    FileInfo fileInfo = new FileInfo(filePath);
                    MessageBox.Show($"Изображение сохранено\nРазмер: {fileInfo.Length / 1024} КБ");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Обрабатывает события нажатия кнопки мыши для операций рисования.
        /// </summary>
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e) 
        {
            if (index == 10) // Операция вставки
            {
                isSelecting = false;
                if (selectionRect.Contains(e.Location)) {
                    selection_down_move(e);
                } else {
                    firstMove = true;
                    selectionStart = e.Location;
                }
            }
            size_change = false;
            if (index == 11) { // Инструмент выделения
                if (selectionRect.Contains(e.Location) && !isSelecting) {
                    selection_down_move(e);
                }
                else {
                    if (selectionRect.Width > 0 && selectionRect.Height > 0)
                        RemoveState();
                    firstMove = false;
                    isSelecting = true;
                    selectionStart = e.Location;
                    selectionRect = new Rectangle(e.Location, Size.Empty);
                    hasSelection = true;
                    Undo.Enabled = undoStack.Count > 0; Redo.Enabled = redoStack.Count > 0;
                }
            }
            
            if (index != 11 && index != 10)
                SaveState();
            isMouse = true;
            putx = e.X; 
            puty = e.Y; 
            click = e.Location; 
            
            TargetColor = map.GetPixel(click.X, click.Y);
            if (index == 9) // Операция вырезания
            {
                selectionStart = e.Location;
                selectionRect = new Rectangle(e.Location, new Size(0, 0));
                isSelecting = true;
                hasSelection = true;
            }
        }

        // Координаты мыши для рисования фигур
        private int upx, upy;

        /// <summary>
        /// Обрабатывает события отпускания кнопки мыши для завершения операций рисования.
        /// </summary>
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            Graphics graphics = Graphics.FromImage(map);
            if (index == 10) { // Операция вставки
                SaveState();
                Undo.Enabled = false;
                if (isMoving) { 
                    selection_up_move(e, graphics);
                } else {
                    PasteCutArea();
                    selectionRect = new Rectangle(selectionStart, cutBuffer.Size);
                }
            }
            if (index == 11) { // Инструмент выделения
                SaveState();
                Undo.Enabled = false;
            }

            if (index == 11)
            {
                if (isSelecting)
                {
                    isSelecting = false;
                    if (selectionRect.Width > 0 && selectionRect.Height > 0)
                    {
                        Cut.Enabled = true;
                        Copy.Enabled = true;
                        try {
                            selectedArea = map.Clone(selectionRect, map.PixelFormat);
                        } catch {
                            selectionRect = new Rectangle(selectionStart, new Size(pictureBox1.Size.Width - selectionStart.X, pictureBox1.Size.Height - selectionStart.Y));
                            selectedArea = map.Clone(selectionRect, map.PixelFormat);
                        }
                    } else {
                        RemoveState();
                        Undo.Enabled = undoStack.Count > 0;
                    }
                }
                else if (isMoving) {
                    selection_up_move(e,graphics);
                }
            }

            isMouse = false;
            arrayPoints.ResetPoints();
            sx = x - putx; 
            sy = y - puty;

            upx = Math.Abs(e.X - putx);
            upy = Math.Abs(e.Y - puty);

            // Отрисовка фигур в зависимости от выбранного инструмента
            if (index == 3) { // Инструмент линии
                graphics.DrawLine(pen, putx, puty, x, y);
                return;
            } else if (index == 4) { // Инструмент круга
                graphics.DrawEllipse(pen, putx, puty, sx, sy);
                return;
            } else if (index == 5) { // Инструмент прямоугольника
                Rectangle a = new Rectangle(Math.Min(putx, x), Math.Min(puty, y), upx, upy);
                graphics.DrawRectangle(pen, a);
                return;
            }
            isSelecting = false;
            if (selectionRect.Width < 2 || selectionRect.Height < 2) {
                hasSelection = false;
            }
            pictureBox1.Refresh();
        }

        /// <summary>
        /// Обрабатывает события перемещения мыши для операций рисования.
        /// </summary>
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e) 
        {
            Graphics graphics = Graphics.FromImage(map);
            if (index == 10 && isMoving && selectedArea != null) {
                selection_move_move(e, graphics);
            }
            if (index == 11) {
                pictureBox1.Image = map;
                if (isSelecting)
                {
                    // Обновление прямоугольника выделения
                    selectionRect = new Rectangle(
                        Math.Min(selectionStart.X, e.X),
                        Math.Min(selectionStart.Y, e.Y),
                        Math.Abs(e.X - selectionStart.X),
                        Math.Abs(e.Y - selectionStart.Y));
                    pictureBox1.Invalidate();
                }
                else if (isMoving && selectedArea != null)
                {
                    selection_move_move(e,graphics);
                }
            }

            if (isSelecting && e.Button == MouseButtons.Left)
            {
                int x = Math.Min(selectionStart.X, e.X);
                int y = Math.Min(selectionStart.Y, e.Y);
                selectionRect = new Rectangle(x, y, Math.Abs(selectionStart.X - e.X),Math.Abs(selectionStart.Y - e.Y));
                pictureBox1.Invalidate(); 
            }

            if (isMouse)
            {
                pictureBox1.Image = map;
                arrayPoints.SetPoint(e.X, e.Y);
                if (arrayPoints.GetCountPoints() >= 2 && (index == 1 || index == 2))
                {
                    if (index == 1)
                        graphics.DrawLines(pen, arrayPoints.GetPoints());
                    if (index == 2)
                        graphics.DrawLines(erase, arrayPoints.GetPoints());
                    arrayPoints.SetPoint(e.X, e.Y);
                }
            }
            pictureBox1.Refresh();

            sx = e.X - putx;
            sy = e.Y - puty;

            x = e.X; 
            y = e.Y; 
        }

        /// <summary>
        /// Обрабатывает выбор цвета для инструментов рисования.
        /// </summary>
        private void Colorr_Click(object sender, EventArgs e)
        {
            var res = colorDialog1.ShowDialog();
            if(res == DialogResult.OK)
            {
                Colorr.BackColor = colorDialog1.Color;
                pen.Color = colorDialog1.Color;
            }
        }

        // Настройки толщины инструментов
        private void Px_1()
        {
            pen.Width = 1f;
            erase.Width = 1f;
        }

        private void Px_3()
        {
            pen.Width = 3f;
            erase.Width = 3f;
        }

        private void Px_5()
        {
            pen.Width = 5f;
            erase.Width = 5f;
        }

        private void Px_8()
        {
            pen.Width = 8f;
            erase.Width = 8f;
        }

        // Текущий индекс инструмента (1 = Кисть)
        private int index = 1;

        // Обработчики активации инструментов
        private void Brush_Click(object sender, EventArgs e)
        {
            using_light(index, 1);
            index = 1;
            pen.Color = Colorr.BackColor;
        }

        private void Erase_Click(object sender, EventArgs e)
        {
            using_light(index, 2);
            index = 2;
            pen.Color = Color.White;
        }

        private void Line_Click(object sender, EventArgs e)
        {
            using_light(index, 3);
            index = 3;
        }

        /// <summary>
        /// Обрабатывает отрисовку холста и прямоугольника выделения.
        /// </summary>
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;

            if (isMouse) {
                if (index == 3) {
                    graphics.DrawLine(pen, putx, puty, x, y);
                    return;
                }
                else if (index == 4) {
                    graphics.DrawEllipse(pen, putx, puty, sx, sy);
                    return;
                }
                else if (index == 5) {
                    Rectangle a = new Rectangle(Math.Min(putx, x), Math.Min(puty, y), Math.Abs(sx), Math.Abs(sy));
                    graphics.DrawRectangle(pen, a);
                    return;
                }
                if (index == 7) {
                    textBox.Size = new Size(sx, sy);
                }
            }
            if (pictureBox1.Image != null) {
                e.Graphics.DrawImage(pictureBox1.Image, 0, 0);
            }

            if (map != null && (pictureBox1.Image != null)) {
                e.Graphics.DrawImage(map, 0, 0);

                if (selectionRect.Width > 0 && selectionRect.Height > 0)
                {
                    using (Pen dashedPen = new Pen(Color.Black, 2))
                    {
                        dashedPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        e.Graphics.DrawRectangle(dashedPen, selectionRect);
                    }

                    if (selectedArea != null && isMoving)
                    {
                        e.Graphics.DrawImage(selectedArea, selectionRect.Location);
                    }
                }
            }
        }

        private void Circle_Click(object sender, EventArgs e)
        {
            using_light(index, 4);
            index = 4;
        }

        /// <summary>
        /// Обрабатывает события клика мыши для инструмента заливки.
        /// </summary>
        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (index == 6)
            {
                Point point = set_point(pictureBox1, e.Location);
                Fill(map, point.X, point.Y, Colorr.BackColor);
            }
        }

        private void Rectangle_Click(object sender, EventArgs e)
        {
            using_light(index, 5);
            index = 5;
        }

        /// <summary>
        /// Реализует алгоритм заливки для инструмента заливки.
        /// </summary>
        public void Fill(Bitmap bm, int x, int y, Color newColor)
        {
            // Получение цвета исходного пикселя
            Color oldColor = bm.GetPixel(x, y);

            if (oldColor.ToArgb() == newColor.ToArgb())
                return;

            // Инициализация очереди для алгоритма заливки
            Queue<Point> pixels = new Queue<Point>();
            pixels.Enqueue(new Point(x, y));
            bm.SetPixel(x, y, newColor);

            // Блокировка битмапа для оптимизации доступа к пикселям
            BitmapData bmData = bm.LockBits(
                new Rectangle(0, 0, bm.Width, bm.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            try
            {
                // Получение массива пикселей
                int[] pixelsData = new int[bm.Width * bm.Height];
                Marshal.Copy(bmData.Scan0, pixelsData, 0, pixelsData.Length);

                int oldArgb = oldColor.ToArgb();
                int newArgb = newColor.ToArgb();
                int width = bm.Width;
                int height = bm.Height;

                // Основной цикл алгоритма заливки
                while (pixels.Count > 0)
                {
                    Point pt = pixels.Dequeue();
                    int currentX = pt.X;
                    int currentY = pt.Y;

                    // Проверка соседних пикселей
                    CheckPixel(pixelsData, pixels, width, height, currentX - 1, currentY, oldArgb, newArgb);
                    CheckPixel(pixelsData, pixels, width, height, currentX + 1, currentY, oldArgb, newArgb);
                    CheckPixel(pixelsData, pixels, width, height, currentX, currentY - 1, oldArgb, newArgb);
                    CheckPixel(pixelsData, pixels, width, height, currentX, currentY + 1, oldArgb, newArgb);
                }

                // Копирование измененных данных обратно в битмап
                Marshal.Copy(pixelsData, 0, bmData.Scan0, pixelsData.Length);
            }
            finally
            {
                bm.UnlockBits(bmData);
            }
        }

        /// <summary>
        /// Вспомогательный метод для алгоритма заливки для проверки и заливки соседних пикселей.
        /// </summary>
        private void CheckPixel(int[] pixelsData, Queue<Point> pixels, int width, int height, int x, int y, int oldArgb, int newArgb)
        {
            if (x >= 0 && x < width && y >= 0 && y < height) {
                int index = y * width + x;
                if (pixelsData[index] == oldArgb)
                {
                    pixelsData[index] = newArgb;
                    pixels.Enqueue(new Point(x, y));
                }
            }
        }

        /// <summary>
        /// Вычисляет фактическую точку на изображении на основе координат PictureBox.
        /// </summary>
        static Point set_point(PictureBox pb, Point pt)
        {
            float pX = 1f * pb.Image.Width / pb.Width;
            float pY = 1f * pb.Image.Height / pb.Height;
            return new Point((int)(pt.X * pX), (int)(pt.Y * pY));
        }

        /// <summary>
        /// Структура для хранения информации о сегментах линий в алгоритме заливки.
        /// </summary>
        private struct LineSegment
        {
            public int X1;
            public int X2;
            public int Y;
            public int Direction;

            public LineSegment(int x1, int x2, int y, int direction)
            {
                X1 = x1;
                X2 = x2;
                Y = y;
                Direction = direction;
            }
        }

        private void Filling_Click(object sender, EventArgs e)
        {
            using_light(index, 6);
            index = 6;
        }

        private void Cut_Click(object sender, EventArgs e)
        {
            CutSelectedArea(9);
            using_light(index, 9);
            index = 9;
        }

        private void Paste_Click(object sender, EventArgs e)
        {
            using_light(index,10);
            index = 10;
        }

        /// <summary>
        /// Обновляет интерфейс для отражения текущего выбранного инструмента.
        /// </summary>
        private void using_light(int prev_index, int new_index)
        {
            ToolStripButton[] buttons = { Brush, Erase, Line, Circle, rectangle, Filling, null, null, Cut, Paste, Select, Copy};
            ClearSelection();
            Copy.Enabled = false;
            Cut.Enabled = false;
            buttons[prev_index - 1].BackColor = SystemColors.Control;
            buttons[new_index - 1].BackColor = Color.LightBlue;
            if (prev_index == 2)
                pen = new Pen(Colorr.BackColor, 3f);
            if(prev_index == 11)
            {
                RemoveState();
            }
            Undo.Enabled = undoStack.Count > 0;
        }

        /// <summary>
        /// Вырезает выделенную область из изображения.
        /// </summary>
        private void CutSelectedArea(int iindex)
        {
            Paste.Enabled = true;
            if (!hasSelection || pictureBox1.Image == null)
                return;

            Bitmap sourceImage = (Bitmap)pictureBox1.Image;

            try
            {
                // Создание буфера для вырезанной области
                cutBuffer = new Bitmap(selectionRect.Width, selectionRect.Height);
                using (Graphics g = Graphics.FromImage(cutBuffer))
                {
                    g.DrawImage(sourceImage,
                               new Rectangle(0, 0, cutBuffer.Width, cutBuffer.Height),
                               selectionRect,
                               GraphicsUnit.Pixel);
                }

                if (iindex == 9)
                {
                    // Заполнение вырезанной области белым цветом
                    using (Graphics g = Graphics.FromImage(sourceImage))
                    {
                        g.FillRectangle(Brushes.White, selectionRect);
                    }
                    pictureBox1.Refresh();
                }
                ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                cutBuffer = null;
                hasSelection = false;
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            SaveImage(map);
        }

        /// <summary>
        /// Удаляет последнее состояние из стека отмены.
        /// </summary>
        private void RemoveState()
        {
            if(undoStack.Count > 0)
            {
                undoStack.Pop();
                if (undoStack.Count == 0)
                {
                    Undo.Enabled = false;
                }
            }
        }

        private void Undo_Click(object sender, EventArgs e)
        {
            UNDO();
        }

        private void Redo_Click(object sender, EventArgs e)
        {
            REDO();
        }

        /// <summary>
        /// Очищает текущее выделение.
        /// </summary>
        private void ClearSelection()
        {
            selectionRect = Rectangle.Empty;
            hasSelection = false;
            pictureBox1.Invalidate();
        }

        /// <summary>
        /// Обрабатывает начало перемещения выделения.
        /// </summary>
        private void selection_down_move(MouseEventArgs e)
        {
            isMoving = true;
            moveStart = e.Location;
            if (firstMove)
            {
                UNDO();
            }
            if (!firstMove)
            {
                firstMove = true;
                using (Graphics g = Graphics.FromImage(map))
                {
                    g.FillRectangle(Brushes.White, selectionRect);
                }
                pictureBox1.Refresh();
            }
        }

        /// <summary>
        /// Обрабатывает завершение перемещения выделения.
        /// </summary>
        private void selection_up_move(MouseEventArgs e, Graphics graphics)
        {
            if (selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                if (selectedArea != null && isMoving)
                {
                    graphics.DrawImage(selectedArea, selectionRect.Location);
                }
            }

            isMoving = false;
        }

        /// <summary>
        /// Обрабатывает перемещение выделения.
        /// </summary>
        private void selection_move_move(MouseEventArgs e, Graphics graphics)
        {
            int dx = e.X - moveStart.X;
            int dy = e.Y - moveStart.Y;
            selectionRect.X += dx;
            selectionRect.Y += dy;
            moveStart = e.Location;
            pictureBox1.Invalidate();
        }

        /// <summary>
        /// Вставляет вырезанную область в текущее местоположение.
        /// </summary>
        private void PasteCutArea()
        {
            if (cutBuffer == null || pictureBox1.Image == null)
                return;

            Bitmap targetImage = (Bitmap)pictureBox1.Image;
            using (Graphics g = Graphics.FromImage(targetImage))
            {
                g.DrawImage(cutBuffer, new Point(putx, puty));
            }
            pictureBox1.Invalidate();
        }

        // Стеки для операций отмены/повтора
        private Stack<Bitmap> undoStack = new Stack<Bitmap>();
        private Stack<Bitmap> redoStack = new Stack<Bitmap>();

        /// <summary>
        /// Добавляет элемент в стек с ограничением размера.
        /// </summary>
        void PushWithLimit(Stack<Bitmap> stack, Bitmap item, int maxSize)
        {
            if (stack.Count >= maxSize)
            {
                var temp = new Stack<Bitmap>();
                while (stack.Count > 1)
                {
                    temp.Push(stack.Pop());
                }
                stack.Clear();
                while (temp.Count > 0)
                {
                    stack.Push(temp.Pop());
                }
            }
            stack.Push(item);
        }

        private void Create_Click(object sender, EventArgs e)
        {
            Paste.Enabled = false;
            Cut.Enabled = false;
            Copy.Enabled = false;

            bool create = CheckChangesAndSave();

            if (!create)
                return;

            Graphics graphics = Graphics.FromImage(map);
            graphics.Clear(pictureBox1.BackColor);

            check = false;
            undoStack = new Stack<Bitmap>();
            redoStack = new Stack<Bitmap>();
            Undo.Enabled = false;
            Redo.Enabled = false;
        }

        private void Open_Click(object sender, EventArgs e)
        {
            if (!CheckChangesAndSave())
                return;

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения (*.bmp;*.jpg;*.jpeg;*.png)|*.bmp;*.jpg;*.jpeg;*.png|" +
                 "Все файлы (*.*)|*.*",
                Title = "Открыть изображение",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            try {
                // Освобождение предыдущих ресурсов
                if (map != null) {
                    map.Dispose();
                }
                if (pictureBox1.Image != null) {
                    pictureBox1.Image.Dispose();
                }

                // Загрузка нового изображения
                map = new Bitmap(openFileDialog.FileName);
                pictureBox1.Image = map;

                // Очистка истории
                undoStack.Clear();
                redoStack.Clear();
                SaveState();

                // Обновление интерфейса
                pictureBox1.Invalidate();
                check = false;
                Undo.Enabled = false;
                Redo.Enabled = false;
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show("Невозможно открыть файл. Возможно, это не изображение или файл поврежден.",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("Файл не найден.", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии файла:\n{ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool check = false;

        private void Exit_Click(object sender, EventArgs e)
        {
             Application.Exit();
        }

        private bool size_change;

        private void pictureBox1_Resize_2(object sender, EventArgs e)
        {
            if (map == null) return;

            if (size_change)
                RemoveState();

            size_change = true;
            SaveState();

            using_light(index, 1);
            index = 1;

            Bitmap prev_map = map;
            map = new Bitmap(pictureBox1.Width, pictureBox1.Height, prev_map.PixelFormat);

            using (Graphics g = Graphics.FromImage(map))
            {
                g.Clear(Color.White);
                if (prev_map != null)
                {
                    g.DrawImage(prev_map, new Point(0, 0));
                }
            }

            pictureBox1.Image = map;
            this.AutoScrollMinSize = pictureBox1.Size;
            pictureBox1.Invalidate();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            pictureBox1.Image = map;
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            CutSelectedArea(12);
            using_light(index,12);
            index = 12;
        }

        private void Select_Click(object sender, EventArgs e)
        {
            using_light(index, 11);
            index = 11;
        }

        private void Thickness_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Thickness.Text == "8 px") Px_8();
            else if (Thickness.Text == "1 px") Px_1();
            else if (Thickness.Text == "3 px") Px_3();
            else if (Thickness.Text == "5 px") Px_5();
        }

        private void Thickness_Click(object sender, EventArgs e)
        {
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (check == false)
                return;

            DialogResult MBox = MessageBox.Show("Изображение было изменено.\nСохранить изменения?",
            "Paint", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

            if (MBox == DialogResult.No) return;
            if (MBox == DialogResult.Cancel) e.Cancel = true;
            if (MBox == DialogResult.Yes)
                SaveImage(map);
        }

        /// <summary>
        /// Сохраняет текущее состояние в стек отмены.
        /// </summary>
        private void SaveState()
        {
            if (map == null) return;

            try
            {
                PushWithLimit(undoStack, (Bitmap)map.Clone(), 10);
                redoStack.Clear();
                check = true;
                Undo.Enabled = undoStack.Count > 0;Redo.Enabled = redoStack.Count > 0;
                Redo.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении состояния: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет наличие несохраненных изменений и предлагает пользователю сохранить их.
        /// </summary>
        private bool CheckChangesAndSave()
        {
            if (check == false)
                return true;

            DialogResult result = MessageBox.Show("Изображение было изменено.\nСохранить изменения?","Paint",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Exclamation);

            switch (result)
            {
                case DialogResult.No:
                    return true;

                case DialogResult.Cancel:
                    return false;

                case DialogResult.Yes:
                    check = false;
                    SaveImage(map);
                    return true;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Выполняет операцию отмены.
        /// </summary>
        private void UNDO()
        {
            if (undoStack.Count == 0) return;

            try
            {
                if (map != null)
                {
                    redoStack.Push((Bitmap)map.Clone());
                }

                map = (Bitmap)undoStack.Pop().Clone();
                pictureBox1.Image = map;
                pictureBox1.Refresh();

                Undo.Enabled = undoStack.Count > 0;
                Redo.Enabled = true;
            }
            catch
            {
                undoStack.Clear();
                redoStack.Clear();
                Undo.Enabled = false;
                Redo.Enabled = false;
            }
        }

        /// <summary>
        /// Выполняет операцию повтора.
        /// </summary>
        private void REDO()
        {
            if (redoStack.Count == 0) return;

            if (map != null)
            {
                undoStack.Push((Bitmap)map.Clone());
            }

            map = (Bitmap)redoStack.Pop().Clone(); 
            pictureBox1.Image = map;
            pictureBox1.Refresh();

            Redo.Enabled = redoStack.Count > 0;
            Undo.Enabled = undoStack.Count > 0;Redo.Enabled = redoStack.Count > 0;
        }
    }
}
