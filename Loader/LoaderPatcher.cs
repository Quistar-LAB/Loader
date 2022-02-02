using ColossalFramework.Packaging;
using HarmonyLib;
using Loader.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Loader {
    internal static class LoaderPatcher {
        private const string HarmonyID = @"com.loader.quistar";

        /// <summary>
        /// Insert custom routines into LoadingManager::LoadLevelCoroutine()
        /// </summary>
        private static IEnumerable<CodeInstruction> LoadLevelCoroutineTranspiler(IEnumerable<CodeInstruction> instructions) {
            MethodInfo preLoadLevel = AccessTools.Method(typeof(LoadingManager), "PreLoadLevel");
            MethodInfo dlc = AccessTools.Method(typeof(LoadingManager), "DLC");
            using (var codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
                    var cur = codes.Current;
                    if (cur.opcode == OpCodes.Call && cur.operand == preLoadLevel) {
                        yield return cur;
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SkipFiles), nameof(SkipFiles.Load)));
                    } else if (cur.opcode == OpCodes.Call && cur.operand == dlc) {
                        cur.operand = AccessTools.Method(typeof(LoaderManager), nameof(LoaderManager.CheckDLC));
                        yield return cur;
                    } else {
                        yield return cur;
                    }
                }
            }
        }

        private static IEnumerable<CodeInstruction> DeserializeAssetTranspiler(IEnumerable<CodeInstruction> instructions) {
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LoaderManager), nameof(LoaderManager.DeserializeAsset)));
            yield return new CodeInstruction(OpCodes.Ret);
        }

        /// <summary>
        /// Patch to LoadingAnimation::OnGUI
        /// </summary>
        private static IEnumerable<CodeInstruction> OnGUITranspiler(IEnumerable<CodeInstruction> instructions) {
            foreach (var code in instructions) {
                if (code.opcode == OpCodes.Ret) {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LoaderUI), nameof(LoaderUI.OnGUI))).WithLabels(code.ExtractLabels());
                }
                yield return code;
            }
        }

        /// <summary>
        /// Patch to LoadingProfiler::BeginLoading
        /// </summary>
        private static IEnumerable<CodeInstruction> BeginLoadingTranspiler(IEnumerable<CodeInstruction> instructions) {
            foreach (var code in instructions) {
                if (code.opcode == OpCodes.Ret) {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LoaderUI), nameof(LoaderUI.UpdateProfiler)));
                }
                yield return code;
            }
        }

        internal static void EnablePatches() {
            Harmony harmony = new Harmony(HarmonyID);
            try {
                harmony.Patch(AccessTools.Method(typeof(global::LoadingManager).GetNestedType("<LoadLevelCoroutine>c__Iterator1",
                    BindingFlags.Instance | BindingFlags.NonPublic), "MoveNext"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(LoaderPatcher), nameof(LoadLevelCoroutineTranspiler))));
            } catch (Exception e) {
                LoaderModule.DebugLog("Failed to patch LoadingManager::LoadLevelCoroutine");
                Debug.LogException(e);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(Package), "DeserializeAsset"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(LoaderPatcher), nameof(DeserializeAssetTranspiler))));
            } catch (Exception e) {
                LoaderModule.DebugLog("Failed to patch Package::DeserializeAsset");
                Debug.LogException(e);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(LoadingProfiler), nameof(LoadingProfiler.BeginLoading)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(LoaderPatcher), nameof(BeginLoadingTranspiler))));
            } catch (Exception e) {
                LoaderModule.DebugLog("Failed to patch LoadingProfiler::BeginLoading");
                Debug.LogException(e);
                throw;
            }
        }

        internal static void LateEnablePatches() {
            Harmony harmony = new Harmony(HarmonyID);
            try {
                harmony.Patch(AccessTools.Method(typeof(LoadingAnimation), "OnGUI"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(LoaderPatcher), nameof(OnGUITranspiler))));
            } catch (Exception e) {
                LoaderModule.DebugLog("Failed to patch LoadingAnimation::OnGUI");
                Debug.LogException(e);
                throw;
            }
        }

        internal static void DisablePatches() {
            Harmony harmony = new Harmony(HarmonyID);
            harmony.Unpatch(AccessTools.Method(typeof(global::LoadingManager).GetNestedType("<LoadLevelCoroutine>c__Iterator1",
                BindingFlags.Instance | BindingFlags.NonPublic), "MoveNext"), HarmonyPatchType.Transpiler, HarmonyID);
            harmony.Unpatch(AccessTools.Method(typeof(Package), "DeserializeAsset"), HarmonyPatchType.Transpiler, HarmonyID);
            harmony.Unpatch(AccessTools.Method(typeof(LoadingProfiler), nameof(LoadingProfiler.BeginLoading)), HarmonyPatchType.Transpiler, HarmonyID);
        }

        internal static void LateDisablePatches() {
            Harmony harmony = new Harmony(HarmonyID);
            harmony.Unpatch(AccessTools.Method(typeof(LoadingAnimation), "OnGUI"), HarmonyPatchType.Transpiler, HarmonyID);
        }
    }
}
