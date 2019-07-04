// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Roslyn.Utilities
{
	internal static partial class SpecializedCollections
	{
		public static IEnumerator<T> EmptyEnumerator<T> ()
		{
			return Empty.Enumerator<T>.Instance;
		}

		public static IEnumerable<T> EmptyEnumerable<T> ()
		{
			return Empty.List<T>.Instance;
		}

		public static ICollection<T> EmptyCollection<T> ()
		{
			return Empty.List<T>.Instance;
		}

		public static IList<T> EmptyList<T> ()
		{
			return Empty.List<T>.Instance;
		}

		public static IReadOnlyList<T> EmptyReadOnlyList<T> ()
		{
			return Empty.List<T>.Instance;
		}
	}
}