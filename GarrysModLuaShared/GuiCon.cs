using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using static GarrysModLuaShared.Global;
using static GarrysModLuaShared.Lua;

namespace GarrysModLuaShared
{
    class GuiCon
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(int dwProcessId);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        public static int SetMode(LuaState luaState)
        {
            string mode = lua_gettop(luaState) > 0 ? Encoding.UTF8.GetString(Encoding.GetEncoding(1251).GetBytes(CheckManagedString(luaState, 1))) : "none"; //default to none

            if (mode == "gui")
            {
                //MessageBox.Show("Welcome to GUI mode");

                //Application.EnableVisualStyles();

                //Application.SetCompatibleTextRenderingDefault(false);

                //Application.Run(new Form1());
            }
            else if (mode == "console")
            {

                //Get a pointer to the forground window.  The idea here is that
                //IF the user is starting our application from an existing console
                //shell, that shell will be the uppermost window.  We'll get it
                //and attach to it
                IntPtr ptr = GetForegroundWindow();

                int u;

                GetWindowThreadProcessId(ptr, out u);

                Process process = Process.GetProcessById(u);

                if (process.ProcessName == "cmd")    //Is the uppermost window a cmd process?
                {
                    AttachConsole(process.Id);

                    //we have a console to attach to ..
                    //Console.WriteLine("hello. It looks like you started me from an existing console.");
                    Console.WriteLine("Console Mod enabled.");
                }
                else
                {
                    //no console AND we're in console mode ... create a new console.

                    AllocConsole();

                    //Console.WriteLine(@"hello. It looks like you double clicked me to start
                    //AND you want console mode.  Here's a new console.");
                    //Console.WriteLine("press any key to continue ...");
                    //Console.ReadLine();
                    Console.WriteLine("Console Mod enabled.");
                }
            }
            else if (mode == "none")
            {
                //FreeConsole(); //Удаляет консоль сервера))
            }
            return 0;
        }
    }
}
