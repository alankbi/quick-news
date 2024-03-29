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
        private static string apiKey = File.ReadAllText("config.txt");

        private List<NewsSource> newsSources;

        private const int MaxSourcesPerCall = 10;
        private const int NumberOfCalls = 1;

        /**
         * Creates a new instance of the class with a list of news articles,
         * which will be later used to create documents for clustering. Will
         * be changed later on to accept NewsSources as parameter. 
         */
        public ArticleFetcher(List<NewsSource> sources)
        {
            newsSources = sources;
        }

        /**
         * Returns the article list pulled from NewsAPI. 
         */
        public List<NewsArticle> GetArticles()
        {
            var newsArticles = new List<NewsArticle>();
			var sourceIds = new string[newsSources.Count];

			for (int i = 0; i < newsSources.Count; i++)
			{
				sourceIds[i] = newsSources[i].ID;
			}

			// Randomize sources to pull articles from
			var r = new Random();
			sourceIds = sourceIds.OrderBy(x => r.Next()).ToArray();

			// How many sources to use
			int totalSources = Math.Min(sourceIds.Length, MaxSourcesPerCall * NumberOfCalls);

			for (int i = 0; i < totalSources; i += MaxSourcesPerCall)
			{
				// Get the next MaxSourcesPerCall sources or however many are remaining
				int sourcesPerCall = Math.Min(MaxSourcesPerCall, sourceIds.Length - i);

				string sourcesAsString = String.Join(",", sourceIds, i, sourcesPerCall);
				newsArticles.AddRange(FetchArticles(sourcesAsString));
			}

            return newsArticles;
        }

        /**
         * Accepts a string of comma-separated (no space after the comma) source 
         * Ids to be passed in the API call. Returns a List of 10 articles per 
         * source. 
         */
        private List<NewsArticle> FetchArticles(string sources)
        {
            string url = "https://newsapi.org/v2/top-headlines?sources=" + sources;
            string json = ExecuteCall(url);

            dynamic response = JObject.Parse(json);

            return response.articles.ToObject<List<NewsArticle>>();
        }

        /**
         * Returns all of the english sources from NewsAPI. Eventually this will be
         * cached in a database. 
         */
        public static List<NewsSource> GetListOfSources() 
        {
			string url = "https://newsapi.org/v2/sources?language=en";
			string json = ExecuteCall(url);

			dynamic response = JObject.Parse(json);

            return response.sources.ToObject<List<NewsSource>>();
        }

        /**
         * Executes an API request for the given url. 
         */
        public static string ExecuteCall(string url)
        {
            string json = null;
			using (WebClient wc = new WebClient())
			{
                try
                {
                    wc.Headers.Add("x-api-key", apiKey);
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
