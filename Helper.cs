using System;
using SteamKit2;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OvergameBot
{
  class Helper
  {

    public static void groupChat(SteamFriends.ChatMsgCallback callback, string message,
                      int delay, System.Timers.Timer gTimer, SteamFriends steamFriends,
                      BooleanWrapper busy)
    {
      int dur = read(callback.Message, delay);
      int dur2 = think(delay);
      int dur3 = type(message, delay);
      busy.set(true);
      gTimer.Interval = dur + dur2 + dur3;
      gTimer.Enabled = true;
      Console.WriteLine("Responding in " + (dur + dur2 + dur3) + " ms...");
      System.Timers.Timer delayMessage = new System.Timers.Timer(dur + dur2 + dur3);
      delayMessage.Elapsed += (sender, e) => DelayedMessage(sender, e, callback, message, steamFriends);
      delayMessage.Enabled = true;
    }

    static void DelayedMessage(Object sender, EventArgs c, SteamFriends.ChatMsgCallback callback, 
                               string message, SteamFriends steamFriends)
    {
      steamFriends.SendChatRoomMessage(callback.ChatRoomID, EChatEntryType.ChatMsg, overgameRandomCaps(message, new Random()));
      ((System.Timers.Timer)sender).Dispose();
    }

    public static string overgameRandomCaps(string message, Random rnd)
    {
      int prob = rndProb();
      if (prob < 40)
      {
        return message.ToLower(); //lowercase everything
      }
      else if (prob < 80)
      {
        return message.ToUpper(); //uppercase everything
      }
      return message.First().ToString().ToUpper() + message.ToLower().Substring(1);
      //first capitalized
    }

    public static Random ri()
    {
      return new Random();
    }

    public static int rndProb()
    {

      return ri().Next(100);
    }

    public static string[] tokenize(char[] delims, string message)
    {
      return message.ToLower().Split(delims, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Removes all punctuation at the end of a string.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static string stripPunct(string message)
    {
      while (!Char.IsLetter(message[message.Length - 1]))
      { message = message.Remove(message.Length - 1); }
      return message;
    }

    public static int read(string message, int delay)
    { return (int)(message.Length * .1) * delay; }

    public static int think(int delay) { return 18 * delay; }

    public static int type(string message, int delay)
    { return (int)(message.Length * .4 * delay); }


  }
}
