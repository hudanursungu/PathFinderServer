using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Debug_Server
{
    class AsyncServer
    {
        private byte[] _buffer = new byte[1024];
        private List<Socket> _clientSockets = new List<Socket>();
        private Socket _serverSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
        static readonly string server_username = "ohm";
        static readonly string server_password = "741895623ohm";
        static readonly string server_host = "35.231.112.9";
        static readonly int server_port = 27017;
        static readonly string database_name = "cool_db";
        private static IMongoClient Client = new MongoClient($"mongodb://{server_username}:{server_password}@{server_host}:{server_port}/{database_name}");

        private static IMongoDatabase _database = Client.GetDatabase(database_name);
        public void SetupServer()
        {
            Console.WriteLine("Starting Server...");
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 8953));
            _serverSocket.Listen(5);
            _serverSocket.BeginAccept(AcceptCallback, null);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Socket socket = _serverSocket.EndAccept(ar);
            _clientSockets.Add(socket);
            Console.WriteLine("Client Connected");
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveCallback, socket);
            _serverSocket.BeginAccept(AcceptCallback, null);
        }
        
        private void ReceiveCallback(IAsyncResult ar)
        {
            Socket socket = (Socket) ar.AsyncState;
            int received=0;
            try
            {
                received = socket.EndReceive(ar);
            }
            catch(SocketException ex)
            {
                Console.WriteLine(ex.Message+$"\n{socket.LocalEndPoint}");
                _clientSockets.Remove(socket);
                return;
            }
            if (received > 0)
            {
                byte[] dataBuf = new byte[received];
                Array.Copy(_buffer, dataBuf, received);
                string[] parameters = Encoding.UTF8.GetString(dataBuf).Split('_');
                if (parameters.Length >  2)
                {
                    string targetName = parameters[1];
                    string startName = parameters[2];
                    string fileName = parameters[3];
                    string viewType = parameters[4];
                    var procInfoUnity = new ProcessStartInfo(Environment.CurrentDirectory + @"\PathProgram\pathprog.exe"); // programın nerede oldğunu belirtiyoruz
                    procInfoUnity.Arguments = "\"" + targetName + "\" \"" + startName + "\" \"" + fileName + "\" \"" +viewType + "\""; // Parametreleri yolu bulancak programa göderiyoruz
                    var unityProc = Process.Start(procInfoUnity); // Programı başlatıyoruz
                    unityProc.EnableRaisingEvents = true;
                    procInfoUnity.UseShellExecute = false;
                    unityProc.WaitForExit(); // Video tamamlanıp program kapanana kadar bekliyoruz

                    string documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    var selectedVideo = "";
                    var videoPath = documentPath + @"\MapPathFinding\Video\"; // pathprog'ın (diğer program) video'yu kaydettiği yer
                    var videos = Directory.GetFiles(videoPath); // O yerdeki tüm videolar çekiyoruz
                    selectedVideo = videos.FirstOrDefault((x) => x.Contains(fileName)); // bizim videoyu buluyoruz               
                    string _id = PushDatabase(selectedVideo); //Server'da çalışacak, server video _id'yi geri gönderecek
                    SendText(socket, _id);
                    socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveCallback, socket);
                }
             
            }
            else
            {
                _clientSockets.Remove(socket);
            }
        }

        public static string PushDatabase(string fileName)
        {
            var videoDocuments = _database.GetCollection<VideoDocument>(typeof(VideoDocument).Name);
            var _video = new byte[16777216]; // En fazla 16 mb olacak şekilde videomuzu byte dizisine çevirip
            var file = File.Open(fileName, FileMode.Open);
            var size = new FileInfo(fileName).Length;
            if (size < 16777216)
            {
                using (var theReader = new BinaryReader(file))
                {
                    _video = theReader.ReadBytes((int)file.Length);
                }
            }
            VideoDocument vd = new VideoDocument(_video);
            videoDocuments.InsertOne(vd); // veri tabanına yazıyoruz
            return vd._id;
        }

        private void SendCallback(IAsyncResult ar)
        {
            Socket socket = (Socket) ar.AsyncState;
            socket.EndSend(ar);
        }

        private void SendText(Socket socket,string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, socket);
        }
       
    }
}
