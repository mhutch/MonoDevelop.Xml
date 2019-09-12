// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.VisualStudio.MiniEditor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using MonoDevelop.Xml.Tests.EditorTestHelpers;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Completion
{
	[TestFixture]
	public class CommitTests : CompletionTestBase
	{
		protected override string ContentTypeName => CompletionTestContentType.Name;
		protected override (EditorEnvironment, EditorCatalog) InitializeEnvironment ()
			=> TestEnvironment.EnsureInitialized ();

		[Test]
		public void SingleClosingTag ()
		{
			TestCommands (
@"<foo>
    <bar>
    $
",
@"<foo>
    <bar>
    </bar>$
",
					(s) => {
						s.Type ("</b");
						s.Enter ();
					}
				);
		}

		[Test]
		public void SingleClosingTagSameLine ()
		{
			TestCommands (
@"<foo>
    <bar>$
",
@"<foo>
    <bar></bar>$
",
					(s) => {
						s.Type ("</b");
						s.Enter ();
					}
				);
		}

		[Test]
		public void SingleClosingTagExplicitInvocation ()
		{
			TestCommands (
@"<foo>
    <bar>
    $
",
@"<foo>
    <bar>
    </bar>$
",
					(s) => {
						s.InvokeCompletion ();
						s.Type ("</b");
						s.Enter ();
					}
				);
		}

		[Test]
		public void SingleClosingTagSameLineExplicitInvocation ()
		{
			TestCommands (
@"<foo>
    <bar>$
",
@"<foo>
    <bar></bar>$
",
					(s) => {
						s.InvokeCompletion ();
						s.Type ("</b");
						s.Enter ();
					}
				);
		}

		[Test]
		public void MultipleClosingTags ()
		{
			TestCommands (
@"<foo>
    <bar>
        <baz>
        $
",
@"<foo>
    <bar>
        <baz>
        </baz>
    </bar>
</foo>$
",
					(s) => {
						s.Type ("</f");
						s.Enter ();
					}
				);
		}

		[Test]
		public void MultipleClosingTagsSameLine ()
		{
			TestCommands (
@"<foo>
    <bar>
        <baz>hello$
",
@"<foo>
    <bar>
        <baz>hello</baz>
    </bar>
</foo>$
",
					(s) => {
						s.Type ("</f");
						s.Enter ();
					}
				);
		}

		void TestTypeCommands (string before, string after, string typeChars)
		{
			// between each separate action, TestCommands will wait for any active completion session to compute its items
			var actions = typeChars.Split ('^').Select (t => { Action<IEditorCommandHandlerService> a = (s) => s.Type (t); return a; });
			TestCommands (before, after, actions);
		}

		[Test]
		[TestCase ("<he\n", "<foo><Hello$")]
		[TestCase ("<he^>", "<foo><Hello>$</Hello>")]
		[TestCase ("<He^ ", "<foo><Hello $")]
		[TestCase ("<He^<", "<foo><He<$")]
		public void CommitElement (string typeChars, string after) => TestTypeCommands ("<foo>$", after, typeChars);

		[Test]
		[TestCase (" T\n", "<Hello There=\"$\"")]
		[TestCase (" T^=", "<Hello There=$")]
		[TestCase (" Th^<", "<Hello Th<$")]
		[TestCase (" Th^ ", "<Hello Th $")]
		public void CommitAttribute (string typeChars, string after) => TestTypeCommands ("<Hello$", after, typeChars);
	}
}
