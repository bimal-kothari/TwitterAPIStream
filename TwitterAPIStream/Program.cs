using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using TwitterAPIStream;

var config = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json")
                .Build();
var service = new TwitterService(config);
await service.GetTweetsAsync();
