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
using System.Diagnostics;
using System.Text;

namespace MonoDevelop.Xml.Parser
{

	public abstract class XmlParser
	{
		protected XmlParserContext Context { get; }

		protected XmlParser (XmlRootState rootState)
		{
			RootState = rootState;

			Context = new XmlParserContext {
				CurrentStateLength = 0,
				CurrentState = rootState,
				PreviousState = rootState,
				KeywordBuilder = new StringBuilder (),
				Nodes = new NodeStack ()
			};

			Context.Nodes.Push (RootState.CreateDocument ());
		}

		protected XmlParser (XmlParserContext context, XmlRootState rootState)
		{
			Context = context;
			RootState = rootState;
		}

		public XmlRootState RootState { get; }

		public void Reset ()
		{
			Context.CurrentState = RootState;
			Context.PreviousState = RootState;
			Context.Position = 0;
			Context.StateTag = 0;
			Context.CurrentStateLength = 0;
			Context.KeywordBuilder.Length = 0;
			Context.Diagnostics?.Clear ();
			Context.Nodes.Clear ();
			Context.Nodes.Push (RootState.CreateDocument ());
		}

		// ideally this would eventually be removed from the public API and be replaced by extension methods that do everything this does
		// without exposing the internal state directly
		internal XmlParserContext GetContext () => Context;

		/// <summary>
		/// Parse a document by pushing characters one at a time.
		/// </summary>
		/// <param name="c">The character</param>
		public void Push (char c)
		{
			for (int loopLimit = 0; loopLimit < 10; loopLimit++) {
				string rollback = null;
				if (Context.CurrentState == null)
					goto done;
				XmlParserState nextState = Context.CurrentState.PushChar (c, Context, ref rollback);

				// no state change
				if (nextState == Context.CurrentState || nextState == null) {
					Context.CurrentStateLength++;
					goto done;
				}

				// state changed; reset stuff
				Context.PreviousState = Context.CurrentState;
				Context.CurrentState = nextState;
				Context.StateTag = 0;
				Context.CurrentStateLength = 0;
				if (Context.KeywordBuilder.Length < 50)
					Context.KeywordBuilder.Length = 0;
				else
					Context.KeywordBuilder = new StringBuilder ();


				// only loop if the same char should be run through the new state
				if (rollback == null)
					goto done;

				//simple rollback, just run same char through again
				if (rollback.Length == 0)
					continue;

				//"complex" rollbacks require actually skipping backwards.
				//Note the previous state is invalid for this operation.

				foreach (char rollChar in rollback)
					Push (rollChar);
			}
			throw new InvalidOperationException ($"Too many state changes for char '{c}'. Current state is {Context.CurrentState.ToString ()}.");

			done:
				Context.Position++;
				return;
		}

		public override string ToString () => Context.ToString ();
	}
}
