//
// MonoDevelop XML Editor
//
// Copyright (C) 2005 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#nullable enable

using System.Xml;
using System.Xml.Schema;

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Editor.Completion
{
	internal static class XmlSchemaExtensions
	{
		/// <summary>
		/// Finds an element in the schema.
		/// </summary>
		/// <remarks>
		/// Only looks at the elements that are defined in the 
		/// root of the schema so it will not find any elements
		/// that are defined inside any complex types.
		/// </remarks>
		public static XmlSchemaElement? FindElement (this XmlSchema schema, XmlQualifiedName name)
		{
			foreach (XmlSchemaElement element in schema.Elements.Values) {
				if (name.Equals (element.QualifiedName)) {
					return element;
				}
			}

			return null;
		}

		/// <summary>
		/// Finds the element that exists at the specified path.
		/// </summary>
		/// <remarks>This method is not used when generating completion data,
		/// but is a useful method when locating an element so we can jump
		/// to its schema definition.</remarks>
		/// <returns><see langword="null"/> if no element can be found.</returns>
		public static XmlSchemaElement? FindElement (this XmlSchema schema, XmlElementPath path)
		{
			XmlSchemaElement? element = null;
			for (int i = 0; i < path.Elements.Count; ++i) {
				QualifiedName name = path.Elements[i];
				if (i == 0) {
					// Look for root element.
					element = FindElement (schema, name);
				} else {
					element = FindChildElement (schema, element!, name);
				}
				if (element is null) {
					break;
				}
			}
			return element;
		}

		/// <summary>
		/// Finds an element in the schema.
		/// </summary>
		/// <remarks>
		/// Only looks at the elements that are defined in the 
		/// root of the schema so it will not find any elements
		/// that are defined inside any complex types.
		/// </remarks>
		public static XmlSchemaElement? FindElement (this XmlSchema schema, QualifiedName name)
		{
			foreach (XmlSchemaElement element in schema.Elements.Values) {
				if (name.Equals (element.QualifiedName)) {
					return element;
				}
			}
			return null;
		}

		public static XmlSchemaElement? FindElement (this XmlSchema schema, string name)
		{
			foreach (XmlSchemaElement element in schema.Elements.Values) {
				if (element.QualifiedName.Name == name) {
					return element;
				}
			}
			return null;
		}

		/// <summary>
		/// Finds the complex type with the specified name.
		/// </summary>
		public static XmlSchemaComplexType? FindComplexType (this XmlSchema schema, QualifiedName name)
		{
			var qualifiedName = new XmlQualifiedName (name.Name, name.Namespace);
			return FindNamedType (schema, qualifiedName);
		}

		/// <summary>
		/// Converts the element to a complex type if possible.
		/// </summary>
		public static XmlSchemaComplexType? GetElementAsComplexType (this XmlSchema schema, XmlSchemaElement element)
		{
			return (element.SchemaType as XmlSchemaComplexType) ?? FindNamedType (schema, element.SchemaTypeName);
		}

		/// <summary>
		/// Finds the specified attribute name given the element.
		/// </summary>
		/// <remarks>This method is not used when generating completion data,
		/// but is a useful method when locating an attribute so we can jump
		/// to its schema definition.</remarks>
		/// <returns><see langword="null"/> if no attribute can be found.</returns>
		public static XmlSchemaAttribute? FindAttribute (this XmlSchema schema, XmlSchemaElement element, string name)
		{
			if (GetElementAsComplexType (schema, element) is XmlSchemaComplexType complexType) {
				return FindAttribute (schema, complexType, name);
			}
			return null;
		}

		/// <summary>
		/// Finds the specified attribute in the schema. This method only checks
		/// the attributes defined in the root of the schema.
		/// </summary>
		public static XmlSchemaAttribute? FindAttribute (this XmlSchema schema, string name)
		{
			foreach (XmlSchemaAttribute attribute in schema.Attributes.Values)
				if (attribute.Name == name)
					return attribute;
			return null;
		}

		/// <summary>
		/// Finds the schema group with the specified name.
		/// </summary>
		public static XmlSchemaGroup? FindGroup (this XmlSchema schema, string name)
		{
			if (name is null)
				return null;

			foreach (XmlSchemaObject schemaObject in schema.Groups.Values) {
				if (schemaObject is XmlSchemaGroup group && group.Name == name)
					return group;
			}

			return null;
		}

		public static XmlSchemaComplexType? FindNamedType (this XmlSchema schema, XmlQualifiedName name)
		{
			if (name is null)
				return null;

			foreach (XmlSchemaObject schemaObject in schema.Items) {
				if (schemaObject is XmlSchemaComplexType complexType && complexType.QualifiedName == name)
					return complexType;
			}

			// Try included schemas.
			foreach (XmlSchemaExternal external in schema.Includes) {
				if (external is XmlSchemaInclude include
					&& include.Schema is XmlSchema includedSchema
					&& FindNamedType (includedSchema, name) is XmlSchemaComplexType matchedComplexType) {
					return matchedComplexType;
				}
			}

			return null;
		}

		/// <summary>
		/// Finds an element that matches the specified <paramref name="name"/>
		/// from the children of the given <paramref name="element"/>.
		/// </summary>
		public static XmlSchemaElement? FindChildElement (this XmlSchema schema, XmlSchemaElement element, QualifiedName name)
		{
			if (GetElementAsComplexType (schema, element) is XmlSchemaComplexType complexType)
				return FindChildElement (schema, complexType, name);
			return null;
		}

		public static XmlSchemaElement? FindChildElement (this XmlSchema schema, XmlSchemaComplexType complexType, QualifiedName name)
		{
			if (complexType.Particle is XmlSchemaSequence sequence)
				return FindElement (schema, sequence.Items, name);

			if (complexType.Particle is XmlSchemaChoice choice)
				return FindElement (schema, choice.Items, name);

			if (complexType.ContentModel is XmlSchemaComplexContent complexContent) {
				if (complexContent.Content is XmlSchemaComplexContentExtension extension)
					return FindChildElement (schema, extension, name);
				if (complexContent.Content is XmlSchemaComplexContentRestriction restriction)
					return FindChildElement (schema, restriction, name);
			}

			if (complexType.Particle is XmlSchemaGroupRef groupRef)
				return FindElement (schema, groupRef, name);

			if (complexType.Particle is XmlSchemaAll all)
				return FindElement (schema, all.Items, name);

			return null;
		}

		/// <summary>
		/// Finds the named child element contained in the extension element.
		/// </summary>
		public static XmlSchemaElement? FindChildElement (this XmlSchema schema, XmlSchemaComplexContentExtension extension, QualifiedName name)
		{
			if (FindNamedType (schema, extension.BaseTypeName) is not XmlSchemaComplexType complexType) {
				return null;
			}

			if (FindChildElement (schema, complexType, name) is XmlSchemaElement matchedElement) {
				return matchedElement;
			}

			return extension.Particle switch {
				XmlSchemaSequence sequence => FindElement (schema, sequence.Items, name),
				XmlSchemaChoice choice => FindElement (schema, choice.Items, name),
				XmlSchemaGroupRef groupRef => FindElement (schema, groupRef, name),
				_ => null
			};
		}

		/// <summary>
		/// Finds the named child element contained in the restriction element.
		/// </summary>
		public static XmlSchemaElement?FindChildElement (this XmlSchema schema, XmlSchemaComplexContentRestriction restriction, QualifiedName name)
		{
			return restriction.Particle switch {
				XmlSchemaSequence sequence => FindElement (schema, sequence.Items, name),
				XmlSchemaGroupRef groupRef => FindElement (schema, groupRef, name),
				_ => null
			};
		}

		/// <summary>
		/// Finds the element in the collection of schema objects.
		/// </summary>
		public static XmlSchemaElement? FindElement (this XmlSchema schema, XmlSchemaObjectCollection items, QualifiedName name)
		{
			foreach (XmlSchemaObject schemaObject in items) {
				if (schemaObject is XmlSchemaElement element) {
					if (element.Name != null) {
						if (name.Name == element.Name) {
							return element;
						}
					} else if (element.RefName != null) {
						if (name.Name == element.RefName.Name) {
							if (FindElement (schema, element.RefName) is XmlSchemaElement match) {
								return match;
							}
						} else {
							if (FindElement (schema, element.RefName) is XmlSchemaElement abstractElement && abstractElement.IsAbstract) {
								if (FindSubstitutionGroupElement (schema, abstractElement.QualifiedName, name) is XmlSchemaElement substGrpEl) {
									return substGrpEl;
								}
							}
						}
					}
					continue;
				}

				if (schemaObject switch {
					XmlSchemaSequence sequence => FindElement (schema, sequence.Items, name),
					XmlSchemaChoice choice => FindElement (schema, choice.Items, name),
					XmlSchemaGroupRef groupRef => FindElement (schema, groupRef, name),
					_ => null
				} is XmlSchemaElement matchedElement) {
					return matchedElement;
				}
			}

			return null;
		}

		public static XmlSchemaElement? FindElement (this XmlSchema schema, XmlSchemaGroupRef groupRef, QualifiedName name)
		{
			var group = FindGroup (schema, groupRef.RefName.Name);
			if (group == null)
				return null;

			var sequence = group.Particle as XmlSchemaSequence;
			if (sequence != null)
				return FindElement (schema, sequence.Items, name);
			var choice = group.Particle as XmlSchemaChoice;
			if (choice != null)
				return FindElement (schema, choice.Items, name);

			return null;
		}

		/// <summary>
		/// Finds the attribute group with the specified name.
		/// </summary>
		public static XmlSchemaAttributeGroup? FindAttributeGroup (this XmlSchema schema, string name)
		{
			if (name is null)
				return null;

			foreach (XmlSchemaObject schemaObject in schema.Items) {
				var group = schemaObject as XmlSchemaAttributeGroup;
				if (group != null && group.Name == name)
					return group;
			}

			// Try included schemas.
			foreach (XmlSchemaExternal external in schema.Includes) {
				var include = external as XmlSchemaInclude;
				if (include != null && include.Schema != null) {
					var found = FindAttributeGroup (include.Schema, name);
					if (found != null)
						return found;
				}
			}
			return null;
		}

		public static XmlSchemaAttribute? FindAttribute (this XmlSchema schema, XmlSchemaComplexType complexType, string name)
		{
			var matchedAttribute = FindAttribute (schema, complexType.Attributes, name);
			if (matchedAttribute != null)
				return matchedAttribute;

			var complexContent = complexType.ContentModel as XmlSchemaComplexContent;
			if (complexContent != null)
				return FindAttribute (schema, complexContent, name);

			return null;
		}

		public static XmlSchemaAttribute? FindAttribute (this XmlSchema schema, XmlSchemaObjectCollection schemaObjects, string name)
		{
			foreach (XmlSchemaObject schemaObject in schemaObjects) {
				var attribute = schemaObject as XmlSchemaAttribute;
				if (attribute != null && attribute.Name == name)
					return attribute;

				var groupRef = schemaObject as XmlSchemaAttributeGroupRef;
				if (groupRef != null) {
					var matchedAttribute = FindAttribute (schema, groupRef, name);
					if (matchedAttribute != null)
						return matchedAttribute;
				}
			}
			return null;
		}

		public static XmlSchemaAttribute? FindAttribute (this XmlSchema schema, XmlSchemaAttributeGroupRef groupRef, string name)
		{
			if (groupRef.RefName != null) {
				var group = FindAttributeGroup (schema, groupRef.RefName.Name);
				if (group != null) {
					return FindAttribute (schema, group.Attributes, name);
				}
			}
			return null;
		}

		public static XmlSchemaAttribute? FindAttribute (this XmlSchema schema, XmlSchemaComplexContent complexContent, string name)
		{
			var extension = complexContent.Content as XmlSchemaComplexContentExtension;
			if (extension != null)
				return FindAttribute (schema, extension, name);

			var restriction = complexContent.Content as XmlSchemaComplexContentRestriction;
			if (restriction != null)
				return FindAttribute (schema, restriction, name);

			return null;
		}

		public static XmlSchemaAttribute? FindAttribute (this XmlSchema schema, XmlSchemaComplexContentExtension extension, string name)
		{
			return FindAttribute (schema, extension.Attributes, name);
		}

		public static XmlSchemaAttribute? FindAttribute (this XmlSchema schema, XmlSchemaComplexContentRestriction restriction, string name)
		{
			var matchedAttribute = FindAttribute (schema, restriction.Attributes, name);
			if (matchedAttribute != null)
				return matchedAttribute;

			var complexType = FindNamedType (schema, restriction.BaseTypeName);
			if (complexType != null)
				return FindAttribute (schema, complexType, name);

			return null;
		}

		public static XmlSchemaSimpleType? FindSimpleType (this XmlSchema schema, XmlQualifiedName name)
		{
			foreach (XmlSchemaObject schemaObject in schema.SchemaTypes.Values) {
				if (schemaObject is XmlSchemaSimpleType simpleType && simpleType.QualifiedName == name)
					return simpleType;
			}
			return null;
		}

		/// <summary>
		/// Looks for the substitution group element of the specified name.
		/// </summary>
		public static XmlSchemaElement? FindSubstitutionGroupElement (this XmlSchema schema, XmlQualifiedName group, QualifiedName name)
		{
			foreach (XmlSchemaElement element in schema.Elements.Values)
				if (element.SubstitutionGroup == group && element.Name != null && element.Name == name.Name)
					return element;

			return null;
		}
	}
}