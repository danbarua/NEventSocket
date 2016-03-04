// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "*", Justification = "Third Party Library Code")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "*", Justification = "Third Party Library Code")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.LayoutRules", "*", Justification = "Third Party Library Code")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "*", Justification = "Third Party Library Code")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "*", Justification = "Third Party Library Code")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "*", Justification = "Third Party Library Code")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "*", Justification = "Third Party Library Code")]
namespace NEventSocket.Util.ObjectPooling
{
    using System.Text;

    /// <summary>
    /// Lifted from Roslyn, http://mattwarren.org/2014/06/10/roslyn-code-base-performance-lessons-part-2/
    /// </summary>
    internal static class StringBuilderPool
    {
        public static StringBuilder Allocate()
        {
            return SharedPools.BigDefault<StringBuilder>().AllocateAndClear();
        }

        public static void Free(StringBuilder builder)
        {
            SharedPools.BigDefault<StringBuilder>().ClearAndFree(builder);
        }

        public static string ReturnAndFree(StringBuilder builder)
        {
            return SharedPools.BigDefault<StringBuilder>().ReturnAndFree(builder);
        }
    }
}