using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Globalization;
using static GarrysModLuaShared.Global;
using static GarrysModLuaShared.Lua;

using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Channels.Groups;
using DotNetty.Common.Concurrency;
using DotNetty.Buffers;
using DotNetty.Handlers.Tls;
using DotNetty.Handlers.Logging;
using Newtonsoft.Json;
using log4net;

namespace GarrysModLuaShared
{
    class MiniMapServer
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //public static Dispatcher MainThreadDispatcher;
        //Network
        public static IChannel ServerChannel;
        public static IEventLoopGroup MainServerWorkers;
        public static IEventLoopGroup ChildServerWorkers;
        //public static IEventExecutor MainServerExecutor;
        public static ConcurrentDictionary<string, ConnectionUser> ClientConnections;
        //public static IChannelGroup MainServerChannelGroup;
        //Variables
        public static string CurrentMap;

        public static async Task RunServer(int Port)
        {
            //MainServerWorkers = ServerFactorySettings.MaxBossSize == 0 ? new MultithreadEventLoopGroup() : new MultithreadEventLoopGroup(ServerFactorySettings.MaxBossSize);
            MainServerWorkers = new MultithreadEventLoopGroup(1);

            //ChildServerWorkers = ServerFactorySettings.MaxWorkerSize == 0 ? new MultithreadEventLoopGroup() : new MultithreadEventLoopGroup(ServerFactorySettings.MaxWorkerSize);
            ChildServerWorkers = new MultithreadEventLoopGroup();

            ClientConnections = new ConcurrentDictionary<string, ConnectionUser>();

            //MainServerExecutor = new SingleThreadEventExecutor("MainServerExecutorThread", TimeSpan.FromSeconds(5));
            //MainServerChannelGroup = new DefaultChannelGroup("MainServerGroup", MainServerExecutor);

            try
            {
                ServerBootstrap server = new ServerBootstrap();

                server
                    .Group(MainServerWorkers, ChildServerWorkers)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.AutoRead, true)
                    .Option(ChannelOption.SoBacklog, 100)
                    .Option(ChannelOption.SoKeepalive, true)
                    .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(120))
                    .Option(ChannelOption.TcpNodelay, true)
                    .Option(ChannelOption.SoRcvbuf, 4096)
                    .Handler(new LoggingHandler(LogLevel.INFO))
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        channel.Pipeline.AddLast(new ServerHandler());

                        ConnectionUser connectionUser = new ConnectionUser(channel);

