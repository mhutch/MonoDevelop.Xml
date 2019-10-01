// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text.Adornments;

namespace MonoDevelop.Xml.Editor.Completion
{
	public static class XmlImages
	{
		public static readonly ImageElement Element = CreateElement (KnownImageIds.XMLElement);
		public static readonly ImageElement Attribute = CreateElement (KnownImageIds.XMLAttribute);
		public static readonly ImageElement AttributeValue = CreateElement (KnownImageIds.Constant);
		public static readonly ImageElement Namespace = CreateElement (KnownImageIds.XMLNamespace);
		public static readonly ImageElement Comment = CreateElement (KnownImageIds.XMLCommentTag);
		public static readonly ImageElement CData = CreateElement (KnownImageIds.XMLCDataTag);
		public static readonly ImageElement Prolog = CreateElement (KnownImageIds.XMLProcessInstructionTag);
		public static readonly ImageElement Entity = Prolog;
		public static ImageElement ClosingTag = Element;

		static readonly Guid KnownImagesGuid = KnownImageIds.ImageCatalogGuid;
		static ImageElement CreateElement (int id) => new ImageElement (new ImageId (KnownImagesGuid, id));
	}
}
