﻿using AutoUpdate.Core.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace AutoUpdate.Core.Utils
{

    internal static class FileUtil
    {
        internal const string SubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{655A2DE6-C9A3-432E-951B-D773791C2653}_is1";

        public static List<FileBase> GetFeils(string packetPath) {
            if (!Directory.Exists(packetPath)) return null;

            List<FileBase> lstFile = new List<FileBase>();
            string[] files = Directory.GetFiles(packetPath);
            foreach (var file in files)
            {
                var fileBase = new FileBase();
                fileBase.MD5 = GetFileMD5(file);
                fileBase.Name = file;
                fileBase.CurrentVersion = "";
                lstFile.Add(fileBase);
            }
            return lstFile;
        }

        public static bool VerifyFileMd5(string fileName, string md5)
        {
            var packetMD5 = GetFileMD5(fileName);

            if (!md5.ToUpper().Equals(packetMD5.ToUpper()))
            {
                Directory.Delete(fileName, true); ;
                return false;
            }
            return true;
        }

        public static bool UnPacket(string filePath, string tempPath)
        {
            try
            {
                ZipFile.ExtractToDirectory(filePath, tempPath);
                File.Delete(filePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void Update32Or64Libs(string currentDir)
        {
            var is64XSystem = Environment.Is64BitOperatingSystem;
            var sourceDir = Path.Combine(currentDir, is64XSystem ? "x64" : "x32");
            var destDir = Path.Combine(currentDir, "dlls");

            if (!Directory.Exists(sourceDir)) return;
            FileUtil.DirectoryCopy(sourceDir, destDir, true, true, null);
            Directory.Delete(sourceDir);
        }



        public static JObject ConfigurationBulider(string jsonPath) {
            using (System.IO.StreamReader file = System.IO.File.OpenText(jsonPath))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject o = (JObject)JToken.ReadFrom(reader);
                    return o;
                }
            }
        }

        internal static string GetFileMD5(string fileName)
        {
            try
            {
                var file = new FileStream(fileName, FileMode.Open);
                var md5 = new MD5CryptoServiceProvider();
                var retVal = md5.ComputeHash(file);
                file.Close();
                var sb = new StringBuilder();
                for (var i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        internal static void DirectoryCopy(string sourceDirName, string destDirName,
          bool copySubDirs, bool isOverWrite, Action<string> action)
        {
            var dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                return;
            }

            var dirs = dir.GetDirectories();
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, isOverWrite);
                if (action != null)
                {
                    action.Invoke(file.Name);
                }
            }

            if (copySubDirs)
            {
                foreach (var subdir in dirs)
                {
                    var temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, true, isOverWrite, action);
                }
            }
        }

        internal static string GetTempDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        internal static void UpdateReg(RegistryKey baseKey, string subKey, string keyName, string keyValue)
        {
            using (var registry = new RegistryHepler(baseKey, subKey))
            {
                if (!String.IsNullOrEmpty(registry.Read(keyName)))
                {
                    registry.Write(keyName, keyValue);
                }
            }
        }
    }

    public class RegistryHepler : IDisposable
    {
        #region Private Members
        private readonly RegistryKey _baseRegistryKey;
        private readonly string _subKey; //SOFTWARE\...
        #endregion

        #region Constructors
        #endregion

        #region Public Properties

        public RegistryHepler(RegistryKey baseKey, string subKey)
        {
            _baseRegistryKey = baseKey;
            _subKey = subKey;
        }
        #endregion

        #region Public Methods

        public string Read(string keyName)
        {
            var rk = _baseRegistryKey;
            using (var sk = rk.OpenSubKey(_subKey))
            {
                return sk == null ? null : sk.GetValue(keyName.ToUpper()).ToString();
            }
        }


        public void Write(string keyName, object value)
        {
            var rk = _baseRegistryKey;
            using (var sk = rk.CreateSubKey(_subKey))
            {
                if (sk != null)
                {
                    sk.SetValue(keyName.ToUpper(), value);
                }
            }
        }


        public void DeleteKey(string keyName)
        {
            var rk = _baseRegistryKey;
            using (var sk = rk.CreateSubKey(_subKey))
            {
                if (sk != null)
                {
                    sk.DeleteValue(keyName);
                }
            }
        }

        public void DeleteSubKeyTree()
        {
            var rk = _baseRegistryKey;
            using (var sk = rk.OpenSubKey(_subKey))
            {
                if (sk != null)
                {
                    sk.DeleteSubKeyTree(_subKey);
                }
            }
        }


        public int SubKeyCount()
        {
            var rk = _baseRegistryKey;
            using (var sk = rk.OpenSubKey(_subKey))
            {
                return sk != null ? sk.SubKeyCount : 0;
            }
        }

        public int ValueCount()
        {
            var rk = _baseRegistryKey;
            using (var sk = rk.OpenSubKey(_subKey))
            {
                return sk != null ? sk.ValueCount : 0;
            }
        }

        public void Dispose()
        {
            if (_baseRegistryKey != null)
            {
                _baseRegistryKey.Close();
                //_baseRegistryKey.Dispose();
            }
        }
        #endregion

    }

}