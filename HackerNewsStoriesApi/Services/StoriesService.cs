using HackerNewsStoriesApi.Models;
using HackerNewsStoriesApi.Utils;
using System.Text.Json;

namespace HackerNewsStoriesApi.Services
{
    public class StoriesService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly TimedLock _lock = new TimedLock();

        private static IList<StoryItem>? Stories;
        private static DateTime FetchTime = DateTime.Now;

        private readonly int MaxDegreeOfParallelism;
        private readonly int MaxCacheTimeInSecs;

        public StoriesService(IConfiguration configuration)
        {
            if (!int.TryParse(configuration["maxDegreeOfParallelism"], out MaxDegreeOfParallelism))
            {
                throw new ApplicationException($"[maxDegreeOfParallelism] must be a number");
            }

            if (!int.TryParse(configuration["maxCacheTimeInSecs"], out MaxCacheTimeInSecs))
            {
                throw new ApplicationException($"[maxCacheTimeInSecs] must be a number");
            }
        }

        public async Task<IEnumerable<StoryItem>> GetBestStories(int number)
        {
            // check if we have an up to date stories cached. Otherwise. Fetch all
            if (Stories == null || (DateTime.Now - FetchTime).TotalSeconds > MaxCacheTimeInSecs)
            {
                // ensure concurent calls handled correctly
                using (await _lock.Lock(TimeSpan.FromSeconds(MaxCacheTimeInSecs * 2)))
                {
                    if (Stories == null || (DateTime.Now - FetchTime).TotalSeconds > MaxCacheTimeInSecs)
                    {
                        Stories = await FetchAllBestStories();
                        FetchTime = DateTime.Now;
                    }
                }
            }

            return Stories.Take(number);
        }

        private async Task<IList<StoryItem>> FetchAllBestStories()
        {
            // get list of stories
            using var response = await _httpClient.GetAsync("https://hacker-news.firebaseio.com/v0/beststories.json");
            response.EnsureSuccessStatusCode();
            var storiesIds = JsonSerializer.Deserialize<int[]>(await response.Content.ReadAsStringAsync());

            var bestStories = new List<StoryItem>(storiesIds.Length);

            // get story details parallely with limits 
            await Parallel.ForEachAsync(
                storiesIds,
                new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
                async (storyId, c) => bestStories.Add(await GetStory(storyId))
            );

            // sort descending by score
            bestStories.Sort((x, y) => y.Score.CompareTo(x.Score));

            return bestStories;
        }

        private DateTime FromUnixTimeStamp(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        private async Task<StoryItem> GetStory(int storyId)
        {
            using var response = await _httpClient.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{storyId}.json");
            response.EnsureSuccessStatusCode();
            var story = JsonSerializer.Deserialize<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());

            return new StoryItem
            {
                Title = story["title"]?.ToString(),
                PostedBy = story["by"]?.ToString(),
                CommentsCount = int.Parse(story["descendants"].ToString()),
                Score = int.Parse(story["score"].ToString()),
                Uri = story.ContainsKey("url") ? story["url"].ToString() : null,
                Time = FromUnixTimeStamp(double.Parse(story["time"].ToString()))
            };

        }
    }
}
