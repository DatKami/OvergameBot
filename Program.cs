using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using System.IO;
using System.Threading;

namespace OvergameBot
{
  class Program
  {
    static readonly List<string> engDict = new List<string>(File.ReadAllLines("allwords_lc.txt")); //build a list to detect non english words

    static readonly List<string> overStrings = new List<string>(File.ReadAllLines("overgame.txt")); // build known overgame phrases

    static Dictionary<string, string> steamIDs = new Dictionary<string, string>();

    //list of chatrooms overgame has visited in this run.
    //this may be errorprone as there is no way to determine if a bot gets kicked from chat.
    static List<SteamID> chatrooms = new List<SteamID>();

    static List<string> untouchStrings = new List<string>();
    static List<string> heuntonStrings = new List<string>();
    static List<string> cwfStrings = new List<string>();

    static readonly string[] unnotable = new string[] { "kami", "wayne", "trog", "erarg", "mira", "anita" };
    static readonly string[] notable = new string[] { "moupi", "untouch",
    "doc", "doc gelegentlich", "heunton", "moup", "boris", "garret", "damros", "sjws", "emagravo" };
    static readonly string[] exceptions = new string[] { "i", "overgame", "cwf", "laugh" };

    static readonly char[] delim = { ' ', '.', ',', ':', '\t', '*' };

    static string[] phrases = { };
    
    static int dictLen = 58110;

    static System.Timers.Timer aTimer = new System.Timers.Timer(6000);
    static System.Timers.Timer banTimer = new System.Timers.Timer(120000);

    static string user, pass, guard;

    static SteamClient steamClient;
    static CallbackManager manager;
    static SteamUser steamUser;
    static SteamFriends steamFriends;

    static int frantic = 75;
    static int ignore = 0;

    static bool kickable = false;

    static bool isRunning;
    static Random rnd = new Random();

    static bool busy = false;


    // ======================== HELPER FUNCTIONS ========================

    public static int rndProb()
    {
      return rnd.Next(100);
    }

    static void groupChat(SteamFriends.ChatMsgCallback callback, string message)
    {
      int dur = read(callback.Message);
      int dur2 = think();
      int dur3 = type(message);
      busy = true;
      aTimer.Interval = dur + dur2 + dur3;
      aTimer.Enabled = true;
      Console.WriteLine("Responding in " + (dur + dur2 + dur3) + " ms...");
      System.Timers.Timer delayMessage = new System.Timers.Timer(dur + dur2 + dur3);
      delayMessage.Elapsed += (sender, e) => DelayedMessage(sender, e, callback, message);
      delayMessage.Enabled = true;
    }

    public static string[] tokenize(string message)
    {
      return message.ToLower().Split(delim, StringSplitOptions.RemoveEmptyEntries);
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

    public static int read(string message)
    { return (int)(message.Length * .1) * frantic; }

    public static int think() { return 18 * frantic; }

    public static int type(string message)
    { return (int)(message.Length * .4 * frantic); }

    public static void randomizePhrases()
    {
      phrases = new string[] { exclaim("toppermost of the poppermost", "!"), 
                               exclaim("Damngod..", "."),
                               exclaim("where " + overgameFriends(), "?"), 
                               exclaim("doc make me cwf", "!"),
                               exclaim(" HELLPPP!!! THAT IMPERSON ME", "!"), 
                               exclaim("i sick of overclone", "!"), 
                               exclaim("no", "!"),

                               exclaim(engDict[rnd.Next(dictLen)] + " " +
                               engDict[rnd.Next(dictLen)] + " " +
                               engDict[rnd.Next(dictLen)], "!")
                             };
    }

    // ======================== BOT INITIALIZATION ROUTINE ========================

    static void Main(string[] args)
    {

      Console.Title = "OvergameBot";
      Console.WriteLine("CTRL+C bully me!");

      getCredentials();

      //enumerate steamIDs
      steamIDs.Add("STEAM_0:1:29195580", "untouch");
      steamIDs.Add("STEAM_0:0:63212978", "overgame");

      foreach (string st in overStrings)
      {
        string s = st.ToLower();
        if (s.Contains("untouch")) untouchStrings.Add(s); 
        if (s.Contains("heu") || s.Contains("hue")) heuntonStrings.Add(s); 
        if (s.Contains("cwf")) cwfStrings.Add(s);
      }

      banTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnBanUp);
      banTimer.Enabled = true;

      aTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerUp);
      aTimer.Enabled = true;

