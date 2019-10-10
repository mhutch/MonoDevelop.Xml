// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.MiniEditor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;

using MonoDevelop.Xml.Tests.EditorTestHelpers;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Completion
{
	[TestFixture]
	public class CommitTests : CompletionTestBase
	{
		protected override string ContentTypeName => CompletionTestContentType.Name;
		protected override (EditorEnvironment, EditorCatalog) InitializeEnvironment ()
			=> XmlTestEnvironment.EnsureInitialized ();

		[Test]
		public Task SingleClosingTag ()
			=> TestCommands (
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

		[Test]
		public Task SingleClosingTagSameLine ()
			=> TestCommands (
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

		[Test]
		public Task SingleClosingTagExplicitInvocation ()
			=> TestCommands (
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

		[Test]
		public Task SingleClosingTagSameLineExplicitInvocation ()
			=> TestCommands (
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

		[Test]
		public Task MultipleClosingTags ()
			=> TestCommands (
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

		[Test]
		public Task  MultipleClosingTagsSameLine ()
			=> TestCommands (
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

		Task TestTypeCommands (string before, string after, string typeChars)
		{
			// between each separate action, TestCommands will wait for any active completion session to compute its items
			var actions = typeChars.Split ('^').Select (t => { Action<IEditorCommandHandlerService> a = (s) => s.Type (t); return a; });
			return TestCommands (before, after, actions, initialize: (ITextView tv) => {
				tv.Options.SetOptionValue ("BraceCompletion/Enabled", true);
			});
		}

		[Test]
		[TestCase ("<he\n", "<foo><Hello$")]
		[TestCase ("<he^>", "<foo><Hello>$</Hello>")]
		[TestCase ("<He^ ", "<foo><Hello $")]
		[TestCase ("<He^<", "<foo><He<$")]
		public Task CommitElement (string typeChars, string after) => TestTypeCommands ("<foo>$", after, typeChars);

		[Test]
		[TestCase (" T\n", "<Hello There=\"$\"")]
		[TestCase (" T\n\"", "<Hello There=\"\"$")]
		[TestCase (" T^=", "<Hello There=$")]
		[TestCase (" T^=\"", "<Hello There=\"$\"")]
		[TestCase (" Th^<", "<Hello Th<$")]
		[TestCase (" Th^ ", "<Hello There $")]
		public Task CommitAttribute (string typeChars, string after) => TestTypeCommands ("<Hello$", after, typeChars);
	}
}
