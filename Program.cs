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

        static string[] unnotable = new string[] { "kami", "wayne", "trog", "erarg", "mira" };
        static string[] notable = new string[] { "moupi", "untouch", "doc", "doc gelegentlich", "heunton", "moup", "boris", "garret", "damros", "sjws", "emagravo" };

        static string[] phrases = { };
        
        static int dictLen = 58110;

        static System.Timers.Timer aTimer = new System.Timers.Timer();

        static string user, pass, guard;

        static SteamClient steamClient;
        static CallbackManager manager;
        static SteamUser steamUser;
        static SteamFriends steamFriends;

        static int frantic = 75;
        static int ignore = 0;

        static bool isRunning;
        static Random rnd = new Random();

        static bool busy = false;

        static void Main(string[] args)
        {

            Console.Title = "OvergameBot";
            Console.WriteLine("CTRL+C bully me!");

            returnPassword();

            aTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerUp);
            aTimer.Interval = 6000;
            aTimer.Enabled = true;

            phrases = new string[] { exclaim("toppermost of the poppermost", "!"), "Damngod...", exclaim("where " + overgameFriends(), "?"), exclaim("doc make me cwf", "!"),
                                   exclaim(" HELLPPP!!! THAT IMPERSON ME", "!"), exclaim("i sick of overclone", "!"), exclaim("no", "!"),
                                    exclaim(engDict[rnd.Next(dictLen)] + " " + engDict[rnd.Next(dictLen)] + " " + engDict[rnd.Next(dictLen)], "!")
                                };

            SteamLogin();
        }

        static void OnTimerUp(object sender, EventArgs c)
        {
            busy = false;
            aTimer.Enabled = false;
            Console.WriteLine("Now accepting requests.");
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

        static void DelayedMessage(Object sender, EventArgs c, SteamFriends.ChatMsgCallback callback, string message)
        {
            steamFriends.SendChatRoomMessage(callback.ChatRoomID, EChatEntryType.ChatMsg, overgameRandomCaps(message));
            ((System.Timers.Timer)sender).Dispose();
        }

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
            new Callback<SteamFriends.ChatEnterCallback>(OnJoinChat, manager);

            steamClient.Connect();


            isRunning = true;

            while (isRunning)
            {
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
            Console.ReadKey(); //exit the program with any key
        }

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
                isRunning = false;
                return;
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
        /*
        static void OnFriendMessage(SteamFriends.FriendMsgCallback callback)
        {
            string[] args;
            if (callback.EntryType == EChatEntryType.ChatMsg)
            {
                //steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "GUNS!!");
                if (callback.Message.Length > 1)
                {
                    if (callback.Message.Remove(1) == "!") //command
                    {
                        string command = callback.Message; //!friend [sid64] = !friend
                        if (command.Contains(' '))
                        {
                            command = callback.Message.Remove(callback.Message.IndexOf(' '));
                        }

                        switch (command)
                        {
                            case "!send": //!send friendname message
                                args = seperate(2, ' ', callback.Message);
                                Console.WriteLine("!send " + args[1] + args[2] + " command received. User: " + steamFriends.GetFriendPersonaName(callback.Sender));
                                if (args[0] == "-1")
                                {
                                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, overgameInvalid());
                                }
                                break;
                        }

                    }
                    else if (callback.Message.Remove(1) == "*") //act
                    {
                        char[] delim = { ' ', '.', ',', ':', '\t', '*' };
                        string[] words = callback.Message.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                        List<string> notValid = new List<string>(new string [] {});
                        Console.Write("Dictionary analysis of words...: ");
                        foreach (string st in words)
                        {
                            string s = st.ToLower();
                            Console.Write(s);
                            if (!engDict.Contains(s) || s == "chicken" || s == "chi" )
                            {
                                notValid.Add(s);
                                Console.Write("(!)");
                            }
                            Console.Write(" ");
                        }
                        Console.WriteLine("");

                        notValid = dictException(notValid);

                        Console.Write("Words to examine: ");
                        foreach (string s in notValid) { Console.Write(s + " "); }
                        Console.WriteLine("");

                        if (notValid.Count >= 1)
                        {
                            steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, overgameUndo(notValid[rnd.Next(notValid.Count)]));
                        }
                        else
                        {
                            steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, overgameIdle());
                        }
                    }
                    else if (callback.Message.Contains("overgame"))//act
                    {
                        steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, overgameIdle());
                    }
                }
            }
        }
        */
        static void OnChatMessage(SteamFriends.ChatMsgCallback callback)
        {
            Console.WriteLine("Received: " + callback.Message);
            if (busy) { Console.WriteLine("Busy."); return; }
            string[] args;
            if (callback.ChatMsgType == EChatEntryType.ChatMsg)
            {
                if (callback.Message.Length > 1)
                {
                    if (callback.Message.Remove(1) == "!") //command
                    { // this doesn't do shit
                        /*
                        string command = callback.Message; //!friend [sid64] = !friend
                        if (command.Contains(' '))
                        {
                            command = callback.Message.Remove(callback.Message.IndexOf(' '));
                        }

                        switch (command)
                        {
                            case "!send": //!send friendname message
                                args = seperate(2, ' ', callback.Message);
                                Console.WriteLine("!send " + args[1] + args[2] + " command received. Group invoked.");
                                if (args[0] == "-1")
                                {
                                    groupChat(callback, overgameInvalid());
                                }
                                break;
                        }
                        */
                    }
                    else if (callback.Message.Contains("*i set ignore percent to"))
                    {
                        string val = callback.Message.Substring(25);
                        string val2 = "";
                        int count = 0;
                        foreach (char c in val)
                        {
                            if (count >= 5) { break; }
                            if (Char.IsDigit(c))
                            {
                                val2 = val2 + c;
                                ++count;
                            }
                        }

                        int i = int.Parse(val2);
                        if (i >= 0)
                        {
                            ignore = i;
                            groupChat(callback, exclaim("get " + i + "% of shit out", "!"));
                        }
                        else
                        {
                            groupChat(callback, exclaim("you wrong", "!"));
                        }

                    }
                    else if (callback.Message.Contains("*i set overgame speed to"))
                    {
                        string val = callback.Message.Substring(25);
                        string val2 = "";
                        foreach (char c in val)
                        {
                            if (Char.IsDigit(c))
                            {
                                val2 = val2 + c;
                            }
                        }

                        int i = int.Parse(val2);
                        if (i <= 0)
                        {
                            groupChat(callback, exclaim("too fast", "!"));
                        }
                        else if (i > 500)
                        {
                            groupChat(callback, exclaim("you wrong", "!"));
                        }
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
                            {
                                val2 = val2 + c;
                            }
                        }
                        steamFriends.SetPersonaName(val2);
                    }
                    else if (rndProb() < ignore) { return; } // random prob in lieu of wait ignore
                    else if (callback.Message.Remove(1) == "*") //act
                    {
                        char[] delim = { ' ', '.', ',', ':', '\t', '*' };
                        string[] words = callback.Message.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                        List<string> notValid = new List<string>(new string[] { });
                        Console.Write("Dictionary analysis of words...: ");
                        foreach (string st in words)
                        {
                            string s = st.ToLower(); //compare to the lowercase dictionary
                            Console.Write(s);
                            if (!engDict.Contains(s))
                            {
                                notValid.Add(s);
                                Console.Write("(!)");
                            }
                            Console.Write(" ");

                            if (s == "kick")
                            {
                                overgameKickRoutine(callback);
                                break;
                            }
                            else if (s == "ban")
                            {
                                overgameBanRoutine(callback);
                                break;
                            }

                            else if (s == "aim")
                            {
                                groupChat(callback, overgameReact(callback));
                                break;
                            }
                            else if (s == "chicken")
                            {
                                groupChat(callback, overgameUndo("chicken"));
                                return;
                            }
                            else if (s == "chi")
                            {
                                groupChat(callback, overgameUndo("chi"));
                                return;
                            }
                            else if (s == "chicarrot")
                            {
                                groupChat(callback, overgameUndo("chicarrot"));
                                return;
                            }
                            else if (s == "bully")
                            {
                                groupChat(callback, overgameUndo("bully"));
                                return;
                            }
                        }
                        Console.WriteLine("");

                        notValid = dictException(notValid);

                        Console.Write("Words to examine: ");
                        foreach (string s in notValid) { Console.Write(s + " "); }
                        Console.WriteLine("");

                        if (notValid.Count >= 1)
                        { groupChat(callback, overgameUndo(notValid[rnd.Next(notValid.Count)])); }
                        else { groupChat(callback, overgameIdle()); }
                    }
                    else if (callback.Message.Remove(1) == "?") //emote
                    {
                        if (callback.Message.Contains("csgogun"))
                        {
                            groupChat(callback, overgameReact(callback));
                        }
                        else if (callback.Message.Contains("chicken"))
                        {
                            groupChat(callback, overgameReact(callback));
                        }
                        else if (callback.Message.Contains("9000"))
                        {
                            groupChat(callback, overgameReact(callback));
                        }
                    }
                    else if (callback.Message.Contains("%"))
                    {
                        spamNO(callback, 300);
                    }
                    else if (callback.Message.ToLower().Contains("ban"))
                    {
                        spamNO(callback, 1000);
                    }
                    else if (callback.Message.ToLower().Contains("overgame"))//act
                    {
                        groupChat(callback, overgameIdle());
                    }
                    else if (rndProb() < 25)
                    {
                        groupChat(callback, overgameIdle());
                    }

                }
            }
        }

        static void OnChatInvite(SteamFriends.ChatInviteCallback callback)
        {
            steamFriends.JoinChat(callback.ChatRoomID);
        }

        static void OnJoinChat(SteamFriends.ChatEnterCallback callback)
        {   
            int prob = rndProb();
            if (prob < 40)
            {
                steamFriends.SendChatRoomMessage(callback.ChatID, EChatEntryType.ChatMsg, exclaim("IM NOT BOT", "!"));
            }
            else if (prob < 80)
            {
                steamFriends.SendChatRoomMessage(callback.ChatID, EChatEntryType.ChatMsg, "hello.");
                Thread.Sleep(1500);
                steamFriends.SendChatRoomMessage(callback.ChatID, EChatEntryType.ChatMsg, "im back.");
            }
            else
            {
                steamFriends.SendChatRoomMessage(callback.ChatID, EChatEntryType.ChatMsg, "good moring.");
            }
        }

        public static List<string> dictException(List<string> invalids)
        {
            List<string> exceptions = new List<string>(new string[] {"i", "overgame"});
            foreach (string s in exceptions)
            {
                invalids.Remove(s);
            }
            foreach (string s in unnotable)
            {
                invalids.Remove(s);
            }
            foreach (string s in notable)
            {
                invalids.Remove(s);
            }

            return invalids;
        }

        public static string overgameUndo(string message)
        {
            if (rndProb() < 50)
            {
                return exclaim("i what un" + message, "!");
            }
            return exclaim("un" + message + " me", "!");
        }

        public static string overgameInvalid()
        {
            string[] phrases = { exclaim("what you say", "!"), exclaim("english motherfucker, do you speak it", "?"), exclaim("STOP", "!") };
            return phrases[rnd.Next(phrases.Length)];
        }

        public static string overgameRandomCaps(string message)
        {
            int prob = rnd.Next(100);
            if (prob < 25)
            {
                return message.ToLower(); //lowercase everything
            }
            else if (prob < 75)
            {
                return message.ToUpper(); //uppercase everything
            }
            return message.First().ToString().ToUpper() + message.ToLower().Substring(1);
            //first capitalized
        }

        public static string overgameIdle()
        {
            int prob = rndProb();
            if (prob < 20) { return phrases[rnd.Next(phrases.Length)]; }
            else if (prob < 60) { return exclaim(overStrings[rnd.Next(overStrings.Count)], "!"); }
            return overStrings[rnd.Next(overStrings.Count)];
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
                groupChat(callback, "*rewind*");
                Thread.Sleep(2000);
                groupChat(callback, "*you aim self*");
                Thread.Sleep(2000);
                groupChat(callback, "*you " + engDict[rnd.Next(dictLen)] +"*");
                Thread.Sleep(2000);
                return "*i laugh*";
            }
            else if (prob < 60)
            {
                groupChat(callback, "*i look to super tendies*");
                Thread.Sleep(2000);
                groupChat(callback, "*it super baby unbrumpert*");
                Thread.Sleep(2000);
                return "*i laugh*";
            }
            else if (prob < 80)
            {
                groupChat(callback, "*i set up " + engDict[rnd.Next(dictLen)] + " shield*");
                Thread.Sleep(2000);
                return "come get me sjws...";
            }
            else if (prob < 90)
            {
                return overgameLook(overgameFriends());
            }
            else if (prob < 91)
            {
                steamFriends.SendChatRoomMessage(callback.ChatRoomID, EChatEntryType.ChatMsg, "That's it. I'm done with this charade. It's over.");
                Thread.Sleep(1500);
                steamFriends.LeaveChat(callback.ChatRoomID);
                return "";
            }
            return overgameInvalid();
        }



        public static void overgameKickRoutine(SteamFriends.ChatMsgCallback callback)
        {
            groupChat(callback, exclaim("no kick me", "!"));
            Thread.Sleep(5000);
            int dur = rnd.Next(15000, 45000);
            Console.WriteLine("Someone threatened to kick overgame. Coming back in {0} ms...", dur);
            steamFriends.LeaveChat(callback.ChatRoomID);
            Thread.Sleep(dur);
            steamFriends.JoinChat(callback.ChatRoomID);
        }
        public static void overgameBanRoutine(SteamFriends.ChatMsgCallback callback)
        {
            groupChat(callback, exclaim("no ban me", "!"));
            Thread.Sleep(5000);
            int dur = rnd.Next(15000, 45000);
            Console.WriteLine("Someone threatened to ban overgame. Coming back in {0} ms...", dur);
            steamFriends.LeaveChat(callback.ChatRoomID);
            Thread.Sleep(dur);
            steamFriends.JoinChat(callback.ChatRoomID);
        }

        public static int rndProb()
        {
            return rnd.Next(100);
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
        {   
            return "*i aim " + a + " to " + b + "*";
        }

        public static string overgameLook(string a)
        {
            return "*i look to " + a + "*";
        }
        public static string overgameFriends()
        {
            if (rnd.Next(100) < 25) { return unnotable[rnd.Next(unnotable.Length)]; }
            return notable[rnd.Next(notable.Length)]; 
        }

        public static string exclaim(string message, string repeatBit)
        {
            string exclaimString = "";
            int limit = rnd.Next(1,6);
            for (int i = 0; i < limit; ++i)
            {
                exclaimString = exclaimString + repeatBit;
            }
            return message + exclaimString;
        }

        public static int read(string message)
        {
            Console.WriteLine("Thinking...");
            Console.WriteLine("Thought!");
            return (int)(message.Length * .1) * frantic;
        }

        public static int think()
        {
            return 18 * frantic;
        }

        public static int type(string message)
        {
            Console.WriteLine("Typing...");
            Console.WriteLine("Typed!");
            return (int)(message.Length * .4 * frantic);
        }
       
        public static string[] seperate(int number, char seperator, string theString)
        {
            string[] returned = new string[4];
            int i = 0;
            int error = 0;
            int length = theString.Length;

            foreach (char c in theString)
            {
                if (i != number)
                {
                    if (error > length || number > 5)
                    {
                        returned[0] = "-1";
                        return returned;
                    }
                    else if (c == seperator)
                    {
                        returned[i] = theString.Remove(theString.IndexOf(c));
                        theString = theString.Remove(0, theString.IndexOf(c) + 1);
                        i++;
                    }
                    error++;

                    if (error == length && i != number)
                    {
                        returned[0] = "-1";
                        return returned;
                    }
                }
                else
                {
                    returned[i] = theString;
                }
            }
            return returned;
        }

        static void returnPassword()
        {
            Console.Write("I what username!: ");
            user = Console.ReadLine();
            Console.Write("I what password!: ");
            pass = "";
            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key != ConsoleKey.Backspace)
                {
                    pass += info.KeyChar;
                    info = Console.ReadKey(true);
                }
                else if (info.Key == ConsoleKey.Backspace)
                {
                    if (!string.IsNullOrEmpty(pass))
                    {
                        pass = pass.Substring
                        (0, pass.Length - 1);
                    }
                    info = Console.ReadKey(true);
                }
            }
            for (int i = 0; i < pass.Length; i++)
            {
                Console.Write("*");
            }
            Console.Write("\n");
        }

    }
}
