// original code by Andy Stobirski: https://github.com/AndyStobirski/RogueLike
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace FOVRecurse_Pseudo_2x_Resolution
{

    /// <summary>
    /// Implementation of "FOV using recursive shadowcasting - improved" as
    /// described on http://roguebasin.roguelikedevelopment.org/index.php?title=FOV_using_recursive_shadowcasting_-_improved
    /// 
    /// The FOV code is contained in the region "FOV Algorithm".
    /// The method GetVisibleCells() is called to calculate the cells
    /// visible to the player by examing each octant sequantially. 
    /// The generic list VisiblePoints contains the cells visible to the player.
    /// 
    /// GetVisibleCells() is called everytime the player moves, and the event playerMoved
    /// is called when a successful move is made (the player moves into an empty cell)
    /// 
    /// </summary>
    public class FOVRecurse
    {
        public Size MapSize { get; set; }
        public int[,] map { get; private set; }

        /// <summary>
        /// Radius of the player's circle of vision
        /// </summary>
        public int VisualRange { get; set; }

        private bool[,] subTilesVisibility;

        private List<(int x, int y)> subTileIndexList =
            new List<(int, int)> { (0, 0), (0, 1), (1, 0), (1, 1) };

        private Point player;
        public Point Player { get { return player; } set { player = value; } }

        /// <summary>
        /// The octants which a player can see
        /// </summary>
        List<int> VisibleOctants = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8 };

        public FOVRecurse(int mapWidth, int mapHeight)
        {
            MapSize = new Size(mapWidth, mapHeight);
            map = new int[MapSize.Width, MapSize.Height];
            VisualRange = 5;
        }

        /// <summary>
        /// Returns the visibility of a sub-tile
        /// Each main-tile has 4 sub-tiles
        /// E.g.: main-tile (x: 13, y = 7) has 4 subtiles:
        ///     - sub-tile (x: 26, y = 14)
        ///     - sub-tile (x: 27, y = 14)
        ///     - sub-tile (x: 26, y = 15)
        ///     - sub-tile (x: 27, y = 15)
        /// </summary>
        /// <param name="x">sub-tile x position</param>
        /// <param name="y">sub-tile y position</param>
        /// <returns>True = visible / False = hidden</returns>
        public bool IsSubTileVisible(int x, int y)
        {
            return subTilesVisibility[x, y];
        }

        /// <summary>
        /// Move the player in the specified direction provided the cell is valid and empty
        /// </summary>
        /// <param name="pX">X offset</param>
        /// <param name="pY">Y Offset</param>
        public void movePlayer(int pX, int pY)
        {
            if (Point_Valid(player.X + pX, player.Y + pY)
                && Point_Get(player.X + pX, player.Y + pY) == 0)
            {
                player.Offset(pX, pY);
                GetVisibleCells();
                playerMoved?.Invoke();
            }
        }

        /// <summary>
        /// Move the player to the specified position provided the cell is valid and empty
        /// </summary>
        /// <param name="pX">X offset</param>
        /// <param name="pY">Y Offset</param>
        public void setPlayer(int x, int y)
        {
            if (Point_Valid(x, y) && Point_Get(x, y) == 0)
            {
                player = new Point(x, y);
                GetVisibleCells();
                playerMoved?.Invoke();
            }
        }

        #region map point code

        /// <summary>
        /// Check if the provided coordinate is within the bounds of the mapp array
        /// </summary>
        /// <param name="pX"></param>
        /// <param name="pY"></param>
        /// <returns></returns>
        private bool Point_Valid(int pX, int pY)
        {
            return pX >= 0 & pX < map.GetLength(0)
                    & pY >= 0 & pY < map.GetLength(1);
        }

        /// <summary>
        /// Get the value of the point at the specified location
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <returns>Cell value</returns>
        public int Point_Get(int _x, int _y)
        {
            return map[_x, _y];
        }

        /// <summary>
        /// Set the map point to the specified value
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_val"></param>
        public void Point_Set(int _x, int _y, int _val)
        {
            if (Point_Valid(_x, _y))
                map[_x, _y] = _val;
        }

        #endregion

        #region FOV algorithm

        //  Octant data
        //
        //    \ 1 | 2 /
        //   8 \  |  / 3
        //   -----+-----
        //   7 /  |  \ 4
        //    / 6 | 5 \
        //
        //  1 = NNW, 2 =NNE, 3=ENE, 4=ESE, 5=SSE, 6=SSW, 7=WSW, 8 = WNW

        /// <summary>
        /// Start here: go through all the octants which surround the player to
        /// determine which open cells are visible
        /// </summary>
        public void GetVisibleCells()
        {
            subTilesVisibility = new bool[MapSize.Width * 2, MapSize.Height * 2];
            ShowAllSubTiles(player.X, player.Y);

            foreach (int o in VisibleOctants)
                ScanOctant(1, o, 1.0, 0.0);

        }

        /// <summary>
        /// Examine the provided octant and calculate the visible cells within it.
        /// </summary>
        /// <param name="pDepth">Depth of the scan</param>
        /// <param name="pOctant">Octant being examined</param>
        /// <param name="pStartSlope">Start slope of the octant</param>
        /// <param name="pEndSlope">End slope of the octance</param>
        protected void ScanOctant(int pDepth, int pOctant, double pStartSlope, double pEndSlope)
        {

            int visrange2 = VisualRange * VisualRange;
            int x = 0;
            int y = 0;

            switch (pOctant)
            {

                case 1: //nnw
                    y = player.Y - pDepth;
                    if (y < 0) return;

                    x = player.X - Convert.ToInt32((pStartSlope * Convert.ToDouble(pDepth)));
                    if (x < 0) x = 0;

                    while (GetSlope(x, y, player.X, player.Y, false) >= pEndSlope)
                    {
                        if (GetVisDistance(x, y, player.X, player.Y) <= visrange2)
                        {
                            if (map[x, y] == 1) //current cell blocked
                            {
                                ShowVisibleSubTilesOfSolidMainTile(x, y, pOctant);

                                var hasTileOnLeft = x - 1 >= 0 && map[x - 1, y] == 0;
                                if (hasTileOnLeft) //prior cell within range AND open...
                                                   //...incremenet the depth, adjust the endslope and recurse
                                    ScanOctant(pDepth + 1, pOctant, pStartSlope, GetSlope(x - 0.5, y + 0.5, player.X, player.Y, false));
                            }
                            else
                            {
                                var hasWallOnLeft = x - 1 >= 0 && map[x - 1, y] == 1;
                                if (hasWallOnLeft) //prior cell within range AND open...
                                {                  //..adjust the startslope
                                    pStartSlope = GetSlope(x - 0.5, y - 0.5, player.X, player.Y, false);
                                }

                                ShowAllSubTiles(x, y);
                            }
                        }
                        x++;
                    }
                    x--;
                    break;

                case 2: //nne

                    y = player.Y - pDepth;
                    if (y < 0) return;

                    x = player.X + Convert.ToInt32((pStartSlope * Convert.ToDouble(pDepth)));
                    if (x >= map.GetLength(0)) x = map.GetLength(0) - 1;

                    while (GetSlope(x, y, player.X, player.Y, false) <= pEndSlope)
                    {
                        if (GetVisDistance(x, y, player.X, player.Y) <= visrange2)
                        {
                            if (map[x, y] == 1)
                            {
                                ShowVisibleSubTilesOfSolidMainTile(x, y, pOctant);

                                var hasTileOnRight = x + 1 < map.GetLength(0) && map[x + 1, y] == 0;
                                if (hasTileOnRight)
                                    ScanOctant(pDepth + 1, pOctant, pStartSlope, GetSlope(x + 0.5, y + 0.5, player.X, player.Y, false));
                            }
                            else
                            {
                                var hasWallOnRight = x + 1 < map.GetLength(0) && map[x + 1, y] == 1;
                                if (hasWallOnRight)
                                {
                                    pStartSlope = -GetSlope(x + 0.5, y - 0.5, player.X, player.Y, false);
                                }

                                ShowAllSubTiles(x, y);
                            }
                        }
                        x--;
                    }
                    x++;
                    break;

                case 3:

                    x = player.X + pDepth;
                    if (x >= map.GetLength(0)) return;

                    y = player.Y - Convert.ToInt32((pStartSlope * Convert.ToDouble(pDepth)));
                    if (y < 0) y = 0;

                    while (GetSlope(x, y, player.X, player.Y, true) <= pEndSlope)
                    {
                        if (GetVisDistance(x, y, player.X, player.Y) <= visrange2)
                        {
                            if (map[x, y] == 1)
                            {
                                ShowVisibleSubTilesOfSolidMainTile(x, y, pOctant);

                                var hasTileAbove = y - 1 >= 0 && map[x, y - 1] == 0;
                                if (hasTileAbove)
                                    ScanOctant(pDepth + 1, pOctant, pStartSlope, GetSlope(x - 0.5, y - 0.5, player.X, player.Y, true));
                            }
                            else
                            {
                                var hasWallAbove = y - 1 >= 0 && map[x, y - 1] == 1;
                                if (hasWallAbove)
                                {
                                    pStartSlope = -GetSlope(x + 0.5, y - 0.5, player.X, player.Y, true);
                                }

                                ShowAllSubTiles(x, y);
                            }
                        }
                        y++;
                    }
                    y--;
                    break;

                case 4:

                    x = player.X + pDepth;
                    if (x >= map.GetLength(0)) return;

                    y = player.Y + Convert.ToInt32((pStartSlope * Convert.ToDouble(pDepth)));
                    if (y >= map.GetLength(1)) y = map.GetLength(1) - 1;

                    while (GetSlope(x, y, player.X, player.Y, true) >= pEndSlope)
                    {
                        if (GetVisDistance(x, y, player.X, player.Y) <= visrange2)
                        {
                            if (map[x, y] == 1)
                            {
                                ShowVisibleSubTilesOfSolidMainTile(x, y, pOctant);

                                var hasTileBelow = y + 1 < map.GetLength(1) && map[x, y + 1] == 0;
                                if (hasTileBelow)
                                    ScanOctant(pDepth + 1, pOctant, pStartSlope, GetSlope(x - 0.5, y + 0.5, player.X, player.Y, true));
                            }
                            else
                            {
                                var hasWallBelow = y + 1 < map.GetLength(1) && map[x, y + 1] == 1;
                                if (hasWallBelow)
                                {
                                    pStartSlope = GetSlope(x + 0.5, y + 0.5, player.X, player.Y, true);
                                }

                                ShowAllSubTiles(x, y);
                            }
                        }
                        y--;
                    }
                    y++;
                    break;

                case 5:

                    y = player.Y + pDepth;
                    if (y >= map.GetLength(1)) return;

                    x = player.X + Convert.ToInt32((pStartSlope * Convert.ToDouble(pDepth)));
                    if (x >= map.GetLength(0)) x = map.GetLength(0) - 1;

                    while (GetSlope(x, y, player.X, player.Y, false) >= pEndSlope)
                    {
                        if (GetVisDistance(x, y, player.X, player.Y) <= visrange2)
                        {
                            if (map[x, y] == 1)
                            {
                                ShowVisibleSubTilesOfSolidMainTile(x, y, pOctant);

                                var hasTileOnRight = x + 1 < map.GetLength(0) && map[x + 1, y] == 0;
                                if (hasTileOnRight)
                                    ScanOctant(pDepth + 1, pOctant, pStartSlope, GetSlope(x + 0.5, y - 0.5, player.X, player.Y, false));
                            }
                            else
                            {
                                var hasWallOnRight = x + 1 < map.GetLength(0) && map[x + 1, y] == 1;
                                if (hasWallOnRight)
                                {
                                    pStartSlope = GetSlope(x + 0.5, y + 0.5, player.X, player.Y, false);
                                }

                                ShowAllSubTiles(x, y);
                            }
                        }
                        x--;
                    }
                    x++;
                    break;

                case 6:

                    y = player.Y + pDepth;
                    if (y >= map.GetLength(1)) return;

                    x = player.X - Convert.ToInt32((pStartSlope * Convert.ToDouble(pDepth)));
                    if (x < 0) x = 0;

                    while (GetSlope(x, y, player.X, player.Y, false) <= pEndSlope)
                    {
                        if (GetVisDistance(x, y, player.X, player.Y) <= visrange2)
                        {
                            if (map[x, y] == 1)
                            {
                                ShowVisibleSubTilesOfSolidMainTile(x, y, pOctant);

                                var hasTileOnLeft = x - 1 >= 0 && map[x - 1, y] == 0;
                                if (hasTileOnLeft)
                                    ScanOctant(pDepth + 1, pOctant, pStartSlope, GetSlope(x - 0.5, y - 0.5, player.X, player.Y, false));
                            }
                            else
                            {
                                var hasWallOnLeft = x - 1 >= 0 && map[x - 1, y] == 1;
                                if (hasWallOnLeft)
                                {
                                    pStartSlope = -GetSlope(x - 0.5, y + 0.5, player.X, player.Y, false);
                                }
                                ShowAllSubTiles(x, y);
                            }
                        }
                        x++;
                    }
                    x--;
                    break;

                case 7:

                    x = player.X - pDepth;
                    if (x < 0) return;

                    y = player.Y + Convert.ToInt32((pStartSlope * Convert.ToDouble(pDepth)));
                    if (y >= map.GetLength(1)) y = map.GetLength(1) - 1;

                    while (GetSlope(x, y, player.X, player.Y, true) <= pEndSlope)
                    {
                        if (GetVisDistance(x, y, player.X, player.Y) <= visrange2)
                        {
                            if (map[x, y] == 1)
                            {
                                ShowVisibleSubTilesOfSolidMainTile(x, y, pOctant);

                                var hasTileBelow = y + 1 < map.GetLength(1) && map[x, y + 1] == 0;
                                if (hasTileBelow)
                                    ScanOctant(pDepth + 1, pOctant, pStartSlope, GetSlope(x + 0.5, y + 0.5, player.X, player.Y, true));
                            }
                            else
                            {
                                var hasWallBelow = y + 1 < map.GetLength(1) && map[x, y + 1] == 1;
                                if (hasWallBelow)
                                {
                                    pStartSlope = -GetSlope(x - 0.5, y + 0.5, player.X, player.Y, true);
                                }

                                ShowAllSubTiles(x, y);
                            }
                        }
                        y--;
                    }
                    y++;
                    break;

                case 8: //wnw

                    x = player.X - pDepth;
                    if (x < 0) return;

                    y = player.Y - Convert.ToInt32((pStartSlope * Convert.ToDouble(pDepth)));
                    if (y < 0) y = 0;

                    while (GetSlope(x, y, player.X, player.Y, true) >= pEndSlope)
                    {
                        if (GetVisDistance(x, y, player.X, player.Y) <= visrange2)
                        {
                            if (map[x, y] == 1)
                            {
                                ShowVisibleSubTilesOfSolidMainTile(x, y, pOctant);

                                var hasTileAbove = y - 1 >= 0 && map[x, y - 1] == 0;
                                if (hasTileAbove)
                                    ScanOctant(pDepth + 1, pOctant, pStartSlope, GetSlope(x + 0.5, y - 0.5, player.X, player.Y, true));

                            }
                            else
                            {
                                var hasWallAbove = y - 1 >= 0 && map[x, y - 1] == 1;
                                if (hasWallAbove)
                                {
                                    pStartSlope = GetSlope(x - 0.5, y - 0.5, player.X, player.Y, true);
                                }

                                ShowAllSubTiles(x, y);
                            }
                        }
                        y++;
                    }
                    y--;
                    break;
            }


            if (x < 0)
                x = 0;
            else if (x >= map.GetLength(0))
                x = map.GetLength(0) - 1;

            if (y < 0)
                y = 0;
            else if (y >= map.GetLength(1))
                y = map.GetLength(1) - 1;

            if (pDepth < VisualRange & map[x, y] == 0)
                ScanOctant(pDepth + 1, pOctant, pStartSlope, pEndSlope);

        }

        /// <summary>
        /// Marks all sub-tiles of the specified main-tile as visible
        /// </summary>
        /// <param name="mainTileX">Main-tile X coordinates</param>
        /// <param name="mainTileY">Main-tile Y coordinates</param>
        private void ShowAllSubTiles(int mainTileX, int mainTileY)
        {
            ShowSubTiles(GetSubTilesOfMainTile(mainTileX, mainTileY));
        }

        /// <summary>
        /// Marks the proper sub-tiles of the specified main-tile as visible
        /// </summary>
        /// <param name="mainTileX">Main-tile X coordinates</param>
        /// <param name="mainTileY">Main-tile Y coordinates</param>
        /// <param name="octant">
        ///
        ///    \ 1 | 2 /
        ///   8 \  |  / 3
        ///   -----+-----
        ///   7 /  |  \ 4
        ///    / 6 | 5 \
        ///
        ///  1 = NNW, 2 =NNE, 3=ENE, 4=ESE, 5=SSE, 6=SSW, 7=WSW, 8 = WNW
        /// </param>
        private void ShowVisibleSubTilesOfSolidMainTile(int mainTileX, int mainTileY, int octant)
        {
            var excludeCoords = new List<(int x, int y)>();
            switch (octant)  // remove the corner in the octant direction (always occluded)
            {
                case 1: excludeCoords.Add((0, 0)); break;
                case 2: excludeCoords.Add((1, 0)); break;
                case 3: excludeCoords.Add((1, 0)); break;
                case 4: excludeCoords.Add((1, 1)); break;
                case 5: excludeCoords.Add((1, 1)); break;
                case 6: excludeCoords.Add((0, 1)); break;
                case 7: excludeCoords.Add((0, 1)); break;
                case 8: excludeCoords.Add((0, 0)); break;
            }

            // when facing horizontally or vertically, remove the back side
            if (player.X == mainTileX)
            {
                if (octant == 1 || octant == 2)
                {
                    excludeCoords.Add((0, 0));
                    excludeCoords.Add((1, 0));
                }
                else
                { // octants 5 || 6
                    excludeCoords.Add((0, 1));
                    excludeCoords.Add((1, 1));
                }
            }
            else if (player.Y == mainTileY)
            {
                if (octant == 3 || octant == 4)
                {
                    excludeCoords.Add((1, 0));
                    excludeCoords.Add((1, 1));
                }
                else
                { // octants 7 || 8
                    excludeCoords.Add((0, 0));
                    excludeCoords.Add((0, 1));
                }
            }

            // remove the visible back corner if occluded by other wall
            var hasWallOnRight = mainTileX + 1 >= map.GetLength(0) || map[mainTileX + 1, mainTileY] == 1;
            if (hasWallOnRight)
            {
                if (octant == 1 || octant == 8)
                    excludeCoords.Add((1, 0));
                else if (octant == 6 || octant == 7)
                    excludeCoords.Add((1, 1));
            }

            var hasWallOnLeft = mainTileX - 1 < 0 || map[mainTileX - 1, mainTileY] == 1;
            if (hasWallOnLeft)
            {
                if (octant == 2 || octant == 3)
                    excludeCoords.Add((0, 0));
                else if (octant == 4 || octant == 5)
                    excludeCoords.Add((0, 1));
            }

            var hasWallOnTop = mainTileY - 1 < 0 || map[mainTileX, mainTileY - 1] == 1;
            if (hasWallOnTop)
            {
                if (octant == 4 || octant == 5)
                    excludeCoords.Add((1, 0));
                else if (octant == 6 || octant == 7)
                    excludeCoords.Add((0, 0));
            }

            var hasWallONBottom = mainTileY + 1 >= map.GetLength(1) || map[mainTileX, mainTileY + 1] == 1;
            if (hasWallONBottom)
            {
                if (octant == 1 || octant == 8)
                    excludeCoords.Add((0, 1));
                else if (octant == 2 || octant == 3)
                    excludeCoords.Add((1, 1));
            }

            var excludePoints = excludeCoords
                .Select(coords => new Point((2 * mainTileX) + coords.x, (2 * mainTileY) + coords.y));

            var partialIndexList = GetSubTilesOfMainTile(mainTileX, mainTileY)
                .Where(point => !excludePoints.Contains(point)).ToList();

            ShowSubTiles(partialIndexList);
        }

        /// <summary>
        /// Sets sub-tiles as visible
        /// </summary>
        /// <param name="points">List of sub-tiles coordinates</param>
        private void ShowSubTiles(List<Point> points)
        {
            points.ForEach(point => subTilesVisibility[point.X, point.Y] = true);
        }

        /// <summary>
        /// Returns the sub-tiles coordinates for a given main-tile coordiantes
        /// </summary>
        /// <param name="mainTileX">Main-tile X coordinates</param>
        /// <param name="mainTileY">Main-tile Y coordinates</param>
        /// <returns>List of sub-tiles coordinates</returns>
        public List<Point> GetSubTilesOfMainTile(int mainTileX, int mainTileY)
        {
            return subTileIndexList
                .Select(subTile => new Point((2 * mainTileX) + subTile.x, (2 * mainTileY) + subTile.y))
                .ToList();
        }

        /// <summary>
        /// Get the gradient of the slope formed by the two points
        /// </summary>
        /// <param name="pX1"></param>
        /// <param name="pY1"></param>
        /// <param name="pX2"></param>
        /// <param name="pY2"></param>
        /// <param name="pInvert">Invert slope</param>
        /// <returns></returns>
        private double GetSlope(double pX1, double pY1, double pX2, double pY2, bool pInvert)
        {
            if (pInvert)
                return (pY1 - pY2) / (pX1 - pX2);
            else
                return (pX1 - pX2) / (pY1 - pY2);
        }


        /// <summary>
        /// Calculate the distance between the two points
        /// </summary>
        /// <param name="pX1"></param>
        /// <param name="pY1"></param>
        /// <param name="pX2"></param>
        /// <param name="pY2"></param>
        /// <returns>Distance</returns>
        private int GetVisDistance(int pX1, int pY1, int pX2, int pY2)
        {
            return ((pX1 - pX2) * (pX1 - pX2)) + ((pY1 - pY2) * (pY1 - pY2));
        }

        #endregion


        //event raised when a player has successfully moved
        public delegate void moveDelegate();
        public event moveDelegate playerMoved;
    }
}
