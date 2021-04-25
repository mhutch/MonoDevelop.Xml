using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.MiniEditor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Xml.Tests.Completion
{
	public class EditorCatalog
	{
		public EditorCatalog (EditorEnvironment env) => Host = env.GetEditorHost ();

		EditorEnvironment.Host Host { get; }

		public ITextViewFactoryService TextViewFactory
			=> Host.GetService<ITextViewFactoryService> ();

		public ITextDocumentFactoryService TextDocumentFactoryService
			=> Host.GetService<ITextDocumentFactoryService> ();

		public IFileToContentTypeService FileToContentTypeService
			=> Host.GetService<IFileToContentTypeService> ();

		public ITextBufferFactoryService BufferFactoryService
			=> Host.GetService<ITextBufferFactoryService> ();

		public IContentTypeRegistryService ContentTypeRegistryService
			=> Host.GetService<IContentTypeRegistryService> ();

		public IAsyncCompletionBroker AsyncCompletionBroker
			=> Host.GetService<IAsyncCompletionBroker> ();

		public IAsyncQuickInfoBroker AsyncQuickInfoBroker
			=> Host.GetService<IAsyncQuickInfoBroker> ();

		public IClassifierAggregatorService ClassifierAggregatorService
			=> Host.GetService<IClassifierAggregatorService> ();

		public IClassificationTypeRegistryService ClassificationTypeRegistryService
			=> Host.GetService<IClassificationTypeRegistryService> ();

		public IEditorOperationsFactoryService EditorOperationsFactoryService
			=> Host.GetService<IEditorOperationsFactoryService> ();

		public IMultiSelectionBrokerFactory MultiSelectionBrokerFactory
			=> Host.GetService<IMultiSelectionBrokerFactory> ();

		public IBufferTagAggregatorFactoryService BufferTagAggregatorFactoryService
			=> Host.GetService<IBufferTagAggregatorFactoryService> ();

		public JoinableTaskContext JoinableTaskContext
			=> Host.GetService<JoinableTaskContext> ();

		public IEditorOperationsFactoryService OperationsFactory
			=> Host.GetService<IEditorOperationsFactoryService> ();

		public IEditorCommandHandlerServiceFactory CommandServiceFactory
			=> Host.GetService<IEditorCommandHandlerServiceFactory> ();

		public ITextStructureNavigatorSelectorService TextStructureNavigatorSelectorService
			=> Host.GetService<ITextStructureNavigatorSelectorService> ();
	}
}
