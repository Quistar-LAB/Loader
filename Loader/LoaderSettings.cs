namespace Loader {
    internal readonly struct LoaderSettings {
        private readonly struct DLCInfo {
            public readonly uint id;
            public DLCInfo(uint id) {
                this.id = id;
            }
        }
        private static bool m_loadEnabledAssets = true;
        private static bool m_loadUsedAssets = true;
        private static bool m_shareTextures = false;
        private static bool m_shareMaterials = false;
        private static bool m_shareMeshes = false;
        private static bool m_optimizeThumbnails = true;
        private static bool m_skipPrefabs = false;
        private static DLCInfo[] m_skipDLCs = null;

        internal static bool LoadEnabledAssets {
            get => m_loadEnabledAssets;
            set {
                if (m_loadEnabledAssets != value) {
                    m_loadEnabledAssets = value;
                }
            }
        }

        internal static bool LoadUsedAssets {
            get => m_loadUsedAssets;
            set {
                if (m_loadUsedAssets != value) {
                    m_loadUsedAssets = value;
                }
            }
        }

        internal static bool ShareTextures {
            get => m_shareTextures;
            set {
                if (m_shareTextures != value) {
                    m_shareTextures = value;
                }
            }
        }

        internal static bool ShareMaterials {
            get => m_shareMaterials;
            set {
                if (m_shareMaterials != value) {
                    m_shareMaterials = value;
                }
            }
        }

        internal static bool ShareMeshes {
            get => m_shareMeshes;
            set {
                if (m_shareMeshes != value) {
                    m_shareMeshes = value;
                }
            }
        }

        internal static bool OptimizeThumbnails {
            get => m_optimizeThumbnails;
            set {
                if (m_optimizeThumbnails != value) {
                    m_optimizeThumbnails = value;
                }
            }
        }

        internal static bool SkipPrefabs {
            get => m_skipPrefabs;
            set {
                if (m_skipPrefabs != value) {
                    m_skipPrefabs = value;
                }
            }
        }

        internal static bool SkipDLC(uint id) {
            DLCInfo[] skipDLCs = m_skipDLCs;
            if (skipDLCs is null) return false;
            for (int i = 0; i < skipDLCs.Length; i++) {
                if (skipDLCs[i].id == id) return true;
            }
            return false;
        }
    }
}
