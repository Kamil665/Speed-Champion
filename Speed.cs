using System;
using System.Collections.Generic;

namespace WindowsFormsApp3
{
    public struct Line
    {
        public float x, y, z;
        public float X, Y, W;
        public float curve;
        public float scale;
        public int spriteX;           // -1 = left, 1 = right
        public string spriteType;     // "tree", "billboard", "pole"
    }

    internal class Speed
    {
        public List<Line> lines = new List<Line>();
        public int roadW = 2000;
        public int segL = 200;
        public int trackLength = 0;

        public Speed()
        {
            GenerateTrack();
        }

        private void GenerateTrack()
        {
            Random rnd = new Random();
            for (int i = 0; i < 2000; i++)
            {
                Line line = new Line();
                line.z = i * segL;

                // Сглаженные холмы
                line.y = (float)(Math.Sin(i / 20.0) * 600 + Math.Sin(i / 60.0) * 1200);

                // Затяжные повороты
                if (i > 100 && i < 350) line.curve = 2.0f;
                else if (i > 500 && i < 750) line.curve = -3.5f;
                else if (i > 900 && i < 1200) line.curve = 1.5f;
                else if (i > 1400 && i < 1700) line.curve = -2.0f;

                // Генерация декораций по бокам
                if (i % 6 == 0 && i > 15)
                {
                    line.spriteX = (i % 12 == 0) ? -1 : 1;

                    int r = rnd.Next(3);
                    if (r == 0) line.spriteType = "tree";
                    else if (r == 1) line.spriteType = "billboard";
                    else line.spriteType = "pole";
                }

                lines.Add(line);
            }
            trackLength = lines.Count * segL;
        }

        public void Project(ref Line line, float camX, float camY, float camZ, float camDepth, int width, int height)
        {
            float targetZ = line.z - camZ;
            if (targetZ <= 0) targetZ = 1;

            line.scale = camDepth / targetZ;
            line.X = (1 + line.scale * (line.x - camX)) * width / 2;
            line.Y = (1 - line.scale * (line.y - camY)) * height / 2;
            line.W = line.scale * roadW * width / 2;
        }
    }
}