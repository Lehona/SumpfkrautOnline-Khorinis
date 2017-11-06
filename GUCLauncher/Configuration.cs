﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Security.Cryptography;

namespace GUCLauncher
{
    static class Configuration
    {
        const string ConfigFile = "launcher.cfg";

        static ServerListItem activeProject = null;
        public static ServerListItem ActiveProject { get { return activeProject; } }
        public static void SetActiveProject(ServerListItem item)
        {
            activeProject = item;
            Save();
        }

        static string gothicPath;
        public static string GothicPath { get { return gothicPath; } }
        public static string GothicApp { get { return Path.Combine(gothicPath, @"System\Gothic2.exe"); } }
        public static string zSpyApp { get { return Path.Combine(gothicPath, @"_work\tools\zSpy\zSpy.exe"); } }

        static ItemCollection items;

        public static void Init(ItemCollection coll)
        {
            items = coll;

            Load();

            CheckGothicPath();
        }

        #region Add & Remove Servers

        public static void AddServer(string address)
        {
            string ip;
            ushort port;
            if (ServerListItem.TryGetAddress(address, out ip, out port))
            {
                items.Add(new ServerListItem(ip, port));
                Save();
            }
        }

        public static void RemoveServer(int index)
        {
            if (index >= 0 && index < items.Count)
            {
                items.RemoveAt(index);
                Save();
            }
        }

        #endregion

        #region Load & Save

        static void Load()
        {
            if (File.Exists(ConfigFile))
            {
                using (StreamReader sr = new StreamReader(ConfigFile))
                {
                    gothicPath = Path.GetFullPath(sr.ReadLine());
                    while (!sr.EndOfStream)
                    {
                        var item = ServerListItem.ReadNew(sr);

                        if (string.Compare(sr.ReadLine(), "active", true) == 0)
                        {
                            activeProject = item;
                        }

                        if (item != null)
                            items.Add(item);
                    }
                }
            }
            else
            {
                gothicPath = "1234";
            }
        }

        public static void Save()
        {
            using (StreamWriter sw = new StreamWriter(ConfigFile))
            {
                sw.WriteLine(gothicPath);
                foreach (ServerListItem item in items)
                {
                    item.Write(sw);
                    if (activeProject == item)
                        sw.WriteLine("Active");
                    else
                        sw.WriteLine();
                }
            }
        }

        #endregion

        #region Check Gothic

        #region Hashes

        public enum HashFile
        {
            Gothic2,
            VDFS32g,
            VDFS32e,
            SHW32
        }

        static readonly Dictionary<HashFile, byte[]> hashDict = new Dictionary<HashFile, byte[]>()
        {
            { HashFile.Gothic2, new byte[16] { 0x3C, 0x43, 0x6B, 0xD1, 0x99, 0xCA, 0xAA, 0xA6, 0x4E, 0x97, 0x36, 0xE3, 0xCC, 0x1C, 0x9C, 0x32 } },
            { HashFile.VDFS32g, new byte[16] { 0xA6, 0xC1, 0x82, 0xA1, 0x5F, 0xB9, 0x14, 0x84, 0xB4, 0x58, 0x54, 0x71, 0xA1, 0x48, 0x4E, 0xF5 } },
            { HashFile.VDFS32e, new byte[16] { 0xF0, 0x58, 0x34, 0xF5, 0x2F, 0x5E, 0x66, 0x8B, 0x24, 0x85, 0xA3, 0x7C, 0x0C, 0x5B, 0x8E, 0xFA } },
            { HashFile.SHW32, new byte[16] { 0xBE, 0xCB, 0x4C, 0xB4, 0x04, 0x68, 0xFC, 0x7B, 0x06, 0x23, 0x65, 0x48, 0x0E, 0xD6, 0x43, 0x7B } }
        };

        public static bool ValidateFileHash(string fileName, HashFile hashFile)
        {
            if (!hashDict.TryGetValue(hashFile, out byte[] validHash))
                return false;

            byte[] computedHash;
            using (Stream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                computedHash = md5.ComputeHash(fs);
            }

            return computedHash.SequenceEqual(validHash);
        }

