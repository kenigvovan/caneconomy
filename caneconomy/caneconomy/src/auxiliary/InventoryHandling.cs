using caneconomy.src.implementations.RealMoney;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace caneconomy.src.auxiliary
{
    public class InventoryHandling
    {
        //Search for itemID item in backpacks and in hotbar
        public static int countOfItemInInventory(IServerPlayer player)
        {
            int countOfItems = 0;
            InventoryPlayerBackPacks playerBackpacks = ((InventoryPlayerBackPacks)player.InventoryManager.GetOwnInventory("backpack"));

            foreach (ItemSlot itemSlot in playerBackpacks)
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
                        countOfItems += iS.StackSize * (int)caneconomy.config.ID_TO_COINS_VALUES[iS.Id];
                    }
                }
            }

            IInventory playerHotbar = player.InventoryManager.GetHotbarInventory();
            foreach (ItemSlot itemSlot in playerHotbar)
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
                        countOfItems += iS.StackSize * (int)caneconomy.config.ID_TO_COINS_VALUES[iS.Id];
                    }
                }
            }
            return countOfItems;
        }
        public static bool cachedBankUsageDelete(Vec3i pos, int amount, string accountname)
        {
            (caneconomy.getHandler() as RealMoneyEconomyHandler).TryGetRealBankInfo(accountname, out RealBankInfo tmp);
            if (tmp == null)
            {
                return false;
            }

            if (tmp.getLastKnownValue() < amount)
            {
                return false;
            }
            if (tmp.getDirty())
            {
                tmp.setValidCachedValue(tmp.getValidCachedValue() - amount);
            }
            else
            {
                tmp.setDirty(true);
                tmp.setValidCachedValue(tmp.getLastKnownValue() - amount);
            }

            (caneconomy.getHandler() as RealMoneyEconomyHandler).AddDirtyAccount(accountname);
            (caneconomy.getHandler() as RealMoneyEconomyHandler).updateAccount(accountname, tmp);
            return true;
        }
        public static bool cachedBankUsageAdd(Vec3i pos, int amount, string accountname)
        {
            (caneconomy.getHandler() as RealMoneyEconomyHandler).TryGetRealBankInfo(accountname, out RealBankInfo tmp);           
            if (tmp == null)
            {
                return false;
            }
            if (tmp.getDirty())
            {
                tmp.setValidCachedValue(tmp.getValidCachedValue() + amount);
            }
            else
            {
                tmp.setDirty(true);
                tmp.setValidCachedValue(tmp.getLastKnownValue() + amount);
            }


            (caneconomy.getHandler() as RealMoneyEconomyHandler).AddDirtyAccount(accountname);
            (caneconomy.getHandler() as RealMoneyEconomyHandler).updateAccount(accountname, tmp);
            return true;
        }
        public static bool deleteChestInventoryAmountOfItems(Vec3i pos, int amount, string accountname)
        {
            BlockEntity entity = caneconomy.sapi.World.BlockAccessor.GetBlockEntity(new BlockPos(pos));
            if (entity == null)
            {
                if (caneconomy.config.CACHE_CHEST_BANK_CHUNKS_USED)
                {
                    return cachedBankUsageDelete(pos, amount, accountname);
                }
                return false;
            }
            else
            {
                return deleteChestInventoryAmountOfItems(pos, amount, entity);
            }
        }
        public static bool deleteChestInventoryAmountOfItems(Vec3i pos, int amount, BlockEntity entity)
        {
            if (entity is BlockEntityGenericTypedContainer)
            {
                (entity as BlockEntityGenericTypedContainer).Inventory.TakeLocked = true;
                (entity as BlockEntityGenericTypedContainer).Inventory.PutLocked = true;

                int savedNeedAmount = amount;
                foreach (ItemSlot itemSlot in (entity as BlockEntityGenericTypedContainer).Inventory)
                {
                    ItemStack iS = itemSlot.Itemstack;

                    //No IS or is not an item
                    if (iS == null)
                    {
                        continue;
                    }
                    //Item is a coin
                    if (caneconomy.config.ID_TO_COINS_VALUES.ContainsKey(iS.Id))
                    {

                        int tmp = ((int)caneconomy.config.ID_TO_COINS_VALUES[iS.Id] * iS.StackSize) - amount;
                        //Slot has greater or equal value than we need
                        if (tmp >= 0)
                        {
                            int coinsTaken = amount / (int)caneconomy.config.ID_TO_COINS_VALUES[iS.Id];
                            if (amount % caneconomy.config.ID_TO_COINS_VALUES[iS.Id] > 0)
                            {
                                coinsTaken++;
                            }
                            //If we take all coins
                            if (coinsTaken >= iS.StackSize)
                            {
                                itemSlot.Itemstack = null;
                            }
                            else
                            {
                                iS.StackSize -= coinsTaken;
                            }
                            if (itemSlot?.Itemstack?.Item != null)
                            {
                                itemSlot.MarkDirty();
                            }
                            //We place difference back in the chest
                            if (amount % caneconomy.config.ID_TO_COINS_VALUES[iS.Id] > 0)
                            {
                                //False because we already locked put/take operations for inventory
                                //I await that it won't fail, because it can spawn ItemEntity if there is not free space
                                addToChestInventoryAmountOfItems(pos, (int)(caneconomy.config.ID_TO_COINS_VALUES[iS.Id] - (amount % caneconomy.config.ID_TO_COINS_VALUES[iS.Id])), entity, false);
                            }
                            (entity as BlockEntityGenericTypedContainer).Inventory.TakeLocked = false;
                            (entity as BlockEntityGenericTypedContainer).Inventory.PutLocked = false;
                            return true;
                        }
                        //Slot has less than we need
                        else
                        {
                            amount -= (int)caneconomy.config.ID_TO_COINS_VALUES[iS.Id] * iS.StackSize;
                            itemSlot.Itemstack = null;
                            if (itemSlot?.Itemstack?.Item != null)
                            {
                                itemSlot.MarkDirty();
                            }
                            continue;
                        }
                    }
                }

                //We didn't find enough coins, return back difference
                addToChestInventoryAmountOfItems(pos, savedNeedAmount - amount, entity, false);
                (entity as BlockEntityGenericTypedContainer).Inventory.TakeLocked = false;
                (entity as BlockEntityGenericTypedContainer).Inventory.PutLocked = false;
                return false;
            }
            return false;
        }
        public static bool deleteFromInventoryAmountOfItems(IServerPlayer player, int amount)
        {
            //Check backpacks and hotbar
            int savedAmount = amount;
            InventoryPlayerBackPacks playerBackpacks = ((InventoryPlayerBackPacks)player.InventoryManager.GetOwnInventory("backpack"));
            playerBackpacks.TakeLocked = true;
            playerBackpacks.PutLocked = true;

            foreach (ItemSlot itemSlot in playerBackpacks)
            {
                ItemStack iS = itemSlot.Itemstack;
                //No IS or is not an item
                if (iS == null || iS.Item == null)
                {
                    continue;
                }
                //Item is a coin
                if (caneconomy.config.ID_TO_COINS_VALUES.ContainsKey(iS.Id))
                {

                    int tmp = (int)((caneconomy.config.ID_TO_COINS_VALUES[iS.Id] * iS.StackSize) - amount);
                    //Slot has greater or equal value than we need
                    if (tmp >= 0)
                    {
                        int coinsTaken = amount / (int)caneconomy.config.ID_TO_COINS_VALUES[iS.Id];
                        if (amount % caneconomy.config.ID_TO_COINS_VALUES[iS.Id] > 0)
                        {
                            coinsTaken++;
                        }
                        //If we take all coins
                        if (coinsTaken >= iS.StackSize)
                        {
                            itemSlot.Itemstack = null;
                        }
                        else
                        {
                            iS.StackSize -= coinsTaken;
                        }
                        itemSlot.MarkDirty();
                        //We place difference back in the chest
                        if (amount % caneconomy.config.ID_TO_COINS_VALUES[iS.Id] > 0)
                        {
                            //False because we already locked put/take operations for inventory
                            //I await that it won't fail, because it can spawn ItemEntity if there is not free space
                            playerBackpacks.TakeLocked = false;
                            playerBackpacks.PutLocked = false;
                            addToInventoryAmountOfItems((int)(caneconomy.config.ID_TO_COINS_VALUES[iS.Id] - (amount % caneconomy.config.ID_TO_COINS_VALUES[iS.Id])), player, false);
                        }
                        playerBackpacks.TakeLocked = false;
                        playerBackpacks.PutLocked = false;
                        return true;
                    }
                    //Slot has less than we need
                    else
                    {
                        amount -= (int)caneconomy.config.ID_TO_COINS_VALUES[iS.Id] * iS.StackSize;
                        itemSlot.Itemstack = null;
                        itemSlot.MarkDirty();
                        continue;
                    }
                }
            }
            playerBackpacks.TakeLocked = false;
            playerBackpacks.PutLocked = false;

            InventoryBase playerHotbar = (InventoryBase)player.InventoryManager.GetHotbarInventory();
            playerHotbar.TakeLocked = true;
            playerHotbar.PutLocked = true;

            foreach (ItemSlot itemSlot in playerHotbar)
            {
                ItemStack iS = itemSlot.Itemstack;
                //No IS or is not an item
                if (iS == null || iS.Item == null)
                {
                    continue;
                }
                //Item is a coin
                if (caneconomy.config.ID_TO_COINS_VALUES.ContainsKey(iS.Id))
                {

                    int tmp = ((int)caneconomy.config.ID_TO_COINS_VALUES[iS.Id] * iS.StackSize) - amount;
                    //Slot has greater or equal value than we need
                    if (tmp >= 0)
                    {
                        int coinsTaken = amount / (int)caneconomy.config.ID_TO_COINS_VALUES[iS.Id];
                        if (amount % caneconomy.config.ID_TO_COINS_VALUES[iS.Id] > 0)
                        {
                            coinsTaken++;
                        }
                        //If we take all coins
                        if (coinsTaken >= iS.StackSize)
                        {
                            itemSlot.Itemstack = null;
                        }
                        else
                        {
                            iS.StackSize -= coinsTaken;
                        }
                        itemSlot.MarkDirty();
                        //We place difference back in the chest
                        if (amount % caneconomy.config.ID_TO_COINS_VALUES[iS.Id] > 0)
                        {
                            //False because we already locked put/take operations for inventory
                            //I await that it won't fail, because it can spawn ItemEntity if there is not free space
                            playerHotbar.TakeLocked = false;
                            playerHotbar.PutLocked = false;
                            addToInventoryAmountOfItems((int)(caneconomy.config.ID_TO_COINS_VALUES[iS.Id] - (amount % caneconomy.config.ID_TO_COINS_VALUES[iS.Id])), player, false);
                        }
                        playerHotbar.TakeLocked = false;
                        playerHotbar.PutLocked = false;
                        return true;
                    }
                    //Slot has less than we need
                    else
                    {
                        amount -= (int)caneconomy.config.ID_TO_COINS_VALUES[iS.Id] * iS.StackSize;
                        itemSlot.Itemstack = null;
                        itemSlot.MarkDirty();
                        continue;
                    }
                }
            }
            playerHotbar.TakeLocked = false;
            playerHotbar.PutLocked = false;
            addToInventoryAmountOfItems(savedAmount - amount, player, false);
            return false;
        }

        public static bool addToChestInventoryAmountOfItems(Vec3i pos, int amount, string accountname)
        {
            BlockEntity entity = caneconomy.sapi.World.BlockAccessor.GetBlockEntity(new BlockPos(pos));
            if (entity == null)
            {
                if (caneconomy.config.CACHE_CHEST_BANK_CHUNKS_USED)
                {
                    return cachedBankUsageAdd(pos, amount, accountname);
                }
                return false;
            }

            return addToChestInventoryAmountOfItems(pos, amount, entity);
        }
        public static bool addToChestInventoryAmountOfItems(Vec3i pos, int amount, BlockEntity entity, bool shouldLock = true)
        {
            int countOfItemsAdded = amount;
            if (entity is BlockEntityGenericTypedContainer)
            {
                if (shouldLock)
                {
                    (entity as BlockEntityGenericTypedContainer).Inventory.TakeLocked = true;
                    (entity as BlockEntityGenericTypedContainer).Inventory.PutLocked = true;
                }
                foreach (ItemSlot itemSlot in (entity as BlockEntityGenericTypedContainer).Inventory)
                {
                    ItemStack iS = itemSlot.Itemstack;

                    //Slot is empty, place here some coins
                    if (iS == null)
                    {
                        foreach (var coinType in caneconomy.config.COINS_VALUES_TO_CODE)
                        {
                            int fullCoins = amount / (int)coinType.Key;
                            if (fullCoins > caneconomy.config.MAX_AMOUNT_COINS_IN_STACK)
                            {
                                fullCoins = caneconomy.config.MAX_AMOUNT_COINS_IN_STACK;
                            }
                            if (fullCoins < 1)
                            {
                                continue;
                            }
                            ItemStack newIS = new ItemStack(caneconomy.sapi.World.GetItem(new AssetLocation(coinType.Value)));
                            newIS.StackSize = fullCoins;
                            iS = newIS;
                            amount -= fullCoins * (int)coinType.Key;
                            itemSlot.Itemstack = newIS;
                            if (itemSlot?.Itemstack?.Item != null)
                            {
                                itemSlot.MarkDirty();
                            }
                            break;
                        }
                    }
                    //There is block
                    else if (iS.Item == null)
                    {
                        continue;
                    }
                    //Here is already some coin item, try to place more here of the same
                    else if (caneconomy.config.ID_TO_COINS_VALUES.ContainsKey(iS.Id))
                    {
                        int fullCoins = amount / (int)caneconomy.config.ID_TO_COINS_VALUES[iS.Id];
                        //Slot can not fit everything
                        if (fullCoins + iS.StackSize >= caneconomy.config.MAX_AMOUNT_COINS_IN_STACK)
                        {
                            fullCoins = caneconomy.config.MAX_AMOUNT_COINS_IN_STACK - iS.StackSize;
                        }
                        //Full
                        if (fullCoins == 0)
                        {
                            continue;
                        }
                        iS.StackSize += fullCoins;
                        amount -= fullCoins * (int)caneconomy.config.ID_TO_COINS_VALUES[iS.Id];
                        if (amount < 0)
                        {
                            if (shouldLock)
                            {
                                (entity as BlockEntityGenericTypedContainer).Inventory.TakeLocked = false;
                                (entity as BlockEntityGenericTypedContainer).Inventory.PutLocked = false;
                            }
                            return true;
                        }
                        if (itemSlot?.Itemstack?.Item != null)
                        {
                            itemSlot.MarkDirty();
                        }
                        continue;
                    }
                }
                //Went through all slots, but have to place some more coins
                foreach (var coinType in caneconomy.config.COINS_VALUES_TO_CODE)
                {
                    int fullCoins = amount / (int)coinType.Key;
                    if (fullCoins > caneconomy.config.MAX_AMOUNT_COINS_IN_STACK)
                    {
                        fullCoins = caneconomy.config.MAX_AMOUNT_COINS_IN_STACK;
                    }

                    ItemStack newIS = new ItemStack(caneconomy.sapi.World.GetItem(new AssetLocation(coinType.Value)));
                    newIS.StackSize = fullCoins;
                    amount -= fullCoins;
                    caneconomy.sapi.World.SpawnItemEntity(newIS, new Vec3d(pos.X, pos.Y, pos.Z));
                    if (amount > 0)
                    {
                        continue;
                    }
                    else
                    {
                        if (shouldLock)
                        {
                            (entity as BlockEntityGenericTypedContainer).Inventory.TakeLocked = false;
                            (entity as BlockEntityGenericTypedContainer).Inventory.PutLocked = false;
                        }
                        return true;
                    }
                }
                if (shouldLock)
                {
                    (entity as BlockEntityGenericTypedContainer).Inventory.TakeLocked = false;
                    (entity as BlockEntityGenericTypedContainer).Inventory.PutLocked = false;
                }

                return false;
            }
            else
            {
                return false;
            }
        }
        public static bool addToInventoryAmountOfItems(int amount, IServerPlayer player, bool shouldLock = true)
        {
            foreach (var coinType in caneconomy.config.COINS_VALUES_TO_CODE)
            {
                int fullCoins = amount / (int)coinType.Key;
                if (fullCoins > caneconomy.config.MAX_AMOUNT_COINS_IN_STACK)
                {
                    fullCoins = caneconomy.config.MAX_AMOUNT_COINS_IN_STACK;
                }
                ItemStack newIS = new ItemStack(player.Entity.Api.World.GetItem(new AssetLocation(coinType.Value)));
                if (fullCoins < 1)
                {
                    continue;
                }
                newIS.StackSize = fullCoins;
                amount -= fullCoins * (int)coinType.Key;
                player.Entity.TryGiveItemStack(newIS);
                if (amount > 0)
                {
                    continue;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }
    }
}
