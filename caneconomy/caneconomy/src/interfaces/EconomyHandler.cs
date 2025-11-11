using System.Collections.Generic;
using caneconomy.src.accounts;
using caneconomy.src.implementations;

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
        bool tryGetAccountByLastKnownName(string lastKnownName, out VirtualMoneyAccount acoount);
    }
}
