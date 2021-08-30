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
    public static Queue wtqueue = new Queue();
    public static TwitchClient client;
    public static string Channel;


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
            case "wt":
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
                        client.SendMessage(Channel, "your request has been added to the queue!");
                        queued = true;
                    }

                }
                if (!queued)
                    client.SendMessage(Channel, "no file found");
                return;
        }
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