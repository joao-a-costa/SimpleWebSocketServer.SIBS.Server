using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Configuration;
using System.Collections.Generic;
using System;
using System.Resources;

namespace SimpleWebSocketServer.SIBS.Server.Service.Controllers
{
    /// <summary>
    /// Controller for the INI file
    /// </summary>
    public class IniFileController
    {
        #region "Constants"

        /// <summary>
        /// The section for the configuration
        /// </summary>
        private const string _SectionConfig = "Config";

        #endregion

        #region "Members"

        /// The path to the INI file
        private string _path;

        #endregion

        #region "Private Methods"

        /// <summary>
        /// The private method to write to the INI file
        /// </summary>
        /// <param name="section">The section to write to</param>
        /// <param name="key">The key to write to</param>
        /// <param name="value">The value to write</param>
        /// <param name="filePath">The path to the INI file</param>
        /// <returns></returns>
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        /// <summary>
        /// The private method to read from the INI file
        /// </summary>
        /// <param name="section">The section to read from</param>
        /// <param name="key">The key to read from</param>
        /// <param name="defaultValue">The default value to return if the key is not found</param>
        /// <param name="returnValue">The value to return</param>
        /// <param name="size">The size of the return value</param>
        /// <param name="filePath">The path to the INI file</param>
        /// <returns></returns>
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder returnValue, int size, string filePath);

        private static string GetPrivateProfileStringManually(string key, string filePath)
        {
            var res = string.Empty;

            try
            {
                // Read all lines from the config file
                string[] lines = File.ReadAllLines(filePath);

                // Dictionary to store key-value pairs
                Dictionary<string, string> configSettings = new Dictionary<string, string>();

                // Process each line
                foreach (string line in lines)
                {
                    // Split each line based on the first occurrence of '='
                    int index = line.IndexOf('=');
                    if (index > 0)
                    {
                        // Add to dictionary
                        configSettings[line.Substring(0, index).Trim()] = line.Substring(index + 1).Trim();
                    }
                }
                // Example usage:
                if (configSettings.ContainsKey(key))
                {
                    res = configSettings[key];
                }
            }
            catch (Exception)
            {
                
            }

            return res;
        }

        private static void WritePrivateProfileStringManually(string key, string value, string filePath)
        {
            try
            {
                // Read all lines from the config file
                string[] lines = File.ReadAllLines(filePath);

                // Dictionary to store key-value pairs
                Dictionary<string, string> configSettings = new Dictionary<string, string>();

                // Process each line
                foreach (string line in lines)
                {
                    // Split each line based on the first occurrence of '='
                    int index = line.IndexOf('=');
                    if (index > 0)
                    {
                        // Add to dictionary
                        configSettings[line.Substring(0, index).Trim()] = line.Substring(index + 1).Trim();
                    }
                }

                // Update or add the key-value pair
                configSettings[key] = value;

                // Write back to the file
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    foreach (var kvp in configSettings)
                    {
                        writer.WriteLine($"{kvp.Key} = {kvp.Value}");
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        #endregion

        #region "Constructor"

        /// <summary>
        /// Constructor for the INI file controller
        /// </summary>
        /// <param name="iniPath">The path to the INI file</param>
        public IniFileController(string iniPath = null)
        {
            var iniPathFull = iniPath.EndsWith(".ini") ? iniPath : iniPath + ".ini";

            _path = new FileInfo(iniPathFull).FullName;
        }

        #endregion

        #region "Public Methods"

        /// <summary>
        /// Write to the INI file
        /// </summary>
        /// <param name="section">The section to write to</param>
        /// <param name="key">The key to write to</param>
        /// <param name="value">The value to write</param>
        public void Write(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, _path);
        }

        /// <summary>
        /// Write to the INI file
        /// </summary>
        /// <param name="key">The key to write to</param>
        /// <param name="value">The value to write</param>
        public void Write(string key, string value)
        {
            WritePrivateProfileString(_SectionConfig, key, value, _path);
        }

        public void WriteManually(string key, string value)
        {
            WritePrivateProfileStringManually(key, value, _path);
        }

        /// <summary>
        /// Read from the INI file
        /// </summary>
        /// <param name="section">The section to read from</param>
        /// <param name="key">The key to read from</param>
        /// <returns></returns>
        public string Read(string section, string key)
        {
            var returnValue = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", returnValue, 255, _path);
            return returnValue.ToString();
        }

        /// <summary>
        /// Read from the INI file
        /// </summary>
        /// <param name="key">The key to read from</param>
        /// <returns>The value read from the INI file</returns>
        public string Read(string key)
        {
            return Read(_SectionConfig, key);
        }

        public string ReadManually(string key)
        {
            return GetPrivateProfileStringManually(key, _path);
        }

        /// <summary>
        /// Get the instance of the ini file controller
        /// </summary>
        /// <returns>The instance of the ini file controller</returns>
        public static IniFileController GetInstance()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Get the current configuration file.
            var iniPath = Path.GetFileNameWithoutExtension(
                Path.GetFileNameWithoutExtension(config.FilePath));

            return new IniFileController($"{Path.GetDirectoryName(config.FilePath)}\\{iniPath}");


        }

        #endregion
    }
}
