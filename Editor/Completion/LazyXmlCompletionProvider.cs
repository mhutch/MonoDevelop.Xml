// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;

namespace MonoDevelop.Xml.Editor.Completion;

/// <summary>
/// Simplifies implementation of lazily loaded completion providers
/// by wrapping the "real" provider and creating it asynchronously when needed.
/// </summary>
abstract class LazyXmlCompletionProvider : IXmlCompletionProvider
{
	readonly Lazy<Task<IXmlCompletionProvider>> providerLoader;
	IXmlCompletionProvider? provider;

	protected LazyXmlCompletionProvider ()
	{
		#pragma warning disable VSTHRD011 // Use AsyncLazy<T>
		providerLoader = new (() => {
			var loaderTask = CreateProviderAsync (CancellationToken.None);
			// capture the result into the provider field so future calls can be optimized a bit
			_ = loaderTask.ContinueWith (t => provider = t.Result, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
			return loaderTask;
		});
		#pragma warning restore VSTHRD011
	}

	protected abstract Task<IXmlCompletionProvider> CreateProviderAsync (CancellationToken token);

	Task<CompletionContext> DispatchToInnerProvider (Func<IXmlCompletionProvider, Task<CompletionContext>> action, CancellationToken token)
	{
		if (provider != null) {
			return action (provider);
		}
		return providerLoader.Value.ContinueWith (
			#pragma warning disable VSTHRD103 // Call async methods when in an async method
			t => action (t.Result),
			#pragma warning restore VSTHRD103
			token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap ();
	}

	public Task<CompletionContext> GetAttributeCompletionDataAsync (IAsyncCompletionSource source, XmlElementPath path, CancellationToken token)
		=> DispatchToInnerProvider (p => p.GetAttributeCompletionDataAsync (source, path, token), token);

	public Task<CompletionContext> GetAttributeCompletionDataAsync (IAsyncCompletionSource source, string tagName, CancellationToken token)
		=> DispatchToInnerProvider (p => p.GetAttributeCompletionDataAsync (source, tagName, token), token);

	public Task<CompletionContext> GetAttributeValueCompletionDataAsync (IAsyncCompletionSource source, XmlElementPath path, string name, CancellationToken token)
		=> DispatchToInnerProvider (p => p.GetAttributeValueCompletionDataAsync (source, path, name, token), token);

	public Task<CompletionContext> GetAttributeValueCompletionDataAsync (IAsyncCompletionSource source, string tagName, string name, CancellationToken token)
		=> DispatchToInnerProvider (p => p.GetAttributeValueCompletionDataAsync (source, tagName, name, token), token);

	public Task<CompletionContext> GetChildElementCompletionDataAsync (IAsyncCompletionSource source, XmlElementPath path, CancellationToken token)
		=> DispatchToInnerProvider (p => p.GetChildElementCompletionDataAsync (source, path, token), token);

	public Task<CompletionContext> GetChildElementCompletionDataAsync (IAsyncCompletionSource source, string tagName, CancellationToken token)
		=> DispatchToInnerProvider (p => p.GetChildElementCompletionDataAsync (source, tagName, token), token);

	public Task<CompletionContext> GetElementCompletionDataAsync (IAsyncCompletionSource source, CancellationToken token)
		=> DispatchToInnerProvider (p => p.GetElementCompletionDataAsync (source, token), token);

	public Task<CompletionContext> GetElementCompletionDataAsync (IAsyncCompletionSource source, string namespacePrefix, CancellationToken token)
		=> DispatchToInnerProvider (p => p.GetElementCompletionDataAsync (source, namespacePrefix, token), token);
}