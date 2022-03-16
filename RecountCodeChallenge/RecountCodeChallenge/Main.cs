using HtmlAgilityPack;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace RecountCodeChallenge
{
	internal class Main
	{
		
		private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		internal static async Task Work(string[] args)
		{
			var rootURL = @"http://localhost/1.html";

			var crawler = new SiteCrawler(rootURL);
			await crawler.Crawl();

			Logger.Warn("Goodbye.");

			
		}
	}
	public class SiteCrawler
	{
		string rootUrl;
		private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public SiteCrawler(string rootUrl)
		{
			this.rootUrl = rootUrl;
		}
		public async Task Crawl()
		{
			List<string> visitedPages = new List<string>();
			List<string> phoneNumbers = new List<string>();
			var pagesToVisit = new ConcurrentQueue<string>();
			var httpClient = new HttpClient();

			pagesToVisit.Enqueue(this.rootUrl);

			Action consumer = async () =>
			{
				string currentUrl;
				while (pagesToVisit.TryDequeue(out currentUrl))
				{
					string html = "";
					html = await httpClient.GetStringAsync(currentUrl); ;
					//var html = await httpClient.GetStringAsync(currentUrl);
					var htmlDoc = new HtmlDocument();
					htmlDoc.LoadHtml(html);
					var links = htmlDoc.DocumentNode.Descendants("a").Select(s => s.GetAttributeValue("href", ""));

					visitedPages.Add(currentUrl);
					phoneNumbers.AddRange(GetPhoneNumbers(html));

					links.Where(s => !visitedPages.Contains(s))
						.ForEach(s => pagesToVisit.Enqueue(s));
				}
			};
			do { Parallel.Invoke(consumer); }
			while (pagesToVisit.Any());

			Logger.Info("FOUND THE FOLLOWING PHONE NUMBERS:");
			phoneNumbers.ForEach(s => Logger.Info(s));
		}

		public static IEnumerable<string> GetPhoneNumbers(string html)
		{
			Regex phoneExpression = new Regex(@"(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]\d{3}[\s.-]\d{4}");
			var matches = phoneExpression.Matches(html).Select(s => s.Value);

			return matches;
		}
	}
}
