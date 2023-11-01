using HackerNewsStoriesApi.Models;
using HackerNewsStoriesApi.Utils;
using System.Text.Json;

namespace HackerNewsStoriesApi.Services
{
    public class StoriesService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly TimedLock _lock = new TimedLock();

        private static IList<BestStoryItem>? _stories;
        private static DateTime _fetchTime = DateTime.Now;


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

        public async Task<IEnumerable<BestStoryItem>> GetBestStories(int number)
        {
            if (_stories == null || (DateTime.Now - _fetchTime).TotalSeconds > MaxCacheTimeInSecs)
            {
                using (_lock.Lock(TimeSpan.FromSeconds(MaxCacheTimeInSecs * 2)))
                {
                    if (_stories == null || (DateTime.Now - _fetchTime).TotalSeconds > MaxCacheTimeInSecs)
                    {
                        _stories = await FetchAllBestStories();
                        _fetchTime = DateTime.Now;
                    }
                }
            }

            return _stories.Take(number);
        }

        private async Task<IList<BestStoryItem>> FetchAllBestStories()
        {
            using var response = await _httpClient.GetAsync("https://hacker-news.firebaseio.com/v0/beststories.json");
            response.EnsureSuccessStatusCode();
            var storiesIds = JsonSerializer.Deserialize<int[]>(await response.Content.ReadAsStringAsync());

            var bestStories = new List<BestStoryItem>(storiesIds.Length);

            await Parallel.ForEachAsync(storiesIds, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
                async (storyId, c) => bestStories.Add(await GetStory(storyId)));


            bestStories.Sort((x, y) => y.Score.CompareTo(x.Score));

            return bestStories;
        }

        private DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        private async Task<BestStoryItem> GetStory(int storyId)
        {
            using var response = await _httpClient.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{storyId}.json");
            response.EnsureSuccessStatusCode();
            var story = JsonSerializer.Deserialize<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());

            return new BestStoryItem
            {
                Title = story["title"]?.ToString(),
                PostedBy = story["by"]?.ToString(),
                CommentsCount = int.Parse(story["descendants"].ToString()),
                Score = int.Parse(story["score"].ToString()),
                Uri = story.ContainsKey("url") ? story["url"].ToString() : null,
                Time = UnixTimeStampToDateTime(double.Parse(story["time"].ToString()))
            };

        }
    }
}
