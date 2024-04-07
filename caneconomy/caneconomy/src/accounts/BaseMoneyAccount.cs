using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace caneconomy.src.accounts
{
    public abstract class BaseMoneyAccount
    {
        string accountName;
        double maxBalance;
        public BaseMoneyAccount(string accountName)
        {
            this.accountName = accountName;
        }

        public double getBalance()
        {
            return caneconomy.getHandler().getBalance(this.accountName);
        }
        public void setName(string str)
        {
            accountName = str;
        }
        public bool depositToOtherAccount(BaseMoneyAccount account, double amount)
        {
            if (getBalance() > amount)
            {
                return this.withdraw(amount) && account.deposit(amount);
            }
            return false;
        }
        public bool deposit(double val)
        {
            if (maxBalance == 0)
            {
                caneconomy.getHandler().deposit(getName(), val);
            }
            if (maxBalance < caneconomy.getHandler().getBalance(getName()) + val)
            {
                return false;
            }
            else
            {
                caneconomy.getHandler().deposit(getName(), val);
            }
            return true;
        }
        public bool withdraw(double val, bool takeToGlobalAccount = false)
        {
            if (caneconomy.getHandler().withdraw(getName(), val))
            {
                if (caneconomy.config.SAVE_TO_GLOBAL_ACCOUNT && takeToGlobalAccount)
                {
                    caneconomy.getHandler().deposit(caneconomy.config.GLOBAL_ACCOUNT_NAME, val);
                }
                return true;
            }
            return false;
        }


        //////////////////////////////////////////////////////////////////////////
        public string getName()
        {
            return accountName;
        }
    }
}
