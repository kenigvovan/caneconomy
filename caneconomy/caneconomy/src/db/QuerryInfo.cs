using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace caneconomy.src.db
{
    public class QuerryInfo
    {
        public string targetTable;
        public QuerryType action;
        public Dictionary<string, object> parameters;

        public QuerryInfo(string targetTable, QuerryType toDelete, Dictionary<string, object> parameters)
        {
            this.targetTable = targetTable;
            this.action = toDelete;
            this.parameters = parameters;
        }
    }
}
