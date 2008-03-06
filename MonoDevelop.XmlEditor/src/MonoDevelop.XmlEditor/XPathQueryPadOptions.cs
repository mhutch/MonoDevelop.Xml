//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using System;
using System.Xml;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// The XPath Query Pad options.
	/// </summary>
	public class XPathQueryPadOptions
	{
		public const string OptionsProperty = "XPathQueryPad.Options";
		public const string NamespacesProperty = "Namespaces";
		public const string LastXPathQueryProperty = "LastXPathQuery";
		public const string XPathHistoryProperty = "XPathQueryHistory";

		static IProperties properties;

		static XPathQueryPadOptions()
 		{
 			properties = (IProperties)Runtime.Properties.GetProperty(OptionsProperty, new DefaultProperties());
		}

 		static IProperties Properties {
			get {
				return properties;
 			}
		}
		
		/// <summary>
		/// The last xpath query entered in the XPath Query Pad's
		/// combo box.
		/// </summary>
		public static string LastXPathQuery {
			get {
				return Properties.GetProperty(XPathHistoryProperty, String.Empty);
			}
			set {
				Properties.SetProperty(XPathHistoryProperty, value);
			}
		}
	}
}