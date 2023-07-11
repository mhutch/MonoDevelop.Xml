// 
// State.cs
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
using System.Diagnostics.CodeAnalysis;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public abstract class XmlParserState
	{
		/// <summary>
		/// When the <see cref="Parser"/> advances by one character, it calls this method 
		/// on the currently active <see cref="XmlParserState"/> to determine the next state.
		/// </summary>
		/// <param name="c">The current character.</param>
		/// <param name = "context">The parser context.</param>
		/// <param name="replayCharacter">
		/// If <c>true</c> when leaving the method, the parser will replay the current character on the next state.
		/// </param>
		/// <param name="isEndOfFile">
		/// Whether the parser is at the end of the file.
		/// If this is <c>true</c>,then <paramref name="c"/> will be <c>\0</c>,
		/// and the state should end nodes with appropriate errors and as much recoverable information as possible.
		/// </param>
		/// <returns>
		/// The next state. A new or parent <see cref="XmlParserState"/> will change the parser state; 
		/// the current state or null will not.
		/// </returns>
		public abstract XmlParserState? PushChar (char c, XmlParserContext context, ref bool replayCharacter, bool isEndOfFile);

		public XmlParserState? Parent { get; private set;  }

		protected TChild Adopt<TChild> (TChild child) where TChild : XmlParserState
		{
			if (child.Parent != null) {
				throw new ArgumentException ("Child already has a Parent", nameof(child));
			}
			child.Parent = this;
			return child;
		}

		public XmlRootState RootState => this as XmlRootState ?? Parent?.RootState ?? throw new InvalidParserGraphException ("Root node must be instance of XmlRootState or a derived class.");

		public abstract XmlParserContext? TryRecreateState (ref XObject xobject, int position);

		public override string ToString ()
		{
			string? result = null;

			if (Parent is XmlParserState parent) {
				result = parent.ToString() + ".";
			}

			result += GetType().Name
				.Replace ("Xml", "")
				.Replace ("State", "");

			return result;
		}
	}


	/// <summary>
	/// Thrown when the XmlParserState node graph was not created correctly
	/// </summary>
	[Serializable]
	class InvalidParserGraphException : Exception
	{
		public InvalidParserGraphException (string message) : base (message) { }
		public InvalidParserGraphException (string message, Exception inner) : base (message, inner) { }
		protected InvalidParserGraphException (
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base (info, context) { }
	}

	/// <summary>
	/// Thrown when the XmlParser is in an invalid state
	/// </summary>
	[Serializable]
	class InvalidParserStateException : Exception
	{
		public InvalidParserStateException (string message) : base (message) { }
		public InvalidParserStateException (string message, Exception inner) : base (message, inner) { }
		protected InvalidParserStateException (
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base (info, context) { }

		[DoesNotReturn]
		internal static void ThrowExpected<T> (XmlParserContext context) where T : XObject
		{
			XObject actual = context.Nodes.Peek ();
			throw new InvalidParserStateException ($"Expected {typeof(T)} on stack, got {actual.GetType ()}");
		}
	}
}
