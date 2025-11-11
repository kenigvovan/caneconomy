using System.Collections.Generic;

namespace caneconomy.src.db
{
    public class QuerryInfo
    {
        public QuerryType action;
        public Dictionary<string, object> parameters;

        public QuerryInfo( QuerryType toDelete, Dictionary<string, object> parameters)
        {
            this.action = toDelete;
            this.parameters = parameters;
        }
    }
}
