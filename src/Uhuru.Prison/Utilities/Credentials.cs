// -----------------------------------------------------------------------
// <copyright file="Credentials.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.Prison.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Web.Security;
    
    /// <summary>
    /// This is a helper class that generates credential strings, such as usernames and passwords.
    /// </summary>
    public static class Credentials
    {
        /// <summary>
        /// Generates a credential string with a default length of 12 characters.
        /// </summary>
        /// <returns>A string containing a randomly generated string with length 12.</returns>
        public static string GenerateCredential()
        {
            return GenerateCredential(12);
        }

        /// <summary>
        /// Generates a credential string using the specified length.
        /// </summary>
        /// <param name="length">An int specifying the length of the generated string.</param>
        /// <returns>A string containing a randomly generated string.</returns>
        public static string GenerateCredential(int length)
        {
            // as per msdn (http://msdn.microsoft.com/en-us/library/system.web.security.membership.generatepassword.aspx)
            // the characters that are non-alphanumeric will be replaced with letters/numbers
            Dictionary<char, char> unwantedCharacterMap = new Dictionary<char, char>() 
                                                                    {
                                                                        { '!', '0' }, { '@', '1' }, { '#', '2' }, { '$', '3' },
                                                                        { '%', '4' }, { '^', '5' }, { '&', '6' }, { '*', '7' },
                                                                        { '(', '8' }, { ')', '9' }, { '_', 'a' }, { '-', 'b' },
                                                                        { '+', 'c' }, { '=', 'd' }, { '[', 'e' }, { '{', 'f' },
                                                                        { ']', 'g' }, { '}', 'h' }, { ';', 'i' }, { ':', 'j' },
                                                                        { '<', 'k' }, { '>', 'l' }, { '|', 'm' }, { '.', 'n' },
                                                                        { '/', 'o' }, { '?', 'p' }
                                                                    };

            string credential = Membership.GeneratePassword(length, 0);
            string result = string.Empty;
            for (int i = 0; i < credential.Length; i++)
            {
                if (unwantedCharacterMap.ContainsKey(credential[i]))
                {
                    result += unwantedCharacterMap[credential[i]];
                }
                else
                {
                    result += credential[i];
                }
            }

            return result;
        }

        /// <summary>
        /// Generated a GUID with a cryptographically secure random number generator.
        /// </summary>
        /// <returns>Secure GUID.</returns>
        public static Guid GenerateSecureGuid()
        {
            var secureGuid = new byte[16];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(secureGuid);
            }

            return new Guid(secureGuid);
        }
    }
}
