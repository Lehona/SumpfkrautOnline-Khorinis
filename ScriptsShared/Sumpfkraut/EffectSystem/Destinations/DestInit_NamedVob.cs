﻿using GUC.Scripts.Sumpfkraut.EffectSystem.Changes;
using GUC.Scripts.Sumpfkraut.EffectSystem.EffectHandlers;
using GUC.Scripts.Sumpfkraut.EffectSystem.Enumeration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GUC.Scripts.Sumpfkraut.EffectSystem.Destinations
{

    /// <summary>
    /// Initialize basic behavior of named vobs when managed by the EffectSystem.
    /// </summary>
    public partial class DestInit_NamedVob : BaseDestInit
    {

        /// <summary>
        /// Singleton which serves as cache for quasi-static data.
        /// </summary>
        new public static DestInit_NamedVob representative;

        public static readonly string Default_Name = "";



        // make sure, the destination makes itself known to its related changes
        static DestInit_NamedVob ()
        {
            representative = new DestInit_NamedVob();
        }

        protected DestInit_NamedVob ()
        {
            AddOrChange(new DestInitInfo(ChangeDestination.NamedVob_Name, 
                new List<ChangeType>() { ChangeType.NamedVob_Name_Set }, 
                CTC_Name, ATC_Name));
        }


        
        partial void pCTC_Name (BaseEffectHandler eh, TotalChange tc);
        public void CTC_Name (BaseEffectHandler eh, TotalChange tc) { pCTC_Name(eh, tc); }
        partial void pATC_Name (BaseEffectHandler eh, TotalChange tc);
        public void ATC_Name (BaseEffectHandler eh, TotalChange tc) { pATC_Name(eh, tc); }

    }

}
