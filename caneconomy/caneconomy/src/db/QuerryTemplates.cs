using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace caneconomy.src.db
{
    public static class QuerryTemplates
    { 
        //BANKS
        public static readonly string DELETE_BANK = "DELETE FROM BANKS WHERE accountname = @accountname";
        public static readonly string INSERT_BANK = "INSERT INTO BANKS (x, y, z, accountname, lastknownvalue,validcachedvalue, dirty) VALUES (@x, @y, @z, @accountname, @lastknownvalue, @validcachedvalue, @dirty)";
        public static readonly string UPDATE_BANK = "UPDATE BANKS SET x=@x, y=@y, z=@z, accountname=@accountname, lastknownvalue=@lastknownvalue, validcachedvalue=@validcachedvalue, dirty=@dirty WHERE accountname=@accountname";

        //VIRTUAL BANKS
        public static readonly string DELETE_VIRTUAL_BANK = "DELETE FROM VIRTUAL_BANKS WHERE accountname = @accountname";
        public static readonly string INSERT_VIRTUAL_BANK = "INSERT INTO VIRTUAL_BANKS (accountname, currentbalance, lastknownname) VALUES (@accountname, @currentbalance, @lastknownname)";
        public static readonly string UPDATE_VIRTUAL_BANK = "UPDATE VIRTUAL_BANKS SET accountname=@accountname, currentbalance=@currentbalance, lastknownname=@lastknownname where accountname=@accountname";


    }
}
