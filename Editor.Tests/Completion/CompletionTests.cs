// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using MonoDevelop.Xml.Editor.Tests.Extensions;
using NUnit.Framework;

namespace MonoDevelop.Xml.Editor.Tests.Completion
{
	[TestFixture]
	public class CompletionTests : XmlEditorTest
	{
		[Test]
		public async Task TestElementStartCompletion ()
		{
			var result = await this.GetCompletionContext ("<$");
			result.AssertNonEmpty ();
			result.AssertContains ("<Hello");
			result.AssertContains ("<!--");
		}

		[Test]
		public async Task TestRootCompletion ()
		{
			var result = await this.GetCompletionContext ("$");
			result.AssertNonEmpty ();
			result.AssertContains ("<Hello");
			result.AssertContains ("<!--");
		}

		[Test]
		public async Task TestElementNameCompletionInvocation ()
		{
			var result = await this.GetCompletionContext ("<foo$");
			result.AssertNonEmpty ();
			result.AssertContains ("Hello");
			result.AssertContains ("!--");
		}
	}
}
