// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Editor;
using MonoDevelop.Xml.Editor.Parsing;

namespace MonoDevelop.MSBuild.Editor
{
	[Export (typeof (ITaggerProvider))]
	[ContentType (XmlContentTypeNames.XmlCore)]
	[TagType (typeof (IErrorTag))]
	[TextViewRole (PredefinedTextViewRoles.Analyzable)]

	class XmlSyntaxValidationTaggerProvider : ITaggerProvider
	{
		public JoinableTaskContext JoinableTaskContext { get; }
		public XmlParserProvider ParserProvider { get; }

		[ImportingConstructor]
		public XmlSyntaxValidationTaggerProvider (JoinableTaskContext joinableTaskContext, XmlParserProvider parserProvider)
		{
			JoinableTaskContext = joinableTaskContext;
			ParserProvider = parserProvider;
		}

		public ITagger<T> CreateTagger<T> (ITextBuffer buffer) where T : ITag
			=> (ITagger<T>)buffer.Properties.GetOrCreateSingletonProperty (() => new XmlSyntaxValidationTagger (buffer, this));
	}
}
