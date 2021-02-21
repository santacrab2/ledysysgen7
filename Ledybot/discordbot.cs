﻿using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Reflection;
using PKHeX.Core;
using System.Net;



public class discordbot
{


    private DiscordSocketClient _client;
    private CommandService _commands;
    Queue tradequeue = new Queue();
    private static readonly WebClient webClient = new WebClient();
    public PKM tradeable;




    public static void tot(string[] args)
    => new discordbot().MainAsync().GetAwaiter().GetResult();



    public async Task MainAsync()
    {
        _client = new DiscordSocketClient();
        _client.Log += Log;
        _commands = new CommandService();
        var token = File.ReadAllText("C:/Users/jordan/source/repos/ledybot/Ledybot/obj/ledybug6/token.txt");


        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        CommandHandler ch = new CommandHandler(_client, _commands);
        ch.InstallCommandsAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);

    }
    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                        services: null);

        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);
        }

    }

    //this will download the byte data from a pkm
    public static async Task<byte[]> DownloadFromUrlAsync(string url)
    {
        return await webClient.DownloadDataTaskAsync(url);
    }

    public class trademodule : ModuleBase
    {
        [Command("trade")]
        public async Task Trade()
        {
            //this grabs the file the user uploads to discord even they even do it.
            var pokm = Context.Message.Attachments.FirstOrDefault();
            if (pokm == default)
            {
                ReplyAsync("no attachment provided");
                return;
            }
            //this cleans up the filename the user submitted and checks that its a pk6 or 7
            var att = Format.Sanitize(pokm.Filename);
            if (!att.Contains(".pk7") && !att.Contains(".pk6"))

            {
                ReplyAsync("no pk7 or pk6 provided");
                return;
            }

            ReplyAsync("file accepted");
            var buffer = await DownloadFromUrlAsync(pokm.Url).ConfigureAwait(false);
            PKM tradeable = PKMConverter.GetPKMfromBytes(buffer, 7);
            ReplyAsync(tradeable.Species.ToString());
        }
    }
}

