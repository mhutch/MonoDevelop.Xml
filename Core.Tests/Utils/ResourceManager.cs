using System.IO;
using System.Resources;

namespace MonoDevelop.Xml.Tests.Utils
{
	/// <summary>
	/// Returns strings from the embedded test resources.
	/// </summary>
	public class ResourceManager
	{
		/// <summary>
		/// Returns the xhtml strict schema xml.
		/// </summary>
		public static Stream GetXhtmlStrictSchema () => GetResource ("xhtml1-strict.xsd");
		
		/// <summary>
		/// Returns the xsd schema.
		/// </summary>
		public static Stream GetXsdSchema () => GetResource("XMLSchema.xsd");

		static Stream GetResource (string name) => typeof (ResourceManager).Assembly.GetManifestResourceStream (name) ?? throw new MissingManifestResourceException($"Missing assembly resource {name}");
	}
}
