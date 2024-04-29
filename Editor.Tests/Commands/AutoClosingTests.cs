// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.VisualStudio.Text.Editor;

using MonoDevelop.Xml.Editor.Options;
using MonoDevelop.Xml.Editor.Tests.Extensions;

using NUnit.Framework;

namespace MonoDevelop.Xml.Editor.Tests.Commands
{
	[TestFixture]
	public class AutoClosingTests : XmlEditorTest
	{

		[TestCase (
			"<r>\r\n <a e='v'|\r\n</r>",
			"<r>\r\n <a e='v'>|</a>\r\n</r>"
		)]
		[TestCase (
			"<r>\n <a e='v' /|\n</r>",
			"<r>\n <a e='v' />|\n</r>"
		)]
		public Task TestComment (string sourceText, string expectedText, string typeChars = ">")
		{
			return this.TestCommands (
				sourceText,
				expectedText,
				(s) => s.Type (typeChars),
				caretMarkerChar: '|',
				initialize: (ITextView tv) => {
					tv.Options.SetOptionValue (XmlOptions.AutoInsertClosingTag, true);
					return GetParser (tv.TextBuffer).GetOrProcessAsync (tv.TextBuffer.CurrentSnapshot, System.Threading.CancellationToken.None);
				});
		}
	}
}