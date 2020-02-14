using System;
using System.Collections.Generic;
using System.Text;

namespace raygunz.io
{
    class Player
    {
        public string username;
        public string id;
        public int score;
        public float health;
        public float maxHealth;
        public float speed;
        public float size;
        public float realSpeed;
        public Vector2 position;
        public Vector2 ray;
        public bool rayActive;
        public Player(string username,string id, int score,float health, float maxHealth,float speed,float size,float realSpeed, Vector2 position, Vector2 ray,bool rayActive)
        {
            this.username = username;
            this.id = id;
            this.score = score;
            this.health = health;
            this.maxHealth = maxHealth;
            this.speed = speed;
            this.size = size;
            this.realSpeed = realSpeed;
            this.position = position;
            this.ray = ray;
            this.rayActive = rayActive;
        }
    }
}
