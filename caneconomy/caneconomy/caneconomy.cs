using caneconomy.src;
using caneconomy.src.db;
using caneconomy.src.harmony;
using caneconomy.src.implementations.RealMoney;
using caneconomy.src.implementations.VirtualMoney;
using caneconomy.src.interfaces;
using HarmonyLib;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace caneconomy
{
    public class caneconomy : ModSystem
    {
        public static ICoreServerAPI sapi;
        public static Harmony harmonyInstance;
        public const string harmonyID = "caneconomy.Patches";
        public static Config config;
        private static EconomyHandler economyHandler;
        public delegate void OnReceivedClientPacketBlockEntitySignDelegate(BlockEntitySign be, IPlayer player, int packetid);
        public static OnReceivedClientPacketBlockEntitySignDelegate OnReceivedClientPacketBlockEntitySign = null;

        
        public delegate void OnBlockRemovedBlockEntityOpenableContainerDelegate(BlockEntityOpenableContainer be);
        public static OnBlockRemovedBlockEntityOpenableContainerDelegate OnBlockRemovedBlockEntityOpenableContainer = null;
       
        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            loadConfig();
            economyHandler = new RealMoneyEconomyHandler();
            harmonyInstance = new Harmony(harmonyID);

            harmonyInstance.Patch(typeof(BlockEntityOpenableContainer).GetMethod("OnBlockRemoved"), prefix: new HarmonyMethod(typeof(harmPatches).GetMethod("Prefix_OnBlockRemoved")));
            harmonyInstance.Patch(typeof(BlockEntitySign).GetMethod("OnReceivedClientPacket"), prefix: new HarmonyMethod(typeof(harmPatches).GetMethod("Prefix_Gui_OnButtonSave")));
           // harmonyInstance.PatchAll();

            sapi.Event.ServerRunPhase(EnumServerRunPhase.RunGame, getMoneyItemID);
            sapi.Event.ServerRunPhase(EnumServerRunPhase.ModsAndConfigReady, selectImplementationIfNeeded);
        }
        private void loadConfig()
        {
            config = sapi.LoadModConfig<Config>(this.Mod.Info.ModID + ".json");
            if(config == null)
            {
                config = new Config();
            }
            sapi.StoreModConfig<Config>(config, this.Mod.Info.ModID + ".json");
            return;
        }
        public static void selectImplementationIfNeeded()
        {
            if(config.SELECTED_ECONOMY_HANDLER == "REAL_MONEY") 
            {
                setHandler(new RealMoneyEconomyHandler());
            }
            else if (config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY")
            {
                setHandler(new VirtualMoneyEconomyHandler());
            }
        }

        public static void getMoneyItemID()
        {
            foreach (var itemVTC in config.COINS_VALUES_TO_CODE)
            {
                Item[] arrayResult = sapi.World.SearchItems(new AssetLocation(itemVTC.Value));

                if(arrayResult.Length > 0)
                {
                    config.ID_TO_COINS_VALUES.Add(arrayResult[0].Id, itemVTC.Key);
                }
            }
        }

       
        public static EconomyHandler getHandler()
        {
            return economyHandler;
        }
        public static bool setHandler(EconomyHandler handler)
        {
            economyHandler = handler;
            return true;
        }
        
    }
}
