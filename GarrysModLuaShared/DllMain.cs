//
// IMPORTANT NOTE /!!\
//   If you are developing a clientside (gmcl) binary module, go to properties of the project, click on "Build" tab and define "CLIENT" (without double-quotes, case-sensitive!) in "Conditional compilation symbols".
//   If you are developing a serverside (gmsv) binary module, then define "SERVER" (without double-quotes, case-sensitive!) in "Conditional compilation symbols".
//
// ADDITIONAL NOTES
//   If you want to call function from Source engine (work in progress), you can do so by defining "SOURCE_SDK" (without double-quotes, case-sensitive!) in "Conditional compilation symbols".
//   For those who defined SOURCE_SDK:
//     This "grants you an access" to GarrysModLuaShared.Source namespace.
//     There is one more step you have to do once you compile C# binary module and you want to try it out:
//       Copy "source_exports.dll" file to GarrysMod folder (in the folder where "hl2.exe" file is). Do not rename "source_exports.dll" file!
//       You can find "source_exports.dll" file in the output folder (in the same folder as C# binary module; bin/Release), you can also find it inside "Dependencies" folder of this project.
//
// WIKI: https://github.com/OmegaExtern/gmod-csharp-binary-module/wiki
//

using System;
using System.Reflection;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using GarrysModLuaShared.Classes;
using System.Windows.Threading;
#if SOURCE_SDK
using GarrysModLuaShared.Source;
#endif
using GarrysModLuaShared.Structs;
using static GarrysModLuaShared.Global;
using static GarrysModLuaShared.Lua;

namespace GarrysModLuaShared
{
    static class DllMain
    {
        public static Dispatcher MainThreadDispatcher;

        //Keep this to avoid it from garbage collection!
        private static readonly lua_CFunction MiniMapSrvSendMessage = MiniMapServer.SendMessage;
        private static readonly lua_CFunction MiniMapSrvStart = MiniMapServer.Start;
        private static readonly lua_CFunction MiniMapSrvSetMode = GuiCon.SetMode;

        private static readonly lua_CFunction TurbostroiInitializeTrain = Turbostroi.InitializeTrain;
        private static readonly lua_CFunction TurbostroiDeinitializeTrain = Turbostroi.DeinitializeTrain;
        private static readonly lua_CFunction TurbostroiThink = Turbostroi.Think;
        private static readonly lua_CFunction TurbostroiSendMessage = Turbostroi.SendMessage;
        private static readonly lua_CFunction TurbostroiRecvMessage = Turbostroi.RecvMessage;
        private static readonly lua_CFunction TurbostroiLoadSystem = Turbostroi.LoadSystem;
        private static readonly lua_CFunction TurbostroiRegisterSystem = Turbostroi.RegisterSystem;
        private static readonly lua_CFunction TurbostroiSetSimulationFPS = Turbostroi.SetSimulationFPS;
        private static readonly lua_CFunction TurbostroiSetTargetTime = Turbostroi.SetTargetTime;
        private static readonly lua_CFunction TurbostroiRunMainThread = Turbostroi.RunMainThread;

        /// <summary>Called when your module is opened.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        /// <returns>Number of return values.</returns>
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static int gmod13_open(LuaState luaState)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            InitializeLua(luaState);
            MainThreadDispatcher = Dispatcher.CurrentDispatcher;
            ConfigureLogging(luaState);

            //MiniMap Server
            RegisterCFunction(luaState, nameof(MiniMapServer), nameof(GuiCon.SetMode), MiniMapSrvSetMode);
            RegisterCFunction(luaState, nameof(MiniMapServer), nameof(MiniMapServer.Start), MiniMapSrvStart);
            RegisterCFunction(luaState, nameof(MiniMapServer), nameof(MiniMapServer.SendMessage), MiniMapSrvSendMessage);
            //Turbostroi
            RegisterCFunction(luaState, nameof(Turbostroi), nameof(Turbostroi.InitializeTrain), TurbostroiInitializeTrain);
            RegisterCFunction(luaState, nameof(Turbostroi), nameof(Turbostroi.DeinitializeTrain), TurbostroiDeinitializeTrain);
            RegisterCFunction(luaState, nameof(Turbostroi), nameof(Turbostroi.Think), TurbostroiThink);
            RegisterCFunction(luaState, nameof(Turbostroi), nameof(Turbostroi.SendMessage), TurbostroiSendMessage);
            //RegisterCFunction(luaState, nameof(Turbostroi), nameof(Turbostroi.RecvMessage), TurbostroiRecvMessage);
            RegisterCFunction(luaState, nameof(Turbostroi), nameof(Turbostroi.LoadSystem), TurbostroiLoadSystem);
            RegisterCFunction(luaState, nameof(Turbostroi), nameof(Turbostroi.RegisterSystem), TurbostroiRegisterSystem);
            RegisterCFunction(luaState, nameof(Turbostroi), nameof(Turbostroi.SetSimulationFPS), TurbostroiSetSimulationFPS);
            RegisterCFunction(luaState, nameof(Turbostroi), nameof(Turbostroi.SetTargetTime), TurbostroiSetTargetTime);
            RegisterCFunction(luaState, nameof(Turbostroi), nameof(Turbostroi.RunMainThread), TurbostroiRunMainThread);

            MsgC(luaState, Color(20, 255, 20), "[MiniMap.Client.Dll]: MiniMap Server (Build: 02.02.2017 Alpha) Loaded.\n");

            return 0;
        }

        static void ConfigureLogging(LuaState luaState)
        {
            if (!log4net.LogManager.GetRepository().Configured)
            {
                // my DLL is referenced by web service applications to log SOAP requests before
                // execution is passed to the web method itself, so I load the log4net.config
                // file that resides in the web application root folder
                var configFile = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "minimap_log4net.config"); // System.Environment.CurrentDirectory or AppDomain.CurrentDomain.BaseDirectory

                if (!configFile.Exists)
                {
                    //throw new FileLoadException(String.Format("The configuration file {0} does not exist", configFile));
                    MsgC(luaState, Color(20, 255, 20), String.Format("[MiniMap.Client.Dll]: The configuration file {0} does not exist.\n", configFile));
                    MsgC(luaState, Color(20, 255, 20), "[MiniMap.Client.Dll]: Logging disabled!\n");
                }
                else
                {
                    log4net.Config.XmlConfigurator.Configure(configFile);
                }
            }
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("Exception caught : " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
        }

        /// <summary>Called when your module is closed.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        /// <returns>Number of return values.</returns>
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static int gmod13_close(LuaState luaState)
        {
            if (MiniMapServer.ServerChannel != null && MiniMapServer.ServerChannel.Active)
            {
                MiniMapServer.Stop();
            }

            return 0;
        }
    }
}