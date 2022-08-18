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

using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;

namespace MonoDevelop.Xml.Editor.Completion
{
	static class XmlSchemaCompletionBuilderExtensions
	{
		public static XmlSchemaCompletionBuilder AddChildElements (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaElement element, string prefix)
			=> schema.GetElementAsComplexType (element) switch {
				XmlSchemaComplexType complexType => AddChildElements (data, schema, complexType, prefix),
				_ => data
			};

		public static XmlSchemaCompletionBuilder AddAttributes (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaElement element)
			=> schema.GetElementAsComplexType (element) switch {
				XmlSchemaComplexType complexType => AddAttributes (data, schema, complexType, new ProhibitedAttributes ()),
				_ => data
			};

		public static XmlSchemaCompletionBuilder AddAttributeValues (this XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaElement element, string name)
			=> schema.GetElementAsComplexType (element) is XmlSchemaComplexType complexType && schema.FindAttribute (complexType, name) is XmlSchemaAttribute attribute
				? AddAttributeValues (data, schema, attribute)
				: data;

		static XmlSchemaCompletionBuilder AddChildElements (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaComplexType complexType, string prefix)
			=> complexType switch {
				{ Particle: XmlSchemaSequence sequence } => AddChildElements (data, schema, sequence.Items, prefix),
				{ Particle: XmlSchemaChoice choice } => AddChildElements (data, schema, choice.Items, prefix),
				{ ContentModel: XmlSchemaComplexContent { Content: XmlSchemaComplexContentExtension extension } } => AddChildElements (data, schema, extension, prefix),
				{ ContentModel: XmlSchemaComplexContent { Content: XmlSchemaComplexContentRestriction restriction } } => AddChildElements (data, schema, restriction, prefix),
				{ Particle: XmlSchemaGroupRef groupRef } => AddChildElements (data, schema, groupRef, prefix),
				{ Particle: XmlSchemaAll all } => AddChildElements (data, schema, all.Items, prefix),
				_ => data
			};

		static XmlSchemaCompletionBuilder AddChildElements (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaObjectCollection items, string prefix)
		{
			foreach (XmlSchemaObject schemaObject in items) _ = schemaObject switch {
				XmlSchemaElement { Name: string name } namedChild => data.AddElement (name, prefix, namedChild.Annotation),
				XmlSchemaElement childElement => schema.FindElement (childElement.RefName) switch {
					XmlSchemaElement { IsAbstract: true } abstractElement => AddSubstitionGroupElements (data, schema, abstractElement.QualifiedName, prefix),
					XmlSchemaElement refElement => data.AddElement (childElement.RefName.Name, prefix, refElement.Annotation),
					_ => data
				},
				XmlSchemaSequence childSequence => AddChildElements (data, schema, childSequence.Items, prefix),
				XmlSchemaChoice childChoice => AddChildElements (data, schema, childChoice.Items, prefix),
				XmlSchemaGroupRef groupRef => AddChildElements (data, schema, groupRef, prefix),
				_ => data
			};
			return data;
		}

		static XmlSchemaCompletionBuilder AddChildElements (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaComplexContentExtension extension, string prefix)
		{
			if (schema.FindNamedType (extension.BaseTypeName) is XmlSchemaComplexType complexType) {
				AddChildElements (data, schema, complexType, prefix);
			}

			return extension.Particle switch {
				XmlSchemaSequence sequence => AddChildElements (data, schema, sequence.Items, prefix),
				XmlSchemaChoice choice => AddChildElements (data, schema, choice.Items, prefix),
				XmlSchemaGroupRef groupRef => AddChildElements (data, schema, groupRef, prefix),
				_ => data
			};
		}

		static XmlSchemaCompletionBuilder AddChildElements (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaGroupRef groupRef, string prefix)
			=> schema.FindGroup (groupRef.RefName.Name)?.Particle switch {
				XmlSchemaSequence sequence => AddChildElements (data, schema, sequence.Items, prefix),
				XmlSchemaChoice choice => AddChildElements (data, schema, choice.Items, prefix),
				_ => data
			};

		static XmlSchemaCompletionBuilder AddChildElements (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaComplexContentRestriction restriction, string prefix)
			=> restriction.Particle switch {
				XmlSchemaSequence sequence => AddChildElements (data, schema, sequence.Items, prefix),
				XmlSchemaChoice choice => AddChildElements (data, schema, choice.Items, prefix),
				XmlSchemaGroupRef groupRef => AddChildElements (data, schema, groupRef, prefix),
				_ => data
			};

		static XmlSchemaCompletionBuilder AddAttributes (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaComplexContentRestriction restriction, ProhibitedAttributes prohibited)
		{
			AddAttributes (data, schema, restriction.Attributes, prohibited);

			return schema.FindNamedType (restriction.BaseTypeName) switch {
				XmlSchemaComplexType baseComplexType => AddAttributes (data, schema, baseComplexType, prohibited),
				_ => data,
			};
		}

		static XmlSchemaCompletionBuilder AddAttributes (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaComplexType complexType, ProhibitedAttributes prohibited)
		{
			AddAttributes (data, schema, complexType.Attributes, prohibited);

			// Add any complex content attributes.
			return complexType.ContentModel switch {
				XmlSchemaComplexContent { Content: XmlSchemaComplexContentExtension extension } => AddAttributes (data, schema, extension, prohibited),
				XmlSchemaComplexContent { Content: XmlSchemaComplexContentRestriction restriction } => AddAttributes (data, schema, restriction, prohibited),
				XmlSchemaSimpleContent { Content: XmlSchemaSimpleContentExtension extension } => AddAttributes (data, schema, extension.Attributes, prohibited),
				_ => data
			};
		}

