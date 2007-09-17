using System;
using System.Collections.Generic;
using System.Text;

namespace OrmCodeGenLib
{
    public enum AccessLevel
    {
        Private,
        Family,
        Assembly,
        Public
    }

    public enum EntityBehaviuor
    {
        /// <summary>
        /// Default behaviour when generator creates default classes(entity and schema) with full method set.
        /// </summary>
        Default = 0,
        /// <summary>
        /// 'Partial object' behaviour when generator creates classes(entity and schema) without user depended behaviour for future extension.
        /// </summary>
        PartialObjects,
        /// <summary>
        /// Force 'partial' modifier with default behaviour.
        /// </summary>
        ForcePartial,
        ///// <summary>
        ///// Set abstract modifier.
        ///// </summary>
        //Abstract
    }
}
