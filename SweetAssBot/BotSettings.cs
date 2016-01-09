using System;
using System.Collections.Generic;

namespace SweetAssBot
{
  public enum SubredditMode
  {
    Whitelist,
    Blacklist
  }

  public class BotSettings
  {
    private readonly List<string> subreddits = new List<string>();

    public SubredditMode SubredditMode { get; set; } = SubredditMode.Whitelist;

    public List<string> Subreddits
    {
      get { return this.subreddits; }
      set
      {
        this.subreddits.Clear();

        foreach (var subreddit in value)
          this.subreddits.Add(
            !subreddit.StartsWith("/r/")
              ? subreddit.Insert(0, "/r/")
              : subreddit);
      }
    }

    public int PostsPerRun { get; set; } = 25;
    public int ResponsesPerThread { get; set; } = 5;
    public TimeSpan DelayBetweenRuns { get; set; } = TimeSpan.FromSeconds(30);
  }
}