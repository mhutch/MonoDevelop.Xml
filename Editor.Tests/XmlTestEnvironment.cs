using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.MiniEditor;
using Microsoft.VisualStudio.Threading;

using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Parser;

using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace MonoDevelop.Xml.Editor.Tests
{
	public class XmlTestEnvironment
	{
		EditorEnvironment editorEnvironment;
		static Exception initException;

		protected static XmlTestEnvironment Instance { get; private set; }

		[Export]
		public static JoinableTaskContext MefJoinableTaskContext = null;

		public static EditorEnvironment EditorEnvironment => initException == null ? Instance.editorEnvironment : throw initException;

		public static EditorCatalog CreateEditorCatalog () => new (EnsureInitialized<XmlTestEnvironment> ().GetEditorHost ());

		protected static EditorEnvironment EnsureInitialized<T> () where T : XmlTestEnvironment, new()
		{
			try {
				if (Instance == null) {
					Instance = new T ();
					Instance.OnInitialize ();
				} else if (!(Instance is T)) {
					throw new InvalidOperationException ($"Already initialized with type '{Instance.GetType ()}'");
				}
			} catch (Exception ex) {
				initException = ex;
			}
			return EditorEnvironment;
		}

		protected virtual void OnInitialize ()
		{
			var mainloop = new Utils.MockMainLoop ();
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

			CustomErrorHandler.ExceptionHandled += (s, e) => HandleError (s, e.Exception);
		}

		protected virtual IEnumerable<string> GetAssembliesToCompose () => new[] {
			typeof (XmlParser).Assembly.Location,
			typeof (XmlCompletionSource).Assembly.Location,
			typeof (XmlTestEnvironment).Assembly.Location
		};

		protected virtual bool ShouldIgnoreCompositionError (string error) => false;

		protected virtual void HandleError (object source, Exception ex)
		{
			TestExecutionContext.CurrentContext.CurrentResult.RecordAssertion (AssertionStatus.Error, ex.Message, ex.StackTrace);

			if (Debugger.IsAttached) {
				Debugger.Break ();
			}
		}
	}
}
