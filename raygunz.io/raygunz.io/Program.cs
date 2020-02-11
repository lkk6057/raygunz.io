using Fleck;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace raygunz.io
{
    class Program
    {
        static void Main(string[] args) => new Program().MainTask().GetAwaiter().GetResult();
        List<IWebSocketConnection> connections = new List<IWebSocketConnection>();
        WebSocketServer server = new WebSocketServer("ws://192.168.1.164:8181");
        DataManager dataManager = new DataManager();
        Random random = new Random();
        public struct Point { public float x; public float y; }
        async Task MainTask()
        {
            server.Start(socket =>
            {
                IWebSocketConnectionInfo info = socket.ConnectionInfo;
                socket.OnOpen = () => clientConnected(socket);
                socket.OnClose = () => clientDisconnected(socket);
                socket.OnMessage = message => parseMessage(message,socket);
            });
            updatePlayers();
            await Task.Delay(-1);
        }
        void clientConnected(IWebSocketConnection socket)
        {
            
            IWebSocketConnectionInfo info = socket.ConnectionInfo;
            connections.Add(socket);
            Console.WriteLine($"Client {info.Id} connected on {info.ClientIpAddress + ":" + info.ClientPort} ");
        }
        void clientDisconnected(IWebSocketConnection socket)
        {
            IWebSocketConnectionInfo info = socket.ConnectionInfo;
            connections.Remove(socket);
            Player disconnectedPlayer = dataManager.getPlayerById(info.Id.ToString());
            if (disconnectedPlayer != null)
            {
                dataManager.removePlayerById(info.Id.ToString());
            }
            Console.WriteLine($"Client {info.Id} closed on {info.ClientIpAddress + ":" + info.ClientPort} ");
        }
        IWebSocketConnection getConnectionById(string id)
        {
            return connections.Find(x => x.ConnectionInfo.Id.ToString() == id);
        }
        void castAwayFromId(string id,string message)
        {
            foreach (IWebSocketConnection connection in connections)
            {
                if (id!=connection.ConnectionInfo.Id.ToString())
                {
                    connection.Send(message);
                }
            }
        }
        void castMessage(string message)
        {
            foreach (IWebSocketConnection connection in connections)
            {
                    connection.Send(message);
            }
        }
        JObject createPlayerJObject(string username,string id, int score, float speed, float posX, float posY, float rayX, float rayY, bool rayActive)
        {
            JObject newPlayer = new JObject();
            newPlayer.Add("username", username);
            newPlayer.Add("id",id);
            newPlayer.Add("score", score);
            newPlayer.Add("speed", speed);
            newPlayer.Add("posX", posX);
            newPlayer.Add("posY", posY);
            newPlayer.Add("rayX", rayX);
            newPlayer.Add("rayY", rayY);
            newPlayer.Add("rayActive", rayActive);
            return newPlayer;
        }
        JObject createStandardJObject(string type, JObject data)
        {
            JObject jsonObject = new JObject();
            jsonObject.Add("type", type);
            jsonObject.Add("data",data);
            return jsonObject;
        }
        Player JSONToPlayer(JObject player,string playerId)
        {
            string username = player.GetValue("username").ToString();
            string id = playerId;
            int score = int.Parse(player.GetValue("score").ToString());
            float speed = float.Parse(player.GetValue("speed").ToString());
            Vector2 position = new Vector2(float.Parse(player.GetValue("posX").ToString()), float.Parse(player.GetValue("posY").ToString()));
            Vector2 ray = new Vector2(float.Parse(player.GetValue("rayX").ToString()), float.Parse(player.GetValue("rayY").ToString()));
            bool rayActive = bool.Parse(player.GetValue("rayActive").ToString());
            Player newPlayer = new Player(username,id,score,speed,position,ray,rayActive);
            return newPlayer;
        }
        int updatePlayersDelay = 20;
        async Task updatePlayers()
        {
            try
            {
                foreach (IWebSocketConnection connection in connections)
                {
                    IWebSocketConnectionInfo info = connection.ConnectionInfo;
                    JArray playerArray = new JArray();
                    foreach (Player player in dataManager.getPlayers())
                    {
                        if (player.id != info.Id.ToString())
                        {
                            JObject playerObject = createPlayerJObject(player.username,player.id, player.score, player.speed, player.position.x, player.position.y, player.ray.x, player.ray.y, player.rayActive);
                            playerArray.Add(playerObject);
                        }
                    }
                        JObject data = new JObject();
                        data.Add("players", playerArray);
                        JObject jsonObject = createStandardJObject("updatePlayers", data);
                        //Console.WriteLine(jsonObject.ToString());
                        connection.Send(jsonObject.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            await Task.Delay(updatePlayersDelay);
            updatePlayers();
        }
        void parseMessage(string message,IWebSocketConnection socket)
        {
            IWebSocketConnectionInfo info = socket.ConnectionInfo;
            //Console.WriteLine($"Received from {info.Id}: {message}");
            try
            {
                JObject jsonObject = JObject.Parse(message);
                string type = jsonObject.GetValue("type").ToString();
                JObject data = (JObject)jsonObject.GetValue("data");
                if (type=="reportPlayer")
                {
                    Player player = JSONToPlayer(data,info.Id.ToString());
                    dataManager.updatePlayerById(info.Id.ToString(),player);
                }
                else if (type=="requestPlayer")
                {
                    string username = data.GetValue("username").ToString();
                    if (username.Length<=25) {
                        Point spawnPoint = getSpawnPoint();
                        float speed = 1500;
                        JObject newPlayer = createPlayerJObject(username,info.Id.ToString(), 0, speed, spawnPoint.x, spawnPoint.y, spawnPoint.x, spawnPoint.y, false);
                        JObject messageObj = messageObject("setPlayer", newPlayer);
                        Vector2 spawnVector2 = new Vector2(spawnPoint.x, spawnPoint.y);
                        dataManager.addPlayer(new Player(username, info.Id.ToString(), 0, speed, spawnVector2, spawnVector2, false));
                        socket.Send(messageObj.ToString());
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        JObject messageObject(string type, JObject data)
        {
            JObject messageObj = new JObject();
            messageObj.Add("type",type);
            messageObj.Add("data",data);
            return messageObj;
        }
        Point getSpawnPoint()
        {
            return new Point() {x=random.Next(4000)-2000, y = random.Next(2800) - 1400 };
        }
    }
}
