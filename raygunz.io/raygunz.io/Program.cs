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
        JObject createPlayerJObject(string username,string id, int score,float health,float maxHealth, float speed,float realSpeed, float posX, float posY, float rayX, float rayY, bool rayActive)
        {
            JObject newPlayer = new JObject();
            newPlayer.Add("username", username);
            newPlayer.Add("id",id);
            newPlayer.Add("score", score);
            newPlayer.Add("health", health);
            newPlayer.Add("maxHealth", maxHealth);
            newPlayer.Add("speed", speed);
            newPlayer.Add("realSpeed", realSpeed);
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
            float health = float.Parse(player.GetValue("health").ToString());
            float maxHealth = float.Parse(player.GetValue("maxHealth").ToString());
            float speed = float.Parse(player.GetValue("speed").ToString());
            float realSpeed = float.Parse(player.GetValue("realSpeed").ToString());
            Vector2 position = new Vector2(float.Parse(player.GetValue("posX").ToString()), float.Parse(player.GetValue("posY").ToString()));
            Vector2 ray = new Vector2(float.Parse(player.GetValue("rayX").ToString()), float.Parse(player.GetValue("rayY").ToString()));
            bool rayActive = bool.Parse(player.GetValue("rayActive").ToString());
            Player newPlayer = new Player(username,id,score,health,maxHealth,speed,realSpeed,position,ray,rayActive);
            return newPlayer;
        }
        int updatePlayersDelay = 20;
        async Task updatePlayers()
        {
            try
            {
                checkRayCollisions();
                foreach (IWebSocketConnection connection in connections)
                {
                    IWebSocketConnectionInfo info = connection.ConnectionInfo;
                    JArray playerArray = new JArray();
                    foreach (Player player in dataManager.getPlayers())
                    {
                        if (player.id != info.Id.ToString())
                        {
                            JObject playerObject = createPlayerJObject(player.username,player.id, player.score,player.health,player.maxHealth, player.speed,player.realSpeed, player.position.x, player.position.y, player.ray.x, player.ray.y, player.rayActive);
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
                    Player realPlayer = dataManager.getPlayerById(info.Id.ToString());
                    player.health = realPlayer.health;
                    player.maxHealth = realPlayer.maxHealth;
                    dataManager.updatePlayerById(info.Id.ToString(),player);
                }
                else if (type=="requestPlayer")
                {
                    string username = data.GetValue("username").ToString();
                    if (username.Length<=25) {
                        Point spawnPoint = getSpawnPoint();
                        float speed = 1500;
                        JObject newPlayer = createPlayerJObject(username,info.Id.ToString(), 0,100,100, speed,10, spawnPoint.x, spawnPoint.y, spawnPoint.x, spawnPoint.y, false);
                        newPlayer.Add("reportDelay",updatePlayersDelay);
                        JObject messageObj = messageObject("setPlayer", newPlayer);
                        Vector2 spawnVector2 = new Vector2(spawnPoint.x, spawnPoint.y);
                        dataManager.addPlayer(new Player(username, info.Id.ToString(), 0,100,100, speed,10, spawnVector2, spawnVector2, false));
                        socket.Send(messageObj.ToString());
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        void killPlayer(string id)
        {
            dataManager.removePlayerById(id);
            JObject messageObj = messageObject("killPlayer",null);
            getConnectionById(id).Send(messageObj.ToString());
        }
        JObject messageObject(string type, JObject data)
        {
            JObject messageObj = new JObject();
            messageObj.Add("type",type);
            messageObj.Add("data",data);
            return messageObj;
        }
        float PlayerRadius = 50;
        void checkRayCollisions()
        {
            foreach (Player player in dataManager.getPlayers())
            {
                foreach (Player rayPlayer in dataManager.getPlayers())
                {
                    if (player.id!=rayPlayer.id)
                    {
                        float dist = distance(player.position.x-rayPlayer.ray.x, player.position.y - rayPlayer.ray.y);
                        if (dist<PlayerRadius)
                        {
                            player.health = clampF(player.health-3,0,player.maxHealth);
                            if (player.health<=0)
                            {
                                killPlayer(player.id);
                            }
                        }
                    }
                }
            }
        }
        float distance(float distX, float distY)
        {
            return MathF.Sqrt(MathF.Pow(distX,2)+MathF.Pow(distY, 2));
        }
        Point getSpawnPoint()
        {
            return new Point() {x=random.Next(4000)-2000, y = random.Next(2800) - 1400 };
        }
        float clampF(float value, float lowerBound, float upperBound)
        {
            float returnValue = value;
            if (returnValue<lowerBound)
            {
                returnValue = lowerBound;
            }
            if (returnValue > upperBound)
            {
                returnValue = upperBound;
            }
            return returnValue;
        }
    }
}
