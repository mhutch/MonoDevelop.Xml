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

interface INamedEditorLoggerFactoryMetadata : IContentTypeMetadata, IOrderable
{
}

[Export(typeof(IEditorLoggerService))]
class EditorLoggerService : IEditorLoggerService
{
	[ImportingConstructor]
	public EditorLoggerService ([ImportMany] Lazy<IEditorLoggerFactory, INamedEditorLoggerFactoryMetadata>[] factories, IGuardedOperations guardedOperations, IContentTypeRegistryService contentTypeRegistryService)
	{
		Factories = Orderer.Order (factories);
		GuardedOperations = guardedOperations;
		ContentTypeRegistryService = contentTypeRegistryService;
	}

	public IList<Lazy<IEditorLoggerFactory, INamedEditorLoggerFactoryMetadata>> Factories { get; }
	public IGuardedOperations GuardedOperations { get; }
	public IContentTypeRegistryService ContentTypeRegistryService { get; }

	ImmutableDictionary<IContentType, IEditorLoggerFactory> loggersByContentType = ImmutableDictionary<IContentType, IEditorLoggerFactory>.Empty;

	// TODO: this could return a proxy that recreates the internal logger if the content type changes
	IEditorLoggerFactory GetEditorLoggerFactoryForBuffer (ITextBuffer buffer) => GetEditorLoggerFactoryForContentType (buffer.ContentType);

	// TODO: this could return a proxy that recreates the internal logger if the content type changes
	IEditorLoggerFactory GetEditorLoggerFactoryForTextView (ITextView textView) => GetEditorLoggerFactoryForContentType (textView.TextBuffer.ContentType);

	IEditorLoggerFactory GetEditorLoggerFactoryForContentType (IContentType contentType)
	{
		if (loggersByContentType.TryGetValue (contentType, out var factory)) {
			return factory;
		}

		factory = GuardedOperations.InvokeBestMatchingFactory (Factories, contentType, ContentTypeRegistryService, this) ?? NullEditorLoggerFactory.Instance;
		ImmutableInterlocked.Update (ref loggersByContentType, (s) => s.Add (contentType, factory));

		return factory;
	}

	public ILogger<T> CreateLogger<T> (ITextBuffer buffer) => GetEditorLoggerFactoryForBuffer (buffer).CreateLogger<T> ();

	public ILogger<T> CreateLogger<T> (ITextView textView) => GetEditorLoggerFactoryForTextView (textView).CreateLogger<T> ();
}
