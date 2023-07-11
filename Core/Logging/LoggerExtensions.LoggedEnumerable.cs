// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Logging;

public static partial class LoggerExtensions
{
	class LoggedEnumerable<T> : IEnumerable<T>
	{
		readonly IEnumerable<T> enumerable;
		readonly ILogger logger;
		readonly string? originMember;

		public LoggedEnumerable (IEnumerable<T> enumerable, ILogger logger, string originMember)
		{
			this.enumerable = enumerable;
			this.logger = logger;
			this.originMember = originMember;
		}

		void Log (Exception ex) => logger.LogInternalException (ex, originMember);

		public IEnumerator<T> GetEnumerator ()
		{
			try {
				var enumerator = enumerable.GetEnumerator ();
				return new LoggedEnumerator (this, enumerator);
			} catch (Exception ex) {
				Log (ex);
				throw;
			}
		}

		IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

		class LoggedEnumerator : IEnumerator<T>
		{
			readonly LoggedEnumerable<T> parent;
			readonly IEnumerator<T> enumerator;

			public LoggedEnumerator (LoggedEnumerable<T> parent, IEnumerator<T> enumerator)
			{
				this.parent = parent;
				this.enumerator = enumerator;
			}

			public T Current {
				get {
					try {
						return enumerator.Current;
					} catch (Exception ex) {
						parent.Log (ex);
						throw;
					}
				}
			}

			object? IEnumerator.Current => Current;

			public void Dispose ()
			{
				try {
					enumerator.Dispose ();
				} catch (Exception ex) {
					parent.Log (ex);
					throw;
				}
			}

			public bool MoveNext ()
			{
				try {
					return enumerator.MoveNext ();
				} catch (Exception ex) {
					parent.Log (ex);
					return false;
				}
			}

			public void Reset ()
			{
				try {
					enumerator.Reset ();
				} catch (Exception ex) {
					parent.Log (ex);
				}
			}
		}
	}
}