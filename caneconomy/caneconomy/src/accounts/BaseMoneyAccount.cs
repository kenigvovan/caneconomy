using static caneconomy.src.implementations.OperationResult;

namespace caneconomy.src.accounts
{
    public abstract class BaseMoneyAccount
    {
        public string accountName;
        public bool MarkedDirty { get; set; }
        public BaseMoneyAccount(string accountName)
        {
            this.accountName = accountName;
            MarkedDirty = false;
        }

        public virtual decimal getBalance()
        {
            return caneconomy.getHandler().getBalance(this.accountName);
        }
        public void setName(string str)
        {
            accountName = str;
        }
        public virtual EnumOperationResultState deposit(decimal val)
        {
            return EnumOperationResultState.NONE;
        }
        public virtual EnumOperationResultState withdraw(decimal val, bool takeToGlobalAccount = false)
        {
            return EnumOperationResultState.NONE;
        }

        public virtual decimal getMaxBalance()
        {
            return 0;
        }


        //////////////////////////////////////////////////////////////////////////
        public string getName()
        {
            return accountName;
        }
    }
}
