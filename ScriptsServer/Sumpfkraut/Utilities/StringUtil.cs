﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GUC.Server.Scripts.Sumpfkraut.Utilities
{
    public class StringUtil : GUC.Utilities.ExtendedObject
    {

        new public static readonly string _staticName = "StringUtil (static)"; 

        public static string Concatenate<T>(IEnumerable<T> source, string delimiter)
        {
            var s = new StringBuilder();
            bool first = true;
            foreach(T t in source) 
            {
                if (first) 
                {
                    first = false;
                } 
                else 
                {
                    s.Append(delimiter);
                }
                s.Append(t);
            }    
            return s.ToString();
        }

    }
}