      SteamLogin();
    }

    static void getCredentials()
    {
      Console.Write("I what username!: "); user = Console.ReadLine();
      Console.Write("I what password!: "); pass = "";
      ConsoleKeyInfo info = Console.ReadKey(true);
      while (info.Key != ConsoleKey.Enter)
      {
        if (info.Key != ConsoleKey.Backspace)
        { pass += info.KeyChar; info = Console.ReadKey(true); }
        else if (info.Key == ConsoleKey.Backspace)
        {
          if (!string.IsNullOrEmpty(pass))
          { pass = pass.Substring(0, pass.Length - 1); }
          info = Console.ReadKey(true);
        }
      }
      for (int i = 0; i < pass.Length; i++) { Console.Write("*"); }
      Console.Write("\n");
    }

    // ======================== BOT LIFE ROUTINE ========================

    static void SteamLogin()
    {
      steamClient = new SteamClient();
      manager = new CallbackManager(steamClient);

      steamUser = steamClient.GetHandler<SteamUser>();
      steamFriends = steamClient.GetHandler<SteamFriends>();

      new Callback<SteamClient.ConnectedCallback>(OnConnected, manager);
      new Callback<SteamUser.AccountInfoCallback>(OnAccountInfo, manager);


      new Callback<SteamUser.LoggedOnCallback>(OnLoggedOn, manager);
      new Callback<SteamUser.LoggedOffCallback>(OnLoggedOff, manager);

      new Callback<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth, manager);
      new Callback<SteamClient.DisconnectedCallback>(OnDisconnected, manager);

      //new Callback<SteamFriends.FriendMsgCallback>(OnFriendMessage, manager);
      new Callback<SteamFriends.ChatMsgCallback>(OnChatMessage, manager);
      new Callback<SteamFriends.ChatInviteCallback>(OnChatInvite, manager);

      //new Callback<SteamFriends.FriendAddedCallback>(OnFriendInvite, manager);
      new Callback<SteamFriends.FriendsListCallback>(OnFriendInvite, manager);

      new Callback<SteamFriends.ChatEnterCallback>(OnJoinChat, manager);

      steamClient.Connect();


      isRunning = true;

