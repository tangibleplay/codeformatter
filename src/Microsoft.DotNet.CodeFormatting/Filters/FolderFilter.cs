// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.CodeFormatting.Filters
{
    [Export(typeof(IFormattingFilter))]
    internal sealed class FolderFilter : IFormattingFilter
    {
        private readonly Options _options;

        [ImportingConstructor]
        public FolderFilter(Options options)
        {
            _options = options;
        }

        public bool ShouldBeProcessed(Document document)
        {
            var folders = _options.Folders;
            if (folders.IsDefaultOrEmpty)
            {
                return true;
            }

            string directoryPath = Path.GetDirectoryName(document.FilePath);
			string[] directories = directoryPath.Split(Path.DirectorySeparatorChar);
            foreach (var folder in folders)
            {
				// any directory == folder case-insensitive
				if (directories.Any(d => d.Equals(folder, StringComparison.OrdinalIgnoreCase)))
				{
					return false;
				}
            }

            return true;
        }
    }
}
