using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Media;

namespace WindowsFormsApp3
{
    public partial class Form1 : Form
    {
        private Timer gameTimer = new Timer();
        private Speed track = new Speed();

        private float speed = 0;
        private float maxSpeed = 320;
        private float accel = 6.5f;
        private float breaking = 18f;
        private float decel = 3.2f;
        private float nitro = 0;

        private float pos = 800;
        private int currentLap = 1;
        private float playerX = 0f;

        private bool keyLeft, keyRight, keyUp, keyDown, keyNitro;

        // AI машины
        private List<AICar> aiCars = new List<AICar>();
        private Random rnd = new Random();

        private SoundPlayer engineSound;

        public Form1()
        {
            InitializeComponent();
            this.Text = "Speed Champion";
            this.Width = 1280;
            this.Height = 800;
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.CenterScreen;

            this.KeyDown += OnKeyDown;
            this.KeyUp += OnKeyUp;
            this.KeyPreview = true;

            // Создаём AI
            for (int i = 0; i < 6; i++)
            {
                aiCars.Add(new AICar { pos = pos - 800 - i * 600, lane = (rnd.Next(3) - 1) * 0.7f, speed = 140 + rnd.Next(80) });
            }

            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            // Простой звук двигателя (можно заменить на wav)
            // engineSound = new SoundPlayer("engine.wav");
        }

        private void GameLoop(object sender, EventArgs e)
        {
            // Управление
            if (keyUp) speed = Math.Min(speed + accel, maxSpeed);
            else if (keyDown) speed = Math.Max(speed - breaking, -80);
            else speed = Math.Max(speed - decel, 0);

            if (keyNitro && nitro > 0)
            {
                speed = Math.Min(speed + 12, maxSpeed + 60);
                nitro -= 0.8f;
            }

            float speedFactor = speed / maxSpeed;

            // Руление
            float target = 0;
            if (keyLeft) target = -1;
            if (keyRight) target = 1;

            playerX += (target - playerX) * 0.18f * (1 + speedFactor * 0.6f);

            // Ограничение
            playerX = Math.Max(-2.8f, Math.Min(2.8f, playerX));

            pos += speed;

            // Лапы
            if (pos >= track.trackLength)
            {
                pos -= track.trackLength;
                currentLap++;
            }

            UpdateAICars();
            CheckCollisions();

            this.Invalidate();
        }

        private void UpdateAICars()
        {
            foreach (var car in aiCars)
            {
                car.pos += car.speed;
                if (car.pos > track.trackLength) car.pos -= track.trackLength;

                // Простой AI: подстраивается под игрока
                if (Math.Abs(car.pos - pos) < 1200)
                    car.lane += (playerX * 0.6f - car.lane) * 0.03f;
            }
        }

