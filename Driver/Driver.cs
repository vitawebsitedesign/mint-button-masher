using log4net;
using MintButtonMasherCore;
using MintFileUtil;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Driver
{
    internal static class Driver
    {
        #region Vars
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        #region Extern delegate for keystrokes
        static ConsoleEventDelegate handler;    // Prevent garbage collection
        private delegate bool ConsoleEventDelegate(int eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
        #endregion

        #region Constructor
        static Driver()
        {
            handler = new ConsoleEventDelegate(PostHook);
            SetConsoleCtrlHandler(handler, true);
            LogUnhandledExceptions();
        }
        #endregion

        public static void Run()
        {
            Core.PressExecuteButtonRepeatedly();
        }

        #region Post hook
        public static bool PostHook(int eventType)
        {
            _log.Info("Starting post hook...");
            TrySaveAddonDataAsMintData();
            TryOpenMintWebSuite();
            _log.Info("Finished.");
            return false;
        }

        private static void TrySaveAddonDataAsMintData()
        {
            string dir = GetConfigValue("WowSavedVariablesDir");
            string filename = GetConfigValue("MintSavedVariablesFilename");
            try
            {
                if (SaveAddonDataAsMintData(dir, filename))
                {
                    _log.Info("Saved auction house data");
                }
                else
                {
                    _log.Error("Failed to save auction house data (does the destination folder exist?)");
                }
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
        }

        private static bool SaveAddonDataAsMintData(string savedVariablesDir, string savedVariablesFilename)
        {
            var savedAddonData = FileUtil.GetSavedAddonData(savedVariablesDir, savedVariablesFilename);
            var mintData = ConvertToMintDataFormat(savedAddonData);
            string mintDataDir = GetMintDataDir();
            string fileExt = GetConfigValue("MintDataFileExtension");
            return FileUtil.SaveMintData(mintData, mintDataDir, fileExt);
        }

        private static bool TryOpenMintWebSuite()
        {
            bool opened = OpenMintWebSuite();
            if (!opened)
            {
                var msg = "Couldnt open mint web suite (does the pnl HTML file exist?)";
                Console.WriteLine(msg);
                _log.Warn(msg);
            }
            return opened;
        }

        private static string ConvertToMintDataFormat(string escapedJsonData)
        {
            var unescapedJsonData = Regex.Unescape(escapedJsonData);
            var jsonData = JsonConvert.DeserializeObject(unescapedJsonData);
            var compressedJsonData = JsonConvert.SerializeObject(jsonData, Formatting.None);
            return compressedJsonData;
        }

        private static bool OpenMintWebSuite()
        {
            var root = GetConfigValue("MintWebSuiteDir");
            var dist = GetConfigValue("MintWebSuiteDistDir");
            var dir = Path.Combine(root, dist);
            var file = GetConfigValue("MintWebSuitePnlFile");
            return FileUtil.TryOpenFile(dir, file);
        }

        private static string GetConfigValue(string key)
        {
            try
            {
                var val = ConfigurationManager.AppSettings[key];
                if (val == null)
                {
                    throw new ConfigurationErrorsException("Add " + key + " to app config");
                }
                return val.ToString();
            }
            catch
            {
                throw;
            }
        }

        private static string GetMintDataDir()
        {
            var baseDir = GetConfigValue("MintWebSuiteDir");
            var folder = GetConfigValue("MintDataFolder");
            return Path.Combine(baseDir, folder);
        }
        #endregion

        #region Misc
        static void LogUnhandledExceptions()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(LogUnhandledException);
        }

        static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _log.Error(e.ToString());
        }
        #endregion
    }
}
