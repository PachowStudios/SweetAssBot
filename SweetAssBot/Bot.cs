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

    private readonly object threadLock = new object();

    private BotSettings Settings { get; }
    private Reddit Reddit { get; }

    private List<Post> ProcessingPosts { get; } 
    private List<Post> ProcessedPosts { get; }
    private List<string> Replies { get; } 
    private Regex PhraseRegex { get; }
    private Regex CommentRegex { get; }

    private CancellationToken CancellationToken { get; set; }

    public Bot(Reddit reddit, BotSettings settings)
    {
      Reddit = reddit;
      Settings = settings;

      ProcessingPosts = new List<Post>();
      ProcessedPosts = new List<Post>();
      Replies = new List<string>();
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

        Console.Clear();
        Console.WriteLine($"Sleeping for {Settings.DelayBetweenRuns}\n");

        await Task.Delay(Settings.DelayBetweenRuns, CancellationToken);

        Console.Clear();
        Console.WriteLine("Waking up...");
      }
    }

    private async Task RunAsync()
    {
      lock (this.threadLock)
      {
        ProcessingPosts.Clear();
        Replies.Clear();
      }

      var posts = await GetPostsAsync();

      await Task.Run(() => Parallel.ForEach(posts, ProcessPost), CancellationToken);
    }

    private async Task<IEnumerable<Post>> GetPostsAsync()
    {
      Console.WriteLine("Retreiving posts...");

      var subreddits = await Task.WhenAll(
        Settings.WhitelistedSubreddits.Select(Reddit.GetSubredditAsync));

      return subreddits
        .OrderBy(s => Guid.NewGuid())
        .SelectMany(s => s.New)
        .Where(p => ProcessedPosts.None(pr => pr.Id == p.Id))
        .Take(Settings.PostsPerRun);
    }

    private void ProcessPost(Post post)
    {
      CheckIsCancelled();
      StartProcessingPost(post);
      GetComments(post).ForEach(ReplyToComment);
      FinishProcessingPost(post);
    }

    private IEnumerable<Comment> GetComments(Post post)
      => post
        .ListComments()
        .Where(c
          => c.Author != Reddit.User.Name
          && !string.IsNullOrEmpty(c.Body)
          && c.Comments.None(sc => sc.Author == Reddit.User.Name)
          && CommentRegex.IsMatch(c.Body))
        .Take(Settings.ResponsesPerPost);

    private void ReplyToComment(Comment comment)
    {
      var context = CommentRegex.Match(comment.Body).Value;
      var phrase = PhraseRegex.Match(context).Value;
      var swappedPhrase = Regex.Replace(phrase, "-ass ", " ass-", RegexOptions.IgnoreCase);
      var sb = new StringBuilder();

      sb.AppendLine($">[{context.Replace(phrase, swappedPhrase)}](https://xkcd.com/37/)");
      sb.AppendLine();
      sb.AppendLine("*****");
      sb.AppendLine(@"I'm a bot. [Check out my source code on GitHub](https://github.com/PachowStudios/SweetAssBot/");

      comment.Reply(sb.ToString());
      Replies.Add($"{comment.Author} => {context}\n");
    }

    private void StartProcessingPost(Post post)
    {
      lock (this.threadLock)
      {
        ProcessingPosts.Add(post);
        PrintStatus();
      }
    }

    private void FinishProcessingPost(Post post)
    {
      lock (this.threadLock)
      {
        ProcessingPosts.Remove(post);
        ProcessedPosts.Add(post);
        PrintStatus();
      }
    }

    private void PrintStatus()
    {
      Console.Clear();
      Console.WriteLine("***** Processing *****");

      foreach (var post in ProcessingPosts)
        Console.WriteLine($"{post.SubredditName} => {post.Title}");

      if (Replies.IsEmpty())
        return;

      Console.WriteLine("***** Replies *****");

      foreach (var reply in Replies)
        Console.WriteLine(reply);
    }

    private bool CheckIsCancelled()
    {
      CancellationToken.ThrowIfCancellationRequested();

      return CancellationToken.IsCancellationRequested;
    }
  }
}