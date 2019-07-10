using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace MintFileUtil
{
    public static class FileUtil
    {
        #region Vars
        private const string WOW_ADDON_DATA_PATTERN = @"\{.+\}";
        private const string MINT_DATA_FILENAME_FORMAT = "M-d-yyyy H-m";
        #endregion

        public static string GetPauseRangesAsStr(string dir, string pauseRangesFilename)
        {
            return GetFileAsStr(dir, pauseRangesFilename);
        }

        public static string GetSavedAddonData(string savedVariablesDir, string savedVariablesFilename)
        {
            var savedVariablesText = GetFileAsStr(savedVariablesDir, savedVariablesFilename);
            var regex = new Regex(WOW_ADDON_DATA_PATTERN);
            var match = regex.Match(savedVariablesText);
            var foundJsonData = match.Success
                && match.Groups.Count > 0
                && match.Groups[0].Captures.Count > 0
                && match.Groups[0].Captures[0].Value != null
                && match.Groups[0].Captures[0].Length > 0;
            if (foundJsonData)
            {
                return match.Groups[0].Captures[0].Value;
            }
            else
            {
                throw new Exception("Failed to find Json data in the WoW saved variables file");
            }
        }

        public static bool SaveMintData(string mintData, string mintDataDir, string fileExt)
        {
            var validArgs = mintData != null && mintData.Length > 0;
            if (validArgs)
            {
                var mintDataFilename = GetMintDataFilename(fileExt);
                return WriteFile(mintDataDir, mintDataFilename, mintData);
            }
            return false;
        }

        public static bool TryOpenFile(string dir, string filename)
        {
            var path = Path.Combine(dir, filename);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                Process.Start(path);
            }
            catch
            {
                return false;
            }
            return true;
        }

        #region Private funcs
        private static string GetFileAsStr(string dir, string filename)
        {
            try
            {
                var path = Path.Combine(dir, filename);
                return File.ReadAllText(path);
            }
            catch
            {
                throw;
            }
        }

        private static string GetMintDataFilename(string fileExt)
        {
            string datetime = DateTime.UtcNow.ToString(MINT_DATA_FILENAME_FORMAT);
            return datetime + "." + fileExt;
        }

        private static bool WriteFile(string dir, string filename, string contents)
        {
            if (Directory.Exists(dir))
            {
                string path = Path.Combine(dir, filename);
                try
                {
                    File.WriteAllText(path, contents);
                }
                catch
                {
                    throw;
                }
                return true;
            }
            return false;
        }
        #endregion
    }
}
