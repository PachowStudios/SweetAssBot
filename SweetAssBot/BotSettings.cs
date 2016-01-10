using System;
using System.Collections.Generic;

namespace SweetAssBot
{
  public struct BotSettings
  {
    public List<string> WhitelistedSubreddits { get; set; }
    public int PostsPerRun { get; set; }
    public int ResponsesPerPost { get; set; }
    public TimeSpan DelayBetweenRuns { get; set; }

    public BotSettings(
      List<string> whitelistedSubreddits,
      int postsPerRun,
      int responsesPerPost,
      TimeSpan delayBetweenRuns)
    {
      WhitelistedSubreddits = whitelistedSubreddits;
      PostsPerRun = postsPerRun;
      ResponsesPerPost = responsesPerPost;
      DelayBetweenRuns = delayBetweenRuns;
    }
  }
}