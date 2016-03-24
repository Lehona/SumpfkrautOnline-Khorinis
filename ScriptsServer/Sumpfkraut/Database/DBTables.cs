﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Data.Sqlite;
using SQLiteDataReader = Mono.Data.Sqlite.SqliteDataReader;
using SQLiteCommand = Mono.Data.Sqlite.SqliteCommand;
//using SQLiteCommand = Mono.Data.Sqlite.SqliteCommand;

namespace GUC.Server.Scripts.Sumpfkraut.Database
{

    public class DBTables : GUC.Utilities.ExtendedObject
    {

        public struct ColumnGetTypeInfo
        {
            public String colName;
            public SQLiteGetTypeEnum getType;

            public ColumnGetTypeInfo (String colName, SQLiteGetTypeEnum getType)
            {
                this.colName = colName;
                this.getType = getType;
            }
        }



        new public static readonly String _staticName = "DBTables (static)";
        new protected String _objName = "DBTables (default)";
        

        
        public static bool ConvertSQLResults (ref List<List<List<object>>> sqlResults, 
            ref List<ColumnGetTypeInfo> colGetTypeInfo)
        {
            bool allConverted = true;
            object tempEntry = null;
            int res = 0;
            int row = 0;
            int col = 0;


            // iterating results (resulted by multiplay statements seperated by ; in sql-command)
            while (res < sqlResults.Count)
            {
                // iterating data-rows
                row = 0;
                while (row < sqlResults[res].Count)
                {
                    // iterating data-columns
                    col = 0;
                    while (col < sqlResults[res][row].Count)
                    {
                        if (sqlResults[res][row][col] == null)
                        {
                            // null might be just fine
                        }
                        else if (sqlResults[res][row][col].GetType() == typeof(DBNull))
                        {
                            // DBNull is aa little unheady because it would need additional type-checks later
                            // use null instead
                            sqlResults[res][row][col] = null;
                        }
                        else
                        {
                            // everything else should be a string and somehow convertable
                            tempEntry = sqlResults[res][row][col].ToString();
                            if (DBTables.SqlStringToData((string) tempEntry, 
                                colGetTypeInfo[col].getType, 
                                ref tempEntry))
                            {
                                sqlResults[res][row][col] = tempEntry;
                            }
                            else
                            {
                                sqlResults[res][row][col] = null;

                                MakeLogErrorStatic(typeof(DBTables), String.Format(
                                    "ConvertSQLResults: Could not convert {0}" 
                                    + "from String to type {1} for column {2}!", 
                                    tempEntry, colGetTypeInfo[col].getType, 
                                    colGetTypeInfo[col].colName));

                                allConverted = false;
                            }
                        }
 
                        col++;
                    }

                    row++;
                }
            }
            
            return allConverted;
        }

        public static void SqlColumnInfo (Dictionary<String, SQLiteGetTypeEnum> getTypeByColumn,
            out List<ColumnGetTypeInfo> colGetTypes)
        {
            colGetTypes = new List<ColumnGetTypeInfo>();
            foreach (KeyValuePair<String, SQLiteGetTypeEnum> keyValPair in getTypeByColumn)
            {
                colGetTypes.Add(new ColumnGetTypeInfo(keyValPair.Key, keyValPair.Value));
            }
        }

        public static void SqlColumnInfo (ref Dictionary<String, SQLiteGetTypeEnum> getTypeByColumn,
            out List<ColumnGetTypeInfo> colGetTypes)
        {
            colGetTypes = new List<ColumnGetTypeInfo>();
            foreach (KeyValuePair<String, SQLiteGetTypeEnum> keyValPair in getTypeByColumn)
            {
                colGetTypes.Add(new ColumnGetTypeInfo(keyValPair.Key, keyValPair.Value));
            }
        }
        
