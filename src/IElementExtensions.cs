using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AngleSharp.Dom
{
    public static class IElementExtensions
    {
        public static string InnerHtmlDecoded(this IElement e)
        {
            return WebUtility.HtmlDecode(e.InnerHtml);
        }
    }
}