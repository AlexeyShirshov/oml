#region Using directives

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;

#endregion

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: AssemblyTitle(@"")]
[assembly: AssemblyDescription(@"")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(@"Worm")]
[assembly: AssemblyProduct(@"Designer")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]

//
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:

[assembly: AssemblyVersion(@"1.0.0.0")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: ReliabilityContract(Consistency.MayCorruptProcess, Cer.None)]

//
// Make the Dsl project internally visible to the DslPackage assembly
//
[assembly: InternalsVisibleTo(@"Worm.Designer.DslPackage, PublicKey=002400000480000094000000060200000024000052534131000400000100010037CE4A88CFBB0E91C32FE4251C134A798A867568DCBAD92EC0A5FB8F213100742E3A431DC7119417ACC0DFA724B42F5B7F0D76F5D1EF220E103EC3D818C8CDFCCFB57D4F7018C72DC31C1874FB8BF61B52ECC4D55A7795B6FE2B765234A8FD0AA5448F060BE9DEAC6497B85DE159F3A789F9FE035D68C0E4170F338BDC2EB5D4")]