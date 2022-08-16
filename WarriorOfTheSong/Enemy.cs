using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

using Helper;

namespace WarriorOfTheSong
{
    public class Enemy
    {
        //Enemy stats
        public int damage;
        public int health;
        public float speed;

        public Timer damageTimer;
        public Timer delayTimer;

        //Positioning
        public Vector2 pos;

        //Image/rec data
        public Texture2D img;
        public Rectangle rec;

        //Hitbox to detect collision
        public GameRectangle hitBox;

        ///////////////
        //Path tracing
        ///////////////
        /////Track the beginning of the path
        public Node start;

        //Track all of the path Nodes
        public List<Node> path = new List<Node>();

        //Maintain two lists, one of Nodes to check and one of potential Nodes
        public List<Node> open = new List<Node>();
        public List<Node> closed = new List<Node>();

        public Enemy(int enemyDamage, int enemyHealth, float enemySpeed, double damageInterval, double delaySpawnInterval, Vector2 enemyPos, Texture2D enemyImg, Rectangle enemyRec, GameRectangle enemyHitBox)
        {
            img = enemyImg;
            rec = enemyRec;

            damageTimer = new Timer(damageInterval, false);
            delayTimer = new Timer(delaySpawnInterval, true);

            damage = enemyDamage;
            health = enemyHealth;
            speed = enemySpeed;

            pos = enemyPos;

            hitBox = enemyHitBox;
        }
    }
}
