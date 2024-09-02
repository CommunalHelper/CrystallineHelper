using MonoMod.ModInterop;
using System;

namespace vitmod.Imports {
    public static class SpeedrunTool {
        public static void Initialize() {
            typeof(SaveLoadImports).ModInterop();

            SaveLoadImports.RegisterStaticTypes?.Invoke(typeof(CustomBridge), [
                "bridgeList"
            ]);
        }

        [ModImportName("SpeedrunTool.SaveLoad")]
        private static class SaveLoadImports {
            public static Func<Type, string[], object> RegisterStaticTypes;
        }
    }
}
