﻿using System.Collections.Generic;
using Sprache;
using System;

namespace SpracheJSON
{
    /// <summary>
    /// Contains the entire JSON Parser
    /// </summary>
    static class JSONParser
    {
        /// <summary>
        /// Parses a literal null value
        /// </summary>
        static readonly Parser<JSONLiteral> JNull =
            from str in Parse.IgnoreCase("null")
            select new JSONLiteral(null, LiteralType.Null);

        /// <summary>
        /// Parses a literal boolean value
        /// </summary>
        static readonly Parser<JSONLiteral> JBoolean =
            from str in Parse.IgnoreCase("true").Text().Or(Parse.IgnoreCase("false").Text())
            select new JSONLiteral(str, LiteralType.Boolean);

        /// <summary>
        /// Parses the exponential part of a number
        /// </summary>
        static readonly Parser<string> JExp =
            from e in Parse.IgnoreCase("e").Text()
            from sign in Parse.String("+").Text()
                         .Or(Parse.String("-").Text())
                         .Optional()
            from digits in Parse.Digit.Many().Text()
            select e + ((sign.IsDefined) ? sign.Get() : "") + digits;

        /// <summary>
        /// Parses the decimal part of a number
        /// </summary>
        static readonly Parser<string> JFrac =
            from dot in Parse.String(".").Text()
            from digits in Parse.Digit.Many().Text()
            select dot + digits;

        /// <summary>
        /// Parses the integer part of anumber
        /// </summary>
        static readonly Parser<string> JInt =
            from minus in Parse.String("-").Text().Optional()
            from digits in Parse.Digit.Many().Text()
            select (minus.IsDefined ? minus.Get() : "") + digits;

        /// <summary>
        /// Parses a JSON number
        /// </summary>
        static readonly Parser<JSONLiteral> JNumber =
            from integer in JInt
            from frac in JFrac.Optional()
            from exp in JExp.Optional()
            select new JSONLiteral(integer +
                                   (frac.IsDefined ? frac.Get() : "") +
                                   (exp.IsDefined ? exp.Get() : ""),
                                   LiteralType.Number);

        static List<char> EscapeChars = new List<char> { '\"', '\\', 'b', 'f', 'n', 'r', 't' };
		
		static Parser<U> EnumerateInput<T, U>(T[] input, Func<T, Parser<U>> parser)
		{
			if (input == null || input.Length == 0) throw new ArgumentNullException("input");
			if (parser == null) throw new ArgumentNullException("parser");

			return i =>
			{
				foreach (var inp in input)
				{
					var res = parser(inp)(i);
					if (res.WasSuccessful) return res;
				}

				return Result.Failure<U>(null, null, null);
			};
		}

		/// <summary>
		/// Parses a control char, which is a character preceded by the escape character '\'
		/// </summary>
		static readonly Parser<char> ControlChar =
            from first in Parse.Char('\\')
            from next in EnumerateInput(EscapeChars.ToArray(), c => Parse.Char(c))
            select ((next == 't') ? '\t' :
                    (next == 'r') ? '\r' :
                    (next == 'n') ? '\n' :
                    (next == 'f') ? '\f' :
                    (next == 'b') ? '\b' :
                    next );

        /// <summary>
        /// Parses a JSON character
        /// </summary>
        static readonly Parser<char> JChar = Parse.AnyChar.Except(Parse.Char('"').Or(Parse.Char('\\'))).Or(ControlChar);

        /// <summary>
        /// Parses a JSON string
        /// </summary>
        static readonly Parser<JSONLiteral> JString =
            from first in Parse.Char('"')
            from chars in JChar.Many().Text()
            from last in Parse.Char('"')
            select new JSONLiteral(chars, LiteralType.String);

        /// <summary>
        /// Parses any literal JSON value: string, number, boolean, null
        /// </summary>
        static readonly Parser<JSONLiteral> JLiteral =
            JString
            .XOr(JNumber)
            .XOr(JBoolean)
            .XOr(JNull);

        /// <summary>
        /// Parses any JSON value
        /// </summary>
        static readonly Parser<IJSONValue> JValue =
            Parse.Ref(() => JObject)
            .Or(Parse.Ref(() => JArray))
            .Or(JLiteral);

        /// <summary>
        /// Parses the elements within a JSON array
        /// </summary>
        static readonly Parser<IEnumerable<IJSONValue>> JElements = JValue.DelimitedBy(Parse.Char(',').Token());

        /// <summary>
        /// Parses a JSON array
        /// </summary>
        static readonly Parser<IJSONValue> JArray =
            from first in Parse.Char('[').Token()
            from elements in JElements.Optional()
            from last in Parse.Char(']').Token()
            select new JSONArray(elements.IsDefined ? elements.Get() : null);

        /// <summary>
        /// Parses a JSON pair
        /// </summary>
        static readonly Parser<KeyValuePair<string, IJSONValue>> JPair =
            from name in JString
            from colon in Parse.Char(':').Token()
            from val in JValue
            select new KeyValuePair<string, IJSONValue>(name.Value, val);

        /// <summary>
        /// Parses all the pairs (members) of a JSON object
        /// </summary>
        static readonly Parser<IEnumerable<KeyValuePair<string, IJSONValue>>> JMembers = JPair.DelimitedBy(Parse.Char(',').Token());

        /// <summary>
        /// Parses a JSON object
        /// </summary>
        static readonly Parser<IJSONValue> JObject =
            from first in Parse.Char('{').Token()
            from members in JMembers.Optional()
            from last in Parse.Char('}').Token()
            select new JSONObject(members.IsDefined ? members.Get() : null);

        /// <summary>
        /// Parses a JObject
        /// </summary>
        /// <param name="toParse">The text to parse</param>
        /// <returns>A IJSONValue cast as a JSONObject</returns>
        public static JSONObject ParseJSON(string toParse)
        {
            return (JSONObject)JObject.Parse(toParse);
        }
    }
}
