using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.MiniEditor;
using Microsoft.VisualStudio.Threading;

using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Parser;
using MonoDevelop.Xml.Tests.Completion;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.EditorTestHelpers
{
	public class XmlTestEnvironment
	{
		EditorEnvironment editorEnvironment;
		EditorCatalog editorCatalog;
		static Exception initException;

		protected static XmlTestEnvironment Instance { get; private set; }

		[Export]
		public static JoinableTaskContext MefJoinableTaskContext = null;

		public static EditorEnvironment EditorEnvironment => initException == null? Instance.editorEnvironment : throw initException;
		public static EditorCatalog EditorCatalog => initException == null? Instance.editorCatalog : throw initException;

		public static (EditorEnvironment, EditorCatalog) EnsureInitialized () => EnsureInitialized<XmlTestEnvironment> ();

		protected static (EditorEnvironment, EditorCatalog) EnsureInitialized<T> () where T: XmlTestEnvironment, new ()
		{
			try {
				if (Instance == null) {
					Instance = new T ();
					Instance.OnInitialize ();
				} else if (!(Instance is T)) {
					throw new InvalidOperationException ($"Already initialized with type '{Instance.GetType()}'");
				}
			} catch (Exception ex) {
				initException = ex;
			}
			return (EditorEnvironment, EditorCatalog);
		}

		protected virtual void OnInitialize ()
		{
			var mainloop = new MockMainLoop ();
			mainloop.Start ().Wait ();
			MefJoinableTaskContext = mainloop.JoinableTaskContext;
			System.Threading.SynchronizationContext.SetSynchronizationContext (mainloop);

			EditorEnvironment.DefaultAssemblies = new string[2]
			{
				typeof(EditorEnvironment).Assembly.Location, // Microsoft.VisualStudio.MiniEditor
				typeof (Microsoft.VisualStudio.Text.VirtualSnapshotPoint).Assembly.Location, //Microsoft.VisualStudio.Text.Logic
			}.ToImmutableArray ();

			// Create the MEF composition
			// can be awaited instead if your framework supports it
			editorEnvironment = EditorEnvironment.InitializeAsync (GetAssembliesToCompose ().ToArray ()).Result;

			if (editorEnvironment.CompositionErrors.Length > 0) {
				var errors = editorEnvironment.CompositionErrors.Where (e => !ShouldIgnoreCompositionError (e)).ToArray ();
				if (errors.Length > 0) {
					throw new CompositionException ($"Composition failure: {string.Join ("\n", errors)}");
				}
			}

			// Register your own logging mechanism to print eventual errors
			// in your extensions
			var errorHandler = editorEnvironment
				.GetEditorHost ()
				.GetService<EditorHostExports.CustomErrorHandler> ();

			errorHandler.ExceptionHandled += (s, e) => HandleError (e.Exception);

			editorCatalog = new EditorCatalog (editorEnvironment);
		}

		protected virtual IEnumerable<string> GetAssembliesToCompose () => new[] {
			typeof (XmlParser).Assembly.Location,
			typeof (XmlCompletionSource).Assembly.Location,
			typeof (XmlTestEnvironment).Assembly.Location
		};

		protected virtual bool ShouldIgnoreCompositionError (string error) => false;

		protected virtual void HandleError (Exception ex)
		{
			Assert.Fail (ex.ToString ());
		}
	}
}
