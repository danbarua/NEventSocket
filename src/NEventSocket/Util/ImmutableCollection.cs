// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImmutableCollection.cs" company="Business Systems (UK) Ltd">
//   Copyright © Business Systems (UK) Ltd and contributors. All rights reserved.
// </copyright>
// <summary>
//   Defines the ImmutableCollection type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Util
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    internal class ImmutableCollection<T> : ICollection<T>, ICollection
    {
        private readonly ICollection<T> inner;
        private readonly object syncObj = new object();

        public ImmutableCollection(ICollection<T> inner)
        {
            this.inner = inner;
        }

        public virtual object SyncRoot
        {
            get { return syncObj; }
        }

        public virtual bool IsSynchronized
        {
            get { return false; }
        }

        public virtual void CopyTo(Array array, int index)
        {
            CopyTo(array.Cast<T>().ToArray(), index);
        }

        public virtual int Count
        {
            get { return this.inner.Count; }
        }

        public virtual bool IsReadOnly
        {
            get { return true; }
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return this.inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual void Add(T item)
        {
            throw new NotSupportedException("ImmutableCollection is readonly.");
        }

        public virtual bool Remove(T item)
        {
            throw new NotSupportedException("ImmutableCollection is readonly.");
        }

        public virtual void Clear()
        {
            throw new NotSupportedException("ImmutableCollection is readonly.");
        }

        public virtual bool Contains(T item)
        {
            return this.inner.Contains(item);
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            this.inner.CopyTo(array, arrayIndex);
        }
    }
}
