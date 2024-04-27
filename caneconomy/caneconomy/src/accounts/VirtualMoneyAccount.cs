using caneconomy.src.implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static caneconomy.src.implementations.OperationResult;

namespace caneconomy.src.accounts
{
    public class VirtualMoneyAccount : BaseMoneyAccount
    {
        public decimal CurrentBalance { get; set; }
        public string LastKnownName { get; set; }
        public VirtualMoneyAccount(string accountName) : base(accountName)
        {
            CurrentBalance = caneconomy.config.DEFAULT_VALUE_VIRTUAL_ACCOUNT;
            LastKnownName = "";
        }
        public override EnumOperationResultState withdraw(decimal val, bool takeToGlobalAccount = false)
        {
            if(getBalance() < val)
            {
                return EnumOperationResultState.SOURCE_NOT_ENOUGH_MONEY;
            }
            this.CurrentBalance -= val;                   
            this.MarkedDirty = true;
            return EnumOperationResultState.SUCCCESS;
        }
        public override EnumOperationResultState deposit(decimal val)
        {
            if (getMaxBalance() == 0)
            {
                this.CurrentBalance += val;             
            }
            else if (getMaxBalance() < (getBalance() + val))
            {
                return EnumOperationResultState.GREATER_THAN_MAX_BALANCE;
            }
            else
            {
                this.CurrentBalance += val;                           
            }
            this.MarkedDirty = true;
            return EnumOperationResultState.SUCCCESS;
        }
        public override decimal getMaxBalance()
        {
            return caneconomy.config.MAX_VIRTUAL_ACCOUNT_BALANCE;
        }
        public override decimal getBalance()
        {
            return CurrentBalance;
        }
    }
}


