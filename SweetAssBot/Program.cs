using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using RedditSharp;

namespace SweetAssBot
{
  public static class Program
  {
    private const string Username = "sweet_ass-bot";

    private static readonly BotSettings Settings = new BotSettings()
    {
      Subreddits = new List<string>
      {
        "funny"
      },
      SubredditMode = SubredditMode.Whitelist,
      PostsPerRun = 5,
      ResponsesPerThread = 5,
      DelayBetweenRuns = TimeSpan.FromSeconds(30)
    };

    public static void Main(string[] args)
    {
      var password = args.FirstOrDefault(); 
      var reddit = Login(password);

      Console.WriteLine("...done!\n");
      Console.Clear();

      RunBot(new Bot(reddit, Settings));
    }

    private static string GetPassword()
    {
      Console.Write("Enter password: ");

      return Console.ReadLine();
    }

    private static Reddit Login(string password)
    {
      var reddit = new Reddit();

      while (string.IsNullOrWhiteSpace(password))
        password = GetPassword();

      do
      {
        try
        {
          Console.Clear();
          Console.Write("Logging in...");
          reddit.LogIn(Username, password);
        }
        catch (AuthenticationException)
        {
          Console.WriteLine("...failed!");
          password = GetPassword();
        }
      } while (reddit.User == null);

      return reddit;
    }

    private static void RunBot(Bot bot)
    {
      var cts = new CancellationTokenSource();
      var token = cts.Token;

      Console.CancelKeyPress += (s, e) =>
      {
        e.Cancel = true;
        cts.Cancel();
      };

      try
      {
        bot.PollAsync(token).Wait();
      }
      catch (AggregateException) { /* Ignored */ }
    }
  }
}
