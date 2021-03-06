﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace MyORMFrame.DBServerProvider
{
    public interface IDbServerProvider
    {
        string ConnectionString
        {
            get;set;
        }
        string DataBase { get; }

        int ExcuteNonQuery(CommandType cmdType, string cmdText);

        DbDataReader ExcuteReader(CommandType cmdType, string cmdText);

        object ExcuteScalar(CommandType cmdType, string cmdText);

        SqlException ConvertToSqlException(Exception exception);
    }
}
