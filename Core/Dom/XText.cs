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

		public override string FriendlyPathRepresentation {
			get { return "[TEXT]"; }
		}
	}
}
