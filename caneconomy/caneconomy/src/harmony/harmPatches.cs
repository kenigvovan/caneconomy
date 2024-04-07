using caneconomy.src.implementations.RealMoney;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static caneconomy.caneconomy;

namespace caneconomy.src.harmony
{
    [HarmonyPatch]
    public class harmPatches
    {

        public static void Prefix_OnBlockRemoved(Vintagestory.GameContent.BlockEntityOpenableContainer __instance)
        {
            if (caneconomy.OnBlockRemovedBlockEntityOpenableContainer != null)
            {
                caneconomy.OnBlockRemovedBlockEntityOpenableContainer(__instance);
            }
        }
        public static void Prefix_Gui_OnButtonSave(Vintagestory.GameContent.BlockEntitySign __instance, IPlayer player, int packetid, byte[] data)
        {
            if (caneconomy.OnReceivedClientPacketBlockEntitySign != null)
            {
                caneconomy.OnReceivedClientPacketBlockEntitySign(__instance, player, packetid);
            }
        }
    }
}
