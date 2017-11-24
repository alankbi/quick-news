﻿using System;
using System.Net;
using System.IO;

using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

namespace NewsApp
{
    public class ArticleFetcher
    {
        private string apiKey = "7f89b3f5d0ee4aa0aada683a0e2757a8";

        private List<NewsArticle> articles;

        private const int MaxSourcesPerCall = 5;
        private const int NumberOfCalls = 4;

        /**
         * Creates a new instance of the class with a list of news articles,
         * which will be later used to create documents for clustering. Will
         * be changed later on to accept NewsSources as parameter. 
         */
        public ArticleFetcher()
        {
			articles = new List<NewsArticle>();

			NewsSource[] sources = FetchAllSources();

            string[] sourceIds = new string[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                sourceIds[i] = sources[i].id;
            }

            // Randomize sources to pull articles from
			Random r = new Random();
            sourceIds = sourceIds.OrderBy(x => r.Next()).ToArray();

            // How many sources to use
            int totalSources = Math.Min(sourceIds.Length, MaxSourcesPerCall * NumberOfCalls);

            for (int i = 0; i < totalSources; i += MaxSourcesPerCall)
			{
                // Get the next MaxSourcesPerCall sources or however many are remaining
                int sourcesPerCall = Math.Min(MaxSourcesPerCall, sourceIds.Length - i);

				string sourcesAsString = String.Join(",", sourceIds, i, sourcesPerCall);
				articles.AddRange(FetchArticles(sourcesAsString));
			}
        }

        /**
         * Returns the article list pulled from NewsAPI. 
         */
        public List<NewsArticle> GetArticles()
        {
            return articles;
        }

        /**
         * Accepts a string of comma-separated (no space after the comma) source 
         * Ids to be passed in the API call. Returns a List of 10 articles per 
         * source. 
         */
        private List<NewsArticle> FetchArticles(string sources)
        {
            string url = "https://newsapi.org/v2/top-headlines?sources=" + sources + "&apiKey=" + apiKey;
            string json = ExecuteCall(url);

            dynamic response = JObject.Parse(json);

            List<NewsArticle> articles = response.articles.ToObject<List<NewsArticle>>();

            return articles;
        }

        /**
         * Returns all of the english sources from NewsAPI. Eventually this will be
         * cached in a database. 
         */
        private NewsSource[] FetchAllSources() 
        {
			string url = "https://newsapi.org/v2/sources?language=en&apiKey=" + apiKey;
			string json = ExecuteCall(url);

			dynamic response = JObject.Parse(json);

            return response.sources.ToObject<NewsSource[]>();
        }

        /**
         * Executes an API request for the given url. 
         */
        private string ExecuteCall(string url)
        {
            string json = null;
			using (WebClient wc = new WebClient())
			{
                try
                {
                    json = wc.DownloadString(url);
                }
                catch (WebException e)
                {
					using (WebResponse response = e.Response)
					{
						HttpWebResponse httpResponse = (HttpWebResponse)response;
						Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
						using (Stream data = response.GetResponseStream())
						using (var reader = new StreamReader(data))
						{
							string text = reader.ReadToEnd();
							Console.WriteLine(text);
						}
					}
                }

			}
            return json;
        }

    }
}
