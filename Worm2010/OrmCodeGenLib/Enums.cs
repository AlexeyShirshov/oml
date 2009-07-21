using System;
using System.Collections.Generic;
using System.Text;

namespace Worm.CodeGen.Core
{
    public enum AccessLevel
    {
        Private,
        Family,
        Assembly,
        Public,
		FamilyOrAssembly
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
        PartialObjects = 1,
        /// <summary>
        /// Force 'partial' modifier with default behaviour.
        /// </summary>
        ForcePartial = 2,
        ///// <summary>
        ///// Set abstract modifier.
        ///// </summary>
        //Abstract
    }
}
