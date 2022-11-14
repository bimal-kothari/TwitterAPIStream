using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitterAPIStream
{
    internal class TwitterService
    {
        private readonly IConfiguration _configuration;

        public TwitterService(IConfiguration config)
        {
            _configuration = config;
        }

        public async Task GetTweetsAsync()
        {
            using (var client = new HttpClient())
            {
                var token = _configuration.GetSection("Token").Value;
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                client.Timeout = TimeSpan.FromMinutes(10);
                var hashTags = new Dictionary<string, int>();
                var topHashTags = new List<string>();
                int totalTweets = 0;
               
                var url = _configuration.GetSection("Url").Value;
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var stream = await response.Content.ReadAsStreamAsync();
                    using (var streamReader = new StreamReader(stream))
                    {
                        var line = streamReader.ReadLine();
                        while (line != null)
                        {
                            totalTweets++;

                            var myTweetData = JsonConvert.DeserializeObject<Tweet>(line);
                            ProcessTweet(myTweetData, hashTags, topHashTags);

                            // write statistics after each 100 tweets
                            if (totalTweets % 100 == 0)
                            {
                                WriteStatistics(totalTweets, topHashTags);
                            }

                            line = streamReader.ReadLine();
                        }
                    }
                }
            }
        }

        private void ProcessTweet(Tweet myTweetData, Dictionary<string, int> hashTags, List<string> topHashTags)
        {
            if (myTweetData != null && myTweetData.Data != null && myTweetData.Data.Entity != null && myTweetData.Data.Entity.Hashtags != null)
            {
                foreach (var hashTag in myTweetData.Data.Entity.Hashtags)
                {
                    if (hashTags.ContainsKey(Convert.ToString(hashTag.Tag)))
                    {
                        hashTags[hashTag.Tag]++;
                    }
                    else
                    {
                        hashTags.Add(hashTag.Tag, 1);
                    }

                    if (!topHashTags.Contains(hashTag.Tag))
                    {
                        if (topHashTags.Count == 10)
                        {
                            // find min in topHashTags
                            int minCount = hashTags[topHashTags[0]];
                            int minIndex = 0;
                            for (int i = 1; i < topHashTags.Count; i++)
                            {
                                var topHashTag = topHashTags[i];
                                if (hashTags[topHashTag] < minCount)
                                {
                                    minCount = hashTags[topHashTag];
                                    minIndex = i;
                                }
                            }

                            // store new hashtag in topHashTags
                            if (hashTags[hashTag.Tag] > minCount)
                            {
                                topHashTags[minIndex] = hashTag.Tag;
                            }
                        }
                        else
                        {
                            topHashTags.Add(hashTag.Tag);
                        }
                    }
                }
            }
        }

        private void WriteStatistics(int totalTweets, List<string> topHashTags)
        {
            Console.WriteLine($"Total number of tweets received: {totalTweets}");
            Console.WriteLine("Top 10 Hashtags:");
            foreach (var topHashTag in topHashTags)
            {
                Console.WriteLine(topHashTag);
            }
        }
    }
}
