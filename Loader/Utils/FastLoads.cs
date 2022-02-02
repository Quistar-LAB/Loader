using System.Collections.Generic;

namespace Loader.Utils {
    internal static class FastLoads {
        private static readonly List<string> m_knownFastLoads = new List<string>(2);

        internal static bool CheckAvailable(ColossalFramework.Packaging.Package.Asset asset) {
            List<string> knownFastLoads = m_knownFastLoads;
            if (knownFastLoads.Contains(asset.checksum)) return true;
            ColossalFramework.Packaging.Package.Asset mainAsset = asset.package.Find(asset.package.packageMainAsset);
            if (!(mainAsset is null)) {
                knownFastLoads.Add(asset.checksum);
                return true;
            }
            return false;
        }
    }
}
