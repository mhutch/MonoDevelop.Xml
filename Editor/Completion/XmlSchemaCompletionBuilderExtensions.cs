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

using System;
using System.Xml;
using System.Xml.Schema;

namespace MonoDevelop.Xml.Editor.Completion
{
	static class XmlSchemaCompletionBuilderExtensions
	{
		public static void GetChildElementCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaElement element, string prefix)
		{
			var complexType = schema.GetElementAsComplexType (element);
			if (complexType != null)
				GetChildElementCompletionData (data, schema, complexType, prefix);
		}

		public static void GetChildElementCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaComplexType complexType, string prefix)
		{
			if (complexType.Particle is XmlSchemaSequence sequence) {
				GetChildElementCompletionData (data, schema, sequence.Items, prefix);
				return;
			}
			if (complexType.Particle is XmlSchemaChoice choice) {
				GetChildElementCompletionData (data, schema, choice.Items, prefix);
				return;
			}
			if (complexType.ContentModel is XmlSchemaComplexContent complexContent) {
				GetChildElementCompletionData (data, schema, complexContent, prefix);
				return;
			}
			if (complexType.Particle is XmlSchemaGroupRef groupRef) {
				GetChildElementCompletionData (data, schema, groupRef, prefix);
				return;
			}
			if (complexType.Particle is XmlSchemaAll all) {
				GetChildElementCompletionData (data, schema, all.Items, prefix);
				return;
			}
		}

		public static void GetChildElementCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaObjectCollection items, string prefix)
		{
			foreach (XmlSchemaObject schemaObject in items) {
				if (schemaObject is XmlSchemaElement childElement) {
					string? name = childElement.Name;
					if (name is null) {
						name = childElement.RefName.Name;
						var element = schema.FindElement (childElement.RefName);
						if (element != null) {
							if (element.IsAbstract) {
								AddSubstitionGroupElements (data, schema, element.QualifiedName, prefix);
							} else {
								data.AddElement (name, prefix, element.Annotation);
							}
						} else {
							data.AddElement (name, prefix, childElement.Annotation);
						}
					} else {
						data.AddElement (name, prefix, childElement.Annotation);
					}
					continue;
				}
				if (schemaObject is XmlSchemaSequence childSequence) {
					GetChildElementCompletionData (data, schema, childSequence.Items, prefix);
					continue;
				}
				if (schemaObject is XmlSchemaChoice childChoice) {
					GetChildElementCompletionData (data, schema, childChoice.Items, prefix);
					continue;
				}
				if (schemaObject is XmlSchemaGroupRef groupRef) {
					GetChildElementCompletionData (data, schema, groupRef, prefix);
					continue;
				}
			}
		}

		public static void GetChildElementCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaComplexContent complexContent, string prefix)
		{
			if (complexContent.Content is XmlSchemaComplexContentExtension extension) {
				GetChildElementCompletionData (data, schema, extension, prefix);
				return;
			}
			if (complexContent.Content is XmlSchemaComplexContentRestriction restriction) {
				GetChildElementCompletionData (data, schema, restriction, prefix);
				return;
			}
		}

		public static void GetChildElementCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaComplexContentExtension extension, string prefix)
		{
			var complexType = schema.FindNamedType (extension.BaseTypeName);
			if (complexType != null)
				GetChildElementCompletionData (data, schema, complexType, prefix);

			if (extension.Particle == null)
				return;

			if (extension.Particle is XmlSchemaSequence sequence) {
				GetChildElementCompletionData (data, schema, sequence.Items, prefix);
				return;
			}
			if (extension.Particle is XmlSchemaChoice choice) {
				GetChildElementCompletionData (data, schema, choice.Items, prefix);
				return;
			}
			if (extension.Particle is XmlSchemaGroupRef groupRef) {
				GetChildElementCompletionData (data, schema, groupRef, prefix);
				return;
			}
		}

		public static void GetChildElementCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaGroupRef groupRef, string prefix)
		{
			var group = schema.FindGroup (groupRef.RefName.Name);
			if (group == null)
				return;
			if (group.Particle is XmlSchemaSequence sequence) {
				GetChildElementCompletionData (data, schema, sequence.Items, prefix);
				return;
			}
			if (group.Particle is XmlSchemaChoice choice) {
				GetChildElementCompletionData (data, schema, choice.Items, prefix);
				return;
			}
		}

		public static void GetChildElementCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaComplexContentRestriction restriction, string prefix)
		{
			if (restriction.Particle == null)
				return;
			if (restriction.Particle is XmlSchemaSequence sequence) {
				GetChildElementCompletionData (data, schema, sequence.Items, prefix);
				return;
			}
			if (restriction.Particle is XmlSchemaChoice choice) {
				GetChildElementCompletionData (data, schema, choice.Items, prefix);
				return;
			}
			if (restriction.Particle is XmlSchemaGroupRef groupRef) {
				GetChildElementCompletionData (data, schema, groupRef, prefix);
				return;
			}
		}

		public static void GetAttributeCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaElement element)
		{
			var complexType = schema.GetElementAsComplexType (element);
			if (complexType != null)
				GetAttributeCompletionData (data, schema, complexType);
		}

		public static void GetAttributeCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaComplexContentRestriction restriction)
		{
			GetAttributeCompletionData (data, schema, restriction.Attributes);

			var baseComplexType = schema.FindNamedType (restriction.BaseTypeName);
			if (baseComplexType != null) {
				GetAttributeCompletionData (data, schema, baseComplexType);
			}
		}

		public static void GetAttributeCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaComplexType complexType)
		{
			GetAttributeCompletionData (data, schema, complexType.Attributes);

			// Add any complex content attributes.
			if (complexType.ContentModel is XmlSchemaComplexContent complexContent) {
				if (complexContent.Content is XmlSchemaComplexContentExtension extension)
					GetAttributeCompletionData (data, schema, extension);
				else if (complexContent.Content is XmlSchemaComplexContentRestriction restriction)
					GetAttributeCompletionData (data, schema, restriction);
			} else {
				if (complexType.ContentModel is XmlSchemaSimpleContent simpleContent)
					GetAttributeCompletionData (data, schema, simpleContent);
			}
		}

		public static void GetAttributeCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaComplexContentExtension extension)
		{
			GetAttributeCompletionData (data, schema, extension.Attributes);
			var baseComplexType = schema.FindNamedType (extension.BaseTypeName);
			if (baseComplexType != null)
				GetAttributeCompletionData (data, schema, baseComplexType);
		}

		public static void GetAttributeCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaSimpleContent simpleContent)
		{
			if (simpleContent.Content is XmlSchemaSimpleContentExtension extension)
				GetAttributeCompletionData (data, schema, extension);
		}

		public static void GetAttributeCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaSimpleContentExtension extension)
		{
			GetAttributeCompletionData (data, schema, extension.Attributes);
		}

		public static void GetAttributeCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaObjectCollection attributes)
		{
			foreach (XmlSchemaObject schemaObject in attributes) {
				if (schemaObject is XmlSchemaAttribute attribute) {
					if (!attribute.IsProhibited ()) {
						data.AddAttribute (attribute);
					}
				} else {
					if (schemaObject is XmlSchemaAttributeGroupRef attributeGroupRef)
						GetAttributeCompletionData (data, schema, attributeGroupRef);
				}
			}
		}

		/// <summary>
		/// Checks that the attribute is prohibited or has been flagged
		/// as prohibited previously. 
		/// </summary>
		static bool IsProhibited (this XmlSchemaAttribute attribute) => attribute.Use == XmlSchemaUse.Prohibited;

		/// <summary>
		/// Gets attribute completion data from a group ref.
		/// </summary>
		public static void GetAttributeCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaAttributeGroupRef groupRef)
		{
			var group = schema.FindAttributeGroup (groupRef.RefName.Name);
			if (group != null)
				GetAttributeCompletionData (data, schema, group.Attributes);
		}

		public static void GetAttributeValueCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaElement element, string name)
		{
			var complexType = schema.GetElementAsComplexType (element);
			if (complexType != null) {
				var attribute = schema.FindAttribute (complexType, name);
				if (attribute != null)
					GetAttributeValueCompletionData (data, schema, attribute);
			}
		}

		public static void GetAttributeValueCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaAttribute attribute)
		{
			if (attribute.SchemaType != null) {
				if (attribute.SchemaType.Content is XmlSchemaSimpleTypeRestriction simpleTypeRestriction) {
					GetAttributeValueCompletionData (data, schema, simpleTypeRestriction);
				}
			} else if (attribute.AttributeSchemaType != null) {
				if (attribute.AttributeSchemaType.TypeCode == XmlTypeCode.Boolean)
					GetBooleanAttributeValueCompletionData (data, schema);
				else
					GetAttributeValueCompletionData (data, schema, attribute.AttributeSchemaType);
			}
		}

		public static void GetAttributeValueCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaSimpleTypeRestriction simpleTypeRestriction)
		{
			foreach (XmlSchemaObject schemaObject in simpleTypeRestriction.Facets) {
				if (schemaObject is XmlSchemaEnumerationFacet enumFacet && enumFacet.Value is string value)
					data.AddAttributeValue (value, enumFacet.Annotation);
			}
		}

		public static void GetAttributeValueCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaSimpleTypeUnion union)
		{
			foreach (XmlSchemaObject schemaObject in union.BaseTypes) {
				if (schemaObject is XmlSchemaSimpleType simpleType)
					GetAttributeValueCompletionData (data, schema, simpleType);
			}
		}

		public static void GetAttributeValueCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaSimpleType simpleType)
		{
			if (simpleType.Content is XmlSchemaSimpleTypeRestriction xsstr) {
				GetAttributeValueCompletionData (data, schema, xsstr);
				return;
			}
			if (simpleType.Content is XmlSchemaSimpleTypeUnion xsstu) {
				GetAttributeValueCompletionData (data, schema, xsstu);
				return;
			}
			if (simpleType.Content is XmlSchemaSimpleTypeList xsstl) {
				GetAttributeValueCompletionData (data, schema, xsstl);
				return;
			}
		}

		public static void GetAttributeValueCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaSimpleTypeList list)
		{
			if (list.ItemType != null) {
				GetAttributeValueCompletionData (data, schema, list.ItemType);
			} else if (list.ItemTypeName != null) {
				var simpleType = schema.FindSimpleType (list.ItemTypeName);
				if (simpleType != null)
					GetAttributeValueCompletionData (data, schema, simpleType);
			}
		}

		/// <summary>
		/// Gets the set of attribute values for an xs:boolean type.
		/// </summary>
		public static void GetBooleanAttributeValueCompletionData (this XmlSchemaCompletionBuilder data, XmlSchema schema)
		{
			data.AddAttributeValue ("0");
			data.AddAttributeValue ("1");
			data.AddAttributeValue ("true");
			data.AddAttributeValue ("false");
		}

		/// <summary>
		/// Adds any elements that have the specified substitution group.
		/// </summary>
		public static void AddSubstitionGroupElements (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlQualifiedName group, string prefix)
		{
			foreach (XmlSchemaElement element in schema.Elements.Values) {
				if (element.SubstitutionGroup == group && element.Name is string name) {
					data.AddElement (name, prefix, element.Annotation);
				}
			}
		}
	}
}