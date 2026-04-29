using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Text.Json;
using ScottPlot;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Count(x => x.IsAlive);
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;
        public int Columns => Cells.GetLength(0);
        public int Rows => Cells.GetLength(1);
        public int Width => Columns * CellSize;
        public int Height => Rows * CellSize;

        public Board(int width, int height, int cellSize, double liveDensity = 0.1)
        {
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();
            ConnectNeighbors();
            Randomize(liveDensity);
        }

        // приватный конструктор для загрузки (без случайного заполнения)
        private Board(int cols, int rows, int cellSize, bool connectNeighbors)
        {
            CellSize = cellSize;
            Cells = new Cell[cols, rows];
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                    Cells[x, y] = new Cell();
            if (connectNeighbors)
                ConnectNeighbors();
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        // Сохранение состояния в файл
        public void Save(string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.WriteLine(Columns);
                sw.WriteLine(Rows);
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < Columns; x++)
                        sw.Write(Cells[x, y].IsAlive ? '1' : '0');
                    sw.WriteLine();
                }
            }
        }

        // Загрузка состояния из файла
        public static Board Load(string path, int cellSize = 1)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                int cols = int.Parse(sr.ReadLine());
                int rows = int.Parse(sr.ReadLine());
                Board board = new Board(cols, rows, cellSize, true);
                for (int y = 0; y < rows; y++)
                {
                    string line = sr.ReadLine();
                    for (int x = 0; x < cols; x++)
                    {
                        if (x < line.Length)
                            board.Cells[x, y].IsAlive = line[x] == '1';
                    }
                }
                return board;
            }
        }

        // Загрузка фигуры из файла (O – живая, . – мёртвая)
        public void LoadPattern(string filePath, int startX, int startY)
        {
            string[] lines = File.ReadAllLines(filePath);
            for (int y = 0; y < lines.Length; y++)
            {
                string line = lines[y].TrimEnd();
                for (int x = 0; x < line.Length; x++)
                {
                    if (line[x] == 'O')
                    {
                        int bx = startX + x;
                        int by = startY + y;
                        if (bx >= 0 && bx < Columns && by >= 0 && by < Rows)
                            Cells[bx, by].IsAlive = true;
                    }
                }
            }
        }

        // Количество живых клеток
        public int CountAlive()
        {
            int count = 0;
            foreach (var cell in Cells)
                if (cell.IsAlive) count++;
            return count;
        }

        // Поиск связных компонент (комбинаций живых клеток)
        public List<List<CellPos>> GetComponents()
        {
            bool[,] visited = new bool[Columns, Rows];
            List<List<CellPos>> comps = new List<List<CellPos>>();
            int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (Cells[x, y].IsAlive && !visited[x, y])
                    {
                        List<CellPos> comp = new List<CellPos>();
                        Stack<CellPos> stack = new Stack<CellPos>();
                        stack.Push(new CellPos(x, y));
                        visited[x, y] = true;
                        while (stack.Count > 0)
                        {
                            CellPos p = stack.Pop();
                            comp.Add(p);
                            for (int d = 0; d < 8; d++)
                            {
                                int nx = (p.X + dx[d] + Columns) % Columns;  // тороидальное поле
                                int ny = (p.Y + dy[d] + Rows) % Rows;
                                if (Cells[nx, ny].IsAlive && !visited[nx, ny])
                                {
                                    visited[nx, ny] = true;
                                    stack.Push(new CellPos(nx, ny));
                                }
                            }
                        }
                        comps.Add(comp);
                    }
                }
            }
            return comps;
        }
    }

    // Простой класс для хранения координат (без кортежей)
    public class CellPos
    {
        public int X;
        public int Y;
        public CellPos(int x, int y) { X = x; Y = y; }
    }

    // Настройки из config.json
    public class Settings
    {
        public int Width { get; set; } = 50;
        public int Height { get; set; } = 20;
        public int CellSize { get; set; } = 1;
        public double LiveDensity { get; set; } = 0.5;
    }

    // Классификация устойчивых фигур
    public static class PatternRecognizer
    {
        // Эталоны (относительные координаты)
        private static List<List<CellPos>> patterns = new List<List<CellPos>>
        {
            new List<CellPos> { new(0,0), new(1,0), new(0,1), new(1,1) },                         // block
            new List<CellPos> { new(1,0), new(2,0), new(0,1), new(3,1), new(1,2), new(2,2) },     // beehive
            new List<CellPos> { new(1,0), new(2,0), new(0,1), new(3,1), new(0,2), new(2,2), new(1,3) }, // loaf
            new List<CellPos> { new(0,0), new(1,0), new(0,1), new(2,1), new(1,2) },               // boat
            new List<CellPos> { new(1,0), new(0,1), new(2,1), new(1,2) },                         // tub            
        };
        // private static string[] names = { "Block", "Beehive", "Loaf", "Boat", "Tub", "Ship" };
        private static string[] names = { "Block", "Beehive", "Loaf", "Boat", "Tub" };

        public static string Classify(List<CellPos> cells)
        {
            if (cells.Count == 0) return "Empty";
            // Нормализуем к началу координат
            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (var c in cells) { if (c.X < minX) minX = c.X; if (c.Y < minY) minY = c.Y; }
            List<CellPos> norm = new List<CellPos>();
            foreach (var c in cells) norm.Add(new CellPos(c.X - minX, c.Y - minY));
            norm.Sort((a, b) => a.X == b.X ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

            for (int i = 0; i < patterns.Count; i++)
            {
                if (MatchPattern(norm, patterns[i])) return names[i];
            }
            return "Unknown";
        }

        private static bool MatchPattern(List<CellPos> norm, List<CellPos> pattern)
        {
            if (norm.Count != pattern.Count) return false;
            // 4 поворота
            for (int rot = 0; rot < 4; rot++)
            {
                List<CellPos> rotated = Rotate(pattern, rot);
                int minX = rotated.Min(p => p.X);
                int minY = rotated.Min(p => p.Y);
                List<CellPos> shifted = new List<CellPos>();
                foreach (var p in rotated) shifted.Add(new CellPos(p.X - minX, p.Y - minY));
                shifted.Sort((a, b) => a.X == b.X ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
                if (AreEqual(shifted, norm)) return true;
            }
            return false;
        }

        private static List<CellPos> Rotate(List<CellPos> pattern, int times)
        {
            List<CellPos> result = new List<CellPos>(pattern);
            for (int i = 0; i < times; i++)
            {
                List<CellPos> rotated = new List<CellPos>();
                foreach (var p in result) rotated.Add(new CellPos(-p.Y, p.X));
                result = rotated;
            }
            return result;
        }

        private static bool AreEqual(List<CellPos> a, List<CellPos> b)
        {
            for (int i = 0; i < a.Count; i++)
                if (a[i].X != b[i].X || a[i].Y != b[i].Y) return false;
            return true;
        }
    }

    class Program
    {
        static Board board;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Игра \"Жизнь\" ===");
                Console.WriteLine("1. Новая случайная игра (из config.json)");
                Console.WriteLine("2. Загрузить сохранённую игру из файла");
                Console.WriteLine("3. Исследование (график стабильности)");
                Console.WriteLine("4. Загрузить фигуру \"Block\"");
                Console.WriteLine("5. Загрузить фигуру \"Beehive\"");
                Console.WriteLine("6. Загрузить фигуру \"Loaf\"");
                Console.WriteLine("7. Загрузить фигуру \"Boat\"");
                Console.WriteLine("8. Загрузить фигуру \"Tub\"");
                Console.WriteLine("0. Выход");
                Console.Write("Выбор: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        board = LoadFromConfig();
                        GameLoop();
                        break;
                    case "2":
                        Console.Write("Путь к файлу сохранения: ");
                        string loadPath = Console.ReadLine();
                        if (File.Exists(loadPath))
                        {
                            board = Board.Load(loadPath);
                            GameLoop();
                        }
                        else Console.WriteLine("Файл не найден. Нажмите Enter...");
                        Console.ReadLine();
                        break;
                    case "3":
                        RunResearch();
                        Console.WriteLine("Готово. Нажмите Enter...");
                        Console.ReadLine();
                        break;
                    case "4": LoadAndRunPattern("patterns/block.txt"); break;
                    case "5": LoadAndRunPattern("patterns/beehive.txt"); break;
                    case "6": LoadAndRunPattern("patterns/loaf.txt"); break;
                    case "7": LoadAndRunPattern("patterns/boat.txt"); break;
                    case "8": LoadAndRunPattern("patterns/tub.txt"); break;
                    case "0": return;
                    default: continue;
                }
            }
        }

        static Board LoadFromConfig()
        {
            if (!File.Exists("config.json"))
                return new Board(50, 20, 1, 0.3);
            string json = File.ReadAllText("config.json");
            Settings s = JsonSerializer.Deserialize<Settings>(json);
            return new Board(s.Width, s.Height, s.CellSize, s.LiveDensity);
        }

        static void LoadAndRunPattern(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"Файл {path} не найден.");
                Console.ReadKey();
                return;
            }
            board = new Board(50, 20, 1, 0);  // пустое поле подходящего размера
            board.LoadPattern(path, 5, 5);     // размещаем фигуру в центре (примерно)
            GameLoop();
        }

        // Главный игровой цикл
        static void GameLoop()
        {
            bool paused = false;
            bool running = true;
            Console.Clear();
            Render();
            Console.WriteLine("Управление: [Space] пауза/продолжить, [S] сохранить, [Q] выход в меню");
            while (running)
            {
                if (!paused)
                {
                    board.Advance();
                    Console.Clear();
                    Render();
                    Console.WriteLine("Управление: [Space] пауза/продолжить, [S] сохранить, [Q] выход в меню");
                    Thread.Sleep(200);
                }
                else
                {
                    Thread.Sleep(50);
                }

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.Spacebar:
                            paused = !paused;
                            if (paused) Console.WriteLine("Пауза. Нажмите Space для продолжения.");
                            break;
                        case ConsoleKey.S:
                            Console.Write("Имя файла для сохранения: ");
                            string fname = Console.ReadLine();
                            board.Save(fname);
                            Console.WriteLine("Сохранено. Нажмите любую клавишу для продолжения...");
                            Console.ReadKey();
                            break;
                        case ConsoleKey.Q:
                            running = false;
                            break;
                    }
                }
            }
        }

        static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                    Console.Write(board.Cells[col, row].IsAlive ? '*' : ' ');
                Console.WriteLine();
            }
        }

        // Исследование: график числа поколений до стабильности
        static void RunResearch()
        {
            const int stablePeriod = 10;
            const int maxGens = 2000;
            const int trials = 100;
            double[] densities = { 0.1, 0.15, 0.2, 0.25, 0.3, 0.35, 0.4, 0.45, 0.5, 0.55, 0.6, 0.65, 0.7, 0.75, 0.8, 0.85, 0.9 };
            var results = new List<double>();

            foreach (double dens in densities)
            {
                long totalGen = 0;
                for (int t = 0; t < trials; t++)
                {
                    Board b = new Board(50, 20, 1, dens);
                    int prevCount = b.CountAlive();
                    int stableCount = 0;
                    int gen = 0;
                    for (gen = 0; gen < maxGens; gen++)
                    {
                        b.Advance();
                        int curCount = b.CountAlive();
                        if (curCount == prevCount) stableCount++;
                        else stableCount = 0;
                        prevCount = curCount;
                        if (stableCount >= stablePeriod) break;
                    }
                    totalGen += (gen == maxGens) ? maxGens : gen;
                }
                double avg = totalGen / (double)trials;
                results.Add(avg);
                Console.WriteLine($"Плотность {dens:F2}: среднее число поколений = {avg:F1}");
            }

            // Путь к папке Data в корне проекта (три уровня вверх от bin/Debug)
            string projectDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "C:\\Users\\Tolya\\source\\repos\\mod-lab05-life"));
            string dataFolder = Path.Combine(projectDir, "Data");
            Directory.CreateDirectory(dataFolder);
            string dataPath = Path.Combine(dataFolder, "data.txt");
            string plotPath = Path.Combine(dataFolder, "plot.png");

            try
            {
                using (StreamWriter writer = new StreamWriter(dataPath))
                {
                    writer.WriteLine("Density\tAvgGenerations");
                    for (int i = 0; i < densities.Length; i++)
                        writer.WriteLine($"{densities[i]}\t{results[i]}");
                }
                Console.WriteLine($"Текстовые данные сохранены в {dataPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении data.txt: {ex.Message}");
            }

            try
            {
                var plot = new ScottPlot.Plot();
                var scatter = plot.Add.Scatter(densities, results.ToArray());
                plot.XLabel("Плотность заполнения");
                plot.YLabel("Среднее число поколений до стабильности");
                plot.Title("Переход в стабильную фазу");
                plot.SavePng(plotPath, 800, 600);
                Console.WriteLine($"График сохранён в {plotPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении графика: {ex.Message}");
            }
        }
    }
}