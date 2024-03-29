﻿global using static System.MathF;
global using static Microsoft.Xna.Framework.MathHelper;
global using static YouBoss.Assets.MiscTexturesRegistry;
global using static YouBoss.Assets.SoundsRegistry.Common;
global using static YouBoss.Common.Utilities.Utilities;
global using static YouBoss.Core.Graphics.SpecificEffectManagers.ScreenShakeSystem;
using Terraria.ModLoader;

namespace YouBoss
{
    public class YouBoss : Mod
    {
        /// <summary>
        /// The instance of this mod.
        /// </summary>
        public static Mod Instance
        {
            get;
            private set;
        }

        public override void Load()
        {
            Instance = this;
        }
    }
}
