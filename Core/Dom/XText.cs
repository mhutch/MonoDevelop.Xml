// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MonoDevelop.Xml.Dom
{
	public class XText : XNode
	{
		public XText (int startOffset) : base (startOffset) { }
		public XText (TextSpan span) : base (span) { }

		protected XText () { }
		protected override XObject NewInstance () { return new XText (); }

		public string Text { get; private set; }

		public override string FriendlyPathRepresentation {
			get { return Ellipsize (Text, 20); }
		}

		public void End (string text)
		{
			Text = text;
			Span = new TextSpan (Span.Start, text.Length);
		}

		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			var other = (XText)copyFrom;
			Text = other.Text;
			base.ShallowCopyFrom (copyFrom);
		}

		static string Ellipsize (string s, int length)
			=> s.Length < length - 3 ? s : s.Substring (0, length - 3) + "...";
	}
}
