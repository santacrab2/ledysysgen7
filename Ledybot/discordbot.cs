using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
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
    public static readonly WebClient webClient = new WebClient();






    public static void tot(string[] args)
    => new discordbot().MainAsync().GetAwaiter().GetResult();



    public async Task MainAsync()
    {
        _client = new DiscordSocketClient();
        _client.Log += Log;
        _commands = new CommandService();
        var token = File.ReadAllText("token.txt");


        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        CommandHandler ch = new CommandHandler(_client, _commands);
        await ch.InstallCommandsAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);

    }
    public static Task Log(LogMessage msg)
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

        public static PKM tradeable;
        public static byte[] buffer;
        public static IAttachment pokm;
        public static string att;
        public static string temppokecurrent = Path.GetTempFileName();
        public static Queue pokequeue = new Queue();
        public static Queue username = new Queue();
        public static Queue pokemonfile = new Queue();
        public static Queue trainername = new Queue();
        public static Queue channel = new Queue();
        public static Queue retpoke = new Queue();
        public static Queue poketosearch = new Queue();
        public static Queue discordname = new Queue();
        public static IUser dmer;
        public static string trainer;

        [Command("trade")]
        public async Task stradepts(int tid, int sid, string trainer,int pts, string set)
        {
            string temppokewait = Path.GetTempFileName();

            ShowdownSet pokeset = new ShowdownSet(set);
            var sav = SaveUtil.GetBlankSAV(GameVersion.UM, "piplup");

            PKM pk = new PK7();

            pk.ApplySetDetails(pokeset);
            sav.ApplyTo(pk);
            pk.HT_Name = "piplup";
            pk.HT_Gender = 0;
            pk.TrainerID7 = tid;
            pk.TrainerSID7 = sid;

            var met = EncounterSuggestion.GetSuggestedMetInfo(pk);
            pk.Met_Location = met.Location;
            pk.Met_Level = met.LevelMin;
            var leg = new LegalityAnalysis(pk);
            if (LegalityFormatting.GetLegalityReport(leg).Contains("Nickname does not match species name") || pk.IsNicknamed == false)
            {
                pk.ClearNickname();
            }


            pk.OT_Name = trainer;


            pk.Move1_PPUps = 0;
            pk.Move2_PPUps = 0;
            pk.Move3_PPUps = 0;
            pk.Move4_PPUps = 0;
            pk.Move1_PP = 5;
            pk.Move2_PP = 5;
            pk.Move3_PP = 5;
            pk.Move4_PP = 5;
            var relearn = MoveSetApplicator.GetSuggestedRelearnMoves(new LegalityAnalysis(pk));

            pk.SetRelearnMoves(relearn);


            pk.Ball = 4;
     

            if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Unable to match an encounter from origin game"))
            {
                pk.SetEggMetData(GameVersion.US, GameVersion.US);
                pk.Met_Location = 78;
                pk.Met_Level = 1;
            }

            var relearn2 = MoveSetApplicator.GetSuggestedRelearnMoves(new LegalityAnalysis(pk));

            pk.SetRelearnMoves(relearn2);
            pk.SetAbility(pk.Ability);
            if (set.Contains("Shiny: Yes"))
            {
                pk.SetIsShiny(true);
            }

            if (pk.Move1 == 0 && pk.Move2 == 0 && pk.Move3 == 0 && pk.Move4 == 0)
            {
                var move = new LegalityAnalysis(pk).GetSuggestedCurrentMoves();
                pk.Moves = move;
            }

            if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move"))
            {

                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 1: Invalid Move"))
                {
                    pk.Move1 = 0;
                    pk.FixMoves();
                }
                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 2: Invalid Move"))
                {
                    pk.Move2 = 0;
                    pk.FixMoves();
                }
                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 3: Invalid Move"))
                {
                    pk.Move3 = 0;
                    pk.FixMoves();
                }
                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 4: Invalid Move"))
                {
                    pk.Move4 = 0;
                    pk.FixMoves();
                }


            }
            if (pk.Move1 == 0)
            { pk.Move1_PP = 0; }
            if (pk.Move2 == 0)
            { pk.Move2_PP = 0; }
            if (pk.Move3 == 0)
            { pk.Move3_PP = 0; }
            if (pk.Move4 == 0)
            { pk.Move4_PP = 0; }
            byte[] pkb = pk.DecryptedBoxData;
            System.IO.File.WriteAllBytes(temppokewait, pkb);
            var l = Legal.ZCrystalDictionary;
            if (l.ContainsValue(pk.HeldItem) || Enumerable.Range(656, 115).Contains(pk.HeldItem))
            {
                await ReplyAsync("no megastones or z-crystals...fixing pokemon");
                pk.ApplyHeldItem(571, pk.Format);
                pk.SetEV(0, 0);
                pk.SetEV(1, 0);
                pk.SetEV(2, 0);
                pk.SetEV(3, 0);
                pk.SetEV(4, 0);
                pk.SetEV(5, 0);
                pk.SetIV(0, 0);
                pk.SetIV(1, 0);
                pk.SetIV(2, 0);
                pk.SetIV(3, 0);
                pk.SetIV(4, 0);
                pk.SetIV(5, 0);

                byte[] y = pk.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, y);

            }
            if (!new LegalityAnalysis(pk).Valid && !LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("assuming illegal"))
            {
                await ReplyAsync("pokemon is illegal dumbass");
                byte[] g = pk.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, g);
                await Context.Channel.SendFileAsync(temppokewait);
                File.Delete(temppokewait);
                return;
            }

            await ReplyAsync("yay its legal good job!");

           
            pokequeue.Enqueue(temppokewait);
            username.Enqueue(Context.User.Id);
            trainername.Enqueue(trainer);
            pokemonfile.Enqueue(pk);
            channel.Enqueue(Context.Channel);
            poketosearch.Enqueue(pts);
            discordname.Enqueue(Context.User);
            await ReplyAsync("added " + Context.User + " to queue");
            await checkstarttrade();

        }

        [Command("trade")]
        public async Task strade(int tid, int sid, string trainer, string set)
        {
            string temppokewait = Path.GetTempFileName();

            ShowdownSet pokeset = new ShowdownSet(set);
            var sav = SaveUtil.GetBlankSAV(GameVersion.UM, "piplup");

            PKM pk = new PK7();
            
            pk.ApplySetDetails(pokeset);
            sav.ApplyTo(pk);
            pk.HT_Name = "piplup";
            pk.HT_Gender = 0;
            pk.TrainerID7 = tid;
            pk.TrainerSID7 = sid;
         
            var met = EncounterSuggestion.GetSuggestedMetInfo(pk);
            pk.Met_Location = met.Location;
            pk.Met_Level = met.LevelMin;
            var leg = new LegalityAnalysis(pk);
            if (LegalityFormatting.GetLegalityReport(leg).Contains("Nickname does not match species name") || pk.IsNicknamed == false)
            {
                pk.ClearNickname();
            }


            pk.OT_Name = trainer;


            pk.Move1_PPUps = 0;
            pk.Move2_PPUps = 0;
            pk.Move3_PPUps = 0;
            pk.Move4_PPUps = 0;
            pk.Move1_PP = 5;
            pk.Move2_PP = 5;
            pk.Move3_PP = 5;
            pk.Move4_PP = 5;



            pk.Ball = 4;
       
           
            if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Unable to match an encounter from origin game"))
            {
                pk.SetEggMetData(GameVersion.US, GameVersion.US);
                pk.Met_Location = 78;
                pk.Met_Level = 1;
            }

            var relearn = MoveSetApplicator.GetSuggestedRelearnMoves(new LegalityAnalysis(pk));

            pk.SetRelearnMoves(relearn);
            pk.SetAbility(pk.Ability);
            if(set.Contains("Shiny: Yes"))
            {
                pk.SetIsShiny(true);
            }

            if (pk.Move1 == 0 && pk.Move2 == 0 && pk.Move3 == 0 && pk.Move4 == 0)
            {
                var move = new LegalityAnalysis(pk).GetSuggestedCurrentMoves();
                pk.Moves = move;
            }

            if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move"))
            {

                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 1: Invalid Move"))
                {
                    pk.Move1 = 0;
                    pk.FixMoves();
                }
                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 2: Invalid Move"))
                {
                    pk.Move2 = 0;
                    pk.FixMoves();
                }
                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 3: Invalid Move"))
                {
                    pk.Move3 = 0;
                    pk.FixMoves();
                }
                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 4: Invalid Move"))
                {
                    pk.Move4 = 0;
                    pk.FixMoves();
                }


            }
            if (pk.Move1 == 0)
            { pk.Move1_PP = 0; }
            if (pk.Move2 == 0)
            { pk.Move2_PP = 0; }
            if (pk.Move3 == 0)
            { pk.Move3_PP = 0; }
            if (pk.Move4 == 0)
            { pk.Move4_PP = 0; }

            byte[] pkb = pk.DecryptedBoxData;
            System.IO.File.WriteAllBytes(temppokewait, pkb);
            var l = Legal.ZCrystalDictionary;
            if (l.ContainsValue(pk.HeldItem) || Enumerable.Range(656, 115).Contains(pk.HeldItem))
            {
                await ReplyAsync("no megastones or z-crystals...fixing pokemon");
                pk.ApplyHeldItem(571, pk.Format);
                pk.SetEV(0, 0);
                pk.SetEV(1, 0);
                pk.SetEV(2, 0);
                pk.SetEV(3, 0);
                pk.SetEV(4, 0);
                pk.SetEV(5, 0);
                pk.SetIV(0, 0);
                pk.SetIV(1, 0);
                pk.SetIV(2, 0);
                pk.SetIV(3, 0);
                pk.SetIV(4, 0);
                pk.SetIV(5, 0);

                byte[] y = pk.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, y);

            }
            if (!new LegalityAnalysis(pk).Valid && !LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("assuming illegal"))
            {
                await ReplyAsync("pokemon is illegal dumbass");
                byte[] g = pk.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, g);
                await Context.Channel.SendFileAsync(temppokewait);
                File.Delete(temppokewait);
                return;
            }

            await ReplyAsync("yay its legal good job!");
         
            
            pokequeue.Enqueue(temppokewait);
            username.Enqueue(Context.User.Id);
            trainername.Enqueue(trainer);
            pokemonfile.Enqueue(pk);
            channel.Enqueue(Context.Channel);
            poketosearch.Enqueue(4321);
            discordname.Enqueue(Context.User);
            await ReplyAsync("added " + Context.User + " to queue");
            await checkstarttrade();


        }
        [Command("trade")]
        public async Task stradenotidpts(string trainer, int pts, string set)
        {
            string temppokewait = Path.GetTempFileName();

            ShowdownSet pokeset = new ShowdownSet(set);
            var sav = SaveUtil.GetBlankSAV(GameVersion.UM, "piplup");

            PKM pk = new PK7();

            pk.ApplySetDetails(pokeset);
            sav.ApplyTo(pk);
            pk.HT_Name = "piplup";
            pk.HT_Gender = 0;
            var met = EncounterSuggestion.GetSuggestedMetInfo(pk);
            pk.Met_Location = met.Location;
            pk.Met_Level = met.LevelMin;
            var leg = new LegalityAnalysis(pk);
            if (LegalityFormatting.GetLegalityReport(leg).Contains("Nickname does not match species name") || pk.IsNicknamed == false)
            {
                pk.ClearNickname();
            }


            pk.OT_Name = trainer;


            pk.Move1_PPUps = 0;
            pk.Move2_PPUps = 0;
            pk.Move3_PPUps = 0;
            pk.Move4_PPUps = 0;
            pk.Move1_PP = 5;
            pk.Move2_PP = 5;
            pk.Move3_PP = 5;
            pk.Move4_PP = 5;



            pk.Ball = 4;
      

            if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Unable to match an encounter from origin game"))
            {
                pk.SetEggMetData(GameVersion.US, GameVersion.US);
                pk.Met_Location = 78;
                pk.Met_Level = 1;
            }

            var relearn = MoveSetApplicator.GetSuggestedRelearnMoves(new LegalityAnalysis(pk));

            pk.SetRelearnMoves(relearn);
            pk.SetAbility(pk.Ability);
            if (set.Contains("Shiny: Yes"))
            {
                pk.SetIsShiny(true);
            }
            if (pk.Move1 == 0 && pk.Move2 == 0 && pk.Move3 == 0 && pk.Move4 == 0)
            {
                var move = new LegalityAnalysis(pk).GetSuggestedCurrentMoves();
                pk.Moves = move;
            }

            if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move"))
            {

                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 1: Invalid Move"))
                {
                    pk.Move1 = 0;
                    pk.FixMoves();
                }
                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 2: Invalid Move"))
                {
                    pk.Move2 = 0;
                    pk.FixMoves();
                }
                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 3: Invalid Move"))
                {
                    pk.Move3 = 0;
                    pk.FixMoves();
                }
                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 4: Invalid Move"))
                {
                    pk.Move4 = 0;
                    pk.FixMoves();
                }


            }
            if (pk.Move1 == 0)
            { pk.Move1_PP = 0; }
            if (pk.Move2 == 0)
            { pk.Move2_PP = 0; }
            if (pk.Move3 == 0)
            { pk.Move3_PP = 0; }
            if (pk.Move4 == 0)
            { pk.Move4_PP = 0; }

            byte[] pkb = pk.DecryptedBoxData;
            System.IO.File.WriteAllBytes(temppokewait, pkb);
            var l = Legal.ZCrystalDictionary;
            if (l.ContainsValue(pk.HeldItem) || Enumerable.Range(656, 115).Contains(pk.HeldItem))
            {
                await ReplyAsync("no megastones or z-crystals...fixing pokemon");
                pk.ApplyHeldItem(571, pk.Format);
                pk.SetEV(0, 0);
                pk.SetEV(1, 0);
                pk.SetEV(2, 0);
                pk.SetEV(3, 0);
                pk.SetEV(4, 0);
                pk.SetEV(5, 0);
                pk.SetIV(0, 0);
                pk.SetIV(1, 0);
                pk.SetIV(2, 0);
                pk.SetIV(3, 0);
                pk.SetIV(4, 0);
                pk.SetIV(5, 0);

                byte[] y = pk.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, y);

            }
            if (!new LegalityAnalysis(pk).Valid && !LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("assuming illegal"))
            {
                await ReplyAsync("pokemon is illegal dumbass");
                byte[] g = pk.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, g);
                await Context.Channel.SendFileAsync(temppokewait);
                File.Delete(temppokewait);
                return;
            }

            await ReplyAsync("yay its legal good job!");

           
            pokequeue.Enqueue(temppokewait);
            username.Enqueue(Context.User.Id);
            trainername.Enqueue(trainer);
            pokemonfile.Enqueue(pk);
            channel.Enqueue(Context.Channel);
            poketosearch.Enqueue(pts);
            discordname.Enqueue(Context.User);
            await ReplyAsync("added " + Context.User + " to queue");
            await checkstarttrade();


        }
        [Command("trade")]
        public async Task stradenotid(string trainer, string set)
        {
            string temppokewait = Path.GetTempFileName();

            ShowdownSet pokeset = new ShowdownSet(set);
            var sav = SaveUtil.GetBlankSAV(GameVersion.UM, "piplup");

            PKM pk = new PK7();
          
            pk.ApplySetDetails(pokeset);
            sav.ApplyTo(pk);
            pk.HT_Name = "piplup";
            pk.HT_Gender = 0;
            var met = EncounterSuggestion.GetSuggestedMetInfo(pk);
            pk.Met_Location = met.Location;
            pk.Met_Level = met.LevelMin;
            var leg = new LegalityAnalysis(pk);
            if (LegalityFormatting.GetLegalityReport(leg).Contains("Nickname does not match species name") || pk.IsNicknamed == false)
            {
                pk.ClearNickname();
            }


            pk.OT_Name = trainer;


            pk.Move1_PPUps = 0;
            pk.Move2_PPUps = 0;
            pk.Move3_PPUps = 0;
            pk.Move4_PPUps = 0;
            pk.Move1_PP = 5;
            pk.Move2_PP = 5;
            pk.Move3_PP = 5;
            pk.Move4_PP = 5;



            pk.Ball = 4;
      
           
            if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Unable to match an encounter from origin game"))
            {
                pk.SetEggMetData(GameVersion.US, GameVersion.US);
                pk.Met_Location = 78;
                pk.Met_Level = 1;
            }

            var relearn = MoveSetApplicator.GetSuggestedRelearnMoves(new LegalityAnalysis(pk));

            pk.SetRelearnMoves(relearn);
            pk.SetAbility(pk.Ability);
            if (set.Contains("Shiny: Yes"))
            {
                pk.SetIsShiny(true);
            }
            if(pk.Move1 == 0 && pk.Move2 == 0 && pk.Move3 == 0 && pk.Move4 == 0)
            {
                var move = new LegalityAnalysis(pk).GetSuggestedCurrentMoves();
                pk.Moves = move;
            }
         
            if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move"))
            {

                if(LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 1: Invalid Move"))
                {
                    pk.Move1 = 0;
                    pk.FixMoves();
                }
                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 2: Invalid Move"))
                {
                    pk.Move2 = 0;
                    pk.FixMoves();
                }
                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 3: Invalid Move"))
                {
                    pk.Move3 = 0;
                    pk.FixMoves();
                }
                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("Invalid Move 4: Invalid Move"))
                {
                    pk.Move4 = 0;
                    pk.FixMoves();
                }


            }
            if(pk.Move1 == 0)
            { pk.Move1_PP = 0; }
            if (pk.Move2 == 0)
            { pk.Move2_PP = 0; }
            if (pk.Move3 == 0)
            { pk.Move3_PP = 0; }
            if (pk.Move4 == 0)
            { pk.Move4_PP = 0; }


            byte[] pkb = pk.DecryptedBoxData;
            System.IO.File.WriteAllBytes(temppokewait, pkb);
            var l = Legal.ZCrystalDictionary;
            if (l.ContainsValue(pk.HeldItem) || Enumerable.Range(656, 115).Contains(pk.HeldItem))
            {
                await ReplyAsync("no megastones or z-crystals...fixing pokemon");
                pk.ApplyHeldItem(571, pk.Format);
                pk.SetEV(0, 0);
                pk.SetEV(1, 0);
                pk.SetEV(2, 0);
                pk.SetEV(3, 0);
                pk.SetEV(4, 0);
                pk.SetEV(5, 0);
                pk.SetIV(0, 0);
                pk.SetIV(1, 0);
                pk.SetIV(2, 0);
                pk.SetIV(3, 0);
                pk.SetIV(4, 0);
                pk.SetIV(5, 0);

                byte[] y = pk.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, y);

            }
            if (!new LegalityAnalysis(pk).Valid && !LegalityFormatting.GetLegalityReport(new LegalityAnalysis(pk)).Contains("assuming illegal"))
            {
                await ReplyAsync("pokemon is illegal dumbass");
                byte[] g = pk.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, g);
                await Context.Channel.SendFileAsync(temppokewait);
                File.Delete(temppokewait);
                return;
            }
           
            await ReplyAsync("yay its legal good job!");
           
            pokequeue.Enqueue(temppokewait);
            username.Enqueue(Context.User.Id);
            trainername.Enqueue(trainer);
            pokemonfile.Enqueue(pk);
            channel.Enqueue(Context.Channel);
            poketosearch.Enqueue(4321);
            discordname.Enqueue(Context.User);
            await ReplyAsync("added " + Context.User + " to queue");
            await checkstarttrade();

        }

        [Command("trade")]
        public async Task ptrade([Summary("poke to search")] int pts, [Remainder] string trainer)
        {
            string temppokewait = Path.GetTempFileName();

            //this grabs the file the user uploads to discord if they even do it.
            pokm = Context.Message.Attachments.FirstOrDefault();
            if (pokm == default)
            {
                await ReplyAsync("no attachment provided wtf are you doing?");
                File.Delete(temppokewait);
                return;
            }
            //this cleans up the filename the user submitted and checks that its a pk6 or 7
            att = Format.Sanitize(pokm.Filename);
            if (!att.Contains(".pk7"))
            {
                await ReplyAsync("no pk7 provided");
                File.Delete(temppokewait);
                return;
            }

            await ReplyAsync("file accepted..now to check if you know what you are doing with pkhex");
            await webClient.DownloadFileTaskAsync(pokm.Url, temppokewait);

            buffer = await DownloadFromUrlAsync(pokm.Url);
            tradeable = PKMConverter.GetPKMfromBytes(buffer, 7);

            var la = new PKHeX.Core.LegalityAnalysis(tradeable);

            var l = Legal.ZCrystalDictionary;
            if (!la.Valid)
            {
                await ReplyAsync("pokemon is illegal...checking/fixing egg moves");
                var egg = MoveSetApplicator.GetSuggestedRelearnMoves(la);
                tradeable.SetRelearnMoves(egg);
                byte[] y = tradeable.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, y);


            }
            var la2 = new LegalityAnalysis(tradeable);
            if (!la2.Valid)
            {
                await ReplyAsync("pokemon is illegal and its not egg moves");
                File.Delete(temppokewait);
                return;
            }
            if (l.ContainsValue(tradeable.HeldItem) || Enumerable.Range(656, 115).Contains(tradeable.HeldItem))
            {
                await ReplyAsync("no megastones or z-crystals...fixing pokemon");
                tradeable.ApplyHeldItem(571, tradeable.Format);
                tradeable.SetEV(0, 0);
                tradeable.SetEV(1, 0);
                tradeable.SetEV(2, 0);
                tradeable.SetEV(3, 0);
                tradeable.SetEV(4, 0);
                tradeable.SetEV(5, 0);
                tradeable.SetIV(0, 0);
                tradeable.SetIV(1, 0);
                tradeable.SetIV(2, 0);
                tradeable.SetIV(3, 0);
                tradeable.SetIV(4, 0);
                tradeable.SetIV(5, 0);

                byte[] y = tradeable.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, y);

            }
            
            




                await ReplyAsync("yay its legal good job!");
                pokequeue.Enqueue(temppokewait);
                username.Enqueue(Context.User.Id);
                trainername.Enqueue(trainer);
                pokemonfile.Enqueue(tradeable);
                poketosearch.Enqueue(pts);
                channel.Enqueue(Context.Channel);
                discordname.Enqueue(Context.User);
                await ReplyAsync("added " + Context.User + " to queue");
                await checkstarttrade();

            

        }



        [Command("trade")]
        public async Task trainertrade([Remainder] string trainer)
        {

            string temppokewait = Path.GetTempFileName();

            //this grabs the file the user uploads to discord if they even do it.
            pokm = Context.Message.Attachments.FirstOrDefault();
            if (pokm == default)
            {
                await ReplyAsync("no attachment provided wtf are you doing?");
                File.Delete(temppokewait);
                return;
            }
            //this cleans up the filename the user submitted and checks that its a pk6 or 7
            att = Format.Sanitize(pokm.Filename);
            if (!att.Contains(".pk7"))

            {
                await ReplyAsync("no pk7 provided");
                File.Delete(temppokewait);
                return;
            }

            await ReplyAsync("file accepted..now to check if you know what you are doing with pkhex");
            await webClient.DownloadFileTaskAsync(pokm.Url, temppokewait);

            buffer = System.IO.File.ReadAllBytes(temppokewait);
            tradeable = PKMConverter.GetPKMfromBytes(buffer, 7);
            var la = new LegalityAnalysis(tradeable);
            var l = Legal.ZCrystalDictionary;


            if (!la.Valid)
            {
                await ReplyAsync("pokemon is illegal...checking/fixing egg moves");
                var egg = MoveSetApplicator.GetSuggestedRelearnMoves(la);
                tradeable.SetRelearnMoves(egg);
                byte[] y = tradeable.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, y);


            }
            var la2 = new LegalityAnalysis(tradeable);
            if (!la2.Valid)
            {
                await ReplyAsync("pokemon is illegal and its not egg moves");
                File.Delete(temppokewait);
                return;
            }

            if (l.ContainsValue(tradeable.HeldItem) || Enumerable.Range(656, 115).Contains(tradeable.HeldItem))
            {
                await ReplyAsync("no megastones or z-crystals...fixing pokemon");
                tradeable.ApplyHeldItem(571, tradeable.Format);
                tradeable.SetEV(0, 0);
                tradeable.SetEV(1, 0);
                tradeable.SetEV(2, 0);
                tradeable.SetEV(3, 0);
                tradeable.SetEV(4, 0);
                tradeable.SetEV(5, 0);
                tradeable.SetIV(0, 0);
                tradeable.SetIV(1, 0);
                tradeable.SetIV(2, 0);
                tradeable.SetIV(3, 0);
                tradeable.SetIV(4, 0);
                tradeable.SetIV(5, 0);

                byte[] y = tradeable.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, y);

            }




            await ReplyAsync("yay its legal good job!");
            pokequeue.Enqueue(temppokewait);
            username.Enqueue(Context.User.Id);
            trainername.Enqueue(trainer);
            pokemonfile.Enqueue(tradeable);
            channel.Enqueue(Context.Channel);
            poketosearch.Enqueue(4321);
            discordname.Enqueue(Context.User);
            await ReplyAsync("added " + Context.User + " to queue");
            await checkstarttrade();

        }




        public async Task checkstarttrade()
        {
            if (!Ledybot.MainForm.botWorking && pokequeue.Count != 0)
            {
                starttrades();
            }
            else
            {
                await ReplyAsync("There are " + pokequeue.Count + " trainers in the queue");
            }
        }

        public async Task starttrades()
        {

            while (pokequeue.Count != 0)
            {
                if (retpoke.Count != 0)
                {

                    IMessageChannel t = (IMessageChannel)channel.Peek();
                    await t.SendFileAsync((string)retpoke.Peek(), discordname.Peek() + " here is the pokemon you traded me ");
                    channel.Dequeue();
                    retpoke.Dequeue();
                    discordname.Dequeue();
                    if (Ledybot.MainForm.game == 0 || Ledybot.MainForm.game == 1)
                        File.Delete(Ledybot.GTSBot7.tpfile);
                    else
                        File.Delete(Ledybot.GTSBot6.tpfile);
                }
                if (!Ledybot.MainForm.botWorking)
                {
                    IMessageChannel chan = (IMessageChannel)channel.Peek();
                    temppokecurrent = (string)pokequeue.Peek();
                    await chan.SendMessageAsync("<@" + username.Peek() + ">" + " deposit your pokemon now");

                    Ledybot.MainForm.btn_Start_Click(null, EventArgs.Empty);


                }
                else
                {
                    continue;
                }



            }

            if (retpoke.Count != 0)
            {

                IMessageChannel t = (IMessageChannel)channel.Peek();
                await t.SendFileAsync((string)retpoke.Peek(), discordname.Peek() + " here is the pokemon you traded me ");
                channel.Dequeue();
                retpoke.Dequeue();
                discordname.Dequeue();
                if (Ledybot.MainForm.game == 0 || Ledybot.MainForm.game == 1)
                    File.Delete(Ledybot.GTSBot7.tpfile);
                else
                    File.Delete(Ledybot.GTSBot6.tpfile);
            }


        }


        [Command("queueclear")]
        [Alias("rq")]
        public async Task queueclear()
        {
            if (Context.User.Id == 763073084676374578)
            {
                pokequeue.Dequeue();
                username.Dequeue();
                pokemonfile.Dequeue();
                trainername.Dequeue();
                channel.Dequeue();
                discordname.Dequeue();
                await ReplyAsync("the first person in line has been removed");
                Ledybot.MainForm.btn_Stop_Click(null, EventArgs.Empty);
            }
            else
            {
                await ReplyAsync("only santacrab can use this command");
            }

        }

        [Command("clear")]
        public async Task clqueue()
        {
            if (Context.User.Id == 763073084676374578)
            {
                pokequeue.Clear();
                username.Clear();
                pokemonfile.Clear();
                trainername.Clear();
                channel.Clear();
                discordname.Clear();
                await ReplyAsync("the entire queue has been cleared");
                Ledybot.MainForm.btn_Stop_Click(null, EventArgs.Empty);
            }
            else
            {
                await ReplyAsync("only santacrab can use this command");
            }
        }

        [Command("help")]
        public async Task help()
        {
            await ReplyAsync("read the pins");
        }

        [Command("hi")]
        public async Task hi()
        {
            await ReplyAsync(":middle_finger:");
        }
        [Command("fuckyou")]
        public async Task fuckyou()
        {
            await ReplyAsync(":kissing_heart:");
        }

        public static async Task ban()
        {
            IMessageChannel chan = (IMessageChannel)channel.Peek();
            await chan.SendMessageAsync(discordname.Peek() + " youve been temporarily or permanently banned from the bot");
            channel.Dequeue();
            discordname.Dequeue();
            pokequeue.Dequeue();
            username.Dequeue();
            pokemonfile.Dequeue();
            trainername.Dequeue();
        }
        
        
    }


}