        /**
         *   Method to convert strings of a sql-result to the hinted datatype.
         *   Return or false if the data conversion succeeds or fails, respectively.
         *   @param sqlString is the string to convert
         *   @param get is the hint on the final data type of the conversion
         *   @param output is the object where the resulting type-converted string will be stored into
         */
        public static bool SqlStringToData (string sqlString, SQLiteGetTypeEnum get, ref object output)
        {
            if (sqlString != null)
            {
                switch (get)
                {
                    case (SQLiteGetTypeEnum.GetBoolean):
                        bool outBool = false;
                        if (bool.TryParse(sqlString, out outBool))
                        {
                            output = outBool;
                            return true;
                        }
                        else if (sqlString.Equals("0"))
                        {
                            output = false;
                            return true;
                        }
                        else if (sqlString.Equals("1"))
                        {
                            output = true;
                            return true;
                        }
                        else
                        {
                            Log.Logger.logError("Could not convert sql-result-string '" + sqlString 
                                + "' with " + get + " in SqlStringToData.");
                            break;
                        }
                    case (SQLiteGetTypeEnum.GetByte):
                        byte outByte = 0;
                        if (byte.TryParse(sqlString, out outByte))
                        {
                            output = outByte;
                            return true;
                        }
                        else
                        {
                            Log.Logger.logError("Could not convert sql-result-string '" + sqlString 
                                + "' with " + get + " in SqlStringToData.");
                            break;
                        }
                    case (SQLiteGetTypeEnum.GetChar):
                        char outChar = '0';
                        if (char.TryParse(sqlString, out outChar))
                        {
                            output = outChar;
                            return true;
                        }
                        else
                        {
                            Log.Logger.logError("Could not convert sql-result-string '" + sqlString 
                                + "' with " + get + " in SqlStringToData.");
                            break;
                        }
                    case (SQLiteGetTypeEnum.GetDateTime):
                        DateTime outDateTime = new DateTime();
                        if (DateTime.TryParse(sqlString, out outDateTime))
                        {
                            output = outDateTime;
                            return true;
                        }
                        else
                        {
                            Log.Logger.logError("Could not convert sql-result-string '" + sqlString 
                                + "' with " + get + " in SqlStringToData.");
                            break;
                        }
                    case (SQLiteGetTypeEnum.GetDecimal):
                        decimal outDecimal = 0;
                        if (decimal.TryParse(sqlString, out outDecimal))
                        {
                            output = outDecimal;
                            return true;
                        }
                        else
                        {
                            Log.Logger.logError("Could not convert sql-result-string '" + sqlString 
                                + "' with " + get + " in SqlStringToData.");
                            break;
                        }
                    case (SQLiteGetTypeEnum.GetDouble):
                        double outDouble = 0;
                        if (double.TryParse(sqlString, out outDouble))
                        {
                            output = outDouble;
                            return true;
                        }
                        else
                        {
                            Log.Logger.logError("Could not convert sql-result-string '" + sqlString 
                                + "' with " + get + " in SqlStringToData.");
                            break;
                        }
                    case (SQLiteGetTypeEnum.GetFloat):
                        float outFloat = 0;
                        if (float.TryParse(sqlString, out outFloat))
                        {
                            output = outFloat;
                            return true;
                        }
                        else
                        {
                            Log.Logger.logError("Could not convert sql-result-string '" + sqlString 
                                + "' with " + get + " in SqlStringToData.");
                            break;
                        }
                    case (SQLiteGetTypeEnum.GetGuid):
                        Guid outGuid = Guid.Empty;
                        if (Guid.TryParse(sqlString, out outGuid))
                        {
                            output = outGuid;
                            return true;
                        }
                        else
                        {
                            Log.Logger.logError("Could not convert sql-result-string '" + sqlString 
                                + "' with " + get + " in SqlStringToData.");
                            break;
                        }
                    case (SQLiteGetTypeEnum.GetInt16):
                        Int16 outInt16 = 0;
                        if (Int16.TryParse(sqlString, out outInt16))
                        {
                            output = outInt16;
                            return true;
                        }
                        else
                        {
                            Log.Logger.logError("Could not convert sql-result-string '" + sqlString 
                                + "' with " + get + " in SqlStringToData.");
                            break;
                        }
                    case (SQLiteGetTypeEnum.GetInt32):
                        Int32 outInt32 = 0;
                        if (Int32.TryParse(sqlString, out outInt32))
                        {
                            output = outInt32;
                            return true;
                        }
                        else
                        {
                            Log.Logger.logError("Could not convert sql-result-string '" + sqlString 
                                + "' with " + get + " in SqlStringToData.");
                            break;
                        }
                    case (SQLiteGetTypeEnum.GetInt64):
                        Int64 outInt64 = 0;
                        if (Int64.TryParse(sqlString, out outInt64))
                        {
                            output = outInt64;
                            return true;
                        }
                        else
                        {
                            Log.Logger.logError("Could not convert sql-result-string '" + sqlString 
                                + "' with " + get + " in SqlStringToData.");
                            break;
                        }
                    case (SQLiteGetTypeEnum.GetString):
                        output = sqlString;
                        return true;
                    default:
                        Log.Logger.logError("Could not convert sql-result-string '" + sqlString 
                                + "' with " + get + " in SqlStringToData because this datatype is not supported.");
                        break;
                }
            }

            return false;
        }

    }
}
