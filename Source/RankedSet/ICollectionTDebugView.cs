﻿//
// Library: KaosCollections
// File:    ICollectionTDebugView.cs
//
// Copyright © 2009-2018 Kasey Osborn (github.com/kaosborn)
// MIT License - Use and redistribute freely
//

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kaos.Collections
{
#if ! NET35 && ! NETSTANDARD1_0
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal class ICollectionDebugView<T>
    {
        private readonly ICollection<T> target;

        public ICollectionDebugView (ICollection<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException (nameof (collection));
            this.target = collection;
        }

        [DebuggerBrowsable (DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var items = new T[target.Count];
                target.CopyTo (items, 0);
                return items;
            }
        }
    }
}
