using caneconomy.src;
using caneconomy.src.harmony;
using caneconomy.src.implementations.RealMoney;
using caneconomy.src.implementations.VirtualMoney;
using caneconomy.src.interfaces;
using caneconomy.src.network;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Channels;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static caneconomy.src.Config;

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

        internal static IServerNetworkChannel serverChannel;
        internal static IClientNetworkChannel clientChannel;
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            clientChannel = api.Network.RegisterChannel("caneconomy");
            if (config == null)
            {
                config = new();
            }
            clientChannel.RegisterMessageType(typeof(ConfigUpdateValuesPacket));
            clientChannel.SetMessageHandler<ConfigUpdateValuesPacket>((packet) =>
            {
                config.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE = JsonConvert.DeserializeObject<System.Collections.Generic.OrderedDictionary<int, CoinInfo>>(packet.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE);
                foreach (var coinInfo in config.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE.Values)
                {
                    if (coinInfo.CoinAttributesStr != null)
                    {
                        var c = new JsonObject(JToken.Parse(coinInfo.CoinAttributesStr)).ToAttribute();
                        if (c is TreeArrayAttribute)
                        {
                            coinInfo.CoinAttributes = new TreeAttribute();
                        }
                        else
                        {
                            coinInfo.CoinAttributes = c as TreeAttribute;
                        }
                        //(TreeAttribute)new JsonObject(JToken.Parse(coinInfo.CoinAttributesStr)).ToAttribute();
                    }
                }
            });
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            loadConfig();
            var now = DateTime.Now;
            now.TimeOfDay.ToString("hh\\:mm\\:ss");
            //economyHandler = new RealMoneyEconomyHandler();
            serverChannel = sapi.Network.RegisterChannel("caneconomy");
            serverChannel.RegisterMessageType(typeof(ConfigUpdateValuesPacket));
            harmonyInstance = new Harmony(harmonyID);
            var p = now.TimeOfDay.ToString("hh\\:mm\\:ss");
            harmonyInstance.Patch(typeof(BlockEntityOpenableContainer).GetMethod("OnBlockRemoved"), postfix: new HarmonyMethod(typeof(harmPatches).GetMethod("Prefix_OnBlockRemoved")));
            harmonyInstance.Patch(typeof(BlockEntitySign).GetMethod("OnReceivedClientPacket"), postfix: new HarmonyMethod(typeof(harmPatches).GetMethod("Prefix_Gui_OnButtonSave")));
           // harmonyInstance.PatchAll();

            sapi.Event.ServerRunPhase(EnumServerRunPhase.RunGame, getMoneyItemID);
            sapi.Event.ServerRunPhase(EnumServerRunPhase.ModsAndConfigReady, selectImplementationIfNeeded);
            sapi.Event.PlayerJoin += (IServerPlayer byPlayer) =>
            {
                serverChannel.SendPacket(
                  new ConfigUpdateValuesPacket()
                  {
                      EXTENDED_COINS_VALUES_TO_CODE_PRIVATE = JsonConvert.SerializeObject(config.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE)
                  }
                  , byPlayer);
            };
        }
        private void loadConfig()
        {
            config = sapi.LoadModConfig<Config>(this.Mod.Info.ModID + ".json");
            if(config == null)
            {
                config = new Config();
            }
            config.InitValues();
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
            if (!caneconomy.config.EXTENDED_COINS_VALUES_TO_CODE_ENABLED)
            {
                foreach (var itemVTC in config.COINS_VALUES_TO_CODE)
                {
                    Item[] arrayResult = sapi.World.SearchItems(new AssetLocation(itemVTC.Value));

                    if (arrayResult.Length > 0)
                    {
                        //config.ID_TO_COINS_VALUES.Add(arrayResult[0].Id, itemVTC.Key);

                        var newCoinInfo = new CoinInfo(itemVTC.Key, itemVTC.Value, new TreeAttribute());
                        newCoinInfo.CoinAttributesStr = JsonConvert.SerializeObject(newCoinInfo.CoinAttributes);
                        newCoinInfo.CollectibleId = arrayResult[0].Id;
                        config.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE.Add(arrayResult[0].Id, newCoinInfo);
                    }
                }
                var sortedEntries = config.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE.OrderByDescending(it => it.Value.CoinValue).ToList();
                config.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE.Clear();
                foreach (var it in sortedEntries)
                {
                    config.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE.Add(it.Key, it.Value);
                }
            }
            else
            {
                foreach (var coinInfo in config.EXTENDED_COINS_VALUES_TO_CODE_PUBLIC)
                {
                    //caneconomy.sapi.Logger.Debug("Searching for item with code " + coinInfo.CollectibleCode);
                    //caneconomy.sapi.Logger.Debug(coinInfo.CoinAttributesStr);
                    Item[] arrayResult = sapi.World.SearchItems(new AssetLocation(coinInfo.CollectibleCode));
                    if (arrayResult.Length > 0)
                    {
                        //config.ID_TO_COINS_VALUES.Add(arrayResult[0].Id, coinInfo.CoinValue);
                        //caneconomy.sapi.Logger.Debug(arrayResult[0].Code);
                        coinInfo.CoinAttributes = (TreeAttribute)new JsonObject(JToken.Parse(coinInfo.CoinAttributesStr)).ToAttribute();
                        coinInfo.CollectibleId = arrayResult[0].Id;
                        config.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE.Add(arrayResult[0].Id, coinInfo);
                    }
                }

                var sortedEntries = config.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE.OrderByDescending(it => it.Value.CoinValue).ToList();
                config.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE.Clear();
                foreach(var it in sortedEntries)
                {
                    config.EXTENDED_COINS_VALUES_TO_CODE_PRIVATE.Add(it.Key, it .Value);
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
