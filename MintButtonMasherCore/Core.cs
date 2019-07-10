using log4net;
using MintButtonMasherCore.models;
using MintFileUtil;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace MintButtonMasherCore
{
    public static class Core
    {
        #region Vars
        private static PauseRanges _pauseRanges;
        private static Random _random;
        private static Process _wow;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private enum PAUSE { KEY_SPRING, PRESSED_KEY, AUCTION_SEARCH }
        private const int KEY_PRESSES_FOR_DATA_SETUP = 2;
        private const int KEY_PRESSES_FOR_DATA_EXTRACTION = 9999;
        private const int F9 = 0x78;
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const string PAUSE_RANGES_FILENAME = @"pause-ranges.json";
        #endregion

        #region Extern
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        #endregion

        #region Constructor
        static Core()
        {
            _pauseRanges = GetPauseRanges();
            _random = new Random();
            _wow = GetWowProcess();

            if (_wow == null)
            {
                _log.Error("Ensure exactly 1 instance of WoW is running");
                Environment.Exit(1);
            }
            Console.WriteLine("Core initialized successfully. Starting data extraction...");
        }
        #endregion

        public static void PressExecuteButtonRepeatedly()
        {
            _log.Info("Doing key presses for data setup...");
            KeyPressesForDataSetup();
            _log.Info("done");

            _log.Info("Doing key presses for remaining actions...");
            KeyPressesForRemainingActions();
            _log.Info("done");
        }

        #region Private funcs
        #region Key pressing
        private static void KeyPressesForDataSetup()
        {
            for (var i = 0; i < KEY_PRESSES_FOR_DATA_SETUP; i++)
            {
                ExecuteNextCommandInQueue();
            }
        }

        private static void KeyPressesForRemainingActions()
        {
            // For safety reasons, we use a "for loop" with a large number of iterations, instead of a "while true" loop.
            for (var i = 0; i < KEY_PRESSES_FOR_DATA_EXTRACTION; i++)
            {
                ExecuteNextCommandInQueue();
                SimulateHumanPause(_pauseRanges.AuctionSearch);
                ExecuteNextCommandInQueue();
                Console.Write(".");
            }
        }

        private static void ExecuteNextCommandInQueue()
        {
            PressKey(F9);
        }

        private static void PressKey(int key)
        {
            var h = _wow.MainWindowHandle;

            PostMessage(h, WM_KEYDOWN, key, 0);
            SimulateHumanPause(_pauseRanges.KeySpring);
            PostMessage(h, WM_KEYUP, key, 0);

            SimulateHumanPause(_pauseRanges.PressedKey);
        }
        #endregion

        #region Misc
        private static PauseRanges GetPauseRanges()
        {
            var jsonStr = GetPauseRangesAsStr();
            try
            {
                var ranges = JObject.Parse(jsonStr).ToObject<PauseRanges>();
                if (ranges == null)
                {
                    throw new InvalidCastException("Was able to read the pause ranges file, but failed to convert the contents to a PauseRanges object");
                }
                return ranges;
            }
            catch
            {
                throw;
            }
        }

        public static string GetPauseRangesAsStr()
        {
            var dir = Directory.GetCurrentDirectory();
            return FileUtil.GetPauseRangesAsStr(dir, PAUSE_RANGES_FILENAME);
        }

        private static Process GetWowProcess()
        {
            Process[] processes = Process.GetProcessesByName("Wow");
            if (processes.Length == 1)
            {
                return processes[0];
            }
            return null;
        }

        private static void SimulateHumanPause(PauseRange pauseRange)
        {
            var r = _random.Next(pauseRange.Min, pauseRange.Max);
            Thread.Sleep(r);
        }
        #endregion
        #endregion
    }
}
