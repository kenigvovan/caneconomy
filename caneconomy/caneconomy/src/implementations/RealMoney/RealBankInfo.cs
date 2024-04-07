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
        double lasknownvalue;
        double validcachedvalue;
        Vec3i chestCoors;
        public string AccountName {  get; set; }

        public RealBankInfo(bool dirty, double lasknownvalue, double validcachedvalue, Vec3i chestCoors, string accountName)
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

        public double getLastKnownValue()
        {
            return lasknownvalue;
        }
        public double getValidCachedValue()
        {
            return validcachedvalue;
        }
        public void setDirty(bool val)
        {
            dirty = val;
        }

        public void setValidCachedValue(double val)
        {
            validcachedvalue = val;
        }
        public void setLastKnownValue(double val)
        {
            lasknownvalue = val;
        }
    }
}
