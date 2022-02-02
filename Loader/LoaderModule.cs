using CitiesHarmony.API;
using ColossalFramework;
using ICities;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Loader {
    public sealed class LoaderModule : IUserMod, ILoadingExtension {
        internal const string m_modName = "Improved Loader";
        internal const string m_modFileVersion = "0.1.0";
        internal const string m_modVersion = m_modFileVersion + ".*";
        internal const string m_modDesc = "A replacement for Loading Screen Mod";
        public string Name => m_modName + ' ' + m_modFileVersion;
        public string Description => m_modDesc;

        public void OnEnabled() {
            CreateDebugFile();
            LoaderManager.Initialize();
            HarmonyHelper.DoOnHarmonyReady(LoaderPatcher.EnablePatches);
        }

        public void OnDisabled() {
            if (HarmonyHelper.IsHarmonyInstalled) {
                LoaderPatcher.DisablePatches();
            }
        }

        public void OnCreated(ILoading loading) {
            LoaderUI.m_startProfiler = true;
            Singleton<LoadingManager>.instance.StartCoroutine(LoaderUI.UpdateStats());
            LoaderPatcher.LateEnablePatches();
        }

        public void OnLevelLoaded(LoadMode mode) {
            LoaderUI.m_startProfiler = false;
        }

        public void OnLevelUnloading() {

        }

        public void OnReleased() {
            LoaderPatcher.LateDisablePatches();
        }

        private const string m_debugLogFile = "00LoaderDebug.txt";
        private static readonly Stopwatch profiler = new Stopwatch();
        private static readonly object fileLock = new object();
        private static void CreateDebugFile() {
            profiler.Start();
            /* Create Debug Log File */
            string path = Path.Combine(Application.dataPath, m_debugLogFile);
            using (FileStream debugFile = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (StreamWriter sw = new StreamWriter(debugFile)) {
                sw.WriteLine(@"--- " + m_modName + ' ' + m_modVersion + " Debug File ---");
                sw.WriteLine(Environment.OSVersion);
                sw.WriteLine(@"C# CLR Version " + Environment.Version);
                sw.WriteLine(@"Unity Version " + Application.unityVersion);
                sw.WriteLine(@"-------------------------------------");
            }
        }

        internal static void DebugLog(string msg) {
            var ticks = profiler.ElapsedTicks;
            Monitor.Enter(fileLock);
            try {
                using (FileStream debugFile = new FileStream(Path.Combine(Application.dataPath, m_debugLogFile), FileMode.Append))
                using (StreamWriter sw = new StreamWriter(debugFile)) {
                    sw.WriteLine($"{(ticks / Stopwatch.Frequency):n0}:{(ticks % Stopwatch.Frequency):D7}-{new StackFrame(1, true).GetMethod().Name} ==> {msg}");
                }
            } finally {
                Monitor.Exit(fileLock);
            }
        }
    }
}
