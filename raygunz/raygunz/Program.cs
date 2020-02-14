using Fleck;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;

namespace raygunz
{
    class Program
    {
        static void Main(string[] args) => new Program().MainTask().GetAwaiter().GetResult();
        List<IWebSocketConnection> connections = new List<IWebSocketConnection>();
        WebSocketServer server;
        DataManager dataManager = new DataManager();
        Random random = new Random();
        public struct Point { public float x; public float y; }
        async Task MainTask()
        {
            string ipv4 = GetLocalIPAddress();
            Console.WriteLine($"ws://{ipv4}:8181");
            server = new WebSocketServer($"ws://{ipv4}:8181");
            server.Start(socket =>
            {
                IWebSocketConnectionInfo info = socket.ConnectionInfo;
                socket.OnOpen = () => clientConnected(socket);
                socket.OnClose = () => clientDisconnected(socket);
                socket.OnMessage = message => parseMessage(message, socket);
            });
            updatePlayers();
            await Task.Delay(-1);
        }
        public string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
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
        void castAwayFromId(string id, string message)
        {
            foreach (IWebSocketConnection connection in connections)
            {
                if (id != connection.ConnectionInfo.Id.ToString())
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
        JObject createPlayerJObject(string username, string id, int score, float health, float maxHealth, float speed, float size, float realSpeed, float posX, float posY, float rayX, float rayY, bool rayActive)
        {
            JObject newPlayer = new JObject();
            newPlayer.Add("username", username);
            newPlayer.Add("id", id);
            newPlayer.Add("score", score);
            newPlayer.Add("health", health);
            newPlayer.Add("maxHealth", maxHealth);
            newPlayer.Add("speed", speed);
            newPlayer.Add("size", size);
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
            jsonObject.Add("data", data);
            return jsonObject;
        }
        Player JSONToPlayer(JObject player, string playerId)
        {
            try
            {
                string username = player.GetValue("username").ToString();
                string id = playerId;
                int score = int.Parse(player.GetValue("score").ToString());
                float health = float.Parse(player.GetValue("health").ToString());
                float maxHealth = float.Parse(player.GetValue("maxHealth").ToString());
                float speed = float.Parse(player.GetValue("speed").ToString());
                float size = float.Parse(player.GetValue("size").ToString());
                float realSpeed = float.Parse(player.GetValue("realSpeed").ToString());
                Vector2 position = new Vector2(float.Parse(player.GetValue("posX").ToString()), float.Parse(player.GetValue("posY").ToString()));
                Vector2 ray = new Vector2(float.Parse(player.GetValue("rayX").ToString()), float.Parse(player.GetValue("rayY").ToString()));
                bool rayActive = bool.Parse(player.GetValue("rayActive").ToString());
                Player newPlayer = new Player(username, id, score, health, maxHealth, speed, size, realSpeed, position, ray, rayActive);
                return newPlayer;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " " + player.ToString());
                return null;

            }
        }
        int updatePlayersDelay = 20;
        float healthHeal = 0.15f;
        async Task updatePlayers()
        {
            try
            {
                //checkRayCollisions(); disabled in favor of client sided hit detections
                foreach (IWebSocketConnection connection in connections)
                {
                    IWebSocketConnectionInfo info = connection.ConnectionInfo;
                    JArray playerArray = new JArray();
                    foreach (Player player in dataManager.getPlayers())
                    {
                        if (!player.rayActive)
                        {
                            player.health = clampF(player.health + healthHeal, 0, player.maxHealth);
                            updateHealth(player.id, player.health, player.maxHealth);
                        }
                        if (player.id != info.Id.ToString())
                        {
                            JObject playerObject = createPlayerJObject(player.username, player.id, player.score, player.health, player.maxHealth, player.speed, player.size, player.realSpeed, player.position.x, player.position.y, player.ray.x, player.ray.y, player.rayActive);
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
        void parseMessage(string message, IWebSocketConnection socket)
        {
            IWebSocketConnectionInfo info = socket.ConnectionInfo;
            //Console.WriteLine($"Received from {info.Id}: {message}");
            try
            {
                JObject jsonObject = JObject.Parse(message);
                string type = jsonObject.GetValue("type").ToString();
                JObject data = (JObject)jsonObject.GetValue("data");
                if (type == "reportPlayer")
                {
                    Player player = JSONToPlayer(data, info.Id.ToString());
                    Player realPlayer = dataManager.getPlayerById(info.Id.ToString());
                    player.health = realPlayer.health;
                    player.maxHealth = realPlayer.maxHealth;
                    player.score = realPlayer.score;
                    player.speed = realPlayer.speed;
                    dataManager.updatePlayerById(info.Id.ToString(), player);
                }
                else if (type == "requestPlayer")
                {
                    string username = data.GetValue("username").ToString();
                    if (username.Length <= 15)
                    {
                        string bannedChars = @"<>/\";
                        foreach (char bannedChar in bannedChars)
                        {
                            username = username.Replace(bannedChar.ToString(), "");
                        }
                        if (username.Length == 0)
                        {
                            username = "Nameless";
                        }
                        Point spawnPoint = getSpawnPoint();
                        float speed = 1000;
                        JObject newPlayer = createPlayerJObject(username, info.Id.ToString(), 0, 100, 100, speed, 50, 10, spawnPoint.x, spawnPoint.y, spawnPoint.x, spawnPoint.y, false);
                        newPlayer.Add("reportDelay", updatePlayersDelay);
                        JObject messageObj = messageObject("setPlayer", newPlayer);
                        Vector2 spawnVector2 = new Vector2(spawnPoint.x, spawnPoint.y);
                        dataManager.addPlayer(new Player(username, info.Id.ToString(), 0, 100, 100, speed, 50, 10, spawnVector2, spawnVector2, false));
                        socket.Send(messageObj.ToString());
                    }
                }
                else if (type == "damagePlayer")
                {
                    string damagerId = socket.ConnectionInfo.Id.ToString();
                    string recipientId = data.GetValue("recipientId").ToString();
                    Player player1 = dataManager.getPlayerById(damagerId);
                    Player player2 = dataManager.getPlayerById(recipientId);
                    if (distance(player1.position.x - player2.position.x, player1.position.y - player2.position.y) < 1200)
                    {
                        damagePlayer(damagerId, recipientId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        void killPlayer(string id)
        {
            dataManager.removePlayerById(id);
            JObject messageObj = messageObject("killPlayer", null);
            getConnectionById(id).Send(messageObj.ToString());
        }
        JObject messageObject(string type, JObject data)
        {
            JObject messageObj = new JObject();
            messageObj.Add("type", type);
            messageObj.Add("data", data);
            return messageObj;
        }
        float PlayerRadius = 50;
        void checkRayCollisions()
        {
            foreach (Player player in dataManager.getPlayers())
            {
                foreach (Player rayPlayer in dataManager.getPlayers())
                {
                    if (player.id != rayPlayer.id && rayPlayer.rayActive)
                    {
                        float dist = distance(player.position.x - rayPlayer.ray.x, player.position.y - rayPlayer.ray.y);
                        if (dist < PlayerRadius)
                        {
                            if (player.health <= 0)
                            {
                                rayPlayer.maxHealth = clampF(rayPlayer.maxHealth + 20, 0, 250);
                                rayPlayer.health = clampF(rayPlayer.health + 50, 0, rayPlayer.maxHealth);
                                rayPlayer.score += player.score + 50;
                                killPlayer(player.id);
                                updateScore(rayPlayer.id.ToString(), rayPlayer.score);
                                updateHealth(rayPlayer.id.ToString(), rayPlayer.health, rayPlayer.maxHealth);
                            }
                        }
                    }
                }
            }
        }
        void damagePlayer(string damagerId, string recipientId)
        {
            Player damager = dataManager.getPlayerById(damagerId);
            Player recipient = dataManager.getPlayerById(recipientId);
            recipient.health = clampF(recipient.health - 5, 0, recipient.maxHealth);
            updateHealth(recipientId, recipient.health, recipient.maxHealth);
            if (recipient.health <= 0)
            {
                damager.maxHealth = clampF(damager.maxHealth + 20, 0, 200);
                damager.health = clampF(damager.health + 50, 0, damager.maxHealth);
                damager.score += (recipient.score / 4) + 50;
                damager.speed = clampF(damager.speed - 50, 500, damager.speed);
                damager.size = clampF(damager.size + 5, 0, 100);
                killPlayer(recipientId);
                updateScore(damagerId, damager.score);
                updateHealth(damagerId, damager.health, damager.maxHealth);
                updateSize(damagerId, damager.size);
                updateSpeed(damagerId, damager.speed);
            }
        }
        void updateHealth(string id, float health, float maxHealth)
        {
            JObject data = new JObject();
            data.Add("health", health);
            data.Add("maxHealth", maxHealth);
            JObject messageObj = messageObject("updateHealth", data);
            getConnectionById(id).Send(messageObj.ToString());
        }
        void updateScore(string id, int score)
        {
            JObject data = new JObject();
            data.Add("score", score);
            JObject messageObj = messageObject("updateScore", data);
            getConnectionById(id).Send(messageObj.ToString());
        }
        void updateSize(string id, float size)
        {
            JObject data = new JObject();
            data.Add("size", size);
            JObject messageObj = messageObject("updateSize", data);
            getConnectionById(id).Send(messageObj.ToString());
        }
        void updateSpeed(string id, float speed)
        {
            JObject data = new JObject();
            data.Add("speed", speed);
            JObject messageObj = messageObject("updateSpeed", data);
            getConnectionById(id).Send(messageObj.ToString());
        }
        float distance(float distX, float distY)
        {
            return (float)Math.Sqrt(Math.Pow(distX, 2) + Math.Pow(distY, 2));
        }
        Point getSpawnPoint()
        {
            return new Point() { x = random.Next(4000) - 2000, y = random.Next(2800) - 1400 };
        }
        float clampF(float value, float lowerBound, float upperBound)
        {
            float returnValue = value;
            if (returnValue < lowerBound)
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
