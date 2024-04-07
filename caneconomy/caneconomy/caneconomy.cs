using caneconomy.src;
using caneconomy.src.db;
using caneconomy.src.harmony;
using caneconomy.src.implementations.RealMoney;
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
        static SQLiteDatabaseHanlder databaseHandler;
        public static SQLiteDatabaseHanlder getDatabaseHandler()
        {
            return databaseHandler;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            loadConfig();
            loadDatabase();
            /* if (config.SELECTED_ECONOMY_HANDLER == "REAL_MONEY")
             {
                 economyHandler = new RealMoneyEconomyHandler();
             }*/
            economyHandler = new RealMoneyEconomyHandler();
            harmonyInstance = new Harmony(harmonyID);

            harmonyInstance.Patch(typeof(BlockEntityOpenableContainer).GetMethod("OnBlockRemoved"), prefix: new HarmonyMethod(typeof(harmPatches).GetMethod("Prefix_OnBlockRemoved")));
            harmonyInstance.Patch(typeof(BlockEntitySign).GetMethod("OnReceivedClientPacket"), prefix: new HarmonyMethod(typeof(harmPatches).GetMethod("Prefix_Gui_OnButtonSave")));
           // harmonyInstance.PatchAll();

            sapi.Event.ServerRunPhase(EnumServerRunPhase.RunGame, getMoneyItemID);
        }
        public static bool loadDatabase()
        {
            try
            {
                databaseHandler = new SQLiteDatabaseHanlder();
            }
            catch (SqliteException ex)
            {
                return false;
            }
            return false;
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

        public static void getMoneyItemID()
        {
            foreach (var itemVTC in caneconomy.config.COINS_VALUES_TO_CODE)
            {
                if (itemVTC.Value.Contains(":"))
                {
                    string[] item_name = itemVTC.Value.Split(':');
                    foreach (var it in caneconomy.sapi.World.Items)
                    {
                        if (it.Code != null && it.Id == 2452)
                        {
                            var o = 2;
                        }

                        if (it.Code != null && it.Code.Domain.Equals(item_name[0]) && it.Code.Path.Equals(item_name[1]))
                        {
                            caneconomy.config.ID_TO_COINS_VALUES.Add(it.Id, itemVTC.Key);
                            //break;
                        }
                    }
                }
                else
                {
                    foreach (var it in caneconomy.sapi.World.Items)
                    {
                        if (it.Code != null && it.Code.Path.Equals(itemVTC.Value))
                        {
                            caneconomy.config.ID_TO_COINS_VALUES.Add(it.Id, itemVTC.Key);
                            break;
                        }
                    }
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
