// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.DotNet.CodeFormatting.Rules
{
    internal static class NamingUtil
    {
        // NOTE (darren): in the case of x_position, we want to replace with xPosition
        // so we whitelist replaceable {0}_example (hungarian) notations
        private static HashSet<char> kHungarianNotationStarters = new HashSet<char>() {
            'm', // m_example
            's', // s_example
        };

        public static string ConvertToCamelCase(string name) {
            name = name.Trim('_');
            if (name.Length > 2 && char.IsLetter(name[0]) && name[1] == '_' && kHungarianNotationStarters.Contains(name[0]))
            {
                name = name.Substring(2);
            }

            // Some .NET code uses "ts_" prefix for thread static
            if (name.Length > 3 && name.StartsWith("ts_", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(3);
            }

            if (name.Length == 0)
            {
                return name;
            }

            if (name.Length > 2 && char.IsUpper(name[0]) && char.IsLower(name[1]))
            {
                name = char.ToLower(name[0]) + name.Substring(1);
            }

            // Example: TEST_POWER => Test_Power
            // Example: WHY_wouldYouDoThis => Why_wouldYouDoThis
            name = Regex.Replace(name, @"([A-Z])([A-Z]+)", (match) => match.Groups[1].Value + match.Groups[2].Value.ToLower(), RegexOptions.Compiled);

            // Convert from snake_case to camelCase
            // note that the name is trimmed of leading / ending underscores atm
            if (name.Contains("_"))
            {
                name = Regex.Replace(name, @"_(\w)", (match) => match.Groups[1].Value.ToUpper(), RegexOptions.Compiled);
            }

			if (name.Length > 0) {
				// lower-case the first letter
	            name = Regex.Replace(name, @"^\w", (match) => match.Groups[0].Value.ToLower(), RegexOptions.Compiled);
			}

            return name;
        }

        public static string Captialized(this string s) {
            if (s.Length <= 0)
            {
                return s;
            }

            if (char.IsLower(s[0]))
            {
                s = Regex.Replace(s, @"^\w", (match) => match.Groups[0].Value.ToUpper(), RegexOptions.Compiled);
            }

            return s;
        }
    }
}
