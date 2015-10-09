using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OvergameBot.Helper;

namespace OvergameBot
{
  class OvergameSpeech
  {

    static readonly string[] unnotable = new string[] { "kami", "wayne", "trog", "erarg", "mira", "anita", "kiz" };
    static readonly string[] notable = new string[] { "moupi", "untouch",
    "doc", "doc gelegentlich", "heunton", "moup", "boris", "garret", "damros", "sjws", "emagravo", "garrett" };
    static readonly string[] exceptions = new string[] { "i", "overgame", "cwf", "laugh" };


    public static string exclaim(string message, string repeatBit)
    {
      string exclaimString = "";
      int limit = ri().Next(1, 6);
      for (int i = 0; i < limit; ++i) { exclaimString = exclaimString + repeatBit; }
      return message + exclaimString;
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
      return imperson[ri().Next(imperson.Length)];
    }

    public static string overgameInvalid()
    {
      string[] inval = { exclaim("what you say", "!"), exclaim("english motherfucker, do you speak it", "?"), exclaim("STOP", "!") };
      return inval[ri().Next(inval.Length)];
    }

    public static void randomizePhrases(string[] phrases, List<string> dict, int dictLen)
    {

      phrases = new string[] { exclaim("toppermost of the poppermost", "!"),
                               exclaim("Damngod..", "."),
                               exclaim("where " + overgameFriends(), "?"),
                               exclaim("doc make me cwf", "!"),
                               exclaim(" HELLPPP!!! THAT IMPERSON ME", "!"),
                               exclaim("i sick of overclone", "!"),
                               exclaim("no", "!"),
                               exclaim(dict[ri().Next(dictLen)] + " " +
                               dict[ri().Next(dictLen)] + " " +
                               dict[ri().Next(dictLen)], "!")
                             };
    }

    public static string overgameFriends()
    {
      if (rndProb() < 25) { return unnotable[ri().Next(unnotable.Length)]; }
      return notable[ri().Next(notable.Length)];
    }

    public static string overgameAim(string a, string b)
    { return "*i aim " + a + " to " + b + "*"; }

    public static string overgameLook(string a)
    { return "*i look to " + a + "*"; }

    public static List<string> dictException(List<string> invalids)
    {
      foreach (string s in exceptions) { while (invalids.Contains(s)) invalids.Remove(s); }
      foreach (string s in unnotable) { while (invalids.Contains(s)) invalids.Remove(s); }
      foreach (string s in notable) { while (invalids.Contains(s)) invalids.Remove(s); }
      return invalids;
    }



  }
}
