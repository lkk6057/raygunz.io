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
        public float speed;
        public Vector2 position;
        public Vector2 ray;
        public bool rayActive;
        public Player(string username,string id, int score,float speed, Vector2 position, Vector2 ray,bool rayActive)
        {
            this.username = username;
            this.id = id;
            this.score = score;
            this.speed = speed;
            this.position = position;
            this.ray = ray;
            this.rayActive = rayActive;
        }
    }
}
