using caneconomy.src.accounts;
using caneconomy.src.db;
using caneconomy.src.implementations.RealMoney;
using caneconomy.src.interfaces;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Concurrent;
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
using static caneconomy.src.implementations.OperationResult;

namespace caneconomy.src.implementations.VirtualMoney
{
    public class VirtualMoneyEconomyHandler : EconomyHandler
    {
        DatabaseHandler databaseHandler;
        //key is uid for players
        ConcurrentDictionary<string, VirtualMoneyAccount> Accounts;
        Dictionary<string, VirtualMoneyAccount> AccountsByName;
        public VirtualMoneyEconomyHandler()
        {
            Accounts = new ConcurrentDictionary<string, VirtualMoneyAccount>();
            AccountsByName = new Dictionary<string, VirtualMoneyAccount>();
            try
            {
                databaseHandler = new SQLiteDatabaseHanlder(QuerryTemplates.INSERT_VIRTUAL_BANK, QuerryTemplates.UPDATE_VIRTUAL_BANK, QuerryTemplates.DELETE_VIRTUAL_BANK,
                                                            "CREATE TABLE IF NOT EXISTS VIRTUAL_BANKS(" +
                                                            "currentbalance TEXT," +
                                                            "accountname TEXT," +
                                                            "lastknownname TEXT," +
                                                            "PRIMARY KEY (accountname));",
                                                            ForAllRead);
            }
            catch (SqliteException ex)
            {
            }
            databaseHandler.readALL();
            this.InitServer();
            caneconomy.setHandler(this);
            RegisterCommands();
            InitEvents();
            
        }
        public void ForAllRead(SqliteConnection sqliteConnection)
        {
            SqliteCommand com = sqliteConnection.CreateCommand();
            com.CommandText = "SELECT * FROM VIRTUAL_BANKS";

            using (var reader = com.ExecuteReader())
            {
                var dt = new DataTable();

                dt.Load(reader);
                foreach (DataRow it in dt.Rows)
                {
                    VirtualMoneyAccount tmpVMA = new VirtualMoneyAccount(it["accountname"].ToString());
                    tmpVMA.CurrentBalance = decimal.Parse(it["currentbalance"].ToString(), CultureInfo.InvariantCulture);
                    tmpVMA.LastKnownName = it["lastknownname"].ToString();
                    Accounts.TryAdd(it["accountname"].ToString(), tmpVMA);
                    AccountsByName[tmpVMA.LastKnownName] = tmpVMA;
                }
            }
        }
        public void InitEvents()
        {
            caneconomy.sapi.Event.PlayerJoin += Event_OnPlayerJoin;
            caneconomy.sapi.Event.Timer((() =>
            {
                SaveAccounts(onylDirty: true);
            }
            ), 60 * caneconomy.config.MINUTES_BETWEEN_DB_SAVED);
            caneconomy.sapi.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, () => SaveAccounts(false));
        }
        public void SaveAccounts(bool onylDirty = true)
        {
            foreach (var account in Accounts.ToArray())
            {
                if (!onylDirty || account.Value.MarkedDirty)
                {
                    Dictionary<string, object> dict = new Dictionary<string, object>
                        {
                            { "accountname", account.Key },
                            { "currentbalance", account.Value.CurrentBalance.ToString(CultureInfo.InvariantCulture) },
                            { "lastknownname", account.Value.LastKnownName }
                        };

                    databaseHandler.updateDatabase(new QuerryInfo(QuerryType.UPDATE, dict));
                    
                    account.Value.MarkedDirty = false;
                }
            }
        }
        public void Event_OnPlayerJoin(IServerPlayer player)
        {
            if (!Accounts.TryGetValue(player.PlayerUID, out VirtualMoneyAccount vma))
            {
                newAccount(player.PlayerUID, new Dictionary<string, object> { { "lastknownname", player.PlayerName } });
            }
            else
            {
                if(!vma.LastKnownName.Equals(player.PlayerName))
                {
                    AccountsByName.Remove(vma.LastKnownName);
                    AccountsByName.Add(player.PlayerName, vma);
                }
            }

        }
        public void InitServer()
        {
            checkForGlobalAccount();
        }
        public void checkForGlobalAccount()
        {
            //CLOSED ECONOMY
            if (caneconomy.config.SAVE_TO_GLOBAL_ACCOUNT)
            {
                //NO GLOBAL ACCOUNT CREATED YET
                newAccount(caneconomy.config.GLOBAL_ACCOUNT_NAME);
            }
        }
        public void RegisterCommands()
        {
            var parsers = caneconomy.sapi.ChatCommands.Parsers;
            caneconomy.sapi.ChatCommands.GetOrCreate("pay")
                            .RequiresPlayer().RequiresPrivilege(Privilege.chat)
                            .WithArgs(parsers.Word("recipient"), parsers.Int("amount"))
                            .HandleWith(PayCommand);

            caneconomy.sapi.ChatCommands.GetOrCreate("balance")
                            .RequiresPlayer().RequiresPrivilege(Privilege.chat)
                            .HandleWith(BalanceCommand);

            caneconomy.sapi.ChatCommands.GetOrCreate("withdraw")
                            .RequiresPlayer().RequiresPrivilege(Privilege.chat)
                            .WithArgs(parsers.Int("amount"))
                            .HandleWith(WithdrawCommand);

            caneconomy.sapi.ChatCommands.GetOrCreate("deposit")
                            .RequiresPlayer().RequiresPrivilege(Privilege.chat)
                            .HandleWith(DepositCommand);
        }
        public TextCommandResult DepositCommand(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (!Accounts.TryGetValue(player.PlayerUID, out VirtualMoneyAccount vma))
            {
                return TextCommandResult.Success(Lang.Get("caneconomy:own_account_not_found"));
            }

            decimal takenValue = TakeCurrencyItemsFromPlayerActiveSlot(player);
            if(takenValue > 0)
            {
                OperationResult resultValue = deposit(player.PlayerUID, takenValue);

                switch (resultValue.ResultState)
                {

                    case EnumOperationResultState.TARGET_ACCOUNT_NOT_FOUND:
                        return TextCommandResult.Success(Lang.Get("caneconomy:deposit_failed"));
                    case EnumOperationResultState.SOURCE_ACCOUNT_NOT_FOUND:
                        return TextCommandResult.Success(Lang.Get("caneconomy:own_account_not_found"));
                    case EnumOperationResultState.SUCCCESS:
                        return TextCommandResult.Success(Lang.Get("caneconomy:number_deposited", takenValue));
                    case EnumOperationResultState.WRONG_PARAMETER_VALUE:
                        return TextCommandResult.Success(Lang.Get("caneconomy:wrong_parameter"));
                    case EnumOperationResultState.SOURCE_NOT_ENOUGH_MONEY:
                        return TextCommandResult.Success(Lang.Get("caneconomy:not_enough_money"));
                    case EnumOperationResultState.FAILED_SOURCE_WITHDRAW:
                        return TextCommandResult.Success(Lang.Get("caneconomy:withdraw_failed"));
                    case EnumOperationResultState.FAILED_TARGET_DEPOSIT:
                        return TextCommandResult.Success(Lang.Get("caneconomy:deposit_failed"));
                    default:
                        return TextCommandResult.Success(Lang.Get("caneconomy:placeholder_return"));
                }
            }
            return TextCommandResult.Success(Lang.Get("caneconomy:no_currency_item_found"));
        }
        public TextCommandResult WithdrawCommand(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            decimal payValue = (int)args.Parsers[0].GetValue();

            OperationResult resultValue = withdraw(player.PlayerUID, payValue);

            switch (resultValue.ResultState)
            {
                case EnumOperationResultState.SOURCE_ACCOUNT_NOT_FOUND:
                    return TextCommandResult.Success(Lang.Get("caneconomy:own_account_not_found"));
                case EnumOperationResultState.SUCCCESS:
                    GiveCurrencyItemsToPlayer(player, payValue);
                    return TextCommandResult.Success(Lang.Get("caneconomy:number_withdrawned", payValue));
                case EnumOperationResultState.WRONG_PARAMETER_VALUE:
                    return TextCommandResult.Success(Lang.Get("caneconomy:wrong_parameter"));
                case EnumOperationResultState.SOURCE_NOT_ENOUGH_MONEY:
                    return TextCommandResult.Success(Lang.Get("caneconomy:not_enough_money"));
                case EnumOperationResultState.FAILED_SOURCE_WITHDRAW:
                    return TextCommandResult.Success(Lang.Get("caneconomy:withdraw_failed"));
                default:
                    return TextCommandResult.Success(Lang.Get("caneconomy:placeholder_return"));
            }
        }
        public static void GiveCurrencyItemsToPlayer(IServerPlayer player, decimal withdrawValue)
        {
            decimal tmpWithdrawValue = withdrawValue;
            
            foreach (var it in caneconomy.config.COINS_VALUES_TO_CODE)
            {
                Item currentItem = caneconomy.sapi.World.GetItem(new AssetLocation(it.Value));
                int stacksToGive = (int)(tmpWithdrawValue / currentItem.MaxStackSize);
                if(stacksToGive > 0)
                {
                    for(int i = 0; i < stacksToGive; i++) 
                    {
                        ItemStack newIS = new ItemStack(currentItem, currentItem.MaxStackSize);
                        if (!player.InventoryManager.TryGiveItemstack(newIS))
                        {
                            caneconomy.sapi.World.SpawnItemEntity(newIS, player.Entity.Pos.XYZ.Clone().Add(0.5f, 0.25f, 0.5f));
                        }
                    }
                    tmpWithdrawValue -= stacksToGive * currentItem.MaxStackSize;
                }
                
                if(tmpWithdrawValue >= 0)
                {
                    if (it.Key != caneconomy.config.COINS_VALUES_TO_CODE.Last().Key)
                    {
                        continue;
                    }
                    ItemStack newIS = new ItemStack(currentItem, (int)tmpWithdrawValue);
                    if (!player.InventoryManager.TryGiveItemstack(newIS))
                    {
                        caneconomy.sapi.World.SpawnItemEntity(newIS, player.Entity.Pos.XYZ.Clone().Add(0.5f, 0.25f, 0.5f));
                    }
                }
                break;
            }
            caneconomy.config.COINS_VALUES_TO_CODE.Last();
        }
        public static decimal TakeCurrencyItemsFromPlayerActiveSlot(IServerPlayer player)
        {
            if (player.InventoryManager.ActiveHotbarSlot.Itemstack != null &&
                caneconomy.config.ID_TO_COINS_VALUES.TryGetValue(player.InventoryManager.ActiveHotbarSlot.Itemstack.Id, out decimal itemValue))
            {
                return player.InventoryManager.ActiveHotbarSlot.TakeOutWhole().StackSize * itemValue;
            }
            return 0;
        }
        public TextCommandResult BalanceCommand(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (!Accounts.TryGetValue(player.PlayerUID, out VirtualMoneyAccount vma))
            {
                return TextCommandResult.Error(Lang.Get("caneconomy:own_account_not_found"));
            }
            return TextCommandResult.Success(Lang.Get("caneconomy:own_balance_state", vma.getBalance()));
        }
        public TextCommandResult PayCommand(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            string receiver = (string)args.Parsers[0].GetValue();

            decimal payValue = (int)args.Parsers[1].GetValue();
            if(payValue <= 0)
            {
                return TextCommandResult.Success(Lang.Get("caneconomy:need_number_greater_than_zero"));
            }

            OperationResult resultValue = depositFromAToB(player.PlayerUID, receiver, payValue);

            switch (resultValue.ResultState)
            {
                case EnumOperationResultState.TARGET_ACCOUNT_NOT_FOUND:
                    return TextCommandResult.Success(Lang.Get("caneconomy:deposit_failed"));
                case EnumOperationResultState.SOURCE_ACCOUNT_NOT_FOUND:
                    return TextCommandResult.Success(Lang.Get("caneconomy:own_account_not_found"));
                case EnumOperationResultState.SUCCCESS:
                    return TextCommandResult.Success(Lang.Get("caneconomy:successfully_paid", payValue,
                        (resultValue.additionalValues?.TryGetValue("targetaccountname", out object lastName) ?? false)
                                                                                                                ? lastName
                                                                                                                : ""));
                case EnumOperationResultState.WRONG_PARAMETER_VALUE:
                    return TextCommandResult.Success(Lang.Get("caneconomy:wrong_parameter"));
                case EnumOperationResultState.SOURCE_NOT_ENOUGH_MONEY:
                    return TextCommandResult.Success(Lang.Get("caneconomy:not_enough_money"));
                case EnumOperationResultState.FAILED_SOURCE_WITHDRAW:
                    return TextCommandResult.Success(Lang.Get("caneconomy:withdraw_failed"));
                case EnumOperationResultState.FAILED_TARGET_DEPOSIT:
                    return TextCommandResult.Success(Lang.Get("caneconomy:deposit_failed"));
                default:
                    return TextCommandResult.Success(Lang.Get("caneconomy:placeholder_return"));
            }
        }

       
        public bool accountExist(string account)
        {
            return Accounts.TryGetValue(account, out var _);
        }
        public bool deleteAccount(string account)
        {
            if(!Accounts.TryRemove(account, out var _))
            {
                return false;
            }
            Dictionary<string, object> dict = new Dictionary<string, object>
            {
                { "accountname", account }
            };

            databaseHandler.deleteFromDatabase(new QuerryInfo(QuerryType.DELETE, dict));
            return true;
        }
        public OperationResult deposit(string account, decimal quantity)
        {
            if (quantity < 0)
            {
                return new OperationResult(EnumOperationResultState.WRONG_PARAMETER_VALUE);
            }
            if (Accounts.TryGetValue(account, out var foundAccount))
            {
                lock (foundAccount)
                {
                    return new OperationResult(foundAccount.deposit(quantity));
                }
            }
            return new OperationResult(EnumOperationResultState.TARGET_ACCOUNT_NOT_FOUND);
        }
        public string getAccountInfoAdmin()
        {
            throw new NotImplementedException();
        }
        public decimal getBalance(string account)
        {
            if (Accounts.TryGetValue(account, out var foundAccount))
            {
                return foundAccount.getBalance();
            }
            return -1;
        }
        public bool newAccount(string account, Dictionary<string, object> additionalInfoNewAccount = null)
        {
            VirtualMoneyAccount tmpVMA = new VirtualMoneyAccount(account);
            if (!this.Accounts.TryAdd(account, tmpVMA))
            {
                return false;
            }
            if(additionalInfoNewAccount?.TryGetValue("lastknownname", out object lastknownname) ?? false)
            {
                tmpVMA.LastKnownName = (string)lastknownname;
            }
            Dictionary<string, object> dict = new Dictionary<string, object>
                {
                    { "accountname", tmpVMA.getName() },
                    { "currentbalance", tmpVMA.CurrentBalance },
                    { "lastknownname", tmpVMA.LastKnownName }
                };          

            databaseHandler.insertToDatabase(new QuerryInfo(QuerryType.INSERT, dict));
            
            return true;
        }
        public OperationResult withdraw(string account, decimal quantity)
        {
            if (quantity < 0)
            {
                return new OperationResult(EnumOperationResultState.WRONG_PARAMETER_VALUE);
            }

            if (Accounts.TryGetValue(account, out var foundAccount))
            {
                lock (foundAccount)
                {
                    return new OperationResult(foundAccount.withdraw(quantity));
                }
            }
            return new OperationResult(EnumOperationResultState.SOURCE_ACCOUNT_NOT_FOUND);
        }
        public bool updateAccount(string account, Dictionary<string, object> additionalInfoUpdateAccount = null)
        {
            if (!this.Accounts.TryGetValue(account, out VirtualMoneyAccount vma))
            {
                return false;
            }
            if (additionalInfoUpdateAccount?.TryGetValue("lastknownname", out object lastknownname) ?? false)
            {
                vma.LastKnownName = (string)lastknownname;
                vma.MarkedDirty = true;
            }
            return true;
        }
        public OperationResult depositFromAToB(string accountA, string accountB, decimal quantity)
        {
            if(quantity < 0)
            {
                return new OperationResult(EnumOperationResultState.WRONG_PARAMETER_VALUE);
            }

            if (!Accounts.TryGetValue(accountA, out VirtualMoneyAccount sourceVMA))
            {
                return new OperationResult(EnumOperationResultState.SOURCE_ACCOUNT_NOT_FOUND);
            }

            if (!AccountsByName.TryGetValue(accountB, out VirtualMoneyAccount receiverVMA))
            {
                return new OperationResult(EnumOperationResultState.TARGET_ACCOUNT_NOT_FOUND);
            }

            if (sourceVMA.CurrentBalance < quantity)
            { 
                return new OperationResult(EnumOperationResultState.SOURCE_NOT_ENOUGH_MONEY);
            }

            EnumOperationResultState withdrawResult = EnumOperationResultState.NONE;
            lock(sourceVMA)
            {
                withdrawResult = sourceVMA.withdraw(quantity);
            }

            EnumOperationResultState depositResult = EnumOperationResultState.NONE;
            if (withdrawResult == EnumOperationResultState.SUCCCESS) 
            {                
                lock (receiverVMA)
                {
                    depositResult = receiverVMA.deposit(quantity);
                }
            }
            else
            {
                return new OperationResult(EnumOperationResultState.FAILED_SOURCE_WITHDRAW);
            }
            
            if(depositResult == EnumOperationResultState.SUCCCESS)
            {
                return new OperationResult(EnumOperationResultState.SUCCCESS, new Dictionary<string, object> { { "targetaccountname", receiverVMA .LastKnownName} });
            }
            else
            {
                lock (sourceVMA)
                {
                    sourceVMA.deposit(quantity);
                }
                return new OperationResult(EnumOperationResultState.FAILED_TARGET_DEPOSIT);
            }

        }
    }
}
