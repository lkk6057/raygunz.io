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
                    
                }
                else if (type=="requestPlayer")
                {
                    string username = data.GetValue("username").ToString();
                    JObject newPlayer = new JObject();
                    Point spawnPoint = getSpawnPoint();
                    newPlayer.Add("username",username);
                    newPlayer.Add("score", 0);
                    newPlayer.Add("speed", 1500);
                    newPlayer.Add("posX", spawnPoint.x);
                    newPlayer.Add("posY", spawnPoint.y);
                    newPlayer.Add("rayX", spawnPoint.x);
                    newPlayer.Add("rayY", spawnPoint.y);
                    newPlayer.Add("rayActive", false);
                    JObject messageObj = messageObject("setPlayer",newPlayer);
                    socket.Send(messageObj.ToString());
                    Console.WriteLine(messageObj.ToString());
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
