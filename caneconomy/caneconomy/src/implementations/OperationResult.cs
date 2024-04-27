using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace caneconomy.src.implementations
{
    public class OperationResult
    {
        public enum EnumOperationResultState
        {
            NONE, SUCCCESS, WRONG_PARAMETER_VALUE, SOURCE_ACCOUNT_NOT_FOUND, TARGET_ACCOUNT_NOT_FOUND,
            SOURCE_NOT_ENOUGH_MONEY, FAILED_TARGET_DEPOSIT, FAILED_SOURCE_WITHDRAW, GREATER_THAN_MAX_BALANCE
        }
        public Dictionary<string, object> additionalValues;
        public EnumOperationResultState ResultState { get; set; }

        public OperationResult(EnumOperationResultState state, Dictionary<string, object> additionalValues = null)
        {
            this.ResultState = state;
            this.additionalValues = additionalValues;
        }
    }
}
