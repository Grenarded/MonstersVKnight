//Author: Ben Petlach
//File Name: Game1.cs
//Project Name: WarriorOfTheSong
//Creation Date: May 20, 2022
//Modified Date: June 22, 2022
//Description: An arena, round-based top-down fighter
//
//Course Content Application:
//Output: Visual output consisting of animation and UI
//Variables: multipliers to timers and speed
//Input: Mouse input for menu, keyboard and mouse for gameplay
//Arrays: 3D array used to track enemy spawning
//Subprograms: Checking hover states, collision states, controllers for player and enemies
//Selection: Gameflow logic (if array index is out of bounds), gameState
//Loops: Loop through lists in path tracing, as well as through arrays

using System; 
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;


using Helper;
using Animation2D;

namespace WarriorOfTheSong
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        const int SCREEN_WIDTH = 870;
        const int SCREEN_HEIGHT = 510;

        //Game States to cycle throughout the program
        const int MENU = 0;
        const int INSTRUCTIONS = 1;
        const int SELECTION = 2;
        const int GAME_SETUP = 3;
        const int GAME = 4;
        const int PAUSE = 5;
        const int GAMEOVER = 6;

        int gameState = MENU;

        //Number of arenas
        const int NUM_ARENAS = 5;

        //Path finding state
        const int NOT_FOUND = -1;

        //Maintain size of tiles
        public const int TILE_SIZE = 30;

        //Grid scaling variables
        public const int NUM_ROWS = (SCREEN_HEIGHT / TILE_SIZE) + 2;
        public const int NUM_COLS = (SCREEN_WIDTH / TILE_SIZE) + 2;

        //Maintain tile types
        public const int ENEMY = 0;
        public const int KNIGHT = 1;
        public const int GRASS = 2;
        public const int STONE = 3;
        public const int TREE = 4;
        public const int IMPASS = 5;
        public const int FENCE = 6;
        public const int SPAWN = 7;
        public const int BLOCK = 8;

        //Grid tile Speeds
        const float GRASS_MULTIPLIER = 1f;
        const float STONE_MULTIPLIER = 1.35f;

        //Fence types
        const int FENCE_CORNER_TOP = 0;
        const int FENCE_MID = 1;
        const int FENCE_CORNER_BOTTOM = 2;
        const int FENCE_SIDE = 3;

        //Access array with the max number of enemies that can be spawned and active in a round simoultaneously
        const int SPAWN_TOTAL = 0;
        const int SPAWN_CURRENT = 1;

        //UI Spacers
        const int HEADING_SPACER = 20;
        const int SUB_HEADING_SPACER = 10;
        const int IN_GAME_SPACER = 15;

        //Access respective buttons stored in array
        const int BTN_PLAY = 0;
        const int BTN_INSTRUCTIONS = 1;
        const int BTN_EXIT = 2;

        const int BTN_RESUME = 0;
        const int BTN_MAIN_MENU = 1;

        const int BTN_PLAY_AGAIN = 0;

        //Values of main arena selection box, as well as the arena side buttons
        const int SELECT_BOX_WIDTH = 780;
        const int SELECT_BOX_HEIGHT = 400;
        const int ARENA_BTN_WIDTH = SELECT_BOX_WIDTH / 3;
        const int ARENA_BTN_HEIGHT = SELECT_BOX_HEIGHT / NUM_ARENAS;

        //Character movement directions (for both enemies and knight)
        const int MOVEMENT_NEGATIVE = -1;
        const int MOVEMENT_STOPPED = 0;
        const int MOVEMENT_POSITIVE = 1;

        //Knight states
        const int KNIGHT_IDLE = 0;
        const int KNIGHT_RUN_LEFT = 1;
        const int KNIGHT_RUN_RIGHT = 2;
        const int KNIGHT_RUN_UP = 3;
        const int KNIGHT_RUN_DOWN = 4;

        const int KNIGHT_ATTACK_DOWN = 5;
        const int KNIGHT_ATTACK_LEFT = 6;
        const int KNIGHT_ATTACK_RIGHT = 7;
        const int KNIGHT_ATTACK_UP = 8;

        const int DOWN = 0;
        const int LEFT = 1;
        const int RIGHT = 2;
        const int UP = 3;

        //Default knight stats
        const float KNIGHT_SPEED = 100f;
        const int KNIGHT_START_HEALTH = 100;
        const int KNIGHT_SWORD_DAMAGE = 5;

        //Default slime enemy stats
        const int SLIME_DAMAGE = 1;
        const int SLIME_START_HEALTH = 5;
        const double SLIME_DAMAGE_TIME = 1300;
        const float SLIME_DEFAULT_SPEED = 60f;
        const double SPAWN_DELAY = 1000;

        //Collision States
        const int KNIGHT_ENEMY_COLLISION = 0;
        const int KNIGHT_ATTACK_COLLISION = 1;

        //Define attack frame so combat can be calculated when sword is out
        const int ATTACK_FRAME = 1;

        //Knight Animation
        Texture2D[] knightImgs;
        Animation[] knightAnims;

        //Check which frame is active in an animation
        int frameCounter = 0;

        //Define starting knight states
        int knightHealth = KNIGHT_START_HEALTH;
        int knightFacing = DOWN;
        bool isKnightAttacking = false;
        int currentKnightAnim = KNIGHT_IDLE;
        int prevKnightAnim;

        //Knight movement and direction 
        Vector2 knightDir = new Vector2(MOVEMENT_STOPPED, MOVEMENT_STOPPED);
        Vector2 knightSpeed = new Vector2(MOVEMENT_STOPPED, MOVEMENT_STOPPED);
        Vector2 knightPos;

        //Area where knight collides with enemy and takes damage
        Rectangle knightHitBoxRec;
        GameRectangle knightHitBox;

        //Values to adjust knight hitbox depending on animation state
        int hitBoxRight = 0;
        int hitBoxDown = 0;
        int subHitBoxWidth = 0;
        int subHitBoxHeight = 0;

        //Rectangle to detect collision between sword point and enemies
        Rectangle swordRec;

        //Knight health bar
        GameRectangle healthBarKnight;
        GameRectangle healthBarBackKnight;
        int healthBarWidth = 110;
        int healthBarHeight = 10;
        int healthBarOffsetX = 140;

        /////////////////////////
        //Arena/Round Management
        /////////////////////////

        //Tracks to see if player won current arena
        bool arenaWin = false;
        int curArena = 0;

        //Store which arena is unlocked and accessible
        bool[] isArenaUnlocked = new bool[NUM_ARENAS] { true, false, false, false, false };

        //Store how many enemies have been spawned in the current round
        int spawnCount = 0;

        //Define variable to store all possible enemy spawn locations
        List<Rectangle> enemySpawnLocs = new List<Rectangle>();
        //Track the next spawn point
        int enemySpawnLocCounter = 0;

        //Store the number of times the group of available spawn locations was re-used in initial round spawning
        int timesSpawnReset = 0;

        //Store time for enemy to be immovable when first spawned
        double spawnDelay;

        //Store current wave and round
        int curWave = 0;
        int curRound = 0;

        //Store total enemies present in a round vs amount to appear at once
        int[,,] enemySpawnCount = new int[,,]
        {
            //Total enemies to spawn in a round based on wave
            {
                {  4,  4, 8 },
                { 10, 16, 24 },
                { 20, 28, 40 }
            },
            //Number of enemies that can appear at once in respective round based on wave
            {
                { 4, 4, 4 },
                { 6, 8, 8 },
                { 9, 10, 10 }
            }

        };

        //Store number of slimes present in the round
        List<Enemy> slimes = new List<Enemy>();   

        Texture2D slimeImg;

        //Enemy movement stats
        float enemySpeedAdj = SLIME_DEFAULT_SPEED;
        Vector2 enemyDir = new Vector2(MOVEMENT_STOPPED, MOVEMENT_STOPPED);
        Vector2 enemySpeed = new Vector2(MOVEMENT_STOPPED, MOVEMENT_STOPPED);

        /////////////////
        ///User Interface
        /////////////////

        //Store button color depending on if it's hovered over or not
        Color btnColor;

        //Menu
        Texture2D titleImg;
        Rectangle titleRec;

        Texture2D bgMenuImg;
        Animation bgMenuAnim;
        Vector2 bgMenuPos;

        Texture2D[] menuBtnImgs;
        Rectangle[] menuBtnRecs;
        int menuBtnY;

        Texture2D[] pauseBtnImgs;

        //Instructions
        Vector2 instructionTxtLoc;

        Texture2D instructionsImg;
        Rectangle instructionsRec;

        //Arena Selection
        string[] arenaNames = new string[NUM_ARENAS]
        {
            "Green Acres",
            "Hidden Ruins",
            "?????",
            "?????",
            "?????"
        };
        Vector2[] arenaNameLocs = new Vector2[NUM_ARENAS];

        Vector2 arenaSelectTxtLoc;

        Texture2D backBtnImg;
        Rectangle backBtnRec;

        GameRectangle selectionBox;

        GameRectangle[] arenaSelectBtnOutlines;
        GameRectangle[] arenaSelectBtns;

        Texture2D[] arenaIcons;
        Rectangle[] arenaIconRecs;

        Texture2D[] arenaPreviews;
        Rectangle arenaPreviewRec;

        Texture2D iconLock;
        Texture2D iconComingSoon;

        Texture2D arenaPlayBtnImg;
        Texture2D arenaLockedBtnImg;
        Rectangle[] arenaBtnRecs = new Rectangle[NUM_ARENAS];

        //Values for play and locked buttons next to each arena 
        int arenaBtnWidth = 125;
        int arenaBtnHeight = 50;

        //Game over
        Texture2D[] gameOverHeadings;
        Rectangle[] gameOverHeadingRecs;

        Texture2D[] gameOverBtnImgs;
        Rectangle[] gameOverBtnRecs;

        //Fonts
        SpriteFont headingFont;
        SpriteFont subHeadingFont;
        SpriteFont textFont;

        //Gameplay
        Vector2 waveTxtLoc;
        Vector2 healthTxtLoc;

        //Hardware states
        KeyboardState kb;
        KeyboardState prevKb;
        MouseState prevMouse;
        MouseState mouse;

        int screenWidth;
        int screenHeight;

        ///////////////
        ///Arena mapping
        ////////////////
        
        /////Base tile images
        Texture2D grassTileImg;
        Texture2D stoneTileImg;

        //Store the different types of fence images
        Texture2D[] fenceTileImgs;

        //Store instances of the tree
        Texture2D treeImg;
        List<Rectangle> treeRecs = new List<Rectangle>();

        //Factor to affect speed by depending on player or enemy's position
        float speedFactor = GRASS_MULTIPLIER;                             

        //Create a 19x31 map grid w/ tiles in int format
        int[,] map = new int[,]
        {
            {GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,SPAWN,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS},
            {GRASS,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,BLOCK,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,GRASS},
            {GRASS,FENCE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,FENCE,GRASS},
            {GRASS,FENCE,GRASS,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,GRASS,FENCE,GRASS},
            {GRASS,FENCE,GRASS,STONE,GRASS,TREE,GRASS,TREE,GRASS,TREE,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,GRASS,STONE,GRASS,FENCE,GRASS},
            {GRASS,FENCE,GRASS,STONE,GRASS,IMPASS,IMPASS,IMPASS,IMPASS,IMPASS,IMPASS,GRASS,GRASS,GRASS,STONE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,GRASS,STONE,GRASS,GRASS,FENCE,GRASS},
            {GRASS,FENCE,GRASS,STONE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,TREE,GRASS,GRASS,STONE,STONE,STONE,STONE,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,FENCE,GRASS},
            {GRASS,FENCE,GRASS,STONE,GRASS,TREE,GRASS,TREE,GRASS,TREE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,IMPASS,IMPASS,GRASS,STONE,TREE,GRASS,STONE,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,FENCE,GRASS},
            {SPAWN,BLOCK,GRASS,STONE,GRASS,IMPASS,IMPASS,IMPASS,IMPASS,IMPASS,IMPASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,STONE,STONE,IMPASS,IMPASS,STONE,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,BLOCK,SPAWN},
            {GRASS,FENCE,GRASS,STONE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,STONE,STONE,STONE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,FENCE,GRASS},
            {GRASS,FENCE,GRASS,STONE,GRASS,GRASS,GRASS,GRASS,STONE,STONE,STONE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,FENCE,GRASS},
            {GRASS,FENCE,GRASS,STONE,STONE,STONE,STONE,STONE,GRASS,GRASS,GRASS,STONE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,FENCE,GRASS},
            {GRASS,FENCE,GRASS,TREE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,TREE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,FENCE,GRASS},
            {GRASS,FENCE,GRASS,IMPASS,IMPASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,IMPASS,IMPASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,TREE,GRASS,STONE,GRASS,FENCE,GRASS},
            {GRASS,FENCE,GRASS,STONE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,STONE,IMPASS,IMPASS,STONE,GRASS,FENCE,GRASS},
            {GRASS,FENCE,GRASS,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,GRASS,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,STONE,GRASS,FENCE,GRASS},
            {GRASS,FENCE,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,FENCE,GRASS},
            {GRASS,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,BLOCK,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,FENCE,GRASS},
            {GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,SPAWN,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS,GRASS}
        };

        ////////////////
        // PATH FINDING 
        ///////////////

        //Define movement cost multiplier based on Numbered tile types
        float[] tileCosts = new float[] {1f,    //Enemy
                                         1f,    //Knight
                                         1f,    //Grass (standard tile)
                                         0.7f,  //Stone
                                         1f,    //Tree
                                         10000f,    //Impassible
                                         10000f,    //Fence
                                         1f,   //Spawn point
                                         1f };  //Block point

        float hvCost = 10f;     //Cost to move horizontally, vertically on standard terrain
        float diagCost = 14f;   //Cost to move diagonally on standard terrain

        //Maintain a map that tracks all of the tile (Node) information
        Node[,] tileMap;

        //Track the end of the path
        Node end;

        ///To track enemy tile to move to
        int curEnemyTile = 0;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = SCREEN_WIDTH;  
            _graphics.PreferredBackBufferHeight = SCREEN_HEIGHT;   
            _graphics.ApplyChanges();

            screenWidth = _graphics.GraphicsDevice.Viewport.Width;
            screenHeight = _graphics.GraphicsDevice.Viewport.Height;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            //Load fonts
            headingFont = Content.Load<SpriteFont>("Fonts/HeadingFont");
            subHeadingFont = Content.Load<SpriteFont>("Fonts/SubHeadingFont");
            textFont = Content.Load<SpriteFont>("Fonts/TextFont");

            //Load tile images
            grassTileImg = Content.Load<Texture2D>("Images/Tilesets/Grass");
            stoneTileImg = Content.Load<Texture2D>("Images/Tilesets/Stone");

            fenceTileImgs = new Texture2D[]
            {
                Content.Load<Texture2D>("Images/Tilesets/Fence0"),
                Content.Load<Texture2D>("Images/Tilesets/Fence1"),
                Content.Load<Texture2D>("Images/Tilesets/Fence2"),
                Content.Load<Texture2D>("Images/Tilesets/Fence3"),
            };

            treeImg = Content.Load<Texture2D>("Images/Tilesets/Tree");

            //Maintain the grid size of the maps X = Rows = 19 and Y = Columns = 31 
            Vector2 mapSize = new Vector2(NUM_ROWS, NUM_COLS);

            //Create simple 19x31 tile map from int map set up
            tileMap = new Node[NUM_ROWS, NUM_COLS];
            for (int row = 0; row < NUM_ROWS; row++)
            {
                for (int col = 0; col < NUM_COLS; col++)
                {
                    //Based on the int map array create a Node of that type in the same grid coordinates
                    tileMap[row, col] = new Node(row, col, map[row, col], mapSize);
                }
            }

            //Setup necessary pathing data, adjacent Nodes(Tiles) and the H cost from each Node to the Target
            for (int row = 0; row < NUM_ROWS; row++)
            {
                for (int col = 0; col < NUM_COLS; col++)
                {
                    //For each tile, find all valid tiles surrounding it that are not obstacles or off the map
                    tileMap[row, col].SetAdjacenies(tileMap);
                }
            }

            //Assign tree rectangles relative to tree tile
            for (int row = 0; row < NUM_ROWS; row++)
            {
                for (int col = 0; col < NUM_COLS; col++)
                {
                    if (tileMap[row, col].tileType == TREE)
                    {
                        Rectangle treeRec = new Rectangle(tileMap[row, col].rec.X, tileMap[row, col].rec.Y - 4, treeImg.Width, treeImg.Height);
                        treeRecs.Add(treeRec);
                    }
                }
            }

            //Add all enemy spawn tiles to the spawn tiles list
            for (int row = 0; row < NUM_ROWS; row++)
            {
                for (int col = 0; col < NUM_COLS; col++)
                {
                    if (tileMap[row, col].tileType == SPAWN)
                    {
                        enemySpawnLocs.Add(tileMap[row, col].rec);
                    }
                }
            }

            //Set knight position
            knightPos = new Vector2(screenWidth / 2, screenHeight / 2);

            //Load knight sprite sheets
            knightImgs = new Texture2D[] {
                                              Content.Load<Texture2D>("Images/Sprites/KnightIdleSS"),
                                              Content.Load<Texture2D>("Images/Sprites/KnightRunLeftSS"),
                                              Content.Load<Texture2D>("Images/Sprites/KnightRunRightSS"),
                                              Content.Load<Texture2D>("Images/Sprites/KnightRunUpSS"),
                                              Content.Load<Texture2D>("Images/Sprites/KnightRunDownSS"),
                                              Content.Load<Texture2D>("Images/Sprites/KnightAttackDownSS"),
                                              Content.Load<Texture2D>("Images/Sprites/KnightAttackLeftSS"),
                                              Content.Load<Texture2D>("Images/Sprites/KnightAttackRightSS"),
                                              Content.Load<Texture2D>("Images/Sprites/KnightAttackUpSS")
                                              };
            //Set up knight animation
            knightAnims = new Animation[] {
                                               new Animation(knightImgs[KNIGHT_IDLE], 4, 1, 4, 0, 0, Animation.ANIMATE_FOREVER, 9, knightPos, 1f, true),
                                               new Animation(knightImgs[KNIGHT_RUN_LEFT], 6, 1, 6, 0, 0, Animation.ANIMATE_FOREVER, 9, knightPos, 1f, true),
                                               new Animation(knightImgs[KNIGHT_RUN_RIGHT], 6, 1, 6, 0, 0, Animation.ANIMATE_FOREVER, 9, knightPos, 1f, true),
                                               new Animation(knightImgs[KNIGHT_RUN_UP], 5, 1, 5, 0, 0, Animation.ANIMATE_FOREVER, 9, knightPos, 1f, true),
                                               new Animation(knightImgs[KNIGHT_RUN_DOWN], 5, 1, 5, 0, 0, Animation.ANIMATE_FOREVER, 9, knightPos, 1f, true),
                                               new Animation(knightImgs[KNIGHT_ATTACK_DOWN], 3, 1, 3, 0, 0, Animation.ANIMATE_ONCE, 6, knightPos, 1f, false),
                                               new Animation(knightImgs[KNIGHT_ATTACK_LEFT], 3, 1, 3, 0, 0, Animation.ANIMATE_ONCE, 6, knightPos, 1f, false),
                                               new Animation(knightImgs[KNIGHT_ATTACK_RIGHT], 3, 1, 3, 0, 0, Animation.ANIMATE_ONCE, 6, knightPos, 1f, false),
                                               new Animation(knightImgs[KNIGHT_ATTACK_UP], 3, 1, 3, 0, 0, Animation.ANIMATE_ONCE, 6, knightPos, 1f, false),
                                               };
            //Load slime image
            slimeImg = Content.Load<Texture2D>("Images/Sprites/BlueSlime");

            //Knight health bar
            healthBarKnight = new GameRectangle(GraphicsDevice, new Rectangle(screenWidth - healthBarOffsetX, screenHeight - IN_GAME_SPACER, healthBarWidth, healthBarHeight));
            healthBarBackKnight = new GameRectangle(GraphicsDevice, new Rectangle(screenWidth - healthBarOffsetX, screenHeight - IN_GAME_SPACER, healthBarWidth, healthBarHeight));
            healthTxtLoc = new Vector2(healthBarBackKnight.Rec.X + healthBarBackKnight.Rec.Width / 2 - textFont.MeasureString("Health").X / 2, healthBarBackKnight.Rec.Y - 4);

            //Knight hitbox
            knightHitBoxRec = new Rectangle(0, 0, knightAnims[0].destRec.Width, knightAnims[0].destRec.Height);
            knightHitBox = new GameRectangle(GraphicsDevice, knightHitBoxRec);

            //Location of current wave text
            waveTxtLoc = new Vector2(screenWidth / 2 - subHeadingFont.MeasureString("Wave 1").X / 2, 10);

            //Menu
            titleImg = Content.Load<Texture2D>("Images/Texts/Title");
            titleRec = new Rectangle(screenWidth / 2 - titleImg.Width / 2, HEADING_SPACER, titleImg.Width, titleImg.Height);

            bgMenuImg = Content.Load<Texture2D>("Images/Backgrounds/MenuBackground");
            bgMenuPos = new Vector2(0, -150);
            bgMenuAnim = new Animation(bgMenuImg, 4, 1, 4, 0, 0, Animation.ANIMATE_FOREVER, 10, bgMenuPos, 1.75f, true);

            menuBtnImgs = new Texture2D[]
            {
                Content.Load<Texture2D>("Images/Buttons/ButtonPlay"),
                Content.Load<Texture2D>("Images/Buttons/ButtonInstructions"),
                Content.Load<Texture2D>("Images/Buttons/ButtonExit")
            };

            menuBtnRecs = new Rectangle[]
            {
                new Rectangle(screenWidth / 2 - menuBtnImgs[BTN_PLAY].Width / 2, (int)(screenHeight / 2.3 - menuBtnImgs[BTN_PLAY].Height), menuBtnImgs[BTN_PLAY].Width, menuBtnImgs[BTN_PLAY].Height),
                new Rectangle(screenWidth / 2 - menuBtnImgs[BTN_INSTRUCTIONS].Width / 2, screenHeight / 2 - menuBtnImgs[BTN_INSTRUCTIONS].Height, menuBtnImgs[BTN_INSTRUCTIONS].Width, menuBtnImgs[BTN_INSTRUCTIONS].Height),
                new Rectangle(screenWidth / 2 - menuBtnImgs[BTN_EXIT].Width / 2, screenHeight / 2 - menuBtnImgs[BTN_EXIT].Height, menuBtnImgs[BTN_EXIT].Width, menuBtnImgs[BTN_EXIT].Height)
            };

            pauseBtnImgs = new Texture2D[]
            {
                Content.Load<Texture2D>("Images/Buttons/ButtonResume"),
                Content.Load<Texture2D>("Images/Buttons/ButtonMainMenu"),
                Content.Load<Texture2D>("Images/Buttons/ButtonExit")
            };

            //Instructions
            instructionTxtLoc = new Vector2(screenWidth / 2 - headingFont.MeasureString("Instructions").X / 2, SUB_HEADING_SPACER);

            instructionsImg = Content.Load<Texture2D>("Images/Backgrounds/Instructions");
            instructionsRec = new Rectangle(screenWidth / 2 - SELECT_BOX_WIDTH / 2, screenHeight / 2 - SELECT_BOX_HEIGHT / 2 + 30, SELECT_BOX_WIDTH, SELECT_BOX_HEIGHT);

            //Arena selection
            arenaSelectTxtLoc = new Vector2(screenWidth / 2 - headingFont.MeasureString("Arena Selection").X / 2, SUB_HEADING_SPACER);

            selectionBox = new GameRectangle(GraphicsDevice, new Rectangle(screenWidth / 2 - SELECT_BOX_WIDTH / 2, screenHeight / 2 - SELECT_BOX_HEIGHT / 2 + 30, SELECT_BOX_WIDTH, SELECT_BOX_HEIGHT));

            arenaSelectBtnOutlines = new GameRectangle[NUM_ARENAS]
            {
                new GameRectangle(GraphicsDevice, new Rectangle(selectionBox.Rec.X, selectionBox.Rec.Y, ARENA_BTN_WIDTH, ARENA_BTN_HEIGHT)),
                new GameRectangle(GraphicsDevice, new Rectangle(selectionBox.Rec.X, selectionBox.Rec.Y + ARENA_BTN_HEIGHT, ARENA_BTN_WIDTH, ARENA_BTN_HEIGHT)),
                new GameRectangle(GraphicsDevice, new Rectangle(selectionBox.Rec.X, selectionBox.Rec.Y + ARENA_BTN_HEIGHT * 2, ARENA_BTN_WIDTH, ARENA_BTN_HEIGHT)),
                new GameRectangle(GraphicsDevice, new Rectangle(selectionBox.Rec.X, selectionBox.Rec.Y + ARENA_BTN_HEIGHT * 3, ARENA_BTN_WIDTH, ARENA_BTN_HEIGHT)),
                new GameRectangle(GraphicsDevice, new Rectangle(selectionBox.Rec.X, selectionBox.Rec.Y + ARENA_BTN_HEIGHT * 4, ARENA_BTN_WIDTH, ARENA_BTN_HEIGHT)),
            };

            arenaSelectBtns = new GameRectangle[NUM_ARENAS]
            {
                new GameRectangle(GraphicsDevice, new Rectangle(selectionBox.Rec.X + 1, selectionBox.Rec.Y + 2, ARENA_BTN_WIDTH - 2, ARENA_BTN_HEIGHT - 2)),
                new GameRectangle(GraphicsDevice, new Rectangle(selectionBox.Rec.X + 1, selectionBox.Rec.Y + ARENA_BTN_HEIGHT + 2, ARENA_BTN_WIDTH - 2, ARENA_BTN_HEIGHT - 2)),
                new GameRectangle(GraphicsDevice, new Rectangle(selectionBox.Rec.X + 1, selectionBox.Rec.Y + ARENA_BTN_HEIGHT * 2 + 2, ARENA_BTN_WIDTH - 2, ARENA_BTN_HEIGHT - 2)),
                new GameRectangle(GraphicsDevice, new Rectangle(selectionBox.Rec.X + 1, selectionBox.Rec.Y + ARENA_BTN_HEIGHT * 3 + 2, ARENA_BTN_WIDTH - 2, ARENA_BTN_HEIGHT - 2)),
                new GameRectangle(GraphicsDevice, new Rectangle(selectionBox.Rec.X + 1, selectionBox.Rec.Y + ARENA_BTN_HEIGHT * 4 + 1, ARENA_BTN_WIDTH - 2, ARENA_BTN_HEIGHT - 2)),
            };

            iconLock = Content.Load<Texture2D>("Images/Sprites/IconLock");
            iconComingSoon = Content.Load<Texture2D>("Images/Sprites/IconComingSoon");

            arenaPreviews = new Texture2D[NUM_ARENAS]
            {
                Content.Load<Texture2D>("Images/Backgrounds/Arena0Map"), iconComingSoon, iconComingSoon, iconComingSoon, iconComingSoon
            };

            arenaPreviewRec = new Rectangle(selectionBox.Rec.X + selectionBox.Rec.Width / 2 - arenaPreviews[0].Width / 4 / 2 + ARENA_BTN_WIDTH / 2, selectionBox.Top + 5, arenaPreviews[0].Width / 4, arenaPreviews[0].Height / 4);

            for (int i = 0; i < NUM_ARENAS; i++)
            {
                arenaNameLocs[i] = new Vector2(arenaPreviewRec.Right - arenaPreviewRec.Width / 2 - headingFont.MeasureString(arenaNames[i]).X / 2, screenHeight - (screenHeight - arenaPreviewRec.Bottom) / 2 - headingFont.MeasureString(arenaNames[i]).Y);
            }

            arenaIcons = new Texture2D[NUM_ARENAS]
            {
                arenaPreviews[0], iconLock, iconLock, iconLock, iconLock
            };

            arenaIconRecs = new Rectangle[NUM_ARENAS]
            {
                new Rectangle(arenaSelectBtns[0].Rec.X, arenaSelectBtns[0].Rec.Y, ARENA_BTN_WIDTH / 3, ARENA_BTN_HEIGHT - 2),
                new Rectangle(arenaSelectBtns[1].Rec.X, arenaSelectBtns[1].Rec.Y, ARENA_BTN_WIDTH / 3, ARENA_BTN_HEIGHT - 2),
                new Rectangle(arenaSelectBtns[2].Rec.X, arenaSelectBtns[2].Rec.Y, ARENA_BTN_WIDTH / 3, ARENA_BTN_HEIGHT - 2),
                new Rectangle(arenaSelectBtns[3].Rec.X, arenaSelectBtns[3].Rec.Y, ARENA_BTN_WIDTH / 3, ARENA_BTN_HEIGHT - 2),
                new Rectangle(arenaSelectBtns[4].Rec.X, arenaSelectBtns[4].Rec.Y, ARENA_BTN_WIDTH / 3, ARENA_BTN_HEIGHT - 2)
            };

            backBtnImg = Content.Load<Texture2D>("Images/Buttons/ButtonBack");
            backBtnRec = new Rectangle(selectionBox.Rec.X, 15, (int)(backBtnImg.Width / 1.5f), (int)(backBtnImg.Height / 1.5f));

            arenaPlayBtnImg = Content.Load<Texture2D>("Images/Buttons/ButtonPlayArena");
            arenaLockedBtnImg = Content.Load<Texture2D>("Images/Buttons/ButtonLocked");

            for (int i = 0; i < NUM_ARENAS; i++)
            {
                arenaBtnRecs[i] = new Rectangle(arenaSelectBtns[i].Rec.X + arenaSelectBtns[i].Rec.Width / 2 - arenaBtnWidth / 2 + arenaIconRecs[i].Width / 2, arenaSelectBtns[i].Rec.Y + arenaSelectBtns[i].Rec.Height / 2 - arenaBtnHeight / 2, arenaBtnWidth, arenaBtnHeight);
            }

            //Game Over
            gameOverHeadings = new Texture2D[]
            {
                Content.Load<Texture2D>("Images/Texts/Lose"),
                Content.Load<Texture2D>("Images/Texts/Win")
            };

            gameOverHeadingRecs = new Rectangle[]
            {
                new Rectangle(screenWidth / 2 - gameOverHeadings[0].Width / 2, HEADING_SPACER, gameOverHeadings[0].Width, gameOverHeadings[0].Height),
                new Rectangle(screenWidth / 2 - gameOverHeadings[1].Width / 2, HEADING_SPACER, gameOverHeadings[1].Width, gameOverHeadings[1].Height),
            };

            gameOverBtnImgs = new Texture2D[]
            {
                Content.Load<Texture2D>("Images/Buttons/ButtonPlayAgain"),
                Content.Load<Texture2D>("Images/Buttons/ButtonMainMenu")
            };

            gameOverBtnRecs = new Rectangle[]
            {
                new Rectangle(screenWidth / 2 - gameOverBtnImgs[0].Width / 2, screenHeight / 2 - gameOverBtnImgs[0].Height, gameOverBtnImgs[0].Width, gameOverBtnImgs[0].Height),
                new Rectangle(screenWidth / 2 - gameOverBtnImgs[1].Width / 2, screenHeight / 2 + gameOverBtnImgs[1].Height, gameOverBtnImgs[1].Width, gameOverBtnImgs[1].Height)
            };
        }

        protected override void Update(GameTime gameTime)
        {
            //Update keyboard and mouse states
            prevMouse = mouse;
            mouse = Mouse.GetState();
            prevKb = kb;
            kb = Keyboard.GetState();

            //Update game states
            switch (gameState)
            {
                case MENU:
                    UpdateMenu(gameTime);
                    break;
                case INSTRUCTIONS:
                    UpdateInstructions(gameTime);
                    break;
                case SELECTION:
                    UpdateArenaSelection(gameTime);
                    break;
                case GAME_SETUP:
                    GameSetUp();
                    break;
                case GAME:
                    UpdateGame(gameTime);
                    break;
                case PAUSE:
                    UpdatePause();
                    break;
                case GAMEOVER:
                    UpdateGameOver(gameTime);
                    break;
            }


            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            //Draw based on game state
            switch (gameState)
            {
                case MENU:
                    DrawMenu();
                    break;
                case INSTRUCTIONS:
                    DrawInstructions();
                    break;
                case SELECTION:
                    DrawArenaSelection();
                    break;
                case GAME:
                    DrawGame();
                    break;
                case PAUSE:
                    DrawPause();
                    break;
                case GAMEOVER:
                    DrawGameOver();
                    break;
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void UpdateMenu(GameTime gameTime)
        {
            //Update animated background
            bgMenuAnim.Update(gameTime);

            //Check which button is pressed and proceed to appropriate screen
            if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton != ButtonState.Pressed)
            {
                if (menuBtnRecs[BTN_PLAY].Contains(mouse.Position))
                {
                    gameState = SELECTION;
                }
                else if (menuBtnRecs[BTN_INSTRUCTIONS].Contains(mouse.Position))
                {
                    gameState = INSTRUCTIONS;
                }
                else if (menuBtnRecs[BTN_EXIT].Contains(mouse.Position))
                {
                    Exit();
                }
            }
        }

        private void UpdateInstructions(GameTime gameTime)
        {
            //Update animated background
            bgMenuAnim.Update(gameTime);

            //Check if back button is pressed and lead back to menu
            if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton != ButtonState.Pressed)
            {
                if (backBtnRec.Contains(mouse.Position))
                {
                    gameState = MENU;
                }
            }
        }

        private void UpdateArenaSelection(GameTime gameTime)
        {
            //Update animated background
            bgMenuAnim.Update(gameTime);

            //Check which arenas are unlocked
            for (int i = 0; i < isArenaUnlocked.Length; i++)
            {
                if (isArenaUnlocked[i])
                {
                    //Change the arena icon to the preview image instead of lock
                    arenaIcons[i] = arenaPreviews[i];
                }
            }

            //Check which button is pressed and proceed to appropriate screen
            if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton != ButtonState.Pressed)
            {
                if (backBtnRec.Contains(mouse.Position))
                {
                    gameState = MENU;
                }
                else if (arenaBtnRecs[0].Contains(mouse.Position))
                {
                    GameSetUp();
                    curArena = 0;
                }
            }
        }

        private void GameSetUp()
        {
            //Reset all fundamental gameplay variables 
            curWave = 0;
            curRound = 0;

            timesSpawnReset = 0;

            slimes.Clear();

            for (int i = 0; i < enemySpawnCount[SPAWN_TOTAL, curWave, curRound]; i++)
            {
                HandleEnemySpawn();
                spawnCount++;
            }

            knightHealth = KNIGHT_START_HEALTH;
            healthBarKnight = new GameRectangle(GraphicsDevice, new Rectangle(screenWidth - healthBarOffsetX, screenHeight - IN_GAME_SPACER, healthBarWidth, healthBarHeight));

            knightPos = new Vector2(screenWidth / 2, screenHeight / 2);

            //Start game as soon as set up is complete
            gameState = GAME;
        }

        private void UpdateGame(GameTime gameTime)
        {
            //CHEAT: Win arena
            if (kb.IsKeyDown(Keys.O) && !prevKb.IsKeyDown(Keys.O))
            {
                arenaWin = true;
                curArena++;
                isArenaUnlocked[curArena] = true;
                gameState = GAMEOVER;
            }

            //Track if game is over
            if (knightHealth <= 0)
            {
                arenaWin = false;
                gameState = GAMEOVER;
            }

            //Check if to pause game 
            if (kb.IsKeyDown(Keys.Escape) && !prevKb.IsKeyDown(Keys.Escape))
            {
                gameState = PAUSE;
            }

            //MAGIC NUMBERS
            for (int i = 0; i < slimes.Count; i++)
            {
                UpdateHitBox(slimes[i].hitBox, slimes[i].rec, 18, 18, 35, 37);
                CheckCollision(knightHitBox.Rec, slimes[i].rec, KNIGHT_ENEMY_COLLISION, i);
                slimes[i].damageTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
                slimes[i].delayTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            }

            //Update each slimes current position in path tracing
            for (int i = 0; i < slimes.Count; i++)
            {
                slimes[i].start = tileMap[slimes[i].rec.Center.Y / TILE_SIZE + 1, slimes[i].rec.Center.X / TILE_SIZE + 1];
            }
            //Update player (target) position for slime's path tracing
            end = tileMap[knightHitBox.Rec.Bottom / TILE_SIZE + 1, knightHitBox.Rec.Center.X / TILE_SIZE + 1];

            //Calculate costs of tiles to reach target
            SetHCosts(tileMap, end.row, end.col);

            //Update each slime's path and position
            for (int i = 0; i < slimes.Count; i++)
            {
                slimes[i].path = FindPath(tileMap, slimes[i].start, end, slimes[i].open, slimes[i].closed);
                UpdateEnemyPos(gameTime, i);
            }

            KnightController(gameTime);

            //Check whether knight is fighting
            if (isKnightAttacking)
            {
                Combat();
            }
            else
            {
                //Reset counter back to 0 for future use in detecing sword-enemy collision
                frameCounter = 0;
            }
        }

        private void UpdatePause()
        {
            //Check is key is pressed to return back to game
            if (kb.IsKeyDown(Keys.Escape) && !prevKb.IsKeyDown(Keys.Escape))
            {
                gameState = GAME;
            }

            //Check which button is pressed and proceed to appropriate screen
            if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton != ButtonState.Pressed)
            {
                if (menuBtnRecs[BTN_RESUME].Contains(mouse.Position))
                {
                    gameState = GAME;
                }
                else if (menuBtnRecs[BTN_MAIN_MENU].Contains(mouse.Position))
                {
                    gameState = MENU;
                }
                else if (menuBtnRecs[BTN_EXIT].Contains(mouse.Position))
                {
                    Exit();
                }
            }

        }

        private void UpdateGameOver(GameTime gameTime)
        {
            //Update animated background
            bgMenuAnim.Update(gameTime);

            //Check which button is pressed and proceed to appropriate screen
            if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton != ButtonState.Pressed)
            {
                if (gameOverBtnRecs[BTN_PLAY_AGAIN].Contains(mouse.Position))
                {
                    GameSetUp();
                }
                else if (gameOverBtnRecs[BTN_MAIN_MENU].Contains(mouse.Position))
                {
                    gameState = MENU;
                }
            }
        }

        //Pre: Gametime
        //Post: None
        //Desc: Controls knight movement, attacks, and speed
        private void KnightController(GameTime gameTime)
        {
            //Default knight movement
            knightDir.X = MOVEMENT_STOPPED;
            knightDir.Y = MOVEMENT_STOPPED;

            //Keep all animations updated
            for (int i = 0; i < knightAnims.Length; i++)
            {
                knightAnims[i].Update(gameTime);
            }

            //Set previous animation to the current animation to use for comparisons 
            prevKnightAnim = currentKnightAnim;

            //Check if space or left mouse is pressed to trigger knight attack. MAGIC NUMBERS
            if ((kb.IsKeyDown(Keys.Space) && !prevKb.IsKeyDown(Keys.Space) && mouse.LeftButton != ButtonState.Pressed)
                || (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton != ButtonState.Pressed && !kb.IsKeyDown(Keys.Space)))
            {
                isKnightAttacking = true;

                //Depending on the previous knight animation state, play the matching attack animation and update hitboxes
                switch (prevKnightAnim)
                {
                    case KNIGHT_IDLE:
                        currentKnightAnim = KNIGHT_ATTACK_DOWN;

                        subHitBoxHeight = 50;
                        hitBoxDown = 35;

                        break;
                    case KNIGHT_RUN_DOWN:
                        currentKnightAnim = KNIGHT_ATTACK_DOWN;

                        subHitBoxHeight = 50;
                        hitBoxDown = 35;

                        break;
                    case KNIGHT_RUN_LEFT:
                        currentKnightAnim = KNIGHT_ATTACK_LEFT;

                        subHitBoxWidth = 70;
                        hitBoxRight = 30;

                        break;
                    case KNIGHT_RUN_RIGHT:
                        currentKnightAnim = KNIGHT_ATTACK_RIGHT;

                        subHitBoxWidth = 70;
                        hitBoxRight = 40;

                        break;
                    case KNIGHT_RUN_UP:
                        currentKnightAnim = KNIGHT_ATTACK_UP;
                        break;
                }
                //Keep knight animated
                knightAnims[currentKnightAnim].isAnimating = true;
            }

            //Check if knight is not attacking
            if (knightAnims[KNIGHT_ATTACK_DOWN].isAnimating == false && knightAnims[KNIGHT_ATTACK_LEFT].isAnimating == false && knightAnims[KNIGHT_ATTACK_RIGHT].isAnimating == false && knightAnims[KNIGHT_ATTACK_UP].isAnimating == false)
            {
                isKnightAttacking = false;

                //Update hitbox
                subHitBoxHeight = 16;
                hitBoxDown = 18;

                //Check if knight is moving; updating the position and hitbox
                if (kb.IsKeyDown(Keys.S) && !kb.IsKeyDown(Keys.W))
                {
                    currentKnightAnim = KNIGHT_RUN_DOWN;
                    knightDir.Y = MOVEMENT_POSITIVE;
                    knightFacing = DOWN;

                    subHitBoxHeight = 37;
                    hitBoxDown = 35;
                    hitBoxRight = 32;
                    subHitBoxWidth = 65;

                    //Check if the tile below can be walked through by the knight
                    KnightImpassCheck(tileMap[end.row + 1, end.col], MOVEMENT_POSITIVE, false);
                }
                else if (kb.IsKeyDown(Keys.W) && !kb.IsKeyDown(Keys.S))
                {
                    currentKnightAnim = KNIGHT_RUN_UP;
                    knightFacing = UP; //WAS MISSING IN ORIGINAL

                    hitBoxRight = 32;
                    subHitBoxWidth = 65;
                    hitBoxDown = 40;
                    subHitBoxHeight = 50;

                    //Check if the tile above can be walked through by the knight
                    KnightImpassCheck(tileMap[end.row - 1, end.col], MOVEMENT_NEGATIVE, false);
                }
                else if (kb.IsKeyDown(Keys.A) && !kb.IsKeyDown(Keys.D))
                {
                    currentKnightAnim = KNIGHT_RUN_LEFT;
                    knightFacing = LEFT;

                    subHitBoxWidth = 67;
                    hitBoxRight = 27;
                    subHitBoxHeight = 35;
                    hitBoxDown = 20;

                    //Check if the tile to the left can be walked through by the knight
                    KnightImpassCheck(tileMap[end.row, end.col - 1], MOVEMENT_NEGATIVE, true);
                }
                else if (kb.IsKeyDown(Keys.D) && !kb.IsKeyDown(Keys.A))
                {
                    currentKnightAnim = KNIGHT_RUN_RIGHT;
                    knightDir.X = MOVEMENT_POSITIVE;
                    knightFacing = RIGHT;

                    subHitBoxWidth = 67;
                    hitBoxRight = 40;
                    subHitBoxHeight = 35;
                    hitBoxDown = 25;

                    //Check if the tile to the right can be walked through by the knight
                    KnightImpassCheck(tileMap[end.row, end.col + 1], MOVEMENT_POSITIVE, true);
                }
                else
                {
                    //Play respective idle animation depending on where knight is facing
                    switch (knightFacing)
                    {
                        case DOWN:
                            currentKnightAnim = KNIGHT_IDLE;

                            hitBoxRight = 32;
                            subHitBoxWidth = 65;
                            subHitBoxHeight = 37;
                            hitBoxDown = 35;

                            break;
                        case UP:
                            knightAnims[currentKnightAnim].curFrame = 2;

                            hitBoxRight = 32;
                            subHitBoxWidth = 65;
                            hitBoxDown = 40;
                            subHitBoxHeight = 50;

                            break;
                        default:
                            knightAnims[currentKnightAnim].curFrame = 5;
                            if (knightFacing == LEFT)
                            {
                                subHitBoxWidth = 67;
                                hitBoxRight = 27;
                                subHitBoxHeight = 35;
                                hitBoxDown = 25;
                            }
                            else
                            {
                                subHitBoxWidth = 67;
                                hitBoxRight = 40;
                                subHitBoxHeight = 35;
                                hitBoxDown = 25;
                            }
                            break;
                    }
                }
            }

            //Multiply knight speed by the speed factor of the tile it's standing on
            if (end.tileType == GRASS)
            {
                speedFactor = GRASS_MULTIPLIER;
            }
            else if (end.tileType == STONE)
            {
                speedFactor = STONE_MULTIPLIER;
            }

            //Update knight speed
            knightSpeed.X = knightDir.X * (KNIGHT_SPEED * speedFactor * (float)gameTime.ElapsedGameTime.TotalSeconds);
            knightSpeed.Y = knightDir.Y * (KNIGHT_SPEED * speedFactor * (float)gameTime.ElapsedGameTime.TotalSeconds);

            knightPos.X += knightSpeed.X;
            knightPos.Y += knightSpeed.Y;

            knightAnims[currentKnightAnim].destRec.X = (int)knightPos.X;
            knightAnims[currentKnightAnim].destRec.Y = (int)knightPos.Y;

            //Update knight hitbox based off previously defined values. MAGIC NUMBERS
            UpdateHitBox(knightHitBox, knightAnims[currentKnightAnim].destRec, hitBoxRight, hitBoxDown, subHitBoxWidth, subHitBoxHeight);
        }

        //Pre: Hitbox rectangle as a gamerectangle, corresponding image rectangle, values to add to x and y values in ints, values to subtract with and height by, in ints
        //Post: None
        //Desc: Updates the dimensions of the given hitbox 
        private void UpdateHitBox(GameRectangle hitBox, Rectangle imgRec, int xAdd, int yAdd, int widthSub, int heightSub)
        {
            //MAGIC NUMBERS
            hitBox.TranslateTo(imgRec.X + xAdd, imgRec.Y + yAdd);
            hitBox.Width = imgRec.Width - widthSub;
            hitBox.Height = imgRec.Height - heightSub;
        }

        //Pre: Gametime, the index of the enemy as an int
        //Post: None
        //Desc: Updates the enemy's position in the arena
        private void UpdateEnemyPos(GameTime gameTime, int enemyIndex)
        {
            //Check if enemy has more than one tile in its path to follow
            if (slimes[enemyIndex].path.Count > 0)
            {
                //Check if enemy still has to move towards the target tile or if it's on it
                if (slimes[enemyIndex].path.Count > 1)
                {
                    curEnemyTile = 1;
                }
                else
                {
                    curEnemyTile = 0;
                }

                //Move enemy in correct X-direction depending on the next path tile
                if (slimes[enemyIndex].path[curEnemyTile].rec.Center.X < slimes[enemyIndex].hitBox.Rec.Center.X)
                {
                    enemyDir.X = MOVEMENT_NEGATIVE;
                }
                else if (slimes[enemyIndex].path[curEnemyTile].rec.Center.X > slimes[enemyIndex].hitBox.Rec.Center.X)
                {
                    enemyDir.X = MOVEMENT_POSITIVE;
                }
                else if (slimes[enemyIndex].path[curEnemyTile].rec.Center.X == slimes[enemyIndex].hitBox.Rec.Center.X)
                {
                    enemyDir.X = MOVEMENT_STOPPED;
                }

                //Move enemy in correct Y-direction depending on the next path tile
                if (slimes[enemyIndex].path[curEnemyTile].rec.Bottom < slimes[enemyIndex].hitBox.Rec.Bottom)
                {
                    enemyDir.Y = MOVEMENT_NEGATIVE;
                }
                else if (slimes[enemyIndex].path[curEnemyTile].rec.Bottom > slimes[enemyIndex].hitBox.Rec.Bottom)
                {
                    enemyDir.Y = MOVEMENT_POSITIVE;
                }
                else if (slimes[enemyIndex].path[curEnemyTile].rec.Bottom == slimes[enemyIndex].hitBox.Rec.Bottom)
                {
                    enemyDir.Y = MOVEMENT_STOPPED;
                }
            }

            //Change enemy speed depending on which tile it's on
            if (slimes[enemyIndex].path[0].tileType == GRASS)
            {
                speedFactor = GRASS_MULTIPLIER;
            }
            else if (slimes[enemyIndex].path[0].tileType == STONE)
            {
                speedFactor = STONE_MULTIPLIER;
            }

            //Diagonal speed calibration
            if (enemyDir.X != MOVEMENT_STOPPED && enemyDir.Y != MOVEMENT_STOPPED)
            {
                enemySpeedAdj = (float)Math.Sqrt(Math.Pow(slimes[enemyIndex].speed * speedFactor, 2) / 2);
            }
            else
            {
                enemySpeedAdj = slimes[enemyIndex].speed * speedFactor;
            }

            //Stop enemy from moving if its delay timer has not finished
            if (slimes[enemyIndex].delayTimer.IsActive())
            {
                enemySpeedAdj = 0;
            }

            //Update enemy position
            enemySpeed.X = enemyDir.X * (enemySpeedAdj * speedFactor * (float)gameTime.ElapsedGameTime.TotalSeconds);
            enemySpeed.Y = enemyDir.Y * (enemySpeedAdj * speedFactor * (float)gameTime.ElapsedGameTime.TotalSeconds);

            slimes[enemyIndex].pos.X += enemySpeed.X;
            slimes[enemyIndex].pos.Y += enemySpeed.Y;

            slimes[enemyIndex].rec.X = (int)slimes[enemyIndex].pos.X;
            slimes[enemyIndex].rec.Y = (int)slimes[enemyIndex].pos.Y;
        }

        //Pre: None
        //Post: None
        //Desc: Checks if knight is fighting and strikes an enemy
        private void Combat() 
        {
            //Set the sword rectangle equal to the animation's rectangle
            swordRec = knightAnims[currentKnightAnim].destRec;

            //Reduce so that the rectangle can be moved accordingly and only hit enemies in the direction its facing
            swordRec.Height = swordRec.Height / 2;

            //Check if knight is facing up or down
            if (knightFacing == DOWN || knightFacing == UP)
            {
                //If facing down, adjust the sword rectangle so its on the bottom half 
                if (knightFacing == DOWN)
                {
                    swordRec.Y = swordRec.Y + swordRec.Height;
                }
            }
            else if (knightFacing == LEFT || knightFacing == RIGHT)
            {
                //Reduce sword rectangle width so it only hits enemies its facing
                swordRec.Width = swordRec.Width / 2;

                //Reduce y-range of sword rectangle
                swordRec.Y = swordRec.Y + knightAnims[currentKnightAnim].destRec.Height / 2 - swordRec.Height / 3;

                //Move sword rectangle if knight is facing right
                if (knightFacing == RIGHT)
                {
                    swordRec.X = swordRec.X + swordRec.Width;
                }
            }
            
            //Check if the current frame is the attack frame
            if (knightAnims[currentKnightAnim].curFrame == ATTACK_FRAME)
            {
                //Check each slime for collision
                for (int i = 0; i < slimes.Count; i++)
                {
                    CheckCollision(slimes[i].hitBox.Rec, swordRec, KNIGHT_ATTACK_COLLISION, i);
                }
            }
        }

        //Pre: the two rectangles to be checking collision between, the collision type as an int (defined as a constant), and index of enemy
        //Post: None
        //Desc: Checks if collision occured and runs appropriate collision subprograms
        private void CheckCollision(Rectangle rec1, Rectangle rec2, int collisionType, int enemyIndex)
        {
            //Check if rectangles intersect
            if (rec1.Intersects(rec2))
            {
                //Perform appropriate collision calculations
                switch (collisionType)
                {
                    case KNIGHT_ENEMY_COLLISION:
                        //Start enemy timer if knight collides with it
                        slimes[enemyIndex].damageTimer.Activate();
                        KnightEnemyCollision(enemyIndex);
                        break;
                    case KNIGHT_ATTACK_COLLISION:
                        KnightAttackCollision(enemyIndex);
                        break;
                }
            }
        }

        //Pre: Index of enemy
        //Post: None
        //Desc: Calculates damage from enemy onto knight
        private void KnightEnemyCollision(int enemyIndex)
        {
            //Check if enemy damager timer is finished
            if (slimes[enemyIndex].damageTimer.IsFinished())
            {
                //Reduce knight health and update health bar
                knightHealth -= slimes[enemyIndex].damage;
                healthBarKnight.Width = (int)((double)knightHealth / KNIGHT_START_HEALTH * healthBarBackKnight.Width);

                //Reset slime's damager timer
                slimes[enemyIndex].damageTimer = new Timer(SLIME_DAMAGE_TIME, true);
            }
        }

        //Pre: Index of enemy
        //Post: None
        //Desc: Calculates damage from knight onto enemy
        private void KnightAttackCollision(int enemyIndex)
        {
            //Update the frame counter
            frameCounter++;

            //Check if current animation frame is equal to the point in the animation where the sword is out
            if (frameCounter == 1)
            {
                //Deal damage to slimes
                slimes[enemyIndex].health -= KNIGHT_SWORD_DAMAGE;
            }

            //Check if slime is dead
            if (slimes[enemyIndex].health <= 0)
            {
                HandleEnemyDeath(enemyIndex);
            }
        }

        //Pre: The node the knight's moving towards, the direction of movement, whether the knight is moving horizontally as a bool
        //Post: None
        //Desc: Checks if knight can move depending on passibility of the node it's moving towards
        private void KnightImpassCheck(Node nextNode, int movement, bool horizontal)
        {
            //Check if knight is moving horizontally and perform appropriate calculations
            if (horizontal)
            {
                //Check if the tile they're moving towards is one they shouldn't be able to walk through
                if (nextNode.tileType == IMPASS || nextNode.tileType == FENCE || nextNode.tileType == BLOCK)
                {

                    knightDir.X = MOVEMENT_STOPPED;
                }
                else
                {
                    knightDir.X = movement;
                }
            }
            else
            {
                //Check if the tile they're moving towards is one they shouldn't be able to walk through
                if (nextNode.tileType == IMPASS || nextNode.tileType == FENCE || nextNode.tileType == BLOCK)
                {
                    knightDir.Y = MOVEMENT_STOPPED;
                }
                else
                {
                    knightDir.Y = movement;
                }
                
            }
        }

        //Pre: Index of enemy
        //Post: None
        //Desc: Checks how enemies should be respawned and rounds/waves affected as a result
        private void HandleEnemyDeath(int enemyIndex)
        {
            //Remove slime from list
            slimes.RemoveAt(enemyIndex);

            //Check if any slimes are remaining
            if (slimes.Count == 0)
            {
                //Check if there are any more rounds remaining in waves, and waves remaining in the arena
                if (curWave + 1 == enemySpawnCount.GetLength(1) && curRound + 1 == enemySpawnCount.GetLength(2))
                {
                    //Arena has been complete
                    gameState = GAMEOVER;
                    arenaWin = true;
                    curArena++;
                    isArenaUnlocked[curArena] = true;
                }
                else
                {
                    //Spawn next round

                    //Reset enemy spawn count
                    spawnCount = 0;

                    //Check if there are rounds remaining in the wave
                    if (curRound + 1 < enemySpawnCount.GetLength(2))
                    {
                        curRound++;
                    }
                    else
                    {
                        //Move onto the next wave
                        curRound = 0;
                        curWave++;
                    }

                    //Spawn each enemy depending on how many can be spawned at once
                    for (int i = 0; i < enemySpawnCount[SPAWN_CURRENT, curWave, curRound]; i++)
                    {
                        HandleEnemySpawn();
                        spawnCount++;

                        //Check how many times the enemies looped over the same spawn point
                        timesSpawnReset = (spawnCount + 1) / enemySpawnLocs.Count;
                    }
                }
            }
            else if (slimes.Count < enemySpawnCount[SPAWN_CURRENT, curWave, curRound])
            {
                //Check if the amount of enemies spawned in this round are less than the total amount that can be spawned
                if (spawnCount < enemySpawnCount[SPAWN_TOTAL, curWave, curRound])
                {
                    //Reset so that new enemies spawned in don't get delayed
                    timesSpawnReset = 0;

                    HandleEnemySpawn();
                    spawnCount++;
                }
            }
        }

        //Pre: None
        //Post: None
        //Desc: Spawns enemies
        private void HandleEnemySpawn()
        {
            //Set the spawn delay equal to the number of enemies spawned in the same spawn point at the same time
            spawnDelay = SPAWN_DELAY * timesSpawnReset;

            //Chec if there are no more spawn points left
            if (enemySpawnLocCounter >= enemySpawnLocs.Count)
            {
                //Reset counter so its back to the first spawn point
                enemySpawnLocCounter = 0;
            }

            //Define rectangle for the slime to be spawned
            Rectangle slimeRec = new Rectangle(enemySpawnLocs[enemySpawnLocCounter].X - slimeImg.Width / 2, enemySpawnLocs[enemySpawnLocCounter].Y - slimeImg.Height / 2, slimeImg.Width * 2, slimeImg.Height * 2);

            //Add slime to list
            slimes.Add(new Enemy(SLIME_DAMAGE, SLIME_START_HEALTH, SLIME_DEFAULT_SPEED, 0, spawnDelay, new Vector2(slimeRec.X, slimeRec.Y),
                                    slimeImg, slimeRec, new GameRectangle(GraphicsDevice, slimeRec)));

            //Move onto next spawn point
            enemySpawnLocCounter++;
        }

        //Pre:Button rectangle, on/off colors
        //Post: None
        //Desc: Changes button color depending on hover state
        private void ButtonHover(Rectangle btnRec, Color hoverClr, Color offClr)
        {
            //Check if mouse cursor is over button
            if (btnRec.Contains(mouse.Position))
            {
                //Change button color to hover color
                btnColor = hoverClr;
            }
            else
            {
                //Change button color to default color
                btnColor = offClr;
            }
        }

        private void DrawMenu()
        {
            bgMenuAnim.Draw(_spriteBatch, Color.LightGray, Animation.FLIP_NONE);
            _spriteBatch.Draw(titleImg, titleRec, Color.White);

            //Check all menu buttons
            for (int i = 0; i < menuBtnImgs.Length; i++)
            {
                //Check if the current button is the first menu buttono
                if (i == 0)
                {
                    //Maintain the original defined position of the first button
                    menuBtnY = menuBtnRecs[i].Y;
                }
                else
                {
                    //Change the y-value of the other buttons based off the previous button's y-value
                    menuBtnY += (int)(menuBtnImgs[i].Height * 1.7f);
                }

                //Apply change in y-position to respective rectangle
                menuBtnRecs[i].Y = menuBtnY;

                //Check if button is being hovered over
                ButtonHover(menuBtnRecs[i], Color.White, Color.LightGray);
                _spriteBatch.Draw(menuBtnImgs[i], menuBtnRecs[i], btnColor);
            }
        }

        private void DrawInstructions()
        {
            bgMenuAnim.Draw(_spriteBatch, Color.LightGray, Animation.FLIP_NONE);
            _spriteBatch.DrawString(headingFont, "Instructions", instructionTxtLoc, Color.White); //CHANGE TXT LOC

            _spriteBatch.Draw(instructionsImg, instructionsRec, Color.White);

            ButtonHover(backBtnRec, Color.White, Color.LightGray);
            _spriteBatch.Draw(backBtnImg, backBtnRec, btnColor);
        }

        private void DrawArenaSelection()
        {
            bgMenuAnim.Draw(_spriteBatch, Color.LightGray, Animation.FLIP_NONE);
            _spriteBatch.DrawString(headingFont, "Arena Selection", arenaSelectTxtLoc, Color.White);

            selectionBox.Draw(_spriteBatch, Color.DimGray, true);

            //Loop through all side selection buttons
            for (int i = 0; i < NUM_ARENAS; i++)
            {
                arenaSelectBtnOutlines[i].Draw(_spriteBatch, Color.Black, false);

                //Check if mouse is over selection button
                ButtonHover(arenaSelectBtns[i].Rec, Color.LightGray, Color.Gray);
                arenaSelectBtns[i].Draw(_spriteBatch, btnColor, true);

                //Check if selection box is hovered over
                if (btnColor == Color.LightGray)
                {
                    //Check if arena is unlocked
                    if (isArenaUnlocked[i] == true)
                    {
                        //Draw arena preview
                        _spriteBatch.Draw(arenaPreviews[i], arenaPreviewRec, Color.White);
                        _spriteBatch.DrawString(headingFont, arenaNames[i], arenaNameLocs[i], Color.White);
                    }
                    //Update arena's play or lock button if mouse is hovered over it
                    ButtonHover(arenaBtnRecs[i], Color.White, Color.LightGray);
                }
                else
                {
                    btnColor = Color.Gray;
                }

                //Check if each arena is unlocked
                if (isArenaUnlocked[i])
                {
                    //Draw play button for respective arena
                    _spriteBatch.Draw(arenaPlayBtnImg, arenaBtnRecs[i], btnColor);
                }
                else
                {
                    //Draw locked button for respective arena
                    _spriteBatch.Draw(arenaLockedBtnImg, arenaBtnRecs[i], btnColor);
                }
            }

            //Loop through all the arena iocns
            for (int i = 0; i < arenaIconRecs.Length; i++)
            {
                //Check if the icon's respective selection button has mouse hovering
                if (arenaSelectBtns[i].Rec.Contains(mouse.Position))
                {
                    btnColor = Color.White;
                }
                else
                {
                    btnColor = Color.LightGray;
                }

                _spriteBatch.Draw(arenaIcons[i], arenaIconRecs[i], btnColor);
            }

            //Check for mouse hover on back button
            ButtonHover(backBtnRec, Color.White, Color.LightGray);
            _spriteBatch.Draw(backBtnImg, backBtnRec, btnColor);
        }

        private void DrawGame()
        {
            //Draw base map
            DrawTerrain(_spriteBatch, tileMap);

            //Draw all slimes
            for (int i = 0; i < slimes.Count; i++)
            {
                _spriteBatch.Draw(slimeImg, slimes[i].rec, Color.White);
            }

            knightAnims[currentKnightAnim].Draw(_spriteBatch, Color.White, Animation.FLIP_NONE);

            //Loop through all the tree instances in the map
            for (int i = 0; i < treeRecs.Count; i++)
            {
                _spriteBatch.Draw(treeImg, treeRecs[i], Color.White);

                //Check whether the knight is on top or below the tree, and shift drawing order respectively to create perspective
                if (treeRecs[i].Contains(knightAnims[currentKnightAnim].destRec.Center.X, knightAnims[currentKnightAnim].destRec.Bottom))
                {
                    knightAnims[currentKnightAnim].Draw(_spriteBatch, Color.White, Animation.FLIP_NONE);
                    _spriteBatch.Draw(treeImg, treeRecs[i], Color.White);
                }
                else if (treeRecs[i].Contains(knightAnims[currentKnightAnim].destRec.Center.X, knightAnims[currentKnightAnim].destRec.Top)
                        || treeRecs[i].Contains(knightAnims[currentKnightAnim].destRec.Right, knightAnims[currentKnightAnim].destRec.Center.Y)
                        || treeRecs[i].Contains(knightAnims[currentKnightAnim].destRec.Left, knightAnims[currentKnightAnim].destRec.Center.Y))
                {
                    _spriteBatch.Draw(treeImg, treeRecs[i], Color.White);
                    knightAnims[currentKnightAnim].Draw(_spriteBatch, Color.White, Animation.FLIP_NONE);
                }
            }

            healthBarBackKnight.Draw(_spriteBatch, Color.DarkRed, true);
            healthBarKnight.Draw(_spriteBatch, Color.Red, true);

            _spriteBatch.DrawString(subHeadingFont, "Wave " + (curWave + 1), waveTxtLoc, Color.White);
            _spriteBatch.DrawString(textFont, "Health", healthTxtLoc, Color.White);
        }

        private void DrawTerrain(SpriteBatch _spriteBatch, Node[,] map)
        {
            //Draw correct tile image over corresponding tile type
            for (int row = 0; row < NUM_ROWS; row++)
            {
                for (int col = 0; col < NUM_COLS; col++)
                {
                    //Set base tile underneath all images as grass
                    _spriteBatch.Draw(grassTileImg, map[row, col].rec, Color.White);

                    //Check if tile is stone
                    if (map[row, col].tileType == STONE)
                    {
                        _spriteBatch.Draw(stoneTileImg, map[row, col].rec, Color.White);
                    }
                    //Check if tile is fence 
                    if (map[row, col].tileType == FENCE)
                    {
                        //Check positioning of the fence to draw appropriate fence image
                        if (row == 1 && (col == 1 || col == NUM_COLS - 2))
                        {
                            switch (col)
                            {
                                case 1:
                                    _spriteBatch.Draw(fenceTileImgs[FENCE_CORNER_TOP], map[row, col].rec, Color.White);
                                    break;
                                case NUM_COLS - 2:
                                    //Flip horizontally
                                    _spriteBatch.Draw(fenceTileImgs[FENCE_CORNER_TOP], map[row, col].rec, null, Color.White, 0f,
                                                       new Vector2(0, 0), SpriteEffects.FlipHorizontally, 0f);
                                    break;
                            }
                        }
                        else if (row == NUM_ROWS - 2 && (col == 1 || col == NUM_COLS - 2))
                        {
                            switch (col)
                            {
                                case 1:
                                    _spriteBatch.Draw(fenceTileImgs[FENCE_CORNER_BOTTOM], map[row, col].rec, Color.White);
                                    break;
                                case NUM_COLS - 2:
                                    //Flip horizontally
                                    _spriteBatch.Draw(fenceTileImgs[FENCE_CORNER_BOTTOM], map[row, col].rec, null, Color.White, 0f,
                                                      new Vector2(0, 0), SpriteEffects.FlipHorizontally, 0f);
                                    break;
                            }
                        }
                        else if (row == 1 || row == NUM_ROWS - 2)
                        {
                            _spriteBatch.Draw(fenceTileImgs[FENCE_MID], map[row, col].rec, Color.White);
                        }
                        else if (col == 1 || col == NUM_COLS - 2)
                        {
                            _spriteBatch.Draw(fenceTileImgs[FENCE_SIDE], map[row, col].rec, Color.White);
                        }
                    }
                }
            }
        }

        private void DrawPause()
        {
            _spriteBatch.Draw(arenaPreviews[curArena], new Rectangle(0, 0, screenWidth, screenHeight), Color.Gray);
            _spriteBatch.Draw(titleImg, titleRec, Color.White);

            //Check all menu buttons
            for (int i = 0; i < pauseBtnImgs.Length; i++)
            {
                //Check if the current button is the first menu buttono
                if (i == 0)
                {
                    //Maintain the original defined position of the first button
                    menuBtnY = menuBtnRecs[i].Y;
                }
                else
                {
                    //Change the y-value of the other buttons based off the previous button's y-value
                    menuBtnY += (int)(menuBtnImgs[i].Height * 1.7f);
                }
                //Apply change in y-position to respective rectangle
                menuBtnRecs[i].Y = menuBtnY;

                //Check if mouse is hovering over button
                ButtonHover(menuBtnRecs[i], Color.White, Color.LightGray);
                _spriteBatch.Draw(pauseBtnImgs[i], menuBtnRecs[i], btnColor);
            }
        }

        private void DrawGameOver()
        {
            bgMenuAnim.Draw(_spriteBatch, Color.LightGray, Animation.FLIP_NONE);

            int gameOverHeading = Convert.ToInt32(arenaWin);
            _spriteBatch.Draw(gameOverHeadings[gameOverHeading], gameOverHeadingRecs[gameOverHeading], Color.White);

            //Loop through each button
            for (int i = 0; i < gameOverBtnImgs.Length; i++)
            {
                //Check if mouse is hovering above
                ButtonHover(gameOverBtnRecs[i], Color.White, Color.LightGray);
                _spriteBatch.Draw(gameOverBtnImgs[i], gameOverBtnRecs[i], btnColor);
            }
        }

        //////////////////////
        ///PATH FINDING LOGIC
        //////////////////////

        //Pre: Map nodes, enemy start node, knight target knight, the open and closed lists
        //Post: List of tiles comprosing of the path
        //Desc: Find the shortest path, if it exists, from the starting enemy tile to the end knight tile
        private List<Node> FindPath(Node[,] map, Node start, Node end, List<Node> open, List<Node> closed)
        {
            //Clear open and closed lists
            open.Clear();
            closed.Clear();

            //Maintain a resulting path to return
            List<Node> result = new List<Node>();

            //Variables to be recalculated in each iteration of finding potential path nodes
            float minF = 10000f;    //Used to find the cheapest F cost in the open list of Nodes. MAGIC NUMBER
            int minIndex = 0;       //Tracks the open list index of the smallest F cost Node
            Node curNode;           //Tracks the Node with the minimum F cost to be further tested

            //Reset values
            for (int row = 0; row < NUM_ROWS; row++)
            {
                for (int col = 0; col < NUM_COLS; col++)
                {
                    tileMap[row, col].g = 0;
                    tileMap[row, col].h = 0;
                    tileMap[row, col].f = tileMap[row, col].g + tileMap[row, col].h;
                    tileMap[row, col].parent = null;
                }
            }

            //2. Add start point to the open list
            open.Add(start);

            ///////////////////////
            ///Repeat the following steps until A) target is added to closed list (path found), or B) open list is empt (no path)
            /////////////////////

            //Loop until a path is found or it is impossible to find a path
            while (true)
            {
                //3. Find smallest F cost in open list, consider it the current node and remove it from the open List
                minF = 10000f;
                for (int i = 0; i < open.Count; i++)
                {
                    if (open[i].f < minF)
                    {
                        //Set the current minimum F and index it is at
                        minF = open[i].f;
                        minIndex = i;
                    }
                }

                //Minimum F cost has been found at minIndex, setup the current Node by
                //tracking it, removing it from the open list and adding it to the closed list
                curNode = open[minIndex];
                open.RemoveAt(minIndex);
                closed.Add(curNode);

                //4. If the added node is the target, Stop (Path found)
                if (curNode.id == end.id)
                {
                    //Path found, stop searching
                    break;
                }

                //5 Go through each of the current node's adjacent Nodes
                //5a. Ignore if it is is already in the closed list or impassible
                //5b. If it is not in the open list, set its parent to current node, recalculate its G,H, and F and add it to the open list
                //5c. If it is already in the open list, compare its current G with its potential new G cost. If the new G cost is less, set its parent to current node
                //5d. If you are sorting the open list by F, this would be the time to redo that
                Node compNode;
                for (int i = 0; i < curNode.adjacent.Count; i++)
                {
                    //Retrieve the next adjacent Node of curNode, this will be our comparison Node
                    compNode = curNode.adjacent[i];

                    //5a. Check it is not in the closed list and its a [walkable type]<-not yet applicable
                    if (ContainsNode(closed, compNode) == NOT_FOUND)
                    {
                        //At this point we know that compNode will be added or is in the open list. In both cases, recalculate its G cost
                        float newG = GetGCost(compNode, curNode);

                        //5b. Check the Open List
                        if (ContainsNode(open, compNode) == NOT_FOUND)
                        {
                            //Set parent
                            compNode.parent = curNode;

                            //Recalculate G and F
                            compNode.g = newG;
                            compNode.f = compNode.g + compNode.h;

                            //Add it to open list
                            open.Add(compNode);
                        }
                        else
                        {
                            //5c, It is in the open list, compare its current G against its potential new G to see which is better(lower)
                            if (newG < compNode.g)
                            {
                                //The new parent is a better parent, reset compNode's parent and G, F cost to reflect this. UPDATE H
                                compNode.parent = curNode;
                                compNode.g = newG;
                                compNode.f = compNode.g + compNode.h;
                            }
                        }
                    }
                }

                //6. If the open
                if (open.Count == 0)
                {
                    //Path not possible, stop searching
                    break;
                }
            }

            //7. If a path is found, retrace the steps starting at the end going through each parent until the start is reached
            if (ContainsNode(closed, end) != NOT_FOUND)
            {
                //If a path was found, track it back from the end Node
                Node pathNode = end;

                //Keep tracking back until the start Node, which has no parent, is reached
                while (pathNode != null)
                {
                    //Always add the next path Node to the front of the list to maintain order
                    result.Insert(0, pathNode);

                    //Get the next parent
                    pathNode = pathNode.parent;
                }
            }

            //8. Return the resulting path (empty or full)
            return result;
        }

        //Pre:  Given grid tile row and column, target tile row and column
        //Post: H cost to get to respective position
        //Desc: Determine the H cost from the given grid coordinate to the target using the Manhattan heuristic
        private float GetHCost(int tileRow, int tileCol, int targetRow, int targetCol)
        {
            //Using the Manhattan heuristic (cost to move from the current location to the target, making only horizontal and vertical movements
            return (float)Math.Abs(targetRow - tileRow) * hvCost + (float)Math.Abs(targetCol - tileCol) * hvCost;
        }

        //Pre: Map nodes, target row and column
        //Post: None
        //Desc: Calculate the H cost of all tiles to the target
        private void SetHCosts(Node[,] map, int targetRow, int targetCol)
        {
            for (int row = 0; row < NUM_ROWS; row++)
            {
                for (int col = 0; col < NUM_COLS; col++)
                {
                    //Calculate and set the cost for EACH tile to the end space
                    map[row, col].h = GetHCost(row, col, targetRow, targetCol);
                    map[row, col].f = map[row, col].g + map[row, col].h;
                }
            }
        }

        //Pre: Comparison node and its parent node
        //Post: G cost to move as a float
        //Desc: Calculate the cost from the starting enemy location to the given tile
        private float GetGCost(Node compNode, Node parentNode)
        {
            if (compNode.row == parentNode.row || compNode.col == parentNode.col)
            {
                return parentNode.g + hvCost * tileCosts[compNode.tileType]; 
            }
            else
            {
                //compNode is diagonal to curNode
                return parentNode.g + diagCost * tileCosts[compNode.tileType]; 
            }
        }

        //Pre: List of nodes to check, tile being checked for
        //Post: Tile id as an int
        //Desc: Determine if a given tile exists within a given collection of tiles
        private int ContainsNode(List<Node> nodeList, Node checkNode)
        {
            for (int i = 0; i < nodeList.Count; i++)
            {
                //If both nodes have the same unique ID (Node number) they are a match
                if(nodeList[i].id == checkNode.id)
                {
                    //Node found in list, return the index
                    return i;
                }
            }

            //Node was not found in entire list, return invalid index
            return NOT_FOUND;
        }
    }
}
