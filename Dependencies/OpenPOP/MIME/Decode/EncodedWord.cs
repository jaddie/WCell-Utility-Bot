﻿using System;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenPOP.MIME.Decode
{
    public static class EncodedWord
    {
        /// <summary>
		/// Decode text that is encoded. See BNF below.
		/// This will decode any encoded-word found in the string.
		/// All unencoded parts will not be touched.
		/// 
        /// From http://tools.ietf.org/html/rfc2047:
        /// Generally, an "encoded-word" is a sequence of printable ASCII
        /// characters that begins with "=?", ends with "?=", and has two "?"s in
        /// between.  It specifies a character set and an encoding method, and
        /// also includes the original text encoded as graphic ASCII characters,
        /// according to the rules for that encoding method.
        /// 
        /// BNF:
        /// encoded-word = "=?" charset "?" encoding "?" encoded-text "?="
        /// 
        /// Example:
        /// =?iso-8859-1?q?this=20is=20some=20text?= other text here
		/// </summary>
        /// <see cref="http://tools.ietf.org/html/rfc2047#section-2">RFC Part 2 "Syntax of encoded-words" for more detail</see>
		/// <param name="encodedWords">Source text</param>
		/// <returns>Decoded text</returns>
        public static string Decode(string encodedWords)
		{
            if(encodedWords == null)
                return null;

		    string strRet = encodedWords;

            // This is the regex that should fit the BNF
            // RFC Says that NO WHITESPACE is allowed in this encoding. See RFC for details.
		    const string strRegEx = @"\=\?(?<Charset>\S+?)\?(?<Encoding>\w)\?(?<Content>\S+?)\?\=";
            // \w	Matches any word character including underscore. Equivalent to "[A-Za-z0-9_]".
            // \S	Matches any nonwhite space character. Equivalent to "[^ \f\n\r\t\v]".
            // +?   non-gready equivalent to +
            // (?<NAME>REGEX) is a named group with name NAME and regular expression REGEX

            MatchCollection matches = Regex.Matches(encodedWords, strRegEx);
		    foreach (Match match in matches)
		    {
		        if(match.Success)
		        {
		            string fullMatchValue = match.Value;

                    string encodedText = match.Groups["Content"].Value;
                    string encoding = match.Groups["Encoding"].Value;
                    string charset = match.Groups["Charset"].Value;

                    // Store decoded text here when done
                    string decodedText;

                    // Encoding may also be written in lowercase
                    switch (encoding.ToUpper())
                    {
                        // RFC:
                        // The "B" encoding is identical to the "BASE64" 
                        // encoding defined by RFC 2045.
                        case "B":
                            decodedText = Base64.Decode(encodedText, charset);
                            break;

                        // RFC:
                        // The "Q" encoding is similar to the "Quoted-Printable" content-
                        // transfer-encoding defined in RFC 2045.
                        // There are mo details to this. Please check section 4.2 in
                        // http://tools.ietf.org/html/rfc2047
                        case "Q":
                            try
                            {
                                decodedText = QuotedPrintable.Decode(encodedText, Encoding.GetEncoding(charset), 0);
                            }
                            catch (ArgumentException)
                            {
                                // The encoding we are using is not supported.
                                // Therefore we cannot decode it. We must simply return
                                // the encoded form
                                decodedText = fullMatchValue;
                            }
                            break;

                        default:
                            throw new ArgumentException("The encoding " + encoding + " was not recognized");
                    }

                    // Repalce our encoded value with our decoded value
		            strRet = strRet.Replace(fullMatchValue, decodedText);
		        }
		    }

		    return strRet;
		}
    }
}