		static XmlSchemaCompletionBuilder AddAttributes (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaComplexContentExtension extension, ProhibitedAttributes prohibited)
		{
			AddAttributes (data, schema, extension.Attributes, prohibited);
			return schema.FindNamedType (extension.BaseTypeName) switch {
				XmlSchemaComplexType baseComplexType => AddAttributes (data, schema, baseComplexType, prohibited),
				_ => data
			};
		}

		static XmlSchemaCompletionBuilder AddAttributes (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaObjectCollection attributes, ProhibitedAttributes prohibited)
		{
			foreach (XmlSchemaObject schemaObject in attributes) {
				if (schemaObject is XmlSchemaAttribute attribute) {
					if (!prohibited.IsProhibited (attribute)) {
						data.AddAttribute (attribute);
					}
				} else {
					if (schemaObject is XmlSchemaAttributeGroupRef attributeGroupRef)
						AddAttributes (data, schema, attributeGroupRef, prohibited);
				}
			}
			return data;
		}

		/// <summary>
		/// Gets attribute completion data from a group ref.
		/// </summary>
		static XmlSchemaCompletionBuilder AddAttributes (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaAttributeGroupRef groupRef, ProhibitedAttributes prohibited)
			=> schema.FindAttributeGroup (groupRef.RefName.Name) switch {
				XmlSchemaAttributeGroup group => AddAttributes (data, schema, group.Attributes, prohibited),
				_ => data
			};

		static XmlSchemaCompletionBuilder AddAttributeValues (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaAttribute attribute)
			=> attribute switch {
				{ SchemaType: XmlSchemaSimpleType { Content: XmlSchemaSimpleTypeRestriction simpleTypeRestriction } } => AddAttributeValues (data, simpleTypeRestriction),
				{ SchemaType: XmlSchemaSimpleType } => data,
				{ AttributeSchemaType: XmlSchemaSimpleType { TypeCode: XmlTypeCode.Boolean } } => AddBooleanAttributeValues (data),
				{ AttributeSchemaType: XmlSchemaSimpleType attributedType } => AddAttributeValues (data, schema, attributedType),
				_ => data
			};

		static XmlSchemaCompletionBuilder AddAttributeValues (XmlSchemaCompletionBuilder data, XmlSchemaSimpleTypeRestriction simpleTypeRestriction)
		{
			foreach (XmlSchemaObject schemaObject in simpleTypeRestriction.Facets) {
				if (schemaObject is XmlSchemaEnumerationFacet enumFacet && enumFacet.Value is string value)
					data.AddAttributeValue (value, enumFacet.Annotation);
			}
			return data;
		}

		static XmlSchemaCompletionBuilder AddAttributeValues (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaSimpleTypeUnion union)
		{
			foreach (XmlSchemaObject schemaObject in union.BaseTypes) {
				if (schemaObject is XmlSchemaSimpleType simpleType)
					AddAttributeValues (data, schema, simpleType);
			}
			return data;
		}

		static XmlSchemaCompletionBuilder AddAttributeValues (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaSimpleType simpleType)
			=> simpleType.Content switch {
				XmlSchemaSimpleTypeRestriction xsstr => AddAttributeValues (data, xsstr),
				XmlSchemaSimpleTypeUnion xsstu => AddAttributeValues (data, schema, xsstu),
				XmlSchemaSimpleTypeList xsstl => AddAttributeValues (data, schema, xsstl),
				_ => data
			};

		static XmlSchemaCompletionBuilder AddAttributeValues (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlSchemaSimpleTypeList list)
			=> list switch {
				{ ItemType: XmlSchemaSimpleType simpleType } => AddAttributeValues (data, schema, simpleType),
				{ ItemTypeName: XmlQualifiedName name } => schema.FindSimpleType (name) is XmlSchemaSimpleType typeFromName ? AddAttributeValues (data, schema, typeFromName) : data,
				_ => data
			};

		/// <summary>
		/// Gets the set of attribute values for an xs:boolean type.
		/// </summary>
		static XmlSchemaCompletionBuilder AddBooleanAttributeValues (XmlSchemaCompletionBuilder data)
			=> data
				.AddAttributeValue ("0")
				.AddAttributeValue ("1")
				.AddAttributeValue ("true")
				.AddAttributeValue ("false");

		/// <summary>
		/// Adds any elements that have the specified substitution group.
		/// </summary>
		static XmlSchemaCompletionBuilder AddSubstitionGroupElements (XmlSchemaCompletionBuilder data, XmlSchema schema, XmlQualifiedName group, string prefix)
		{
			foreach (XmlSchemaElement element in schema.Elements.Values) {
				if (element.SubstitutionGroup == group && element.Name is string name) {
					data.AddElement (name, prefix, element.Annotation);
				}
			}
			return data;
		}

		/// <summary>
		/// Checks that the attribute is prohibited or has been flagged
		/// as prohibited previously. 
		/// </summary>
		class ProhibitedAttributes
		{
			readonly HashSet<XmlQualifiedName> prohibitedAttributes = new ();
			
			public bool IsProhibited (XmlSchemaAttribute attribute)
			{
				if (attribute.Use == XmlSchemaUse.Prohibited) {
					prohibitedAttributes.Add (attribute.QualifiedName);
					return true;
				}
				return prohibitedAttributes.Contains (attribute.QualifiedName);
			}
		}
	}
}