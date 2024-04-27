using caneconomy.src.accounts;
using caneconomy.src.auxiliary;
using caneconomy.src.db;
using caneconomy.src.interfaces;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static caneconomy.src.implementations.OperationResult;

namespace caneconomy.src.implementations.RealMoney
{
    public class RealMoneyEconomyHandler : EconomyHandler
    {
        DatabaseHandler databaseHandler;
        //(Players UID/CITY account name/nation account name): (vector - coords where chest is)
        Dictionary<string, RealBankInfo> mapCityBanks;
        //chunk coords (real coords /16) and list of dicts for banks in that plot
        //if list became empty we delete key from dict
        Dictionary<Vec2i, List<Tuple<string, Vec3i>>> auxBanksDictionary;
        HashSet<string> dirtyAccounts;
        public RealMoneyEconomyHandler()
        {
            mapCityBanks = new Dictionary<string, RealBankInfo>();
            auxBanksDictionary = new Dictionary<Vec2i, List<Tuple<string, Vec3i>>>();
            dirtyAccounts = new HashSet<string>();
            
            

            try
            {
                databaseHandler = new SQLiteDatabaseHanlder(QuerryTemplates.INSERT_BANK, QuerryTemplates.UPDATE_BANK, QuerryTemplates.DELETE_BANK,
                                                            "CREATE TABLE IF NOT EXISTS BANKS(" +
                                                            "x INTEGER," +
                                                            "y INTEGER," +
                                                            "z INTEGER," +
                                                            "accountname TEXT," +
                                                            "lastknownvalue TEXT," +
                                                            "validcachedvalue TEXT," +
                                                            "dirty INTEGER DEFAULT 0," +
                                                            "PRIMARY KEY (accountname));",
                                                            ForAllRead);
            }
            catch (SqliteException ex)
            {
            }
            databaseHandler.readALL();
            caneconomy.setHandler(this);
            this.InitServer();
        }
        public void ForAllRead(SqliteConnection sqliteConnection)
        {
            SqliteCommand com = sqliteConnection.CreateCommand();
            com.CommandText = "SELECT * FROM BANKS";
            using (var reader = com.ExecuteReader())
            {
                var dt = new DataTable();

                dt.Load(reader);
                foreach (DataRow it in dt.Rows)
                {

                    int x = int.Parse(it["x"].ToString());
                    int y = int.Parse(it["y"].ToString());
                    int z = int.Parse(it["z"].ToString());
                    string name = it["accountname"].ToString();
                    mapCityBanks.Add(name,
                        new RealBankInfo(it["dirty"].ToString().Equals("0")
                                                                                ? false
                                                                                : true,
                                            decimal.Parse(it["lastknownvalue"].ToString()),
                                            decimal.Parse(it["validcachedvalue"].ToString()),
                                            new Vec3i(x, y, z),
                                            name));
                    if (it["dirty"].ToString().Equals("0") ? false : true)
                    {
                        dirtyAccounts.Add(name);
                    }

                    //WE DON'T NEED TO TRACK GLOBAL ACCOUNT CHEST - THERE ISN'T ANY
                    if (name.Equals(caneconomy.config.GLOBAL_ACCOUNT_NAME))
                    {
                        continue;
                    }

                    List<Tuple<string, Vec3i>> tmpList;
                    if (auxBanksDictionary.TryGetValue(new Vec2i(x, z), out tmpList))
                    {
                        auxBanksDictionary[new Vec2i(x / 32, z / 32)].Add(new Tuple<string, Vec3i>(name, new Vec3i(x, y, z)));
                    }
                    else
                    {
                        auxBanksDictionary[new Vec2i(x / 32, z / 32)] = new List<Tuple<string, Vec3i>>
                        {
                            new Tuple<string, Vec3i>(name, new Vec3i(x, y, z))
                        };
                    }
                }
            }
        }
        public Dictionary<Vec2i, List<Tuple<string, Vec3i>>> getAuxBanksDictionary()
        {
            return auxBanksDictionary;
        }
        public bool TryGetRealBankInfo(string accountName, out RealBankInfo realBankInfo)
        {
            return mapCityBanks.TryGetValue(accountName, out realBankInfo);
        }
        public bool TryGetRealBankInfo(Vec3i coords, out RealBankInfo realBankInfo)
        {
            foreach(var bank in mapCityBanks)
            {
                if (coords.Equals(bank.Value.getChestCoors()))
                {
                    realBankInfo = bank.Value;
                    return true;
                }
            }
            realBankInfo = null;
            return false;
        }
        public bool AddDirtyAccount(string accountName)
        {
            return dirtyAccounts.Add(accountName);
        }
        public bool TryGetDirtyAccount(string accountName, out RealBankInfo realBankInfo)
        {
            if(dirtyAccounts.Contains(accountName))
            {
                return mapCityBanks.TryGetValue(accountName, out realBankInfo);
            }
            realBankInfo = null;
            return false;
        }
        public void InitServer()
        {
            checkForGlobalAccount();
            caneconomy.sapi.Event.ChunkColumnLoaded += onChunkColumnLoaded;
            caneconomy.sapi.Event.ChunkColumnUnloaded += onChunkColumnUnload;
        }
        public void checkForGlobalAccount()
        {
            //CLOSED ECONOMY
            if (caneconomy.config.SAVE_TO_GLOBAL_ACCOUNT)
            {
                //NO GLOBAL ACCOUNT CREATED YET
                if (!mapCityBanks.ContainsKey(caneconomy.config.GLOBAL_ACCOUNT_NAME))
                {
                    mapCityBanks.Add(caneconomy.config.GLOBAL_ACCOUNT_NAME, new RealBankInfo(false, 0, 0, new Vec3i(-1, -1, -1), caneconomy.config.GLOBAL_ACCOUNT_NAME));
                    
                    Dictionary<string, object> dict = new Dictionary<string, object>
                    {
                        { "accountname", caneconomy.config.GLOBAL_ACCOUNT_NAME },
                        { "x", -1 },
                        { "y", -1 },
                        { "z", -1 },
                        { "dirty", 0 },
                        { "lastknownvalue", 0 },
                        { "validcachedvalue", 0 }
                    };
                    List<string> li = new List<string> { "accountname" };

                    databaseHandler.insertToDatabase(new QuerryInfo(QuerryType.INSERT, dict));
                    
                }
            }
        }
 