                        //if (ClientConnections.ContainsKey(connectionUser.IpAddress))
                        //    connectionUser.HandShakePartialCompleted = true;
                        ClientConnections.AddOrUpdate(connectionUser.IpAddress, connectionUser, (key, value) => connectionUser);
                        //MainServerChannelGroup.Add(channel);
                    }));

                //ServerChannel = await server.BindAsync(ServerFactorySettings.AllowedAddresses, ServerFactorySettings.ServerPort);
                ServerChannel = await server.BindAsync(IPAddress.Any, Port);
                //MainServerChannelGroup.Add(ServerChannel);
            }
            catch (Exception ex)
            {
                //MsgC(LuaState(), Color(255, 20, 20), string.Format("[MiniMap.Client.Dll]: Error: {0}\n", ex.Message));
                //MsgC(LuaState(), Color(255, 20, 20), string.Format("[MiniMap.Client.Dll]: - Source: {0}\n", ex.Source));
                //MsgC(LuaState(), Color(255, 20, 20), string.Format("[MiniMap.Client.Dll]: - StackTrace: {0}\n", ex.StackTrace));
                //MsgC(LuaState(), Color(255, 20, 20), string.Format("[MiniMap.Client.Dll]: - TargetSite: {0}\n", ex.TargetSite));
                log.Fatal("Server startup error!", ex);
            }
        }

        public static async void Start(int Port) => await RunServer(Port);

        public static async void Stop()
        {
            await ServerChannel.CloseAsync();

            await MainServerWorkers.ShutdownGracefullyAsync();

            await ChildServerWorkers.ShutdownGracefullyAsync();
        }

        public static int Start(LuaState luaState)
        {
            //MainThreadDispatcher = Dispatcher.CurrentDispatcher;

            int Port = 5890;
            int nargs = lua_gettop(luaState);
            if (nargs < 1)
            {
                MsgC(luaState, Color(20, 255, 20), "[MiniMap.Client.Dll]: Port missing using default 5890...\n");
            } else
            {
                luaL_checktype(luaState, 1, Type.Number);
                int LPort = (int)lua_tonumber(luaState, 1);
                if (LPort > 0 && 65535 > LPort)
                {
                    MsgC(luaState, Color(20, 255, 20), string.Format("[MiniMap.Client.Dll]: Using port: {0}...\n", LPort));
                    Port = LPort;
                } else
                {
                    MsgC(luaState, Color(20, 255, 20), "[MiniMap.Client.Dll]: Port invalid using default 5890...\n");
                }
            }

            var IP = IPAddress.Any;

            MsgC(luaState, Color(20, 255, 20), "[MiniMap.Client.Dll]: Starting echo server...\n");
            MsgC(luaState, Color(20, 255, 20), string.Format("[MiniMap.Client.Dll]: Will begin listening for requests on {0}:{1}\n", IP, Port));

            if (ServerChannel != null && ServerChannel.Active)
            {
                Stop();
            }

            Start(Port);
            CurrentMap = game.GetMap(luaState);

            return 0;
        }
        public static void ReceiveMessage(string message)
        {
            ServerNetRootObj recvmessage = JsonConvert.DeserializeObject<ServerNetRootObj>(message);
            switch (recvmessage.Action)
            {
                case "chat":
                    ServerNetChatObj chatMsg = JsonConvert.DeserializeObject<ServerNetChatObj>(recvmessage.Message);
                    string enc1251ChatMsg = Encoding.GetEncoding(1251).GetString(Encoding.UTF8.GetBytes(chatMsg.Message)); //От этой строки можно и избавится
                    DllMain.MainThreadDispatcher.BeginInvoke(new Action(() =>
                    {
                        lock (SyncRoot)
                        {
                            lua_getglobal(LuaState(), "MiniMapServer");
                            lua_getfield(LuaState(), -1, "DispMessage");
                            lua_pushstring(LuaState(), enc1251ChatMsg);
                            lua_pcall(LuaState(), 1);
                            lua_pop();
                        }
                    }));
                    BroadCast(JsonConvert.SerializeObject(recvmessage));
                    break;
                case "login":
                    ServerNetLoginObj loginMsg = JsonConvert.DeserializeObject<ServerNetLoginObj>(recvmessage.Message);
                    // Ну тут код логина дописать :)
                    // А пока мы отправим ответ ввиде карты на сервере, пока только это :)

                    ServerNetInitObj initmsg = new ServerNetInitObj();
                    initmsg.Map = CurrentMap;

                    ServerNetRootObj sendMessage = new ServerNetRootObj();
                    sendMessage.Action = "init";
                    sendMessage.Message = JsonConvert.SerializeObject(initmsg);

                    BroadCast(JsonConvert.SerializeObject(sendMessage));
                    break;
            }
        }
        public static int SendMessage(LuaState luaState)
        {
            int nargs = lua_gettop(luaState);
            if (nargs < 1 || nargs > 3)
            {
                return 0;
            }
            luaL_checktype(luaState, 1, Type.Number);
            int action = (int)lua_tonumber(luaState, 1);
            switch (action)
            {
                case 1:
                    //Send Chat Message
                    //luaL_checktype(luaState, 2, Type.String);
                    //luaL_checktype(luaState, 3, Type.String);

                    ServerNetChatObj chatMessage = new ServerNetChatObj();
                    chatMessage.Name = Encoding.UTF8.GetString(Encoding.GetEncoding(1251).GetBytes(CheckManagedString(luaState, 2))); //Get string in right Encoding)
                    chatMessage.Message = Encoding.UTF8.GetString(Encoding.GetEncoding(1251).GetBytes(CheckManagedString(luaState, 3)));

                    ServerNetRootObj sendСhatMessage = new ServerNetRootObj();
                    sendСhatMessage.Action = "chat";
                    sendСhatMessage.Message = JsonConvert.SerializeObject(chatMessage);

                    BroadCast(JsonConvert.SerializeObject(sendСhatMessage));
                    break;
                case 2:
                    //Send Occupied Message
                    //luaL_checktype(luaState, 2, Type.String);
                    luaL_checktype(luaState, 3, Type.Bool);

                    ServerNetOccupiedObj occupiedMessage = new ServerNetOccupiedObj();
                    occupiedMessage.Signals = Encoding.UTF8.GetString(Encoding.GetEncoding(1251).GetBytes(CheckManagedString(luaState, 2)));
                    occupiedMessage.Occupied = Convert.ToBoolean(lua_toboolean(luaState, 3));

                    ServerNetRootObj sendOccupiedMessage = new ServerNetRootObj();
                    sendOccupiedMessage.Action = "occupied";
                    sendOccupiedMessage.Message = JsonConvert.SerializeObject(occupiedMessage);

                    BroadCast(JsonConvert.SerializeObject(sendOccupiedMessage));
                    break;
            }
            return 0;
        }
        public static void BroadCast(string message)
        {
            message += "\0";
            //IByteBuffer sendBytes = ServerChannel.Allocator.Buffer().WriteBytes(Encoding.UTF8.GetBytes(message));
            //MainServerChannelGroup.WriteAsync(Encoding.UTF8.GetBytes(message));
            foreach (var client in ClientConnections)
            {
                //IByteBuffer sendBytes = client.Value.ConnectionChannel.Allocator.Buffer().WriteBytes(Encoding.UTF8.GetBytes(message));
                IByteBuffer sendBytes = ServerChannel.Allocator.Buffer().WriteBytes(Encoding.UTF8.GetBytes(message));
                client.Value.ConnectionChannel.WriteAsync(sendBytes);
            }
        }
    }

    public class ServerHandler : ChannelHandlerAdapter
    {
        public override Task CloseAsync(IChannelHandlerContext context)
        {
            string clientAddress = (context.Channel.RemoteAddress as IPEndPoint)?.Address.ToString();

            if (clientAddress != null)
            {
                ConnectionUser connectionUser;

                MiniMapServer.ClientConnections.TryRemove(clientAddress, out connectionUser);
            }

            return context.CloseAsync();
        }
        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            string clientAddress = (context.Channel.RemoteAddress as IPEndPoint)?.Address.ToString();

            if (clientAddress != null)
            {
                ConnectionUser connectionUser;

                MiniMapServer.ClientConnections.TryRemove(clientAddress, out connectionUser);
                connectionUser.CompleteClose();
            }
        }
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            IByteBuffer buffer = message as IByteBuffer;
            if (buffer != null)
            {
                //Console.WriteLine("Received from client: " + buffer.ToString(Encoding.UTF8));
                foreach (string msg in buffer.ToString(Encoding.UTF8).Split(new Char[] { '\0' }))
                {
                    if (msg.Length > 0)
                    {
                        //Console.WriteLine("MSG: " + msg);
                        MiniMapServer.ReceiveMessage(msg);
                    }
                }
            }
            //context.WriteAsync(message);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            //Console.WriteLine("Exception: " + exception);
            context.CloseAsync();
        }
        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            if (message is IByteBuffer)
                return context.WriteAndFlushAsync(message);

            IByteBuffer buffer = context.Allocator.Buffer().WriteBytes(message as byte[]);

            return context.WriteAndFlushAsync(buffer);
        }
        public override void ChannelRegistered(IChannelHandlerContext context)
        {
            string clientAddress = (context.Channel.RemoteAddress as IPEndPoint)?.Address.ToString();

            if (clientAddress != null)
            {
                ConnectionUser connectionUser;

                MiniMapServer.ClientConnections.TryGetValue(clientAddress, out connectionUser);

                if (connectionUser != null)
                {
                    connectionUser.SameHandledCount++;
                }
            }
        }
    }

    public class ConnectionUser
    {
        /// <summary>
        ///     Data Parser
        /// </summary>
        //public ServerPacketParser DataParser;

        /// <summary>
        ///     Connection Id
        /// </summary>
        public string ConnectionId;

        /// <summary>
        ///     IP Address
        /// </summary>
        public string IpAddress;

        /// <summary>
        ///    Connection Channel
        /// </summary>
        public IChannel ConnectionChannel;

        /// <summary>
        ///     Is in HandShake Process
        /// </summary>
        //internal bool HandShakeCompleted;

        /// <summary>
        ///     Is in HandShake Process
        /// </summary>
        //internal bool HandShakePartialCompleted;

        /// <summary>
        ///     Count of Same IP Connection.
        /// </summary>
        internal int SameHandledCount;

        //public ConnectionActor(ServerPacketParser dataParser, IChannel context)
        public ConnectionUser(IChannel context)
        {
            //DataParser = dataParser;

            ConnectionChannel = context;

            ConnectionId = context.Id.ToString();

            IpAddress = (context.RemoteAddress as IPEndPoint)?.Address.ToString();

            SameHandledCount = 0;

            //HandShakeCompleted = false;

            //HandShakePartialCompleted = false;
        }

        public void Close()
        {
            ConnectionChannel?.Flush();

            //DataParser?.Dispose();
        }

        public void CompleteClose()
        {
            ConnectionChannel?.CloseAsync();

            Close();
        }
    }

    public class ServerNetRootObj
    {
        public string Action { get; set; }
        public string Message { get; set; }
    }
    public class ServerNetChatObj
    {
        public string Name { get; set; }
        public string Message { get; set; }
    }
    public class ServerNetLoginObj
    {
        public string SteamID { get; set; }
        public string Name { get; set; }
    }
    public class ServerNetInitObj
    {
        public string Map { get; set; }
    }
    public class ServerNetOccupiedObj
    {
        public string Signals { get; set; }
        public bool Occupied { get; set; }
    }
}