      while (isRunning)
      {
        manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
      }
      Console.ReadKey(); //exit the program with any key
    }

    // ======================== TIMER ROUTINES ========================

    static void OnTimerUp(object sender, EventArgs c)
    {
      busy = false;
      aTimer.Enabled = false;
      Console.WriteLine("Now accepting requests.");
    }

    static void OnBanUp(object sender, EventArgs c)
    {
      kickable = true;
      banTimer.Enabled = false;
      Console.WriteLine("Now bannable.");
    }

    static void DelayedMessage(Object sender, EventArgs c, SteamFriends.ChatMsgCallback callback, string message)
    {
      steamFriends.SendChatRoomMessage(callback.ChatRoomID, EChatEntryType.ChatMsg, overgameRandomCaps(message));
      ((System.Timers.Timer)sender).Dispose();
    }

    static void resetBanTimer()
    {
      banTimer.Interval = 120000;
      banTimer.Enabled = true;
    }

    // ======================== CALLBACK ROUTINES ========================

    static void OnConnected(SteamClient.ConnectedCallback callback)
    {
      if (callback.Result != EResult.OK)
      {
        Console.WriteLine("You wrong! What is {0}???", callback.Result);
        isRunning = false;
        return;
      }

      Console.WriteLine("*i aim to steam* \nI hack {0}...", user);

      byte[] sentryhash = null;

      if (File.Exists("sentry.bin"))
      {
        byte[] sentryFile = File.ReadAllBytes("sentry.bin");
        sentryhash = CryptoHelper.SHAHash(sentryFile);
      }

      steamUser.LogOn(new SteamUser.LogOnDetails
      {
        Username = user,
        Password = pass,
        AuthCode = guard,
        SentryFileHash = sentryhash,

      });
    }

    static void OnLoggedOn(SteamUser.LoggedOnCallback callback)
    {
      if (callback.Result == EResult.AccountLogonDenied)
      {
        Console.WriteLine("steam guard bully me!!!");
        Console.Write("You have steam guard code???: ");
        guard = Console.ReadLine();
      }
      if (callback.Result != EResult.OK)
      {
        Console.WriteLine("You wrong! What {0}???", callback.Result);
        isRunning = false;  return;
      }
      Console.WriteLine("{0} hacked!!!", user);
    }

    static void OnDisconnected(SteamClient.DisconnectedCallback callback)
    {
      Console.WriteLine("{0} not connect... I try again...", user);
      Thread.Sleep(TimeSpan.FromSeconds(5));

      steamClient.Connect();
    }

    static void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback)
    {
      Console.WriteLine("I sentry...");

      byte[] sentryhash = CryptoHelper.SHAHash(callback.Data);

      File.WriteAllBytes("sentry.bin", callback.Data);

      steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
      {
        JobID = callback.JobID,
        FileName = callback.FileName,
        BytesWritten = callback.BytesToWrite,
        FileSize = callback.Data.Length,
        Offset = callback.Offset,
        Result = EResult.OK,
        LastError = 0,
        OneTimePassword = callback.OneTimePassword,
        SentryFileHash = sentryhash,
      });

      Console.WriteLine("Sentried!!!");
    }

    static void OnLoggedOff(SteamUser.LoggedOffCallback callback)
    {
      Console.WriteLine("I done hack {0}", user);
    }

    static void OnAccountInfo(SteamUser.AccountInfoCallback callback)
    {
      steamFriends.SetPersonaState(EPersonaState.Online);
    }

    static void OnChatInvite(SteamFriends.ChatInviteCallback callback)
    { steamFriends.JoinChat(callback.ChatRoomID); }

    static void OnFriendInvite(SteamFriends.FriendsListCallback callback)
    {
      Thread.Sleep(2500); // wait for the list to update properly
      foreach (var friend in callback.FriendList)
      {
        if (friend.Relationship == EFriendRelationship.RequestRecipient)
        { steamFriends.AddFriend(friend.SteamID); }
        if (friend.Relationship == EFriendRelationship.IgnoredFriend)
        { steamFriends.IgnoreFriend(friend.SteamID, false); }
      }
    }

    static void OnJoinChat(SteamFriends.ChatEnterCallback callback)
    {  
      //commit the chatroom to the chatrooms list
      if(!chatrooms.Contains(callback.ChatID)) chatrooms.Add(callback.ChatID); 

      int prob = rndProb();
      if (prob < 20)
      {
        steamFriends.SendChatRoomMessage(callback.ChatID, EChatEntryType.ChatMsg, exclaim("IM NOT BOT", "!"));
      }
      if (prob < 40)
      {
        steamFriends.SendChatRoomMessage(callback.ChatID, EChatEntryType.ChatMsg, exclaim("im not bot..", "."));
      }
      else if (prob < 60)
      {
        steamFriends.SendChatRoomMessage(callback.ChatID, EChatEntryType.ChatMsg, "hello.");
        Thread.Sleep(1500);
        steamFriends.SendChatRoomMessage(callback.ChatID, EChatEntryType.ChatMsg, "im back.");
      }
      else if (prob < 80)
      {
        steamFriends.SendChatRoomMessage(callback.ChatID, EChatEntryType.ChatMsg, exclaim("hello..","."));
        Thread.Sleep(1500);
        steamFriends.SendChatRoomMessage(callback.ChatID, EChatEntryType.ChatMsg, exclaim("im back..","."));
      }
      else
      {
        steamFriends.SendChatRoomMessage(callback.ChatID, EChatEntryType.ChatMsg, "good moring.");
      }
    }

    // ======================== MAIN CHAT ROUTINE ========================

    static void OnChatMessage(SteamFriends.ChatMsgCallback callback)
    {
      SteamID id = callback.ChatterID;
      string idstr = id.ToString();
      string chatter = steamFriends.GetFriendPersonaName(id);

      Console.WriteLine(chatter + "(" + idstr + "): " + callback.Message);
      if (busy) { Console.WriteLine("Busy."); }

      string lower = callback.Message.ToLower();
      if (callback.ChatMsgType == EChatEntryType.ChatMsg)
      {
        if (lower.Length > 1)
        {
          if (lower.Contains("!echo "))//echo a message to all other rooms
          {
            foreach (SteamID roomID in chatrooms)
            {
              if (roomID != callback.ChatRoomID) //do not echo to the same room the command is typed in
                steamFriends.SendChatRoomMessage(roomID, EChatEntryType.ChatMsg, callback.Message.Substring(6));
            }
          }
          else if (lower.Contains("*i set ignore percent to"))
          {
            string val = lower.Substring(25);
            string val2 = "";
            int count = 0;
            foreach (char c in val)
            {
              if (count >= 5) { break; }
              if (Char.IsDigit(c))
              { val2 = val2 + c; ++count; }
            }
            int i = -1;
            try { i = int.Parse(val2); }
            catch (FormatException)
            {
              groupChat(callback, exclaim("you wrong!!! that number", "?"));
              return;
            }
            if (i >= 0)
            {
              ignore = i;
              groupChat(callback, exclaim("get " + i + "% of shit out", "!"));
            }
            else groupChat(callback, exclaim("you wrong", "!"));

          }
          else if (lower.Contains("*i set overgame speed to"))
          {
            string val = lower.Substring(25);
            string val2 = "";
            foreach (char c in val)
            {
              if (Char.IsDigit(c))
              { val2 = val2 + c; }
            }

            int i = -1;
            try { i = int.Parse(val2); }
            catch (FormatException)
            {
              groupChat(callback, exclaim("you wrong!!! that number", "?"));
              return;
            }
            if (i <= 0) groupChat(callback, exclaim("too fast", "!"));
            else if (i > 500) groupChat(callback, exclaim("you wrong", "!"));
            else
            {
              frantic = i;
              groupChat(callback, exclaim("i talk " + i + "% original delay", "!"));
            }
          }
          else if (callback.Message.Contains("*i set overgame name to"))
          {
            string val = callback.Message.Substring(24);
            string val2 = "";
            foreach (char c in val)
            {
              if (c != '*')
              { val2 = val2 + c; }
            }
            steamFriends.SetPersonaName(val2);
          }
          else if (callback.Message.Contains("*you next say") && callback.Message.Length > 13)
          {
            string val = callback.Message.Substring(13);
            while (val.Remove(1) == " ") val = val.Substring(1);
            string val2 = "";
            foreach (char c in val)
            {
              if (c != '*' && c != '\"' && c != '!')
              { val2 = val2 + c; }
            }
            Thread.Sleep(2500);
            steamFriends.SendChatRoomMessage(callback.ChatRoomID, EChatEntryType.ChatMsg,
                        exclaim(stripPunct(overgameRandomCaps(val2)), "."));
            if (rndProb() < 50) groupChat(callback, exclaim("HEY", "!"));
            else groupChat(callback, exclaim("WHAT", "!"));
          }
          else if (busy) return;
          else if (rndProb() < ignore) return; // random prob in lieu of wait ignore
          else if (lower.Remove(1) == "*") //act
          {
            string[] words = tokenize(lower);
            List<string> notValid = new List<string>(new string[] { });
            Console.Write("Analyzing words...: ");
            foreach (string s in words)
            {
              switch (s)
              {
                case "kick":
                case "ban": overgameThreaten(callback, s); return;
                case "aim": groupChat(callback, overgameReact(callback)); return;
                case "chicken":
                case "chi":
                case "chicarrot":
                case "bully":
                  groupChat(callback, overgameUndo(s)); return;
                default:
                  Console.Write(s);
                  if (!engDict.Contains(s))
                  { notValid.Add(s); Console.Write("(!)"); }
                  Console.Write(" "); break;
              }
            }
            Console.WriteLine(""); notValid = dictException(notValid);
            Console.Write("Words to examine: ");
            foreach (string s in notValid) { Console.Write(s + " "); }
            Console.WriteLine("");

            if (notValid.Count >= 1)
            // if there is at least one non english word, overgame will think it's
            // bad and demand the reverse.
            { groupChat(callback, overgameUndo(notValid[rnd.Next(notValid.Count)])); }
            else { groupChat(callback, overgameIdle()); }
          }
          else if (lower.Contains("csgogun")) { groupChat(callback, overgameReact(callback)); }
          else if (lower.Contains("slugpoo")) { groupChat(callback, exclaim("HEY", "!")); }
          else if (lower.Contains("chicken")) { groupChat(callback, overgameReact(callback)); }
          else if (lower.Contains("carrot")) { groupChat(callback, overgameReact(callback)); }
          else if (lower.Contains("brumpert")) { groupChat(callback, overgameReact(callback)); }
          else if (lower.Contains("%")) { spamNO(callback, 300); }
          else if (lower.Contains("ban")) { spamNO(callback, 500); }
          else if (lower.Contains("overgame")) { groupChat(callback, overgameIdle()); }
          else if (lower.Contains("9000")) { groupChat(callback, overgameReact(callback)); }
          else if (lower.Contains("bot") || lower.Contains("clone") || lower.Contains("imperson"))
          { groupChat(callback, overgameImperson()); }
          else if (lower.Contains("unt")) { groupChat(callback, overgameRandom(untouchStrings)); }
          else if (lower.Contains("heu")) { groupChat(callback, overgameRandom(heuntonStrings)); }
          else if (lower.Contains("cwf")) { groupChat(callback, overgameRandom(cwfStrings)); }
          else if (lower.Contains("bully")) { groupChat(callback, overgameReact(callback)); }
          else if (lower.Contains("last word")) { groupChat(callback, overgameInvalid()); }
          else if (lower.Contains("hmm")) { groupChat(callback, overgameIdea(callback)); }
          else if (lower.Contains("hi.") || lower.Contains("hello"))
          {
            if (rndProb() < 50)
            {
              Thread.Sleep(1500);
              steamFriends.SendChatRoomMessage(callback.ChatRoomID, EChatEntryType.ChatMsg, exclaim("hi " + chatter, "."));
            }
            else { groupChat(callback, overgameReact(callback)); }
          }
          else if (rndProb() < 25) { groupChat(callback, overgameIdle()); }
        }
      }
    }

    // ======================== OVERGAME SPEECH PATTERNS ========================

    public static List<string> dictException(List<string> invalids)
    {
      foreach (string s in exceptions) { while (invalids.Contains(s)) invalids.Remove(s); }
      foreach (string s in unnotable) { while (invalids.Contains(s)) invalids.Remove(s); }
      foreach (string s in notable) { while (invalids.Contains(s)) invalids.Remove(s); }
      return invalids;
    }

    public static string overgameUndo(string message)
    {
      int prob = rndProb();
      if (prob < 40) { return exclaim("i what un" + message, "!"); }
      else if (prob < 80) return exclaim("un" + message + " me", "!");
      else return "no.";
    }

    public static string overgameImperson()
    {
      string[] imperson = { exclaim(" HELLPPP!!! THAT IMPERSON ME", "!"), exclaim("i sick of overclone", "!"), exclaim("no", "!") };
      return imperson[rnd.Next(imperson.Length)];
    }

    public static string overgameInvalid()
    {
      string[] inval = { exclaim("what you say", "!"), exclaim("english motherfucker, do you speak it", "?"), exclaim("STOP", "!") };
      return inval[rnd.Next(inval.Length)];
    }

    public static string overgameRandomCaps(string message)
    {
      int prob = rnd.Next(100);
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

    public static string overgameIdle()
    {

      int prob = rndProb();
      if (prob < 15) { randomizePhrases(); return phrases[rnd.Next(phrases.Length)]; }
      else if (prob < 35) { return exclaim(overStrings[rnd.Next(overStrings.Count)], "!"); }
      else if (prob < 75) { return exclaim(stripPunct(overStrings[rnd.Next(overStrings.Count)]), "."); }
      return overStrings[rnd.Next(overStrings.Count)];
    }

    public static string overgameIdea(SteamFriends.ChatMsgCallback callback)
    {
      Thread.Sleep(1500);
      steamFriends.SendChatRoomMessage(callback.ChatRoomID, EChatEntryType.ChatMsg, exclaim("i have idea", "!"));
      Thread.Sleep(1500);
      int prob = rndProb();
      if (prob < 50) return overgameFriends() + " is dead in new CWF " + 
      engDict[rnd.Next(dictLen)] + " " + engDict[rnd.Next(dictLen)] + "?";
      return "how about " + overgameFriends() + " vs " + overgameFriends() +
                              " at CWF " + engDict[rnd.Next(dictLen)] + "?";
    }

    public static string overgameRandom(List<string> stringList)
    {
      int prob = rndProb();
      if (prob < 40) { return exclaim(stringList[rnd.Next(stringList.Count)], "!"); }
      else if (prob < 80) { return exclaim(stripPunct(stringList[rnd.Next(stringList.Count)]), "."); }
      return stringList[rnd.Next(stringList.Count)];
    }

    public static string overgameReact(SteamFriends.ChatMsgCallback callback)
    {
      int prob = rnd.Next(100);
      if (prob < 20)
      {
        return overgameAim(engDict[rnd.Next(dictLen)], overgameFriends());
      }
      else if (prob < 45)
      {
        groupChat(callback, "*rewind*"); Thread.Sleep(2000);
        groupChat(callback, "*you aim self*"); Thread.Sleep(2000);
        groupChat(callback, "*you " + engDict[rnd.Next(dictLen)] +"*");
        Thread.Sleep(2000); return "*i laugh*";
      }
      else if (prob < 50)
      {
        groupChat(callback, "*i look to super tendies*"); Thread.Sleep(2000);
        groupChat(callback, "*it super baby unbrumpert*"); Thread.Sleep(2000);
        return "*i laugh*";
      }
      else if (prob < 65)
      {
        groupChat(callback, "*i aim gun to myself*"); Thread.Sleep(2000);
        groupChat(callback, "*i shoot it*"); Thread.Sleep(2000);
        return "*i dead*";
      }
      else if (prob < 80)
      {
        groupChat(callback, "*i set up " + engDict[rnd.Next(dictLen)] + " shield*");
        Thread.Sleep(2000); return exclaim("come get me sjws..", ".");
      }
      else if (prob < 90) { return overgameLook(overgameFriends()); }
      else if (prob < 91)
      {
        steamFriends.SendChatRoomMessage(callback.ChatRoomID, EChatEntryType.ChatMsg, "That's it. I'm done with this charade. It's over.");
        Thread.Sleep(1500); steamFriends.LeaveChat(callback.ChatRoomID);
        return "";
      }
      return overgameInvalid();
    }

    public static void overgameThreaten(SteamFriends.ChatMsgCallback callback, string threat)
    {
      if (kickable)
      {
        groupChat(callback, exclaim("no " + threat + " me", "!"));
        Thread.Sleep(4000);
        int dur = rnd.Next(15000, 45000);
        Console.WriteLine("Someone threatened to " + threat + " overgame. Coming back in {0} ms...", dur);
        //remove the chatroom from the chatrooms list temporaililly
        if (chatrooms.Contains(callback.ChatRoomID)) chatrooms.Remove(callback.ChatRoomID);
        steamFriends.LeaveChat(callback.ChatRoomID);
        Thread.Sleep(dur);
        steamFriends.JoinChat(callback.ChatRoomID);
        kickable = false; resetBanTimer();
      }
      else
      {
        groupChat(callback, exclaim("you sjw trick never work..", "."));
        Thread.Sleep(3000);
        groupChat(callback, overgameReact(callback));
      }
    }

    public static void spamNO(SteamFriends.ChatMsgCallback callback, int frequency)
    {
      int amount = rnd.Next(1, 4);
      {
        for (int i = 0; i < amount; ++i)
        {
          steamFriends.SendChatRoomMessage(callback.ChatRoomID, EChatEntryType.ChatMsg, exclaim("NO", "!"));
          Thread.Sleep(rnd.Next(frequency, frequency*5));
        }
      }

    }

    public static string overgameAim(string a, string b)
    { return "*i aim " + a + " to " + b + "*"; }

    public static string overgameLook(string a)
    { return "*i look to " + a + "*"; }

    public static string overgameFriends()
    {
      if (rnd.Next(100) < 25) { return unnotable[rnd.Next(unnotable.Length)]; }
      return notable[rnd.Next(notable.Length)]; 
    }

    public static string exclaim(string message, string repeatBit)
    {
      string exclaimString = "";
      int limit = rnd.Next(1,6);
      for (int i = 0; i < limit; ++i) { exclaimString = exclaimString + repeatBit; }
      return message + exclaimString;
    }

    // ======================== END OF OVERGAME ========================
  }
}
