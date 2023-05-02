// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Xml.Editor.Logging;

interface IEditorLoggerProviderMetadata : IContentTypeMetadata, IOrderable
{
}

[Export(typeof(IEditorLoggerFactory))]
partial class EditorLoggerFactory : IEditorLoggerFactory
{
	[ImportingConstructor]
	public EditorLoggerFactory ([ImportMany] Lazy<IEditorLoggerProvider, IEditorLoggerProviderMetadata>[] providers, IGuardedOperations guardedOperations, IContentTypeRegistryService contentTypeRegistryService)
	{
		Providers = Orderer.Order (providers);
		GuardedOperations = guardedOperations;
		ContentTypeRegistryService = contentTypeRegistryService;
	}

	public IList<Lazy<IEditorLoggerProvider, IEditorLoggerProviderMetadata>> Providers { get; }
	public IGuardedOperations GuardedOperations { get; }
	public IContentTypeRegistryService ContentTypeRegistryService { get; }

	ImmutableDictionary<IContentType, ILoggerFactory> loggersByContentType = ImmutableDictionary<IContentType, ILoggerFactory>.Empty;

	// TODO: this could return a proxy that recreates the internal logger if the content type changes
	ILoggerFactory GetLoggerFactoryForBuffer (ITextBuffer buffer) => GetLoggerFactoryForContentType (buffer.ContentType);

	// TODO: this could return a proxy that recreates the internal logger if the content type changes
	ILoggerFactory GetLoggerFactoryForTextView (ITextView textView) => GetLoggerFactoryForContentType (textView.TextBuffer.ContentType);

	ILoggerFactory GetLoggerFactoryForContentType (IContentType contentType)
	{
		if (loggersByContentType.TryGetValue (contentType, out var factory)) {
			return factory;
		}

		var providers = GuardedOperations.InvokeMatchingFactories (Providers, p => p, contentType, this);

		if (providers.Count == 0) {
			factory = NullLoggerFactory.Instance;
		} else {
			factory = new AggregatedLoggerFactory (providers);
		}

		ImmutableInterlocked.Update (ref loggersByContentType, (s) => s.Add (contentType, factory));

		return factory;
	}

	/// <summary>
	/// This always creates an instance. Use <see cref="EditorLoggerFactoryExtensions.GetLogger{T}(IEditorLoggerFactory, ITextBuffer)"/> to get a shared instance.
	/// </summary>
	public ILogger<T> CreateLogger<T> (ITextBuffer buffer) => GetLoggerFactoryForBuffer (buffer).CreateLogger<T> ();

	/// <summary>
	/// This always creates an instance. Use <see cref="EditorLoggerFactoryExtensions.GetLogger{T}(IEditorLoggerFactory, ITextView)"/> to get a shared instance.
	/// </summary>
	public ILogger<T> CreateLogger<T> (ITextView textView) => GetLoggerFactoryForTextView (textView).CreateLogger<T> ();

	/// <summary>
	/// Get a logger using the appropriate providers for the content type.
	/// <para>
	/// This always creates a new instance. Share instances via composition, or use <see cref="EditorLoggerFactoryExtensions.GetLogger{T}(IEditorLoggerFactory, ITextBuffer)"/> or
	/// <see cref="EditorLoggerFactoryExtensions.GetLogger{T}(IEditorLoggerFactory, ITextView)"/> to share instances specific to a buffer or view.
	/// </para>
	/// </summary>
	public ILogger<T> CreateLogger<T> (string contentTypeName)
	{
		var contentType = ContentTypeRegistryService.GetContentType (contentTypeName) ?? throw new ArgumentException ($"Unknown content type '{contentTypeName}'");
		return GetLoggerFactoryForContentType (contentType).CreateLogger<T> ();
	}

	/// <inheritdoc cref="CreateLogger{T}(string)"/>
	public ILogger<T> CreateLogger<T> (IContentType contentType) => GetLoggerFactoryForContentType (contentType).CreateLogger<T>();
}
