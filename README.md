# hacker-news-stories-api

An API which returns an array of the best n stories from the Hacker News API in descending order based on score.

There are following parameters to specify:

``` json
  "maxBestStories": "200",
  "maxDegreeOfParallelism": "20",
  "maxCacheTimeInSecs": "120"
}
```

 - *maxBestStories* - a maximimum stories cound that could be queries
 - *maxDegreeOfParallelism* - a maximum number of conurent calls when fetching details
 - *maxCacheTimeInSecs* - Time for cache life in seconds.

## Example

Get top 10 best stories

```
https://hacker-news-stories-api.azurewebsites.net/stories/best/10
```

## Resources

A simple client can be found here:  [https://run.worksheet.systems/app/Pavlo/HackerNewsStories](https://run.worksheet.systems/app/Pavlo/HackerNewsStories)

Client source code can be found here can be found here:  [https://run.worksheet.systems/data-studio/app/Pavlo/HackerNewsStories?file=home!index.dcml](https://run.worksheet.systems/data-studio/app/Pavlo/HackerNewsStories?file=home!index.dcml)

Server deployed # [https://hacker-news-stories-api.azurewebsites.net/swagger](https://hacker-news-stories-api.azurewebsites.net/swagger/index.html)
