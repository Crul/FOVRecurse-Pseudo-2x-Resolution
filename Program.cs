using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace FOVRecurse_Pseudo_2x_Resolution
{
    class Program
    {
        private const string MAP_FILE = "map.txt";

        static void Main()
        {
            var map = ReadMap();
            var fov = GetFOV(map);
            var position = GetPlayerPosition(map);
            fov.movePlayer(position.x, position.y);

            while (true)
            {
                PrintMap(map, fov, position);

                var pressedKey = Console.ReadKey(true).Key;
                switch (pressedKey)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.RightArrow:
                        position = MovePlayer(map, position, pressedKey);
                        fov.setPlayer(position.x, position.y);
                        break;

                    case ConsoleKey.Add:
                    case ConsoleKey.OemPlus:
                        fov.VisualRange++;
                        fov.setPlayer(position.x, position.y);
                        break;

                    case ConsoleKey.Subtract:
                    case ConsoleKey.OemMinus:
                        if (fov.VisualRange > 1)
                        {
                            fov.VisualRange--;
                            fov.setPlayer(position.x, position.y);
                        }
                        break;

                    case ConsoleKey.Escape:
                    case ConsoleKey.Q:
                        return;
                }
            }
        }

        /// <summary>
        /// Reads the map from the file
        /// </summary>
        /// <returns>A 2 dimensional boolean array[x,y]. TRUE = tile, FALSE = wall</returns>
        private static bool[,] ReadMap()
        {
            var mapText = File.ReadAllLines(MAP_FILE);
            if (mapText.Length == 0)
                throw new Exception($"Empty map file: {MAP_FILE}");

            var width = mapText[0].Length;
            var height = mapText.Length;

            var map = new bool[width, height];
            for (var y = 0; y < height; y++)
            {
                var line = mapText[y];
                for (var x = 0; x < width; x++)
                {
                    map[x, y] = line.Substring(x, 1) != "#";
                }
            }

            return map;
        }

        /// <summary>
        /// Creates and initializes the FOVRecurse object
        /// </summary>
        /// <returns>FOVRecurse instance</returns>
        private static FOVRecurse GetFOV(bool[,] map)
        {
            var fov = new FOVRecurse(map.GetLength(0), map.GetLength(1));
            for (int x = 0; x < map.GetLength(0); x++)
                for (int y = 0; y < map.GetLength(1); y++)
                    fov.Point_Set(x, y, map[x, y] ? 0 : 1);

            return fov;
        }

        /// <summary>
        /// Selects a free tile to position the player
        /// </summary>
        /// <returns>A tuple with (x, y) tile position</returns>
        private static (int x, int y) GetPlayerPosition(bool[,] map)
        {
            for (int x = 0; x < map.GetLength(0); x++)
                for (int y = 0; y < map.GetLength(1); y++)
                    if (map[x, y])
                        return (x, y);

            throw new Exception("No tiles found in map");
        }

        /// <summary>
        /// Prints the map applying FOV
        /// </summary>
        private static void PrintMap(bool[,] map, FOVRecurse fov, (int x, int y) position)
        {
            var mapText = string.Join(
                Environment.NewLine,
                new string[]
                {
                    "Controls:",
                    "  [ ESC / Q ]: exit",
                    "  [ arrows ]:  move player",
                    "  [ + / - ]:   change visibility range",
                    string.Empty,
                    "Note: tiles are 2 characters wide",
                    string.Empty,
                    $"Visual range: {fov.VisualRange}",
                    string.Empty
                }
                .Select(line => $"\t{line}")
            );

            for (int y = 0; y < map.GetLength(1); y++)
            {
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    mapText += (x, y) == position
                       ? "@@"
                       : (
                           !fov.VisiblePoints.Contains(new Point(x, y))
                           ? "  "
                           : (map[x, y] ? ".." : "##")
                       );
                }
                mapText += Environment.NewLine;
            }

            Console.Clear();
            Console.Write(mapText);
        }

        /// <summary>
        /// Moves player in specified direction if possible
        /// </summary>
        /// <returns>The new player position</returns>
        private static (int x, int y) MovePlayer(
            bool[,] map, (int x, int y) position, ConsoleKey direction)
        {
            switch (direction)
            {
                case ConsoleKey.UpArrow:
                    if (map[position.x, position.y - 1])
                        position = (position.x, position.y - 1);
                    break;

                case ConsoleKey.DownArrow:
                    if (map[position.x, position.y + 1])
                        position = (position.x, position.y + 1);
                    break;

                case ConsoleKey.LeftArrow:
                    if (map[position.x - 1, position.y])
                        position = (position.x - 1, position.y);
                    break;

                case ConsoleKey.RightArrow:
                    if (map[position.x + 1, position.y])
                        position = (position.x + 1, position.y);
                    break;
            }

            return position;
        }
    }
}
