// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.MiniEditor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
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
	}
}