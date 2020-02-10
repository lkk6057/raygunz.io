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
        public List<Player> getPlayers()
        {
            return players;
        }
    }
}
