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
        bool newAccount(string account, Vec3i pos);
        bool deleteAccount(string account);
        double getBalance(string account);
        bool withdraw(string account, double quantity);
        bool deposit(string account, double quantity);
        bool accountExist(string account);
        string getAccountInfoAdmin();
    }
}
