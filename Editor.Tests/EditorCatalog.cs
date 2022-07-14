using System.Collections.Generic;
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

namespace MonoDevelop.Xml.Editor.Tests
{
	public class EditorCatalog
	{
		readonly EditorEnvironment.Host host;

		public EditorCatalog (EditorEnvironment.Host host) => this.host = host;

		/// <summary>
		/// Gets a service of specified type from the composition graph
		/// </summary>
		/// <typeparam name="T">Service type</typeparam>
		/// <returns>Object of requested type</returns>
		public T GetService<T> () where T : class => host.GetService<T> ();

		/// <summary>
		/// Gets services of specified type from the composition graph
		/// </summary>
		/// <typeparam name="T">Service type</typeparam>
		/// <returns>Enumeration of objects of requested type</returns>
		public IEnumerable<T> GetServices<T> () where T : class => host.GetServices<T> ();

		public ITextViewFactoryService TextViewFactory => GetService<ITextViewFactoryService> ();
		public ITextDocumentFactoryService TextDocumentFactoryService => GetService<ITextDocumentFactoryService> ();
		public IFileToContentTypeService FileToContentTypeService => GetService<IFileToContentTypeService> ();
		public ITextBufferFactoryService BufferFactoryService => GetService<ITextBufferFactoryService> ();
		public IContentTypeRegistryService ContentTypeRegistryService => GetService<IContentTypeRegistryService> ();
		public IAsyncCompletionBroker AsyncCompletionBroker => GetService<IAsyncCompletionBroker> ();
		public IAsyncQuickInfoBroker AsyncQuickInfoBroker => GetService<IAsyncQuickInfoBroker> ();
		public IClassifierAggregatorService ClassifierAggregatorService => GetService<IClassifierAggregatorService> ();
		public IClassificationTypeRegistryService ClassificationTypeRegistryService => GetService<IClassificationTypeRegistryService> ();
		public IBufferTagAggregatorFactoryService BufferTagAggregatorFactoryService => GetService<IBufferTagAggregatorFactoryService> ();
		public JoinableTaskContext JoinableTaskContext => GetService<JoinableTaskContext> ();
		public IEditorOperationsFactoryService OperationsFactory => GetService<IEditorOperationsFactoryService> ();
		public IEditorCommandHandlerServiceFactory CommandServiceFactory => GetService<IEditorCommandHandlerServiceFactory> ();
		public ITextStructureNavigatorSelectorService TextStructureNavigatorSelectorService => GetService<ITextStructureNavigatorSelectorService> ();
	}
}
