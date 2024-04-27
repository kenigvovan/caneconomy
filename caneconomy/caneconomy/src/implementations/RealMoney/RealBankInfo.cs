using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace caneconomy.src.implementations.RealMoney
{
    public class RealBankInfo
    {
        bool dirty;
        decimal lasknownvalue;
        decimal validcachedvalue;
        Vec3i chestCoors;
        public string AccountName {  get; set; }

        public RealBankInfo(bool dirty, decimal lasknownvalue, decimal validcachedvalue, Vec3i chestCoors, string accountName)
        {
            this.dirty = dirty;
            this.lasknownvalue = lasknownvalue;
            this.validcachedvalue = validcachedvalue;
            this.chestCoors = chestCoors;
            this.AccountName = accountName;
        }
        public Vec3i getChestCoors()
        {
            return chestCoors;
        }
        public bool getDirty()
        {
            return dirty;
        }

        public decimal getLastKnownValue()
        {
            return lasknownvalue;
        }
        public decimal getValidCachedValue()
        {
            return validcachedvalue;
        }
        public void setDirty(bool val)
        {
            dirty = val;
        }

        public void setValidCachedValue(decimal val)
        {
            validcachedvalue = val;
        }
        public void setLastKnownValue(decimal val)
        {
            lasknownvalue = val;
        }
    }
}
