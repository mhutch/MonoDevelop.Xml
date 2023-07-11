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
using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{

	public abstract class XmlParser
	{
		protected XmlParserContext Context { get; }

		protected XmlParser (XmlRootState rootState)
		{
			RootState = rootState;

			Context = new XmlParserContext (
				currentState: rootState,
				previousState: rootState
			);

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
			Context.ResetKeywordBuilder ();
			Context.Diagnostics?.Clear ();
			Context.Nodes.Clear ();
			Context.Nodes.Push (RootState.CreateDocument ());
		}

		// ideally this would eventually be removed from the public API and be replaced by extension methods that do everything this does
		// without exposing the internal state directly
		internal XmlParserContext GetContext () => Context;

		const int REPLAY_LIMIT_PER_CHARACTER = 10;
		const int REPLAY_LIMIT_PER_NODE = 10;

		/// <summary>
		/// Parse a document by pushing characters one at a time.
		/// </summary>
		/// <param name="c">The character</param>
		public void Push (char c)
		{
			for (int loopLimit = 0; loopLimit < REPLAY_LIMIT_PER_CHARACTER; loopLimit++) {
				bool replayCharacter = false;
				if (Context.CurrentState == null)
					goto done;

				XmlParserState? nextState = Context.CurrentState.PushChar (c, Context, ref replayCharacter, isEndOfFile: false);

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

				Context.ResetKeywordBuilder ();

				// only loop if the same char should be run through the new state
				if (!replayCharacter) {
					goto done;
				}
			}
			throw new InvalidOperationException ($"Too many state changes for char '{c}'. Current state is {Context.CurrentState}.");

			done:
				Context.Position++;
				return;
		}

		/// <summary>
		/// Indicate to the parser that the text document has ended, so it can end all nodes on the stack.
		/// </summary>
		public (XDocument document, IReadOnlyList<XmlDiagnostic>? diagnostics) EndAllNodes ()
		{
			XObject[] nodes = Context.Nodes.ToArray ();

			Context.Position++;
			Context.IsAtEndOfFile = true;

			int loopMax = Context.Nodes.Count * REPLAY_LIMIT_PER_CHARACTER;
			for (int i = 0; i < loopMax && Context.Nodes.Count > 1; i++) {

				bool replayCharacter = false;
				if (Context.CurrentState.PushChar ('\0', Context, ref replayCharacter, isEndOfFile: true) is XmlParserState state) {
					Context.CurrentState = state;
				} else {
					break;
				}
			}

			if (Context.Nodes.Count != 1 || Context.Nodes.Pop () is not XDocument doc) {
				throw new InvalidParserStateException ("Malformed state stack when ending all nodes");
			}
			doc.End (Context.PositionBeforeCurrentChar);

			for (int i = nodes.Length - 1; i >= 0; i--) {
				var node = nodes[i];
				if (!node.IsEnded) {
					throw new InvalidParserStateException ($"Parser states did not end '{node}' node");
				}

				// if we are in spine mode, restore all nodes that were on the stack at EOF
				if (!Context.BuildTree) {
					Context.Nodes.Push (nodes[i]);
				}
			}

			return (doc, Context.Diagnostics);
		}

		public override string ToString () => Context.ToString ();
	}
}
