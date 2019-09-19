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
using System.IO;
using System.Text;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlParser : ICloneable, IForwardParser
	{
		readonly XmlParserContext context;

		public XmlParser (XmlRootState rootState, bool buildTree)
		{
			RootState = rootState;

			context = new XmlParserContext {
				CurrentStateLength = 0,
				CurrentState = rootState,
				PreviousState = rootState,
				BuildTree = buildTree,
				KeywordBuilder = new StringBuilder (),
				Nodes = new NodeStack ()
			};

			if (buildTree) {
				context.Diagnostics = new List<XmlDiagnosticInfo> ();
			}

			context.Nodes.Push (RootState.CreateDocument ());
		}

		public XmlParser (XmlParserContext context, XmlRootState rootState)
		{
			if (context.BuildTree) {
				throw new ArgumentException ("When re-using existing context, it cannot be in tree mode");
			}
			this.context = context;
			RootState = rootState;
		}

		XmlParser (XmlParser copyFrom)
			: this (new XmlParserContext {
				Position = copyFrom.context.Position,
				CurrentState = copyFrom.context.CurrentState,
				CurrentStateLength = copyFrom.context.CurrentStateLength,
				StateTag = copyFrom.context.StateTag,
				PreviousState = copyFrom.context.PreviousState,
				BuildTree = false,
				KeywordBuilder = new StringBuilder (copyFrom.context.KeywordBuilder.ToString ()),
				Nodes = copyFrom.context.Nodes.ShallowCopy ()
			},
			copyFrom.RootState
		)
		{ }

		public XmlRootState RootState { get; }

		public void Reset ()
		{
			context.CurrentState = RootState;
			context.PreviousState = RootState;
			context.Position = 0;
			context.StateTag = 0;
			context.CurrentStateLength = 0;
			context.KeywordBuilder.Length = 0;
			context.Diagnostics?.Clear ();
			context.Nodes.Clear ();
			context.Nodes.Push (RootState.CreateDocument ());
		}

		public void Parse (TextReader reader)
		{
			int i = reader.Read ();
			while (i >= 0) {
				char c = (char)i;
				Push (c);
				i = reader.Read ();
			}
		}

		public void Push (char c)
		{
			for (int loopLimit = 0; loopLimit < 10; loopLimit++) {
				string rollback = null;
				if (CurrentState == null)
					goto done;
				XmlParserState nextState = CurrentState.PushChar (c, context, ref rollback);

				// no state change
				if (nextState == CurrentState || nextState == null) {
					context.CurrentStateLength++;
					goto done;
				}

				// state changed; reset stuff
				context.PreviousState = CurrentState;
				context.CurrentState = nextState;
				context.StateTag = 0;
				context.CurrentStateLength = 0;
				if (context.KeywordBuilder.Length < 50)
					context.KeywordBuilder.Length = 0;
				else
					context.KeywordBuilder = new StringBuilder ();


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
			throw new InvalidOperationException ($"Too many state changes for char '{c}'. Current state is {CurrentState.ToString ()}.");

			done:
				context.Position++;
				return;
		}

		object ICloneable.Clone ()
		{
			if (context.BuildTree)
				throw new InvalidOperationException ("Parser can only be cloned when in stack mode");
			return new XmlParser (this);
		}

		public XmlParser GetTreeParser ()
		{
			if (context.BuildTree)
				throw new InvalidOperationException ("Parser can only be cloned when in stack mode");

			var parser = new XmlParser (this);
			parser.context.BuildTree = true;
			parser.context.ConnectNodes ();

			return parser;
		}

		public override string ToString () => context.ToString ();


		public XmlParserState CurrentState => context.CurrentState;
		public int CurrentStateLength => context.CurrentStateLength;
		public NodeStack Nodes => context.Nodes;
		public int Position => context.Position;

		public List<XmlDiagnosticInfo> Diagnostics => context.Diagnostics;

		//FIXME: remove this once callers have better APIs to use
		public IXmlParserContext GetContext () => context;

		/// <summary>
		/// Efficiently creates a spine parser using information from an existing document. The position of
		/// the parser is not guaranteed but will not exceed <paramref name="maximumPosition" />.
		/// </summary>
		/// <returns></returns>
		public XmlParser GetSpineParser (XDocument xdocument, int maximumPosition)
			=> xdocument.FindNodeAtOrBeforeOffset (maximumPosition) is XObject obj
				&& RootState.TryRecreateState (obj, maximumPosition) is XmlParserContext ctx
			? new XmlParser (ctx, RootState)
			: null;
	}
}
