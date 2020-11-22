﻿using System;
using System.Threading;
using System.Collections.Generic;
using timers = System.Timers;

namespace SnakeCLI
{
    class Program
    {
        char floorChar = (char) 79;
        char snakeChar = (char) 35;
        char foodChar = (char) 64;

        private char[,] board;
        private int boardWidth;
        private int boardHeight;
        private int points;
        private Random random;

        private LinkedList<(int w, int h)> snake;
        private Heading heading;
        private Heading? requestHeading;

        private Thread detectPlayerInput;
        private timers.Timer t;

        public Program()
        {
            boardWidth = 15;
            boardHeight = 10;
            board = new char[boardWidth, boardHeight];

            points = 0;
            random = new Random();

            for(int x = 0; x < boardWidth; x++) {
                for(int y = 0; y < boardHeight; y++) {
                    board[x,y] = floorChar;
                }
            }

            snake = new LinkedList<(int x, int y)>();
            snake.AddFirst((4,5));
            snake.AddLast((5,5));
            heading = Heading.EAST;
            requestHeading = null;

            SpawnFood();

            ThreadStart ts = DetectInput;
            detectPlayerInput = new Thread(ts);
            detectPlayerInput.Start();

            t = new timers.Timer();
            t.Interval = 500;
            t.Elapsed += Update;
            t.Start();
        }

        public void DetectInput()
        {
            while(true) {
                var input = Console.ReadKey(false).Key;
                switch(input)
                {
                    case ConsoleKey.Escape:
                        GameOver();
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

        static void Main(string[] args)
        {
            Program program = new Program();
            

            while(true);
        }

        private void SpawnFood()
        {
            bool emptyCell = false;
            while(!emptyCell)
            {
                int y = random.Next(boardHeight);
                int x = random.Next(boardWidth);
                var charAtPosition = board[x,y];
                if(charAtPosition == floorChar)
                {
                    emptyCell = true;
                    board[x,y] = foodChar;
                }
            }
        }

        private void GameOver()
        {
            try
            {
                t.Stop();
                Console.WriteLine();
                Console.WriteLine("GAMEOVER!");
                Console.WriteLine("SCORE: " + points);
                Environment.Exit(0);
            } catch (Exception e) {
                Console.Error.Write(e.InnerException);
            }
        }

        private void Update(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (requestHeading == Heading.NORTH && heading != Heading.SOUTH) heading = Heading.NORTH;
            else if (requestHeading == Heading.SOUTH && heading != Heading.NORTH) heading = Heading.SOUTH;
            else if (requestHeading == Heading.EAST && heading != Heading.WEST) heading = Heading.EAST;
            else if (requestHeading == Heading.WEST && heading != Heading.EAST) heading = Heading.WEST;
            requestHeading = null;

            int nextPositionX = snake.First.Value.w;
            int nextPositionY = snake.First.Value.h;
            switch(heading)
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

            char charAtNextPositon = board[nextPositionXCalculated, nextPositionYCalculated];
            bool eating = false;
            if(charAtNextPositon == foodChar) eating = true;
            else if (charAtNextPositon == snakeChar) GameOver();

            MoveTo(nextPositionCalculatedTuple, eating);

            Console.Clear();
            PrintBoard();
        }

        private void MoveTo((int w, int h) nextPosition, bool eating)
        {
            snake.AddFirst(nextPosition);
            board[snake.First.Value.w,snake.First.Value.h] = snakeChar;
            if(!eating)
            {
                board[snake.Last.Value.w,snake.Last.Value.h] = floorChar;
                snake.RemoveLast();
            }
            else
            {
                points++;
                //SpeedUp();
                SpawnFood();
                //if(random.Next(100) < riskOfBomb) SpawnGameObject(GameObjectEnum.BOMB);
            }
        }

        private void PrintBoard()
        {
            for(int y = 0; y < boardHeight; y++) {
                for(int x = 0; x < boardWidth; x++) {
                    Console.Write(board[x,y]);
                }
                Console.WriteLine();
            }
        }

        int mod(int x, int m) {return (x%m + m)%m;}
    }

    public enum Heading
    {
        NORTH,
        SOUTH,
        WEST,
        EAST
    }
}
