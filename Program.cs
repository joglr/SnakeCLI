﻿using System.Drawing;
using System;
using System.Threading;
using System.Collections.Generic;
using timers = System.Timers;
using System.Text;
using Pastel;

namespace SnakeCLI
{
    class Program
    {
        string floorChar = " ";
        // string floorChar = "·";
        string snakeChar = "■";
        string foodChar = "●";
        string bombChar = "¤";

        Dictionary<string, ConsoleColor> colorMap = new Dictionary<string, ConsoleColor>() {
                {"·", ConsoleColor.DarkGray},
                {"■", ConsoleColor.Green},
                {"●", ConsoleColor.Red},
                {"│", ConsoleColor.Blue},
                {"─", ConsoleColor.Blue},
                {"┘", ConsoleColor.Blue},
                {"└", ConsoleColor.Blue},
                {"┌", ConsoleColor.Blue},
                {"┐", ConsoleColor.Blue},
                {"¤", ConsoleColor.Blue},
        };

        private string[,] board;
        private int boardWidth;
        private int boardHeight;
        private int points;
        private Random random;
        private SpeedCurve speedCurve;
        private int bombPct;
        private bool deathWalls;

        private LinkedList<(int w, int h)> snake;
        private Heading heading;
        private Heading? requestHeading;

        private Thread detectPlayerInput;
        private timers.Timer t;

        static void Main(string[] args)
        {
            int width;
            int height;
            int bombPct;

            if (args.Length == 0) {
                width = 10;
                height = 10;
                bombPct = 15;
            } else {
                if (args.Length < 3)
                {
                    System.Console.WriteLine("ERROR Missing arguments. Arguments should be: <width> <height> <bombPct> <gamemode>?");
                    return;
                }

                try
                {
                    width = int.Parse(args[0]);
                    height = int.Parse(args[1]);
                    bombPct = int.Parse(args[2]);
                }
                catch (Exception)
                {
                    System.Console.WriteLine("ERROR Invalid arguments. Some argument was expected to be an integer, but was not.");
                    return;
                }
            }

            bool deathWalls = false;
            if (args.Length > 3)
            {
                switch (args[3])
                {
                    case "dw":
                        deathWalls = true;
                        break;
                    default:
                        throw new Exception("***Unknown gamemode");
                }
            }
            CurrentProgram = new Program(width, height, bombPct, deathWalls);

            while (true) ;
        }

        public Program(int width, int height, int bombPct, bool deathWalls)
        {
            Console.CursorVisible = false;
            boardWidth = width;
            boardHeight = height;
            board = new string[boardWidth, boardHeight];

            this.bombPct = bombPct;
            this.deathWalls = deathWalls;

            points = 0;
            random = new Random();

            speedCurve = new SpeedCurve(50, 50, 400);

            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    board[x, y] = floorChar;
                }
            }

            snake = new LinkedList<(int x, int y)>();
            var startX = width / 2;
            var startY = height / 2;
            snake.AddFirst((startX, startY));
            snake.AddLast((startX + 1, startY));
            heading = Heading.EAST;

            requestHeading = null;

            SpawnFood();

            ThreadStart ts = DetectInput;
            detectPlayerInput = new Thread(ts);
            detectPlayerInput.Start();

            t = new timers.Timer();
            t.Interval = speedCurve.CalculateY(points);
            t.Elapsed += Update;
            t.Start();

