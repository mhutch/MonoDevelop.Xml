using System;

namespace MonoDevelop.Xml.Tests.Utils
{
	public static class SelectionHelper
	{
		public static (string document, int caretOffset) ExtractCaret (string document, char caretMarkerChar = '$')
		{
			var caretOffset = document.IndexOf (caretMarkerChar);
			if (caretOffset < 0) {
				throw new ArgumentException ("Document does not contain a caret marker");
			}
			return (document.Substring (0, caretOffset) + document.Substring (caretOffset + 1), caretOffset);
		}
	}
}