        #endregion

        static void ShowMessageBox(string text, params object[] args)
        {
            MessageBox.Show(args.Length == 0 ? text : string.Format(text, args), "Gothic 2 - Verzeichnis suchen.", MessageBoxButton.OK);
        }

        static void CheckGothicPath()
        {
            System.Windows.Forms.FolderBrowserDialog dlg = null;
            string path = gothicPath;

            while (true)
            {
                if (path == null)
                { // no launcher.cfg, first start?
                    ShowMessageBox("Bitte wähle dein Gothic 2 DNDR - Verzeichnis aus.");
                }
                else
                {
                    if (string.Equals(Path.GetFileName(path), "SYSTEM", StringComparison.OrdinalIgnoreCase))
                        path = Path.GetDirectoryName(path); // move up to base

                    FailCode code = CheckGothicVersion(path);
                    switch (code)
                    {
                        case FailCode.GothicNotFound:
                        case FailCode.VDFS32NotFound:
                        case FailCode.SHW32NotFound:
                            ShowMessageBox("Gothic konnte nicht gefunden werden ({0}).\nBitte wähle dein Gothic 2 DNDR - Verzeichnis aus.", code);
                            break;
                        case FailCode.GothicWrongVersion:
                            ShowMessageBox("Falsche Version gefunden.\nEs wird 'Gothic 2 Die Nacht des Raben - Report-Version 2.6.0.0' benötigt!");
                            break;
                        case FailCode.VDFS32WrongVersion:
                        case FailCode.SHW32WrongVersion:
                            ShowMessageBox("Eine wichtige Datei ist modifiziert wodurch der GUC nicht gestartet werden kann ({0}). Dies kann z.B. durch das Systempack erfolgt sein. "
                                            + "Bitte deinstalliere die Modifikation oder wähle eine andere Installation.", code);
                            break;
                        case FailCode.IsValid:
                            gothicPath = path;
                            Save();
                            return;
                    }
                }

                if (dlg == null)
                {
                    dlg = new System.Windows.Forms.FolderBrowserDialog();
                    dlg.ShowNewFolderButton = false;
                    dlg.SelectedPath = Directory.Exists(path) ? path : Directory.GetCurrentDirectory();
                    dlg.Description = "Verzeichnis von 'Gothic 2 - Die Nacht des Raben' suchen.";
                }

                if (dlg.ShowDialog(MainWindow.Self.GetIWin32Window()) != System.Windows.Forms.DialogResult.OK)
                {
                    Application.Current.Shutdown();
                    return;
                }

                path = dlg.SelectedPath;
            }
        }

        enum FailCode
        {
            IsValid,
            GothicNotFound,
            GothicWrongVersion,
            VDFS32NotFound,
            VDFS32WrongVersion,
            SHW32NotFound,
            SHW32WrongVersion,
        }

        // Fixme: english version?
        static FailCode CheckGothicVersion(string path)
        {
            string gothic2 = Path.Combine(path, "System\\Gothic2.exe");
            if (!File.Exists(gothic2))
                return FailCode.GothicNotFound;

            if (!ValidateFileHash(gothic2, HashFile.Gothic2))
                return FailCode.GothicWrongVersion;

            string vdfs32g = Path.Combine(path, "System\\vdfs32g.dll");
            if (!File.Exists(vdfs32g))
                return FailCode.VDFS32NotFound;

            if (!ValidateFileHash(vdfs32g, HashFile.VDFS32g))
                return FailCode.VDFS32WrongVersion;

            string shw32 = Path.Combine(path, "System\\shw32.dll");
            if (!File.Exists(shw32))
                return FailCode.SHW32NotFound;

            if (!ValidateFileHash(shw32, HashFile.SHW32))
                return FailCode.SHW32WrongVersion;

            return FailCode.IsValid;
        }

        #endregion
    }
}
