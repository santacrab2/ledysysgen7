using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;


//TwitchBot Instance/Namespace to call upon from forms
public class TwitchBot
{
    public static Queue wtuser = new Queue();
    public static Queue wtqueue = new Queue();
    public static TwitchClient client;
    public static string Channel;
    public static discordbot.trademodule disbot = new discordbot.trademodule();


    public TwitchBot()
    {
        var clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 100,
            ThrottlingPeriod = TimeSpan.FromSeconds(30),

            WhispersAllowedInPeriod = 100,
            WhisperThrottlingPeriod = TimeSpan.FromSeconds(60),

            // message queue capacity is managed (10_000 for message & whisper separately)
            // message send interval is managed (50ms for each message sent)
        };
        WebSocketClient WebCli = new(clientOptions);
        client = new TwitchClient(WebCli);
        var prefix = '!';
        var User = Ledybot.Program.f1.twuser.Text;
        var Token = Ledybot.Program.f1.twtoken.Text;
        Channel = Ledybot.Program.f1.twchannel.Text;
        var Credentials = new ConnectionCredentials(User, Token);

        client.Initialize(Credentials, Channel, prefix, prefix);
        client.OnDisconnected += Client_OnDisconnected;
        client.OnJoinedChannel += Client_onJoin;
        client.OnLeftChannel += Client_OnLeftChannel;
        client.OnChatCommandReceived += Client_commandhandler;
        client.Connect();
        
       
        
    }
    private void Client_onJoin(object sender, OnJoinedChannelArgs e)
    {
        client.SendMessage(Channel, "connected");
    }
    private void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
    {

        while (!client.IsConnected)
            client.Reconnect();
    }

    private void Client_OnLeftChannel(object sender, OnLeftChannelArgs e)
    {
        client.JoinChannel(e.Channel);
    }

    private void Client_commandhandler(object sender, OnChatCommandReceivedArgs e)
    {
        var queued = false;
        var command = e.Command.CommandText;
        switch (command)
        {
            case "t":
                var commandformat = e.Command.ArgumentsAsString.Split(',');
                twitchtrade(e,commandformat[0], commandformat[1], commandformat[2]);
                return;
              
                    
                
            case "wt":
                if (!wtuser.Contains(e.Command.ChatMessage.Username))
                {
                    var files = Directory.GetFiles(Ledybot.Program.f1.wtfolder.Text);

                    var converset = ConvertToShowdown(e.Command.ArgumentsAsString);

                    var sav = TrainerSettings.DefaultFallback(7);
                    var comppk = new PK7();
                    comppk.ApplySetDetails(converset);
                    foreach (string file in files)
                    {
                        var temppk = PKMConverter.GetPKMfromBytes(File.ReadAllBytes(file), 7);
                        if (temppk.Species == comppk.Species && temppk.Form == comppk.Form && temppk.IsShiny == comppk.IsShiny)
                        {
                            wtqueue.Enqueue(temppk);
                            wtuser.Enqueue(e.Command.ChatMessage.Username);
                            client.SendMessage(Channel, "your request has been added to the queue!");
                            queued = true;
                        }

                    }
                    if (!queued)
                        client.SendMessage(Channel, "no file found");
                    return;
                }
                else
                {
                    client.SendMessage(TwitchBot.Channel, "you are already in queue");
                    return;
                }
            case "wtlist":
                List<string> wtlists = new List<string>();
                var wtfiles = Directory.GetFiles(Ledybot.Program.f1.wtfolder.Text);
                var sb = new System.Text.StringBuilder();
                foreach(string file in wtfiles)
                {
                    var sotemppk = PKMConverter.GetPKMfromBytes(File.ReadAllBytes(file));
                    sb.AppendLine((sotemppk.IsShiny ? "★" : "") + (sotemppk.Form == 0 ? $"{(Species)sotemppk.Species}" : $"{(Species)sotemppk.Species}-{ShowdownParsing.GetStringFromForm(sotemppk.Form, GameInfo.Strings, sotemppk.Species, sotemppk.Format)}"));
                }
                var wtfilelist = sb.ToString();
                while (wtfilelist.Length > 0)
                {
                    if (wtfilelist.Length > 500)
                        wtlists.Add(wtfilelist.Substring(0, 500));
                    else
                        wtlists.Add(wtfilelist.Substring(0, wtfilelist.Length));

                    if (wtfilelist.Length > 500)
                        wtfilelist = wtfilelist.Remove(0, 500);
                    else
                        wtfilelist = wtfilelist.Remove(0, wtfilelist.Length);



                }
                foreach(string thelist in wtlists)
                {
                    client.SendMessage(Channel, thelist);
                }
                return;
        }
    }

    public async Task twitchtrade(OnChatCommandReceivedArgs t,string trainer, string pts, string set)
    {
        int ptsstr = Array.IndexOf(Ledybot.Program.PKTable.Species7, pts);
        if (ptsstr == -1)
        {
            client.SendMessage(Channel,"did not recognize your deposit pokemon");
            return;
        }
        ptsstr = ptsstr + 1;
        if (discordbot.trademodule.tradevolvs.Contains(ptsstr))
        {
            client.SendMessage(Channel,"you almost just broke the bot by depositing a trade evolution");
            return;
        }
        string[] pset = set.Split(' ');
        var l = Legal.ZCrystalDictionary;
        string temppokewait = Path.GetTempFileName();

        PKM pk = BuildPokemon(set, 7);
        if (File.Exists($"{Directory.GetCurrentDirectory()}//trainerinfo//{t.Command.ChatMessage.UserId}.txt"))
        {
            string[] trsplit = File.ReadAllText($"{Directory.GetCurrentDirectory()}//trainerinfo//{t.Command.ChatMessage.UserId}.txt").Split('\n');
            int q = 0;
            foreach (string b in trsplit)
            {
                if (trsplit[q].Contains("OT:"))
                    pk.OT_Name = trsplit[q].Replace("OT: ", "");
                q++;
            }
            int h = 0;
            foreach (string v in trsplit)
            {
                if (trsplit[h].Contains("TID:"))
                {
                    int trid7 = Convert.ToInt32(trsplit[h].Replace("TID: ", ""));
                    pk.TrainerID7 = trid7;

                }
                h++;
            }
            int hd = 0;
            foreach (string v in trsplit)
            {
                if (trsplit[hd].Contains("SID:"))
                {
                    int trsid7 = Convert.ToInt32(trsplit[hd].Replace("SID: ", ""));
                    pk.TrainerSID7 = trsid7;

                }
                hd++;
            }
        }

        if (pk.OT_Name.ToLower() == "pkhex")
            pk.OT_Name = trainer;
        if (set.Contains("OT:"))
        {
            int q = 0;
            foreach (string b in pset)
            {
                if (pset[q].Contains("OT:"))
                    pk.OT_Name = pset[q].Replace("OT: ", "");
                q++;
            }
        }
        if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).ToLower().Contains("ot name too long"))
            pk.OT_Name = "Pip";
        if (set.Contains("TID:"))
        {

            int h = 0;
            foreach (string v in pset)
            {
                if (pset[h].Contains("TID:"))
                {
                    int trid7 = Convert.ToInt32(pset[h].Replace("TID: ", ""));
                    pk.TrainerID7 = trid7;

                }
                h++;
            }
        }
        if (set.Contains("SID:"))
        {
            int h = 0;
            foreach (string v in pset)
            {
                if (pset[h].Contains("SID:"))
                {
                    int trsid7 = Convert.ToInt32(pset[h].Replace("SID: ", ""));
                    pk.TrainerSID7 = trsid7;

                }
                h++;
            }
        }
        if (set.ToLower().Contains("shiny: yes"))
        {
            pk.SetIsShiny(true);
        }
        if (new LegalityAnalysis(pk).Report().Contains("Invalid: SID should be 0"))
            pk.SID = 0;






        if (!new LegalityAnalysis(pk).Valid)
        {
            client.SendMessage(Channel,"Pokemon is illegal");
            client.SendMessage(Channel,LegalityFormatting.Report(new LegalityAnalysis(pk)));
            File.Delete(temppokewait);
            return;

        }
        client.SendMessage(Channel,"yay its legal good job!");

        byte[] g = pk.DecryptedBoxData;
        System.IO.File.WriteAllBytes(temppokewait, g);
        discordbot.trademodule.pokequeue.Enqueue(temppokewait);
        discordbot.trademodule.username.Enqueue(t.Command.ChatMessage.UserId);
        discordbot.trademodule.trainername.Enqueue(trainer);
        discordbot.trademodule.pokemonfile.Enqueue(pk);
        discordbot.trademodule.channel.Enqueue(Channel);
        discordbot.trademodule.poketosearch.Enqueue(ptsstr);
        discordbot.trademodule.discordname.Enqueue(t.Command.ChatMessage.Username);

        client.SendMessage(Channel,"added " + t.Command.ChatMessage.Username + " to queue");
        await checkstarttrade();


    }
    public static PKM BuildPokemon(string Set, int Generation)
    {
        try
        {
            // Disable Easter Eggs
            Legalizer.EnableEasterEggs = false;
            APILegality.SetAllLegalRibbons = false;
            APILegality.SetMatchingBalls = true;
            APILegality.ForceSpecifiedBall = true;
            APILegality.UseXOROSHIRO = true;
            APILegality.UseTrainerData = true;
            APILegality.AllowTrainerOverride = true;
            APILegality.AllowBatchCommands = true;
            APILegality.PrioritizeGame = true;
            APILegality.Timeout = 30;
            APILegality.PrioritizeGameVersion = GameVersion.USUM;
            // Reload Database & Ribbon Index





            // Convert the given Text into a Showdown Set
            ShowdownSet set = ConvertToShowdown(Set);
            IBattleTemplate re = new RegenTemplate(set, 7);
            // Generate a Blank Savefile


            var sav = TrainerSettings.DefaultFallback(7);
            PK7 tru = new PK7();
            // Generates a PKM from Showdown Set
            var pk = sav.GetLegalFromTemplate(tru, re, out _);
            PKMConverter.SetPrimaryTrainer(sav);

            PKMConverter.AllowIncompatibleConversion = true;
            pk = PKMConverter.ConvertToType(pk, typeof(PK7), out _);
            var sug = EncounterSuggestion.GetSuggestedMetInfo(pk);
            pk.Met_Location = sug.Location;
            pk.Met_Level = sug.LevelMin;


            pk = pk.Legalize();



            // In case its illegal, return null
            if (!new LegalityAnalysis(pk).Valid)
            {
                pk.SetEggMetData(GameVersion.US, GameVersion.US);
                pk.Met_Location = 78;
                pk.Met_Level = 1;
                pk = pk.Legalize();
                return pk;
            }



            // Return PKM
            return pk;
        }
        catch
        {
            // Text isn't a Showdown Set
            return null;
        }
    }
    public async Task checkstarttrade()
    {

        if (discordbot.trademodule.pokequeue.Count == 1)
            client.SendMessage(Channel,"finishing an ad trade, be right with you!");
        else
            client.SendMessage(Channel,"There are " + discordbot.trademodule.pokequeue.Count + " trainers in the queue");

    }
    public static async Task slow()
    {
        client.SendMessage(Channel,discordbot.trademodule.discordname.Peek() + " I could not find your deposit on the GTS, so the trades been cancelled");
        discordbot.trademodule.channel.Dequeue();
        discordbot.trademodule.discordname.Dequeue();
        discordbot.trademodule.pokequeue.Dequeue();
        discordbot.trademodule.username.Dequeue();
        discordbot.trademodule.pokemonfile.Dequeue();
        discordbot.trademodule.trainername.Dequeue();
    }
    public static async Task notrade()
    {
        client.SendMessage(Channel,discordbot.trademodule.discordname.Peek() + " something went wrong with your trade, please try again. if you get this message two times in a row, please ping Santacrab2");
        discordbot.trademodule.channel.Dequeue();
        discordbot.trademodule.discordname.Dequeue();
        discordbot.trademodule.pokequeue.Dequeue();
        discordbot.trademodule.username.Dequeue();
        discordbot.trademodule.pokemonfile.Dequeue();
        discordbot.trademodule.trainername.Dequeue();
    }
    public static ShowdownSet? ConvertToShowdown(string setstring)
    {
        // LiveStreams remove new lines, so we are left with a single line set
        var restorenick = string.Empty;

        var nickIndex = setstring.LastIndexOf(')');
        if (nickIndex > -1)
        {
            restorenick = setstring.Substring(0, nickIndex + 1);
            if (restorenick.TrimStart().StartsWith("("))
                return null;
            setstring = setstring.Substring(nickIndex + 1);
        }

        foreach (string i in splittables)
        {
            if (setstring.Contains(i))
                setstring = setstring.Replace(i, $"\r\n{i}");
        }

        var finalset = restorenick + setstring;
        return new ShowdownSet(finalset);
    }

    private static readonly string[] splittables =
    {
            "Ability:", "EVs:", "IVs:", "Shiny:", "Gigantamax:", "Ball:", "- ", "Level:",
            "Happiness:", "Language:", "OT:", "OTGender:", "TID:", "SID:",
            "Adamant Nature", "Bashful Nature", "Brave Nature", "Bold Nature", "Calm Nature",
            "Careful Nature", "Docile Nature", "Gentle Nature", "Hardy Nature", "Hasty Nature",
            "Impish Nature", "Jolly Nature", "Lax Nature", "Lonely Nature", "Mild Nature",
            "Modest Nature", "Naive Nature", "Naughty Nature", "Quiet Nature", "Quirky Nature",
            "Rash Nature", "Relaxed Nature", "Sassy Nature", "Serious Nature", "Timid Nature"
        };
}