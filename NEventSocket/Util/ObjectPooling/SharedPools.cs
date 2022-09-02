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
    using System;
    using System.Collections.Generic;

    internal static class SharedPools
    {
        /// <summary>
        /// pool that uses default constructor with 100 elements pooled
        /// </summary>
        public static ObjectPool<T> BigDefault<T>() where T : class, new()
        {
            return DefaultBigPool<T>.Instance;
        }

        /// <summary>
        /// pool that uses default constructor with 20 elements pooled
        /// </summary>
        public static ObjectPool<T> Default<T>() where T : class, new()
        {
            return DefaultNormalPool<T>.Instance;
        }

        /// <summary>
        /// pool that uses string as key with StringComparer.OrdinalIgnoreCase as key comparer
        /// </summary>
        public static ObjectPool<Dictionary<string, T>> StringIgnoreCaseDictionary<T>()
        {
            return StringIgnoreCaseDictionaryNormalPool<T>.Instance;
        }

        /// <summary>
        /// pool that uses string as element with StringComparer.OrdinalIgnoreCase as element comparer
        /// </summary>
        public static readonly ObjectPool<HashSet<string>> StringIgnoreCaseHashSet =
            new ObjectPool<HashSet<string>>(() => new HashSet<string>(StringComparer.OrdinalIgnoreCase), 20);

        /// <summary>
        /// pool that uses string as element with StringComparer.Ordinal as element comparer
        /// </summary>
        public static readonly ObjectPool<HashSet<string>> StringHashSet =
            new ObjectPool<HashSet<string>>(() => new HashSet<string>(StringComparer.Ordinal), 20);

        /// <summary>
        /// Used to reduce the # of temporary byte[]s created to satisfy serialization and
        /// other I/O requests
        /// </summary>
        public static readonly ObjectPool<byte[]> ByteArray = new ObjectPool<byte[]>(() => new byte[ByteBufferSize], ByteBufferCount);

        /// pooled memory : 4K * 512 = 8MB
        public const int ByteBufferSize = 4 * 1024; //8k ~ = 1 event message
        private const int ByteBufferCount = 512;

        private static class DefaultBigPool<T> where T : class, new()
        {
            public static readonly ObjectPool<T> Instance = new ObjectPool<T>(() => new T(), 100);
        }

        private static class DefaultNormalPool<T> where T : class, new()
        {
            public static readonly ObjectPool<T> Instance = new ObjectPool<T>(() => new T(), 20);
        }

        private static class StringIgnoreCaseDictionaryNormalPool<T>
        {
            public static readonly ObjectPool<Dictionary<string, T>> Instance =
                new ObjectPool<Dictionary<string, T>>(() => new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase), 20);
        }
    }
}