using System;
using System.Collections.Generic;
using System.Text;

namespace raygunz.io
{
    class DataManager
    {
        List<Player> players = new List<Player>();
        public Player getPlayerById(string id)
        {
            return players.Find(x=>x.id==id);
        }
        public void addPlayer(Player player)
        {
            players.Add(player);
        }
        public void removePlayerById(string id)
        {
            players.Remove(getPlayerById(id));
        }
        public void updatePlayerById(string id, Player newPlayer)
        {
            Player player = getPlayerById(id);
            player.username = newPlayer.username;
            player.health = newPlayer.health;
            player.maxHealth = newPlayer.maxHealth;
            player.speed = newPlayer.speed;
            player.size = newPlayer.size;
            player.realSpeed = newPlayer.realSpeed;
            player.position = newPlayer.position;
            player.ray = newPlayer.ray;
            player.rayActive = newPlayer.rayActive;
        }
        public List<Player> getPlayers()
        {
            return players;
        }
    }
}
