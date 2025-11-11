using HarmonyLib;
using Vintagestory.API.Common;

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
