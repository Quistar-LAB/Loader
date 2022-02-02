using ColossalFramework;
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Loader {
    internal static class LoaderUI {
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct MEMORYINFO {
            public readonly uint dwLength; //Current structure size
            public readonly uint dwMemoryLoad; //Current memory utilization
            public readonly ulong totalPhys; //Total physical memory size
            public readonly ulong availPhys; //Available physical memory size
            public readonly ulong totalPageFile; //Total Exchange File Size
            public readonly ulong availPageFile; //Total Exchange File Size
            public readonly ulong totalVirtual; //Total virtual memory size
            public readonly ulong availVirtual; //Available virtual memory size
            public readonly ulong availExtendedVirtual; //Keep this value always zero
            public MEMORYINFO(bool _) {
                dwLength = 0;
                dwMemoryLoad = 0;
                totalPhys = 0;
                availPhys = 0;
                totalPageFile = 0;
                availPageFile = 0;
                totalVirtual = 0;
                availVirtual = 0;
                availExtendedVirtual = 0;
                dwLength = (uint)Marshal.SizeOf(this);
            }
        }

        private const float assetListWidth = 480f;
        private const float maxWidth = 430f;
        private static int m_assetLines = 0;
        private static int m_maxAssetLines = 30;
        private static readonly StringBuilder m_assets = new StringBuilder("<color=grey>Scenes and Assets:</color>\n", 16384);
        private static readonly StringBuilder m_assetLoader = new StringBuilder("<color=grey>Asset Loader</color>", 512);
        private static readonly StringBuilder m_loadingStats = new StringBuilder("Loading Time:\nRAM Usage:\nVirtual RAM Usage:", 512);
        private static readonly StringBuilder m_mainProfiler = new StringBuilder("<color=yellow>Main</color>\n", 128);
        private static readonly StringBuilder m_simProfiler = new StringBuilder("<color=yellow>Simulation</color>\n", 128);
        private static GUIStyle m_style;
        internal static bool m_startProfiler;

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYINFO mi);

        internal static void SetProgress(int assetsCount, int assetsTotal, int beginMillis, int nowMillis) {
            StringBuilder assetLoader = m_assetLoader;
            assetLoader.Length = 0;
            if (assetsCount > 0 && nowMillis > beginMillis) {
                assetLoader.Append("<color=grey>Asset Loader</color>\n");
                assetLoader.AppendFormat("{0} / {1}\n", assetsCount, assetsTotal);
                assetLoader.AppendFormat("{0:P1} / second", (assetsCount * 1000f) / (nowMillis - beginMillis));
            } else {
                assetLoader.AppendLine("<color=grey>Asset Loader</color>");
            }
        }

        internal static void UpdateProfiler(LoadingProfiler profiler, string name) {
            LoadingManager lmInstance = Singleton<LoadingManager>.instance;
            if (profiler == lmInstance.m_loadingProfilerMain) {
                const int startLen = 27;
                StringBuilder mainProfiler = m_mainProfiler;
                mainProfiler.Length = startLen;
                mainProfiler.AppendLine(name);
            } else if (profiler == lmInstance.m_loadingProfilerSimulation) {
                const int startLen = 33;
                StringBuilder simProfiler = m_simProfiler;
                simProfiler.Length = startLen;
                simProfiler.AppendLine(name);
            } else if (profiler == lmInstance.m_loadingProfilerScenes ||
                      /*profiler == lmInstance.m_loadingProfilerCustomContent || */
                      profiler == lmInstance.m_loadingProfilerCustomAsset) {
                const int startLen = 39;
                int len = name.Length;
                int startIndex = 0, endIndex;
                if ((len == 16 && name[0] == 'L' && name[1] == 'o' && name[2] == 'a' && name[3] == 'd' && name[4] == 'i' && name[5] == 'n' && name[6] == 'g' &&
                                 name[7] == 'A' && name[8] == 'n' && name[9] == 'i' && name[10] == 'm' && name[11] == 'a' && name[12] == 't' && name[13] == 'i' &&
                                 name[14] == 'o' && name[15] == 'n') ||
                   (len == 8 && name[0] == 'M' && name[1] == 'a' && name[2] == 'i' && name[3] == 'n' && name[4] == 'M' && name[5] == 'e' && name[6] == 'n' &&
                                name[7] == 'u')) {
                    return;
                }
                if (name[0] >= '0' && name[0] <= '9') {
                    while (name[startIndex++] != '.' && startIndex < len) ;
                    if (startIndex < len) {
                        endIndex = startIndex;
                        while (endIndex < len) {
                            if (endIndex + 4 < len && name[endIndex] == '_' && name[endIndex + 1] == 'D' && name[endIndex + 2] == 'a' &&
                                                              name[endIndex + 3] == 't' && name[endIndex + 4] == 'a') {
                                break;
                            }
                            endIndex++;
                        }
                        name = name.Substring(startIndex, endIndex - startIndex);
                    }
                }
                StringBuilder asset = m_assets;
                if (m_assetLines > m_maxAssetLines) {
                    endIndex = startLen;
                    while (asset[endIndex++] != '\n') ;
                    asset.Remove(startLen, endIndex - startLen);
                    asset.AppendLine(name);
                } else {
                    m_assetLines++;
                    asset.AppendLine(name);
                }
            }
        }

        internal static void StartCustomAssets() => m_assets.Append("Custom Assets\n");

        internal static void ShowFailedAsset(string name) => m_assets.Append("<color=red>").Append(name).Append(' ').Append("(failed)").Append("</color>\n");

        internal static void ShowDuplicateAsset(string name) => m_assets.Append("<color=lightblue>").Append(name).Append(' ').Append("(duplicate)").Append("</color>\n");

        internal static IEnumerator UpdateStats() {
            const double kbDivider = 1d / 1024d;
            const double gbDivider = 1d / (1024d * 1024d * 1024d);
            double mem, totalMem, fraction;
            MEMORYINFO mi = new MEMORYINFO(true);
            WaitForSeconds wait = new WaitForSeconds(0.50f);
            StringBuilder stats = m_loadingStats;
            Stopwatch sw = Stopwatch.StartNew();
            while (m_startProfiler) {
                yield return null;
                TimeSpan elapsed = sw.Elapsed;
                GlobalMemoryStatusEx(ref mi);
                stats.Length = 0;
                stats.Append("Loading Time:").AppendFormat(" {0:D2}:{1:D2}\n", elapsed.Minutes, elapsed.Seconds)
                    .Append("Total RAM Usage:");
                mem = (mi.totalPhys - mi.availPhys) * gbDivider;
                totalMem = mi.totalPhys * gbDivider;
                fraction = mem / totalMem;
                if (fraction >= 1d) {
                    stats.Append(@" <color=red>");
                } else if (fraction >= 0.90d) {
                    stats.Append(@" <color=orange>");
                } else {
                    stats.Append(@" <color=lime>");
                }
                stats.AppendFormat("{0:N2}GB / {1:N2}GB</color>\n", mem, totalMem).Append("Virtual RAM Usage:");
                mem = (mi.totalVirtual - mi.availVirtual) * gbDivider;
                totalMem = mi.totalVirtual * gbDivider;
                fraction = mem / totalMem;
                if (fraction >= 1d) {
                    stats.Append(@" <color=red>");
                } else if (fraction >= 0.75d) {
                    stats.Append(@" <color=orange>");
                } else {
                    stats.Append(@" <color=lime>");
                }
                stats.AppendFormat(" {0:N2}GB / ", mem);
                if (totalMem > 1000) {
                    totalMem *= kbDivider;
                    stats.AppendFormat("{0:N2}TB</color>\n", totalMem).Append("Pagefile Usage:");
                } else {
                    stats.AppendFormat("{0:N2}GB</color>\n", totalMem).Append("Pagefile Usage:");
                }
                mem = (mi.totalPageFile - mi.availPageFile) * gbDivider;
                totalMem = mi.totalPageFile * gbDivider;
                fraction = mem / totalMem;
                if (fraction >= 1d) {
                    stats.Append(@" <color=red>");
                } else if (fraction >= 0.90d) {
                    stats.Append(@" <color=orange>");
                } else {
                    stats.Append(@" <color=lime>");
                }
                stats.AppendFormat("{0:N2}GB / {1:N2}GB</color>\n", mem, totalMem);
                yield return wait;
            }
        }

        /// <summary>
        /// This is attached to LoadingAnimator::OnGUI via Harmony
        /// </summary>
        internal static void OnGUI() {
            const float padding = 50f;
            ref GUIStyle style = ref m_style;
            if (style is null) {
                style = new GUIStyle(GUI.skin.box) {
                    alignment = TextAnchor.UpperLeft,
                    margin = new RectOffset(10, 10, 10, 10),
                    padding = new RectOffset(10, 10, 10, 10),
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    border = new RectOffset(3, 3, 3, 3),
                    richText = true
                };
                m_maxAssetLines = (int)((Screen.height - padding * 2f) / style.lineHeight - 1f);
            }
            GUI.Box(new Rect(padding, padding, assetListWidth, Screen.height - (padding * 2f)), m_assets.ToString(), style);
            float xOffset = Screen.width - padding - maxWidth;
            float x4height = style.lineHeight * 4f + style.padding.top * 2f;
            GUI.Box(new Rect(xOffset, padding, maxWidth, x4height), m_assetLoader.ToString(), style);
            Rect profilerRect = new Rect(xOffset, (padding * 2f) + x4height, maxWidth, x4height);
            GUI.Box(profilerRect, m_loadingStats.ToString(), style);
            GUI.Box(new Rect(xOffset, profilerRect.y + profilerRect.height + padding, maxWidth, x4height), m_mainProfiler.ToString() + m_simProfiler.ToString(), style);
        }
    }
}
