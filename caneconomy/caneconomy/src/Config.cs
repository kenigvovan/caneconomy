using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Datastructures;

namespace caneconomy.src
{
    public class Config
    {
        public string PATH_TO_DB_AND_JSON_FILES = "";
        public string DB_NAME = "caneconomy.db";
        public string GLOBAL_ACCOUNT_NAME = "global_account_server";
        
        public OrderedDictionary<int, decimal> ID_TO_COINS_VALUES = new OrderedDictionary<int, decimal>()
        {

        };
        public OrderedDictionary<decimal, string> COINS_VALUES_TO_CODE = new OrderedDictionary<decimal, string>() {
                { 1, "gear-rusty" }
        };
        public bool SAVE_TO_GLOBAL_ACCOUNT = true;
        public bool CACHE_CHEST_BANK_CHUNKS_USED = true;
        public int MAX_AMOUNT_COINS_IN_STACK = 256;
        public decimal MAX_VIRTUAL_ACCOUNT_BALANCE = 100000;
        public string SELECTED_ECONOMY_HANDLER = "VIRTUAL_MONEY";
        public decimal DEFAULT_VALUE_VIRTUAL_ACCOUNT = 0;
        public int MINUTES_BETWEEN_DB_SAVED = 1;

    }
}
