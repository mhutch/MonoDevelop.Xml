// 
// Parser.cs
// 
// Author:
//   Mikayla Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class NodeStack : Stack<XObject>
	{
		public NodeStack (IEnumerable<XObject> collection) : base (collection) { }
		public NodeStack (params XObject[] collection) : base (collection) { }

		public NodeStack () { }

		public NodeStack (int capacity) : base (capacity) { }

		public XObject Peek (int down)
		{
			int i = 0;
			foreach (XObject o in this) {
				if (i == down)
					return o;
				i++;
			}
			throw new InvalidOperationException ($"Cannot peek {down} deep in stack with depth {Count}");
		}

		public bool TryPeek (int down, [NotNullWhen (true)] out XObject? value)
		{
			int i = 0;
			foreach (XObject o in this) {
				if (i == down) {
					value = o;
					return true;
				}
				i++;
			}
			value = null;
			return false;
		}

		public XObject? TryPeek (int down) => TryPeek (down, out XObject? val) ? val : null;

		public bool TryPeek<T> ([NotNullWhen (true)] out T? value) where T : class
		{
			if (Count > 0 && Peek () is T instance) {
				value = instance;
				return true;
			}
			value = null;
			return false;
		}

		public T? TryPeek<T> () where T : class => TryPeek (out T? val) ? val : null;

		public bool TryPeek<T> (int down, [NotNullWhen (true)] out T? value) where T : class
		{
			if (TryPeek (down, out XObject? actual) && actual is T instance) {
				value = instance;
				return true;
			}
			value = null;
			return false;
		}

		public T? TryPeek<T> (int down) where T : class => TryPeek (down, out T? val) ? val : null;

		public XDocument? GetRoot ()
		{
			XObject? last = null;
			foreach (XObject o in this)
				last = o;
			return last as XDocument;
		}

		internal NodeStack ShallowCopy ()
		{
			IEnumerable<XObject> CopyXObjects ()
			{
				foreach (XObject o in this)
					yield return o.ShallowCopy ();
			}

			var copies = new List<XObject> (CopyXObjects ());
			copies.Reverse ();
			return new NodeStack (copies);
		}

		internal static NodeStack FromParents (XObject fromObject)
		{
			var newStack = new NodeStack ();

			DepthFirstAddParentsToStack (fromObject);

			void DepthFirstAddParentsToStack (XObject o)
			{
				if (o.Parent is XObject parent) {
					DepthFirstAddParentsToStack (parent);
					newStack.Push (parent.ShallowCopy ());
				}
			}

			return newStack;
		}

		/// <summary>
		/// Search down the stack for a node of type <typeparamref name="TNode"/>.
		/// </summary>
		/// <typeparam name="TNode">Type of the node</typeparam>
		/// <param name="maxDepth">Zero-indexed limit for the search depth.
		/// If negative, the search has no depth limit.</param>
		/// <returns>A node of type <typeparamref name="TNode"/>, or <c>null</c> if none was found</returns>

		/// Search down the stack for a node of type {TNode}.
		/// TNode: Type of the node
		/// maxDepth: Zero-indexed limit for the search depth. If negative, the search has no depth limit.
		/// returns: A node of type {TNode} or `null` if none was found
		public TNode? TryFind<TNode> (int maxDepth = -1) where TNode : class
		{
			if (maxDepth < 0) {
				maxDepth = int.MaxValue;
			}

			foreach (XObject o in this) {
				if (o is TNode val) {
					return val;
				}
				if (maxDepth-- < 0) {
					break;
				}
			}
			return null;
		}
	}
}
