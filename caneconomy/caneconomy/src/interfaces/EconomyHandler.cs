using caneconomy.src.db;
using caneconomy.src.implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace caneconomy.src.interfaces
{
    public interface EconomyHandler
    {      
        bool newAccount(string account, Dictionary<string, object> additionalInfoNewAccount = null);
        bool updateAccount(string account, Dictionary<string, object> additionalInfoUpdateAccount = null);
        bool deleteAccount(string account);
        decimal getBalance(string account);
        OperationResult withdraw(string account, decimal quantity);
        OperationResult deposit(string account, decimal quantity);
        OperationResult depositFromAToB(string accountA, string accountB, decimal quantity);
        bool accountExist(string account);
        string getAccountInfoAdmin();
    }
}
