using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RedditSharp;
using RedditSharp.Things;

namespace SweetAssBot
{
  public class Bot
  {
    private const string PhrasePattern = @"\b\w+-ass\s\b\w+\b";
    private const string CommentPattern = @"(\b[^.?!]*{0}[^.?!]*\.?)";

    private BotSettings Settings { get; }
    private Reddit Reddit { get; }

    private List<string> ProcessedPosts { get; }
    private Regex PhraseRegex { get; }
    private Regex CommentRegex { get; }

    private CancellationToken CancellationToken { get; set; }

    public Bot(Reddit reddit)
      : this(reddit, new BotSettings()) { }

    public Bot(Reddit reddit, BotSettings settings)
    {
      Reddit = reddit;
      Settings = settings;

      ProcessedPosts = new List<string>();
      PhraseRegex = new Regex(PhrasePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
      CommentRegex = new Regex(
        string.Format(CommentPattern, PhrasePattern),
        RegexOptions.IgnoreCase |  RegexOptions.Multiline | RegexOptions.Compiled);
    }

    public async Task PollAsync(CancellationToken cancellationToken)
    {
      CancellationToken = cancellationToken;

      Console.WriteLine("Starting...");

      while (!CheckIsCancelled())
      {
        await RunAsync();

        Console.WriteLine($"Sleeping for {Settings.DelayBetweenRuns}\n");

        await Task.Delay(Settings.DelayBetweenRuns, CancellationToken);

        Console.Clear();
        Console.WriteLine("Waking up...");
      }
    }

    private async Task RunAsync()
      => ProcessPosts(await GetPostsAsync());

    private async Task<IEnumerable<Post>> GetPostsAsync()
    {
      Console.Write("Retreiving posts...");

      var posts = Settings.SubredditMode == SubredditMode.Whitelist
        ? await GetPostsByWhitelistAsync()
        : await GetPostsByBlacklistAsync();

      Console.WriteLine("...done!");

      return posts
        .Take(Settings.PostsPerRun)
        .Where(p => !ProcessedPosts.Contains(p.Id));
    }

    private async Task<IEnumerable<Post>> GetPostsByWhitelistAsync()
      => (await Task.WhenAll(Settings.Subreddits
          .Select(Reddit.GetSubredditAsync)))
        .SelectMany(s => s.New);

    private async Task<IEnumerable<Post>> GetPostsByBlacklistAsync()
      => (await Reddit.GetSubredditAsync("/r/all"))
        .New.Where(p => !Settings.Subreddits.Contains(p.SubredditName));

    private void ProcessPosts(IEnumerable<Post> posts)
    {
      Console.Write("Scanning posts...");

      posts
        .SelectMany(GetTargetComments)
        .ForEach(ReplyToComment);
    }

    private IEnumerable<Comment> GetTargetComments(Post post)
    {
      CheckIsCancelled();

      Console.Write("\r" + new string(' ', Console.BufferWidth - 1));
      Console.Write($"\rScanning: {post.Title}");

      ProcessedPosts.Add(post.Id);

      return post
        .ListComments()
        .Take(Settings.ResponsesPerThread)
        .Where(c
          => c.Author != Reddit.User.Name
          && c.Comments.None(sc => sc.Author == Reddit.User.Name)
          && CommentRegex.IsMatch(c.Body));
    }

    private void ReplyToComment(Comment comment)
    {
      CheckIsCancelled();

      var context = CommentRegex.Match(comment.Body).Value;
      var phrase = PhraseRegex.Match(context).Value;
      var swappedPhrase = Regex.Replace(phrase, @"-ass\s", @"\sass-", RegexOptions.IgnoreCase);
      var sb = new StringBuilder();

      sb.AppendLine($">[{context.Replace(phrase, swappedPhrase)}](https://xkcd.com/37/)");
      sb.AppendLine("*****");
      sb.AppendLine(@"^^(I am a bot. Send hatemail to /u/ryansworld10)");

      comment.Reply(sb.ToString());

      Console.WriteLine($"\nReplied to {comment.Author} => {context}\n");
    }

    private bool CheckIsCancelled()
    {
      CancellationToken.ThrowIfCancellationRequested();

      return CancellationToken.IsCancellationRequested;
    }

  }
}