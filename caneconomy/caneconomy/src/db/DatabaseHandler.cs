using caneconomy.src.accounts;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace caneconomy.src.db
{
    public class DatabaseHandler
    {
        public virtual void readALL()
        {
            throw new NotImplementedException();
        }
        public virtual bool updateDatabase(QuerryInfo querry)
        {
            throw new NotImplementedException();
        }
        public virtual bool deleteFromDatabase(QuerryInfo querry)
        {
            throw new NotImplementedException();
        }
        public virtual bool insertToDatabase(QuerryInfo querry)
        {
            throw new NotImplementedException();
        }

    }
}