        private void CheckCollisions()
        {
            foreach (var car in aiCars)
            {
                if (Math.Abs(car.pos - pos) < 180 && Math.Abs(car.lane - playerX) < 0.45f)
                {
                    speed *= 0.4f; // удар
                    playerX += (playerX - car.lane) * 0.8f;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.FromArgb(100, 180, 255));

            int startPos = (int)(pos / track.segL);
            float camH = 1400 + track.lines[startPos % track.lines.Count].y;

            float x = 0, dx = 0;

            // Предрасчёт кривой
            for (int i = 0; i < startPos + 280; i++)
                if (i < track.lines.Count) dx += track.lines[i].curve;

            for (int i = startPos + 280; i > startPos; i--)
            {
                int idx = i % track.lines.Count;
                Line l = track.lines[idx];
                Line p = track.lines[(i - 1) % track.lines.Count];

                float camX = playerX * track.roadW - x;

                track.Project(ref l, camX, camH, pos, 0.85f, Width, Height);
                track.Project(ref p, camX - dx, camH, pos, 0.85f, Width, Height);

                Color grass = (i / 3 % 2 == 0) ? Color.DarkGreen : Color.ForestGreen;
                Color road = (i / 3 % 2 == 0) ? Color.FromArgb(50, 50, 55) : Color.FromArgb(65, 65, 70);
                Color rumble = (i / 3 % 2 == 0) ? Color.Red : Color.White;

                DrawTrapezoid(g, grass, 0, p.Y, Width, 0, l.Y, Width);
                DrawTrapezoid(g, rumble, p.X - p.W * 1.25f, p.Y, p.W * 2.5f, l.X - l.W * 1.25f, l.Y, l.W * 2.5f);
                DrawTrapezoid(g, road, p.X, p.Y, p.W, l.X, l.Y, l.W);

                // Центральная разметка
                if (i % 6 == 0)
                    DrawTrapezoid(g, Color.White, p.X - 12, p.Y, 24, l.X - 12, l.Y, 24);

                // Спрайты
                if (l.spriteX != 0 && l.scale > 0.1f)
                {
                    float spriteScreenX = l.X + l.spriteX * (l.W + 80);
                    DrawSprite(g, l.spriteType, spriteScreenX, l.Y, l.scale);
                }

                x += dx;
                dx -= l.curve;
            }

            DrawAICars(g, startPos);
            DrawPlayerCar(g);

            // HUD
            DrawHUD(g);
        }

        private void DrawSprite(Graphics g, string type, float x, float y, float scale)
        {
            int size = (int)(180 * scale);
            if (size < 8) return;

            if (type == "tree")
                g.FillRectangle(Brushes.DarkGreen, x - size / 2, y - size, size, size * 1.4f);
            else if (type == "billboard")
                g.FillRectangle(Brushes.OrangeRed, x - size / 2, y - size * 0.7f, size, size * 0.6f);
            else
                g.FillRectangle(Brushes.Gray, x - size / 3, y - size, size * 0.6f, size);
        }

        private void DrawAICars(Graphics g, int startPos)
        {
            foreach (var car in aiCars)
            {
                int idx = (int)(car.pos / track.segL) % track.lines.Count;
                if (idx < 0 || idx >= track.lines.Count) continue;

                Line line = track.lines[idx];
                float screenY = line.Y;
                if (screenY > Height - 100) continue;

                float screenX = Width / 2 + (car.lane - playerX) * line.W * 0.8f;

                // Простая машина
                g.FillRectangle(Brushes.Blue, screenX - 35 * line.scale, screenY - 60 * line.scale, 70 * line.scale, 45 * line.scale);
                g.FillRectangle(Brushes.DarkBlue, screenX - 28 * line.scale, screenY - 75 * line.scale, 56 * line.scale, 25 * line.scale);
            }
        }

        private void DrawPlayerCar(Graphics g)
        {
            // Более крутая машина (как в NFS)
            float cx = Width / 2 - 75;
            float cy = Height - 170;

            g.FillRectangle(Brushes.Black, cx + 15, cy + 55, 45, 38); // колёса
            g.FillRectangle(Brushes.Black, cx + 105, cy + 55, 45, 38);

            g.FillRectangle(Brushes.Lime, cx + 8, cy + 12, 134, 68);           // кузов
            g.FillRectangle(Brushes.DarkGreen, cx + 25, cy - 8, 100, 38);      // крыша
            g.FillRectangle(Brushes.Cyan, cx + 38, cy + 8, 74, 22);            // стекло
        }

        private void DrawTrapezoid(Graphics g, Color c, float x1, float y1, float w1, float x2, float y2, float w2)
        {
            PointF[] pts = {
                new PointF(x1 - w1, y1),
                new PointF(x2 - w2, y2),
                new PointF(x2 + w2, y2),
                new PointF(x1 + w1, y1)
            };
            using (SolidBrush b = new SolidBrush(c))
                g.FillPolygon(b, pts);
        }

        private void DrawHUD(Graphics g)
        {
            using (Font f = new Font("Impact", 24, FontStyle.Bold))
            {
                g.DrawString($"SPEED: {(int)speed} km/h", f, Brushes.Yellow, 30, 20);
                g.DrawString($"LAP {currentLap}", f, Brushes.White, Width - 220, 20);
                g.DrawString($"NITRO: {(int)nitro}%", f, Brushes.Orange, 30, 70);
            }
        }

        // Клавиши
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.A: keyLeft = true; break;
                case Keys.D: keyRight = true; break;
                case Keys.W: keyUp = true; break;
                case Keys.S: keyDown = true; break;
                case Keys.Space: keyNitro = true; if (nitro <= 0) nitro = 100; break;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.A: keyLeft = false; break;
                case Keys.D: keyRight = false; break;
                case Keys.W: keyUp = false; break;
                case Keys.S: keyDown = false; break;
                case Keys.Space: keyNitro = false; break;
            }
        }
    }

    public class AICar
    {
        public float pos;
        public float lane;
        public float speed;
    }
}