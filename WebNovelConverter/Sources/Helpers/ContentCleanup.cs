using AngleSharp.Dom;
using System.Linq;

namespace WebNovelConverter.Sources.Helpers
{
    public class ContentCleanup
    {
        private string _baseUrl;

        public ContentCleanup(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public string Execute(IDocument doc, IElement element)
        {
            RemoveEmptyElements(element);
            RemoveMultipleBr(element);

            if (_baseUrl == "http://lnmtl.com")
            {
                AddWhitespaces(element);
                CreateParagraphsLNMTL(doc, element);
            }
            else
            {
                CreateParagraphs(doc, element);
            }

            RemoveEmptyTextNodes(element);
            MakeURLsAbsolute(doc, element);

            return element.InnerHtml;
        }

        private void MakeURLsAbsolute(IDocument doc, IElement element)
        {
            if (_baseUrl != null)
            {
                foreach (IElement el in element.QuerySelectorAll("img"))
                {
                    var src = el.Attributes["src"]?.Value;
                    if (src != null && src.StartsWith("/"))
                    {
                        el.SetAttribute("src", UrlHelper.ToAbsoluteUrl(_baseUrl, src));
                    }
                }
            }
        }

        /// <summary>
        /// Removes useless empty elements
        /// </summary>
        /// <param name="rootElement"></param>
        private void RemoveEmptyElements(IElement rootElement)
        {
            foreach (IElement el in rootElement.QuerySelectorAll("div,span"))
            {
                RemoveEmptyElements(el);

                if (string.IsNullOrWhiteSpace(el.TextContent) && el.ChildElementCount == 0)
                {
                    el.Remove();
                }
            }
        }

        /// <summary>
        /// Adds a whitespace after each (<w>) element (for LNMTL)
        /// </summary>
        /// <param name="rootElement"></param>
        private void AddWhitespaces(IElement rootElement)
        {
            foreach (IElement el in rootElement.QuerySelectorAll("w"))
            {
                AddWhitespaces(el);

                el.TextContent = el.TextContent.PadRight(el.TextContent.Length + 1);
            }
        }

        private void RemoveEmptyTextNodes(IElement element)
        {
            int i = 0;
            while (i < element.ChildNodes.Length)
            {
                if (element.ChildNodes[i].NodeType == NodeType.Text && string.IsNullOrWhiteSpace(element.ChildNodes[i].TextContent))
                {
                    element.RemoveChild(element.ChildNodes[i]);
                    continue;
                }

                // Recursive
                if (element.ChildNodes[i].NodeType == NodeType.Element && element.ChildNodes[i].HasChildNodes)
                    RemoveEmptyTextNodes(element.ChildNodes[i] as IElement);

                i++;
            }
        }

        /// <summary>
        /// Limit sequential &lt;br&gt; to only 2
        /// </summary>
        /// <param name="element"></param>
        private void RemoveMultipleBr(IElement element)
        {
            int i = 2;
            while (i < element.ChildNodes.Length)
            {
                if (element.ChildNodes[i].NodeName == "BR"
                    && element.ChildNodes[i - 1].NodeName == "BR"
                    && element.ChildNodes[i - 2].NodeName == "BR")
                {
                    element.RemoveChild(element.ChildNodes[i]);
                    continue;
                }

                // Recursive
                if (element.ChildNodes[i].NodeType == NodeType.Element && element.ChildNodes[i].ChildNodes.Count(x => x.NodeName == "BR") > 2)
                    RemoveMultipleBr(element.ChildNodes[i] as IElement);

                i++;
            }
        }

        /// <summary>
        /// Converts text separated with multiple &lt;br&gt; into paragraphs with surrounding &lt;p&gt;
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="element"></param>
        private void CreateParagraphs(IDocument doc, IElement element)
        {
            foreach (var child in element.ChildNodes.ToList())
            {
                if (child.NodeType == NodeType.Text && child.NextSibling?.NodeName == "BR" && child.NextSibling?.NextSibling?.NodeName == "BR")
                {
                    var parent = child.ParentElement;

                    // Remove following <BR>
                    parent.RemoveChild(child.NextSibling.NextSibling);
                    parent.RemoveChild(child.NextSibling);

                    // Replace text node with <p>
                    var pEl = ReplaceElementWithParagraph(doc, child);

                    // If following element is also text node, ALSO MAKE IT <p>
                    if (pEl.NextSibling?.NodeType == NodeType.Text)
                    {
                        ReplaceElementWithParagraph(doc, pEl.NextSibling);
                    }
                }

                else if (child.NodeType == NodeType.Element)
                    CreateParagraphs(doc, child as IElement);
            }
        }

        /// <summary>
        /// Inserts a paragraph after each 'SENTENCE' element (for LNMTL)
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="element"></param>
        private void CreateParagraphsLNMTL(IDocument doc, IElement element)
        {
            foreach (var child in element.ChildNodes.ToList())
            {
                if (child.NodeType == NodeType.Element && child.NodeName == "SENTENCE")
                {
                    var parent = child.ParentElement;

                    // Add Node to replace it with a paragraph
                    parent.InsertBefore(child, child);

                    // Replace text node with <p>
                    var pEl = ReplaceElementWithParagraph(doc, child);
                }
            }
        }

        /// <summary>
        /// Replaces an element with &lt;p&gt; element
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private IElement ReplaceElementWithParagraph(IDocument doc, INode element)
        {
            var newEl = doc.CreateElement("P");
            newEl.TextContent = element.TextContent.Trim();
            element.Parent.ReplaceChild(newEl, element);
            return newEl;
        }
    }
}
