// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Text.Adornments;

namespace MonoDevelop.Xml.Editor.Completion
{
	public static class XmlImages
	{
		public static readonly ImageElement Element = new ImageElement (new ImageId ());
		public static readonly ImageElement Attribute = new ImageElement (new ImageId ());
		public static readonly ImageElement AttributeValue = new ImageElement (new ImageId ());
		public static readonly ImageElement Namespace = new ImageElement (new ImageId ());
		public static readonly ImageElement Directive = new ImageElement (new ImageId ());
		public static readonly ImageElement Entity = Directive;
		public static ImageElement ClosingTag = Element;
	}
}
