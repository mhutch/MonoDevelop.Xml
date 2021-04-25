using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace MonoDevelop.Xml.Editor.Classification
{
	/// <summary>
	/// An extension for the XML classifier that can enable or disable the classifier
	/// for a given buffer, as well as replace classification spans.
	/// </summary>
	public interface IXmlClassifierExtension
	{
		/// <summary>
		/// Called when the XML classifier provider is asked to provide a classifier for a new buffer.
		/// Return false to avoid classifying the buffer, true to allow classification.
		/// </summary>
		/// <param name="buffer">A text buffer being created.</param>
		/// <returns>True if the buffer should get an XML classifier.</returns>
		bool ShouldClassify (ITextBuffer buffer);

		/// <summary>
		/// Called back for each span classified by the XML classifier.
		/// An extension has an opportunity to replace the default classification with zero or more
		/// classifications.
		/// </summary>
		/// <param name="classificationSpan">The default classification provided by the XML classifier.</param>
		/// <param name="sink">A callback to consume custom classification spans provided by this extension.
		/// When the method returns true, the default classification is not used. If the sink is not called,
		/// the classification is effectively removed. When it is called once, the original is being replaced
		/// by a single classification. Call it multiple times to replace with multiple classifications.
		/// </param>
		/// <returns>False if no replacements were made and the classifier should use the default classification.</returns>
		bool TryReplace (ClassificationSpan classificationSpan, Action<ClassificationSpan> sink);
	}
}