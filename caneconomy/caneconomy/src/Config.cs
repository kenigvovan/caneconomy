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
        
        public OrderedDictionary<int, int> ID_TO_COINS_VALUES = new OrderedDictionary<int, int>()
        {

        };
        public OrderedDictionary<int, string> COINS_VALUES_TO_CODE = new OrderedDictionary<int, string>() {
                { 1, "gear-rusty" }
        };
        public bool SAVE_TO_GLOBAL_ACCOUNT = true;
        public bool CACHE_CHEST_BANK_CHUNKS_USED = true;
        public int MAX_AMOUNT_COINS_IN_STACK = 256;
        
    }
}
