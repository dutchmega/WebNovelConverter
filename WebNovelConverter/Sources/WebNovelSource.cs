﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using WebNovelConverter.Sources.Models;
using System.IO;
using WebNovelConverter.Sources.Helpers;
using System.Reflection;

namespace WebNovelConverter.Sources
{
    public abstract class WebNovelSource
    {
        public enum Mode
        {
            TableOfContents,
            NextChapterLink
        }

        public abstract List<Mode> AvailableModes { get; }

        protected static readonly ChapterLink[] EmptyLinks = new ChapterLink[0];

        public virtual string BaseUrl { get; protected set; }

        public string SourceName { get; private set; }

        protected readonly HtmlParser Parser = new HtmlParser();

        protected WebNovelSource(string sourceName)
        {
            SourceName = sourceName;
        }

        public virtual Task<IEnumerable<ChapterLink>> GetChapterLinksAsync(string baseUrl, CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual Task<WebNovelChapter> GetChapterAsync(ChapterLink link, ChapterRetrievalOptions options = default(ChapterRetrievalOptions), CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual Task<WebNovelInfo> GetNovelInfoAsync(string baseUrl, CancellationToken token = default(CancellationToken))
        {
            return Task.FromResult((WebNovelInfo)null);
        }

        protected virtual IEnumerable<ChapterLink> CollectChapterLinks(string baseUrl, IEnumerable<IElement> linkElements,
            Func<IElement, bool> linkFilter = null)
        {
            if (linkFilter != null)
                linkElements = linkElements.Where(linkFilter);

            linkElements = linkElements.Where(p => p.LocalName == "a");

            foreach (IElement e in linkElements)
            {
                if (string.IsNullOrWhiteSpace(e.TextContent) || !e.HasAttribute("href"))
                    continue;

                // Skip social media share links
                if (e.HasAttribute("class") && e.GetAttribute("class").Contains("share-icon"))
                    continue;

                bool otherDomain = UrlHelper.IsOtherDomain(e.GetAttribute("href"), baseUrl);
                string url = UrlHelper.ToAbsoluteUrl(baseUrl, e.GetAttribute("href"));

                if (string.IsNullOrEmpty(url))
                    continue;

                ChapterLink link = new ChapterLink
                {
                    Name = e.GetInnerText(),
                    Url = url,
                    Unknown = otherDomain
                };

                yield return link;
            }
        }

        protected async Task<string> GetWebPageAsync(string url, CancellationToken token = default(CancellationToken))
        {
            if(url.StartsWith("file://"))
            {
                var path = Path.GetFullPath(Uri.UnescapeDataString(new Uri(url).AbsolutePath));
                if (path.StartsWith(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
                    return File.ReadAllText(path);
                else
                    throw new Exception("Path not allowed");
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WebNovelConverter", "1.0"));
                
                var resp = await client.GetAsync(url, token);
                resp.EnsureSuccessStatusCode();

                byte[] content = await resp.Content.ReadAsByteArrayAsync();

                return Encoding.UTF8.GetString(content, 0, content.Length);
            }
        }

        public override string ToString()
        {
            return SourceName;
        }
    }
}
