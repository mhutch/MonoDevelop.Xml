// 
// LocalOnlyResolver.cs
//  
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2011 Novell, Inc.
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

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Editor.Completion
{
	partial class LocalOnlyXsdResolver : XmlUrlResolver
	{
		readonly ILogger logger;
		readonly string originFile;

		public LocalOnlyXsdResolver (ILogger logger, string originFile)
		{
			this.logger = logger;
			this.originFile = originFile;
		}

		#pragma warning disable CS8764 // annotation appears to be incorrect, docs say returning null is okay
		public override Uri? ResolveUri (Uri? baseUri, string? relativeUri)
		#pragma warning restore CS8764
		{
			var absoluteUri = base.ResolveUri (baseUri, relativeUri);
			if (absoluteUri.IsFile && absoluteUri.LocalPath.EndsWith (".xsd", StringComparison.OrdinalIgnoreCase))
				return absoluteUri;

			LogUrlResolutionBlocked (logger, absoluteUri, originFile);

			return null;
		}

		[LoggerMessage (Level = LogLevel.Trace, Message = "LocalOnlyXmlResolver discarded non-local URI '{absoluteUri}' in file '{originFile}'")]
		static partial void LogUrlResolutionBlocked (ILogger logger, Uri absoluteUri, string originFile);
	}
}