        public OperationResult deposit(string accountName, decimal amount)
        {
            if (accountName.StartsWith(caneconomy.config.GLOBAL_ACCOUNT_NAME))
            {
                if (mapCityBanks.ContainsKey(caneconomy.config.GLOBAL_ACCOUNT_NAME))
                {
                    mapCityBanks.TryGetValue(caneconomy.config.GLOBAL_ACCOUNT_NAME, out RealBankInfo rbi);
                    rbi.setLastKnownValue(rbi.getLastKnownValue() + amount);
                    updateAccount(accountName, rbi);
                    return new OperationResult(EnumOperationResultState.SUCCCESS);
                }
                return new OperationResult(EnumOperationResultState.TARGET_ACCOUNT_NOT_FOUND);
            }

            if (!mapCityBanks.ContainsKey(accountName))
            {
                return new OperationResult(EnumOperationResultState.TARGET_ACCOUNT_NOT_FOUND);
            }
            else
            {
                RealBankInfo tmp;
                mapCityBanks.TryGetValue(accountName, out tmp);
                if (InventoryHandling.addToChestInventoryAmountOfItems(tmp.getChestCoors(), (int)amount, accountName))
                {
                    mapCityBanks.TryGetValue(accountName, out RealBankInfo rbi);
                    updateAccount(accountName, rbi);
                    return new OperationResult(EnumOperationResultState.SUCCCESS);
                }
                return new OperationResult(EnumOperationResultState.FAILED_TARGET_DEPOSIT);
            }
            

        }
        public bool deleteAccount(string accountName)
        {
            if (!mapCityBanks.TryGetValue(accountName, out _))
            {
                return false;
            }
            //caneconomy.sapi.Logger.Debug(accountName + " account has been deleted.");
            dirtyAccounts.Remove(accountName);
            Vec3i tmp = mapCityBanks[accountName].getChestCoors();
            bool removeFlag = false;
            var t = auxBanksDictionary[new Vec2i(tmp.X / 32, tmp.Z / 32)];
            foreach (var it in auxBanksDictionary[new Vec2i(tmp.X / 32, tmp.Z / 32)])
            {
                if (it.Item1 == accountName)
                {
                    auxBanksDictionary[new Vec2i(tmp.X / 32, tmp.Z / 32)].Remove(it);
                    if (auxBanksDictionary[new Vec2i(tmp.X / 32, tmp.Z / 32)].Count == 0)
                    {
                        removeFlag = true;
                    }
                    break;
                }
            }
            if (removeFlag)
                auxBanksDictionary.Remove(new Vec2i(tmp.X / 32, tmp.Z / 32));

            mapCityBanks.Remove(accountName);
            Dictionary<string, object> dict = new Dictionary<string, object>
            {
                { "accountname", accountName }
            };

            databaseHandler.deleteFromDatabase(new QuerryInfo( QuerryType.DELETE, dict));
            
            return true;

        }
        public void updateAccount(string accountName, RealBankInfo bankInfo)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>
                {
                    { "accountname", accountName },
                    { "x", bankInfo.getChestCoors().X },
                    { "y", bankInfo.getChestCoors().Y },
                    { "z", bankInfo.getChestCoors().Z },
                    { "dirty", bankInfo.getDirty() },
                    { "lastknownvalue", bankInfo.getLastKnownValue() },
                    { "validcachedvalue", bankInfo.getValidCachedValue() }
                };

