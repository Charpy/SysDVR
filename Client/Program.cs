﻿using SDL2;
using SysDVR.Client.Core;
using SysDVR.Client.Platform;
using System;
using System.Threading.Tasks;
#if ANDROID_LIB
using System.Runtime.InteropServices;
#endif

namespace SysDVR.Client
{
    public static class Program
    {
        public static ClientApp Instance;
        public static string Version = "6.0";
        public static string BuildID = "unknown";

        public static Options Options = new();

#if ANDROID_LIB
        public static NativeInitBlock Native;

	    [UnmanagedCallersOnly(EntryPoint = "sysdvr_entrypoint")]
	    public static NativeError sysdvr_entrypoint(IntPtr __arg_init)
        {
            var result = NativeInitBlock.Read(__arg_init, out Native);
            if (result != NativeError.Success)
                return result;

            NativeLogger.Setup();
            Resources.SettingsStorePath = Native.GetSettingsStoragePath?.Invoke() ?? ""; 
            
            RunApp(new string[0]);

            return NativeError.Success;
        }
#else
		public static void Main(string[] args)
        {
            RunApp(args);
        }
#endif

        private static void RunApp(string[] args)
        {
#if !DEBUG
            try
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Failed to set global exception handler: " + ex.ToString());
            }

            try
            {
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Failed to set tasl exception handler: " + ex.ToString());
            }
#endif

#if !DEBUG
            try
#endif
            {
#if DEBUG
                Console.WriteLine("SysDVR Client entrypoint");
#endif

                if (Instance is null)
                {
                    DynamicLibraryLoader.Initialize();

					BuildID = Resources.GetBuildId() ?? "<unknown commit>";
                    
                    Console.WriteLine($"SysDVR-Client {Version} - by exelix");
					Console.WriteLine("https://github.com/exelix11/SysDVR");
                    Console.WriteLine($"Build ID: {BuildID}\n");

					var cli = CommandLineOptions.Parse(args);

                    if (cli.Version)
						return;
					else if (cli.Help)
					{
						Console.WriteLine(CommandLineOptions.HelpMessage);
						return;
					}
					else if (cli.DebugList)
					{
						Console.WriteLine(CommandLineOptions.GetDebugFlagsList());
						return;
					}
					else if (cli.ShowDecoders)
					{
                        Targets.Player.LibavUtils.PrintAllCodecs();
						return;
					}


					Instance = new ClientApp(cli);
                    Instance.Initialize();
                }

                Instance.EntryPoint();
            }
#if !DEBUG
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                SDL.SDL_Quit();
            }
#endif
        }

        private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine("TaskScheduler_UnobservedTaskException: " + e.Exception.ToString());
            SDL.SDL_Quit();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("CurrentDomain_UnhandledException: " + e.ExceptionObject.ToString());
            SDL.SDL_Quit();
        }
    }
}