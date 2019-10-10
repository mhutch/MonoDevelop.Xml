// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Editor;

namespace MonoDevelop.MSBuild.Editor
{
	[Export (typeof (ITaggerProvider))]
	[ContentType (XmlContentTypeNames.XmlCore)]
	[TagType (typeof (IErrorTag))]
	[TextViewRole (PredefinedTextViewRoles.Analyzable)]

	class XmlSyntaxValidationTaggerProvider : ITaggerProvider
	{
		readonly JoinableTaskContext joinableTaskContext;

		[ImportingConstructor]
		public XmlSyntaxValidationTaggerProvider (JoinableTaskContext joinableTaskContext)
		{
			this.joinableTaskContext = joinableTaskContext;
		}

		public ITagger<T> CreateTagger<T> (ITextBuffer buffer) where T : ITag
			=> (ITagger<T>)buffer.Properties.GetOrCreateSingletonProperty (() => new XmlSyntaxValidationTagger (buffer, joinableTaskContext));
	}
}
