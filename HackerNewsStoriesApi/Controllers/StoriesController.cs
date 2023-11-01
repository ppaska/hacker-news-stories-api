using HackerNewsStoriesApi.Models;
using HackerNewsStoriesApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsStoriesApi.Controllers
{
    [ApiController]
    [Route("stories")]
    public class StoriesController : ControllerBase
    {
        private readonly ILogger<StoriesController> _logger;
        private readonly StoriesService _storiesService;
        private readonly int _maxBestStories;

        public StoriesController(ILogger<StoriesController> logger, StoriesService storiesService, IConfiguration configuration)
        {
            _logger = logger;
            _storiesService = storiesService;

            if (!int.TryParse(configuration["maxBestStories"], out _maxBestStories))
            {
                throw new ApplicationException($"[maxBestStories] must be a number");
            }
        }

        [HttpGet("best/{count}")]
        public async Task<ActionResult<StoryItem[]>> GetBestStories(int count)
        {
            if (count < 1 || count > _maxBestStories)
            {
                return BadRequest("Number of best stories must be specified");
            }

            try
            {
                return Ok(await _storiesService.GetBestStories(count));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                return BadRequest("Unexpected application error. Please ");
            }

        }
    }
}
