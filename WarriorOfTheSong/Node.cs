using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace WarriorOfTheSong
{
    public class Node
    {
        //Path costs to get from the start to this tile(g) and from this tile to the target(h)
        public float f = 0f;
        public float g = 0f;
        public float h = 0f;

        //Store the Tile that led to this tile so it can be backtracked later to create the path
        public Node parent = null;

        //Tile specific data
        public int row;
        public int col;
        public int id;
        public int tileType;

        //Maintain a list of all valid tiles touching this tile that can be walked to
        public List<Node> adjacent = new List<Node>();

        //Tile graphical data
        public Rectangle rec;

        //This code is called whenever a new Tile is to be created
        public Node(int row, int col, int tileType, Vector2 mapSize)
        {
            //Store the grid location of the tile by row and column
            this.row = row;
            this.col = col;

            //Calculate the tiles numberical order (left to right, top to bottom) in the grid
            this.id = row * (int)mapSize.Y + col;

            //Store the tile's graphical data
            this.tileType = tileType;

            //Create a rectangle representing the tile's coordinates and size
            rec = new Rectangle(col * Game1.TILE_SIZE - Game1.TILE_SIZE, row * Game1.TILE_SIZE - Game1.TILE_SIZE, Game1.TILE_SIZE, Game1.TILE_SIZE);
        }

        //Pre: Map nodes (collection of all the tiles in the game world)
        //Post: None
        //Desc: Determine and store the tile's adjacent tiles
        public void SetAdjacenies(Node [,] map)
        {
            //Only add walkable terrain
            for (int curRow = row - 1; curRow <= row + 1; curRow++)
            {
                for (int curCol = col - 1; curCol <= col + 1; curCol++)
                {
                    //Do not add itself
                    if (row != curRow || col != curCol)
                    {
                        //Add only Nodes at valid row and columns that is walkable terrain
                        //Add later if necessary to if statement: map[curRow, curCol].tileType != Game1.placeholder     //Valid terrain type
                        if (curRow >= 0 && curRow < Game1.NUM_ROWS &&   //Within bounds vertically
                            curCol >= 0 && curCol < Game1.NUM_COLS)      //Within bounds horizontally
                        {
                            adjacent.Add(map[curRow, curCol]);
                        }

                    }
                }
            }
        }
    }
}
