// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;

namespace MonoDevelop.Xml.Editor.Completion;

class EmptyXmlCompletionProvider : IXmlSchemaCompletionProvider
{
	public XmlSchema? Schema => null;
	public string? FileName => null;
	public string? NamespaceUri => null;

	public Task<CompletionContext> GetAttributeCompletionDataAsync (IAsyncCompletionSource source, XmlElementPath path, CancellationToken token) => Task.FromResult (CompletionContext.Empty);
	public Task<CompletionContext> GetAttributeCompletionDataAsync (IAsyncCompletionSource source, string tagName, CancellationToken token) => Task.FromResult (CompletionContext.Empty);
	public Task<CompletionContext> GetAttributeValueCompletionDataAsync (IAsyncCompletionSource source, XmlElementPath path, string name, CancellationToken token) => Task.FromResult (CompletionContext.Empty);
	public Task<CompletionContext> GetAttributeValueCompletionDataAsync (IAsyncCompletionSource source, string tagName, string name, CancellationToken token) => Task.FromResult (CompletionContext.Empty);
	public Task<CompletionContext> GetChildElementCompletionDataAsync (IAsyncCompletionSource source, XmlElementPath path, CancellationToken token) => Task.FromResult (CompletionContext.Empty);
	public Task<CompletionContext> GetChildElementCompletionDataAsync (IAsyncCompletionSource source, string tagName, CancellationToken token) => Task.FromResult (CompletionContext.Empty);
	public Task<CompletionContext> GetElementCompletionDataAsync (IAsyncCompletionSource source, CancellationToken token) => Task.FromResult (CompletionContext.Empty);
	public Task<CompletionContext> GetElementCompletionDataAsync (IAsyncCompletionSource source, string namespacePrefix, CancellationToken token) => Task.FromResult (CompletionContext.Empty);
}