            Console.Clear();
        }

        public void DetectInput()
        {
            while (true)
            {
                var input = Console.ReadKey(false).Key;
                switch (input)
                {
                    case ConsoleKey.Escape:
                        RequestGameOver();
                        break;
                    case ConsoleKey.UpArrow:
                        requestHeading = Heading.NORTH;
                        break;
                    case ConsoleKey.DownArrow:
                        requestHeading = Heading.SOUTH;
                        break;
                    case ConsoleKey.LeftArrow:
                        requestHeading = Heading.WEST;
                        break;
                    case ConsoleKey.RightArrow:
                        requestHeading = Heading.EAST;
                        break;
                }
            }
        }

        static Program CurrentProgram;

    public bool IsGameOver { get; private set; }

    static void OnProcessExit(object sender, EventArgs e) {
            CurrentProgram.RequestGameOver();
        }

        private void SpawnFood()
        {
            bool validCell = false;
            while (!validCell)
            {
                int y = random.Next(boardHeight);
                int x = random.Next(boardWidth);
                var charAtPosition = board[x, y];
                int emptyCount = 0;
                if (isEmpty(x - 1, y)) emptyCount++;
                if (isEmpty(x + 1, y)) emptyCount++;
                if (isEmpty(x, y - 1)) emptyCount++;
                if (isEmpty(x, y + 1)) emptyCount++;
                if (isEmpty(x, y) && emptyCount >= 2)
                {
                    validCell = true;
                    board[x, y] = foodChar;
                }
            }
        }

        private bool isEmpty(int x, int y)
        {
            try
            {
                if (board[x, y] == floorChar) return true;
                else return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void SpawnBomb()
        {
            bool emptyCell = false;
            while (!emptyCell)
            {
                int y = random.Next(boardHeight);
                int x = random.Next(boardWidth);
                var charAtPosition = board[x, y];
                if (charAtPosition == floorChar)
                {
                    emptyCell = true;
                    board[x, y] = bombChar;
                }
            }
        }

        private void RequestGameOver() {
            this.IsGameOver = true;
        }

        private void GameOver()
        {
            try
            {
                t.Stop();
                Console.WriteLine();
                Console.WriteLine("GAME OVER!");
                Console.CursorVisible = true;
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.Error.Write(e.InnerException);
            }
        }

        private void Update(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (IsGameOver) {
                GameOver();
                return;
            }
            if (requestHeading == Heading.NORTH && heading != Heading.SOUTH) heading = Heading.NORTH;
            else if (requestHeading == Heading.SOUTH && heading != Heading.NORTH) heading = Heading.SOUTH;
            else if (requestHeading == Heading.EAST && heading != Heading.WEST) heading = Heading.EAST;
            else if (requestHeading == Heading.WEST && heading != Heading.EAST) heading = Heading.WEST;
            requestHeading = null;

            int nextPositionX = snake.First.Value.w;
            int nextPositionY = snake.First.Value.h;
            switch (heading)
            {
                case Heading.NORTH:
                    nextPositionY -= 1;
                    break;
                case Heading.SOUTH:
                    nextPositionY += 1;
                    break;
                case Heading.EAST:
                    nextPositionX += 1;
                    break;
                case Heading.WEST:
                    nextPositionX -= 1;
                    break;
            }
            (int w, int h) nextPositionTuple = (nextPositionX, nextPositionY);

            int nextPositionXCalculated = Math.Abs(mod(nextPositionX, boardWidth));
            int nextPositionYCalculated = Math.Abs(mod(nextPositionY, boardHeight));
            (int w, int h) nextPositionCalculatedTuple = (nextPositionXCalculated, nextPositionYCalculated);

            var charAtNextPositon = board[nextPositionXCalculated, nextPositionYCalculated];
            bool eating = false;
            if (charAtNextPositon == foodChar) eating = true;
            else if ((deathWalls && IsOutOfBound(nextPositionTuple)) || charAtNextPositon == snakeChar || charAtNextPositon == bombChar) RequestGameOver();

            MoveTo(nextPositionCalculatedTuple, eating);

            Console.SetCursorPosition(0, 0);
            PrintBoard();
        }

        private void MoveTo((int w, int h) nextPosition, bool eating)
        {
            snake.AddFirst(nextPosition);
            board[snake.First.Value.w, snake.First.Value.h] = snakeChar;
            if (!eating)
            {
                board[snake.Last.Value.w, snake.Last.Value.h] = floorChar;
                snake.RemoveLast();
            }
            else
            {
                points++;
                SpeedUp();
                SpawnFood();
                if (random.Next(100) < bombPct) SpawnBomb();
            }
        }

        private bool IsOutOfBound((int w, int h) nextPosition)
        {
            if (nextPosition.w < 0 || nextPosition.w > boardWidth - 1 || nextPosition.h < 0 || nextPosition.h > boardHeight - 1) return true;
            return false;
        }

        private void SpeedUp()
        {
            t.Interval = speedCurve.CalculateY(points);
        }

        private void PrintBoard()
        {
            if (deathWalls)
            {
                WritePiece("┌", WriteMode.Single);
                WritePiece("─", WriteMode.Repeat, boardWidth * 2 + 1);
                WritePiece("┐");
                Console.WriteLine();
            }
            for (int y = 0; y < boardHeight; y++)
            {
                if (deathWalls) WritePiece("│");
                for (int x = 0; x < boardWidth; x++)
                {
                    WritePiece(board[x, y]);
                }
                if (deathWalls) WritePiece("│", WriteMode.Single);
                Console.WriteLine();
            }
            if (deathWalls)
            {
                WritePiece("└", WriteMode.Single);
                WritePiece("─", WriteMode.Repeat, boardWidth * 2 + 1);
                WritePiece("┘");
            }
            Console.WriteLine();
            Console.Write("Score: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(points);
            Console.ResetColor();

            Console.WriteLine();
            Console.Write("TBU: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Math.Round(Convert.ToDecimal(speedCurve.CalculateY(points)), 2));
            Console.ResetColor();

            Console.WriteLine();
        }

    private void WritePiece(string v, WriteMode writeMode = WriteMode.Spaced, int times = 1)
    {
        if (colorMap.TryGetValue(v, out ConsoleColor color))
        {
            // v = v.Pastel(Color.FromName(color.ToString()));
            Console.ForegroundColor = color;
        }
        if (writeMode == WriteMode.Single) Console.Write(v);
        if (writeMode == WriteMode.Double) Console.Write(v + v);
        if (writeMode == WriteMode.Spaced) Console.Write(v + " ");
        if (writeMode == WriteMode.Repeat) {
            for (int x = 0; x < times; x++)
            {
                Console.Write(v);
            }
        }
        Console.ResetColor();
    }

    enum WriteMode {
        Single,
        Spaced,
        Double,
        Repeat
    }

    int mod(int x, int m) { return (x % m + m) % m; }
    }

    public enum Heading
    {
        NORTH,
        SOUTH,
        WEST,
        EAST
    }
}
