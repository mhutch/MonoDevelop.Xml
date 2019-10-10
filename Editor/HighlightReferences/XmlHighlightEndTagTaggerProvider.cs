// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Xml.Editor;
using MonoDevelop.Xml.Editor.Tagging;

namespace MonoDevelop.MSBuild.Editor.HighlightReferences
{
	[Export(typeof(IViewTaggerProvider))]
	[ContentType (XmlContentTypeNames.XmlCore)]
	[TagType (typeof (NavigableHighlightTag))]
	[TextViewRole (PredefinedTextViewRoles.Interactive)]
	class XmlHighlightEndTagTaggerProvider : IViewTaggerProvider
	{
		[Import]
		public JoinableTaskContext JoinableTaskContext { get; set; }

		public ITagger<T> CreateTagger<T> (ITextView textView, ITextBuffer buffer) where T : ITag
			=>  (ITagger<T>) buffer.Properties.GetOrCreateSingletonProperty (
				() => new XmlHighlightEndTagTagger (textView, this)
			);
	}
}