            databaseHandler.updateDatabase(new QuerryInfo(QuerryType.UPDATE, dict));
            
        }
        public decimal getBalance(string accountName)
        {           
            if(!mapCityBanks.TryGetValue(accountName, out RealBankInfo tmp))
            {
                //no account
                return 0;
            }
            BlockEntity chest = caneconomy.sapi.World.BlockAccessor.
                GetBlockEntity(new BlockPos(tmp.getChestCoors()));

            if (chest == null)
            {
                RealBankInfo tmpBankInfo;
                mapCityBanks.TryGetValue(accountName, out tmpBankInfo);
                if (tmpBankInfo != null)
                {
                    return mapCityBanks[accountName].getValidCachedValue();
                }
                return 0;
            }

            return countOfItemCoinsInInventory(chest);
        }
        public decimal countOfItemCoinsInInventory(BlockEntity chest)
        {
            decimal countOfItems = 0;
            foreach (ItemSlot itemSlot in (chest as BlockEntityGenericTypedContainer).Inventory)
            {
                ItemStack iS = itemSlot.Itemstack;
                if (iS == null || iS.Item == null)
                {
                    continue;
                }
                else
                {
                    if (caneconomy.config.ID_TO_COINS_VALUES.ContainsKey(iS.Id))
                    {
                        countOfItems += iS.StackSize * caneconomy.config.ID_TO_COINS_VALUES[iS.Id];
                    }
                }
            }
            return countOfItems;
        }
        public bool accountExist(string accountName)
        {          
            if (mapCityBanks.ContainsKey(accountName))
            {
                RealBankInfo tmp;
                mapCityBanks.TryGetValue(accountName, out tmp);
                BlockEntity chest = caneconomy.sapi.World.BlockAccessor.
                GetBlockEntity(new BlockPos(tmp.getChestCoors()));
                if (tmp != null && chest is BlockEntityGenericTypedContainer)
                    return true;
                else
                    return false;
            }
            return false;
        }
        public bool newAccount(string accountName, Dictionary<string, object> additionalInfoNewAccount)
        {
            Vec3i pos = new Vec3i();
            if(additionalInfoNewAccount.TryGetValue("chestPos", out object posObject))
            {
                pos = (Vec3i)posObject;
            }

            RealBankInfo tmp;
            mapCityBanks.TryGetValue(accountName, out tmp);
            // No updates here
            if (tmp != null && tmp.getChestCoors().X == pos.X && tmp.getChestCoors().Y == pos.Y && tmp.getChestCoors().Z == pos.Z)
            {
                return false;
            }
            mapCityBanks[accountName] = new RealBankInfo(false, 0, 0, pos.Clone(), accountName);//new Vec3i(x, y, z);
                                                                                        //List for that plot coords exists
            if (auxBanksDictionary.TryGetValue(new Vec2i(pos.X / 32, pos.Z / 32), out _))
            {
                auxBanksDictionary[new Vec2i(pos.X / 32, pos.Z / 32)].Add(new Tuple<string, Vec3i>(accountName, new Vec3i(pos.X, pos.Y, pos.Z)));
            }
            //not exists, create list
            else
            {
                auxBanksDictionary[new Vec2i(pos.X / 32, pos.Z / 32)] = new List<Tuple<string, Vec3i>>
                {
                    new Tuple<string, Vec3i>(accountName, new Vec3i(pos.X, pos.Y, pos.Z))
                };
            }
            // Update info or add new bank of city
            Dictionary<string, object> dict = new Dictionary<string, object>
            {
                { "accountname", accountName },
                { "x", pos.X },
                { "y", pos.Y },
                { "z", pos.Z },
                { "dirty", 0 },
                { "lastknownvalue", 0 },
                { "validcachedvalue", 0 }
            };
            List<string> li = new List<string> { "accountname" };

            databaseHandler.insertToDatabase(new QuerryInfo(QuerryType.INSERT, dict));
            return true;
        }
        public bool updateAccount(string account, Dictionary<string, object> additionalInfoUpdateAccount = null)
        {
            return false;
        }
        public OperationResult withdraw(string accountName, decimal amount)
        {
            if (accountName.StartsWith(caneconomy.config.GLOBAL_ACCOUNT_NAME))
            {
                if (mapCityBanks.ContainsKey(caneconomy.config.GLOBAL_ACCOUNT_NAME))
                {
                    mapCityBanks.TryGetValue(caneconomy.config.GLOBAL_ACCOUNT_NAME, out RealBankInfo rbi);
                    rbi.setLastKnownValue(rbi.getLastKnownValue() - amount);
                    updateAccount(accountName, rbi);
                    return new OperationResult(EnumOperationResultState.SUCCCESS);
                }
                return new OperationResult(EnumOperationResultState.TARGET_ACCOUNT_NOT_FOUND);
            }

            if (!mapCityBanks.ContainsKey(accountName))
            {
                return new OperationResult(EnumOperationResultState.TARGET_ACCOUNT_NOT_FOUND);
            }
            else
            {
                RealBankInfo tmp;
                mapCityBanks.TryGetValue(accountName, out tmp);
                if (InventoryHandling.deleteChestInventoryAmountOfItems(tmp.getChestCoors(), (int)amount, accountName))
                {
                    updateAccount(accountName, tmp);
                    return new OperationResult(EnumOperationResultState.SUCCCESS);
                }
                return new OperationResult(EnumOperationResultState.SOURCE_ACCOUNT_NOT_FOUND);
            }                   
        }
        public string getAccountInfoAdmin()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var it in mapCityBanks)
            {
                sb.Append(string.Format("Name:{0}; coords:{1}; LNV: {2}; dirty: {3}; VCV: {4}\n", it.Key,
                                                                  it.Value.getChestCoors(),
                                                                  it.Value.getLastKnownValue(),
                                                                  it.Value.getDirty(),
                                                                  it.Value.getValidCachedValue()
                                                                  ));
            }
            return sb.ToString();
        }
        public void onChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            //go through the list of the banks and for using cach changed update context of the banks
            //reset "dirty" flag
            //need to add "dirty" flag and last known value of the bank (used only if dirty)
            //set dirty when chunk is unloaded but we need to add or withdraw money from chest-bank in this area

            List<Tuple<string, Vec3i>> tmp;
            auxBanksDictionary.TryGetValue(new Vec2i(chunkCoord.X, chunkCoord.Y), out tmp);
            if (tmp == null)
                return;
            foreach (Tuple<string, Vec3i> pair in tmp)
            {
                if (TryGetDirtyAccount(pair.Item1, out RealBankInfo tmpBankInfo))
                {
                    if (tmpBankInfo.getDirty() && tmpBankInfo.getLastKnownValue() != tmpBankInfo.getValidCachedValue())
                    {
                        decimal diff = tmpBankInfo.getLastKnownValue() - tmpBankInfo.getValidCachedValue();
                        if (!chunks[pair.Item2.Y / 32].BlockEntities.TryGetValue(new BlockPos(pair.Item2), out BlockEntity be))
                        {
                            continue;
                        }

                        if (diff < 0)
                        {
                            deposit(pair.Item1, (int)diff);
                        }
                        else
                        {
                            withdraw(pair.Item1, (int)diff);
                        }
                    }
                    tmpBankInfo.setDirty(false);
                    tmpBankInfo.setLastKnownValue(tmpBankInfo.getValidCachedValue());
                    updateAccount(pair.Item1, tmpBankInfo);
                }
            }
            
        }
        public void onChunkColumnUnload(Vec3i chunkCoord)
        {
            //go through all of the chest-banks and for this chunk we cache value of the banks
            //do not mark them dirty (only when we do something this "virtual" money of the cached bank)
            //System.Threading.Thread.Sleep(10000);

            List<Tuple<string, Vec3i>> tmp;
            auxBanksDictionary.TryGetValue(new Vec2i(chunkCoord.X, chunkCoord.Z), out tmp);
            if (tmp != null)
            {
                foreach (Tuple<string, Vec3i> pair in tmp)
                {
                    BlockEntity chest = caneconomy.sapi.World.BlockAccessor.
                    GetBlockEntity(new BlockPos(pair.Item2));
                    if (chest == null)
                    {
                        return;
                    }
                    RealBankInfo tmpBankInfo;
                    decimal valueInChest = countOfItemCoinsInInventory(chest);
                    if (TryGetRealBankInfo(pair.Item1, out tmpBankInfo))
                    {
                        return;
                    }

                    tmpBankInfo.setValidCachedValue(valueInChest);
                    tmpBankInfo.setLastKnownValue(valueInChest);
                    tmpBankInfo.setDirty(false);
                    updateAccount(pair.Item1, tmpBankInfo);
                }
            } 
        }

        public OperationResult depositFromAToB(string accountA, string accountB, decimal quantity)
        {
            throw new NotImplementedException();
        }
    }
}
