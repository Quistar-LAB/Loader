using ColossalFramework.Packaging;
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using PackageDeserializer = Loader.PackagingOverrides.PackageDeserializer;
using PackageReader = Loader.PackagingOverrides.PackageReader;
using System.Collections.Generic;

namespace Loader {
    internal static class LoaderManager {
        private static readonly Dictionary<int, Texture2D> m_sharedTextures = new Dictionary<int, Texture2D>(128);
        /// <summary>
        /// Delegated getter for PackageDeserializer::m_customDeserializeHandler
        /// </summary>
        internal static Func<ColossalFramework.Packaging.PackageDeserializer.CustomDeserializeHandler> GetCustomDeserializeHandler;

        /// <summary>
        /// This gets called in LoaderModule::OnEnabled
        /// </summary>
        internal static void Initialize() {
            GetCustomDeserializeHandler = CreateGetter<ColossalFramework.Packaging.PackageDeserializer.CustomDeserializeHandler>("m_CustomDeserializer");
        }

        /// <summary>
        /// This gets called from LoadingManager::LoadLevelCoroutine()
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool CheckDLC(LoadingManager _, uint id) => SteamHelper.IsDLCOwned((SteamHelper.DLC)id) &&
                                                                    (!LoaderSettings.SkipPrefabs || !LoaderSettings.SkipDLC(id));

        /// <summary>
        /// Overrides Package::DeserializeAsset to use custom PackageReader
        /// </summary>
        internal static object DeserializeAsset(Package package, Stream stream) {
            object obj;
            using (PackageReader reader = new PackageReader(stream)) {
                obj = PackageDeserializer.Deserialize(package, reader);
            }
            return obj;
        }

        /// <summary>
        /// Helper function to create delegated fast dynamic method to access private fields in PackageDeserializer
        /// </summary>
        private static Func<T> CreateGetter<T>(string fieldname) {
            FieldInfo field = typeof(ColossalFramework.Packaging.PackageDeserializer).GetField(fieldname, BindingFlags.Static | BindingFlags.NonPublic);
            string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, typeof(T), null, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldsfld, field);
            gen.Emit(OpCodes.Ret);
            return (Func<T>)setterMethod.CreateDelegate(typeof(Func<T>));
        }
    }
}
