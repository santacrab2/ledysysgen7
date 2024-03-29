﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Discord.Rest;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

public class discordbot 
{
    public static Discord.Interactions.IResult result;
    public static int selectedmove = -1;
    public static int page = 0;
    public static IUserMessage msg;
    public static DiscordSocketClient _client;
    private CommandService _commands;
    Queue tradequeue = new Queue();
    public static readonly WebClient webClient = new WebClient();



    public IServiceProvider _services;


    public static void tot(string[] args)
    => new discordbot().MainAsync().GetAwaiter().GetResult();



    public async Task MainAsync()
    {
        _client = new DiscordSocketClient();
        _client.Log += Log;
        _client.Ready += ready;
        _commands = new CommandService();
        var token = Ledybot.Program.f1.token.Text;
        //var token = File.ReadAllText("token.txt");
        
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        CommandHandler ch = new CommandHandler(_client, _commands);
        await ch.InstallCommandsAsync();
   
        // Block this task until the program is closed.
        await Task.Delay(-1);

    }
    private async Task ready()
    {
        var _interactionService = new InteractionService(_client);
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        await _interactionService.RegisterCommandsToGuildAsync(872587205787394119);
        _client.InteractionCreated += async interaction =>
        {

            var ctx = new SocketInteractionContext(_client, interaction);
            result = await _interactionService.ExecuteCommandAsync(ctx, null);
        };
        _client.SlashCommandExecuted += slashtask;
    }
    public Task slashtask(SocketSlashCommand arg1)
    {

        if (!result.IsSuccess)
        {
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    // implement
                    break;
                case InteractionCommandError.UnknownCommand:
                    // implement
                    break;
                case InteractionCommandError.BadArgs:
                    // implement
                    break;
                case InteractionCommandError.Exception:
                    // implement
                    break;
                case InteractionCommandError.Unsuccessful:
                    // implement
                    break;
                default:
                    break;
            }
        }

        return Task.CompletedTask;

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
            _client.ReactionAdded += HandleReactionAsync;
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
            if(!message.HasCharPrefix('!', ref argPos)){
                if(message.Attachments.Count>0)
                {
                    await TryHandleMessageAsync(message);
                }
            }

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
        public async Task TryHandleMessageAsync(SocketMessage msg)
        {
            var attach = msg.Attachments.FirstOrDefault();
            if (attach == default)
                return;
            var att = Format.Sanitize(attach.Filename);
            if (!PKX.IsPKM(attach.Size)) 
                return;
            
            var pokme = PKMConverter.GetPKMfromBytes(await DownloadFromUrlAsync(attach.Url), 7);
            var newShowdown = new List<string>();
            var showdown = ShowdownParsing.GetShowdownText(pokme);
            foreach (var line in showdown.Split('\n'))
                newShowdown.Add(line);

            if (pokme.IsEgg)
                newShowdown.Add("\nPokémon is an egg");
            if (pokme.Ball > (int)Ball.None)
                newShowdown.Insert(newShowdown.FindIndex(z => z.Contains("Nature")), $"Ball: {(Ball)pokme.Ball} Ball");
            if (pokme.IsShiny)
            {
                var index = newShowdown.FindIndex(x => x.Contains("Shiny: Yes"));
                if (pokme.ShinyXor == 0 || pokme.FatefulEncounter)
                    newShowdown[index] = "Shiny: Square\r";
                else newShowdown[index] = "Shiny: Star\r";
            }
            var SID = string.Format("{0:0000}", pokme.TrainerSID7);
            var TID = string.Format("{0:000000}", pokme.TrainerID7);
            newShowdown.InsertRange(1, new string[] { $"OT: {pokme.OT_Name}", $"TID: {TID}", $"SID: {SID}", $"OTGender: {(Gender)pokme.OT_Gender}", $"Language: {(LanguageID)pokme.Language}" });
           await msg.Channel.SendMessageAsync(Format.Code(string.Join("\n", newShowdown).TrimEnd()));
        }

        public static async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cachedMsg, Cacheable<IMessageChannel,ulong>cc, SocketReaction reaction)
        {
            selectedmove = -1;
           
            
        var user = reaction.User.Value;
            if (user.IsBot)
                return;
            

            if (!cachedMsg.HasValue)
                msg = await cachedMsg.GetOrDownloadAsync().ConfigureAwait(false);
            else msg = cachedMsg.Value;




            IEmote[] reactions2 = { new Emoji("1️⃣"), new Emoji("2️⃣"), new Emoji("3️⃣"), new Emoji("4️⃣") };
            IEmote[] reactions = { new Emoji("⬅️"), new Emoji("➡️") };
        
            if (reaction.Emote.Name == reactions[0].Name || reaction.Emote.Name == reactions[1].Name)
            {
                if (reaction.Emote.Name == reactions[0].Name)
                {
                    if (page == 0)
                        page = trademodule.n.Count - 1;
                    else page--;
                }
                else
                {
                    if (page + 1== trademodule.n.Count)
                        page = 0;
                   else page++;
                    
                }

                
                trademodule.embed.Fields[0].Value=trademodule.n[page].ToString();
                trademodule.embed.Footer.Text = $"Page {page + 1} of {trademodule.n.Count}";
                
                await msg.RemoveReactionAsync(reactions[reaction.Emote.Name == reactions[0].Name ? 0 : 1], user);
                await msg.ModifyAsync(x => x.Embed = trademodule.embed.Build()).ConfigureAwait(false);

            }
            if (reaction.Emote.Name == reactions2[0].Name || reaction.Emote.Name == reactions2[1].Name|| reaction.Emote.Name == reactions2[2].Name || reaction.Emote.Name == reactions2[3].Name)
            {
                if (reaction.Emote.Name == reactions2[0].Name)
                    selectedmove = 0;
                else if (reaction.Emote.Name == reactions2[1].Name)
                    selectedmove = 1;
                else if (reaction.Emote.Name == reactions2[2].Name)
                    selectedmove = 2;
                else if (reaction.Emote.Name == reactions2[3].Name)
                    selectedmove = 3;
               await Ledybot.gymbattlemodule.battle(selectedmove);
              
            }


        }

    }

    //this will download the byte data from a pkm
    public static async Task<byte[]> DownloadFromUrlAsync(string url)
    {
        return await webClient.DownloadDataTaskAsync(url);
    }

    public class trademodule : InteractionModuleBase<SocketInteractionContext>
    {
        public static EmbedBuilder embed = new EmbedBuilder();
        
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
        public static string distribute = "false";
  
       // public static int[] tradevolvs = { 525, 75, 533, 93, 64, 67, 708, 710, 61, 79, 95, 123, 117, 137, 366, 112, 125, 126, 233, 356, 684, 682, 349 };
        public static int[] mythic = { 151, 251, 385, 386, 490, 491, 492, 493, 494, 646, 647, 648, 649, 719, 720, 721, 801, 802, 807 };
        public static bool distributestart = false;
        public static List<string> n;



        [SlashCommand("trade", "Receive the pokemon you want with the stats you want from the bot.")]


        public async Task stradedstr([Discord.Interactions.Summary(description: "your in game name")] string YourTrainerName, [Discord.Interactions.Summary(description: "Capitalize the Name like Piplup")] string DepositPokemon, [Discord.Interactions.Summary(description: "Put Showdown Text Here")] string ReceivingPokemon = "", Attachment Pk7 = default)
        {
            if (ReceivingPokemon != "")
            {
                var correctchannelcheck = Ledybot.Program.f1.BotChannels.Text.Split(',');
                if (!correctchannelcheck.Contains(Context.Channel.Id.ToString()))
                {
                    await RespondAsync("You can not use this command in this channel",ephemeral:true);
                    return;
                }
                if (discordname.Contains(Context.User))
                {
                    await RespondAsync("You are already in queue",ephemeral:true);
                    return;
                }
                int ptsstr = Array.IndexOf(Ledybot.Program.PKTable.Species7, DepositPokemon);
                if (ptsstr == -1)
                {
                    await RespondAsync("did not recognize your deposit pokemon",ephemeral:true);
                    return;
                }
                ptsstr = ptsstr + 1;
                //  if (tradevolvs.Contains(ptsstr))
                // {
                //       await ReplyAsync("you almost just broke the bot by depositing a trade evolution");
                //      return;
                //  }
                string[] pset = ReceivingPokemon.Split('\n');
                var l = Legal.ZCrystalDictionary;
                string temppokewait = Path.GetTempFileName();

                PKM pk = BuildPokemon(ReceivingPokemon, 7);
                if (File.Exists($"{Directory.GetCurrentDirectory()}//trainerinfo//{Context.User.Id}.txt"))
                {
                    string[] trsplit = File.ReadAllText($"{Directory.GetCurrentDirectory()}//trainerinfo//{Context.User.Id}.txt").Split('\n');
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
                    pk.OT_Name = YourTrainerName;
                if (ReceivingPokemon.Contains("OT:"))
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
                if (ReceivingPokemon.Contains("TID:"))
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
                if (ReceivingPokemon.Contains("SID:"))
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
                if (ReceivingPokemon.ToLower().Contains("shiny: yes"))
                {
                    pk.SetIsShiny(true);
                }
                if (new LegalityAnalysis(pk).Report().Contains("Invalid: SID should be 0"))
                    pk.SID = 0;
                if (!new LegalityAnalysis(pk).Valid)
                    pk = pk.Legalize();





                if (!new LegalityAnalysis(pk).Valid)
                {
                    await RespondAsync("Pokemon is illegal",ephemeral:true);
                    ShowdownSet Set = new(ReceivingPokemon);
                    var sav = TrainerSettings.DefaultFallback(7);
                    PK7 tru = new PK7();
                    await FollowupAsync(Set.SetAnalysis(sav, pk),ephemeral:true);
                    File.Delete(temppokewait);
                    return;

                }
                await RespondAsync("yay its legal good job!");

                byte[] g = pk.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, g);
                pokequeue.Enqueue(temppokewait);
                username.Enqueue(Context.User.Id);
                trainername.Enqueue(YourTrainerName);
                pokemonfile.Enqueue(pk);
                channel.Enqueue(Context.Channel);
                poketosearch.Enqueue(ptsstr);
                discordname.Enqueue(Context.User);

                await FollowupAsync("added " + Context.User + " to queue");
                await checkstarttrade();
            }
            if(Pk7 != default) {
                var correctchannelcheck = Ledybot.Program.f1.BotChannels.Text.Split(',');
                if (!correctchannelcheck.Contains(Context.Channel.Id.ToString()))
                {
                    await RespondAsync("You can not use this command in this channel",ephemeral:true);
                    return;
                }
                if (discordname.Contains(Context.User))
                {
                    await RespondAsync("you are already in queue",ephemeral:true);
                    return;
                }
                int ptsstr = Array.IndexOf(Ledybot.Program.PKTable.Species7, DepositPokemon);
                if (ptsstr == -1)
                {
                    await RespondAsync("Deposit pokemon not recognized",ephemeral:true);
                    return;
                }
                ptsstr = ptsstr + 1;
                string temppokewait = Path.GetTempFileName();
                //  if (tradevolvs.Contains(ptsstr))
                // {
                //      await ReplyAsync("you almost just broke the bot by depositing a trade evolution");
                //      return;
                //     }
                //this grabs the file the user uploads to discord if they even do it.
                pokm = Pk7;
                if (pokm == default)
                {
                    await RespondAsync("no attachment provided wtf are you doing?",ephemeral:true);
                    File.Delete(temppokewait);
                    return;
                }
                //this cleans up the filename the user submitted and checks that its a pk6 or 7
                att = Format.Sanitize(pokm.Filename);
                if (!att.Contains(".pk7"))
                {
                    await RespondAsync("no pk7 provided",ephemeral:true);
                    File.Delete(temppokewait);
                    return;
                }

                await RespondAsync("file accepted..now to check if you know what you are doing with pkhex");
                await webClient.DownloadFileTaskAsync(pokm.Url, temppokewait);

                buffer = await DownloadFromUrlAsync(pokm.Url);
                tradeable = PKMConverter.GetPKMfromBytes(buffer, 7);

                var la = new PKHeX.Core.LegalityAnalysis(tradeable);

                var l = Legal.ZCrystalDictionary;
                if (!la.Valid)
                {
                
                    var egg = MoveSetApplicator.GetSuggestedRelearnMoves(la);
                    tradeable.SetRelearnMoves(egg);
                    byte[] y = tradeable.DecryptedBoxData;
                    System.IO.File.WriteAllBytes(temppokewait, y);


                }


                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(tradeable)).Contains("Invalid Move"))
                {

                    if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(tradeable)).Contains("Invalid Move 1: Invalid Move"))
                    {
                        await ReplyAsync("invalid move 1, removing move");
                        tradeable.Move1 = 0;

                    }
                    if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(tradeable)).Contains("Invalid Move 2: Invalid Move"))
                    {


                        await ReplyAsync("invalid move 2, removing move");
                        tradeable.Move2 = 0;


                    }
                    if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(tradeable)).Contains("Invalid Move 3: Invalid Move"))
                    {

                        await ReplyAsync("invalid move 3, removing move");
                        tradeable.Move3 = 0;


                    }
                    if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(tradeable)).Contains("Invalid Move 4: Invalid Move"))
                    {
                        await ReplyAsync("invalid move 4, removing move");
                        tradeable.Move4 = 0;
                    }
                    tradeable.FixMoves();
                    tradeable.FixMoves();
                    tradeable.FixMoves();

                }
                if (tradeable.Move1 == 0 && tradeable.Move2 == 0 && tradeable.Move3 == 0 && tradeable.Move4 == 0)
                {
                    await ReplyAsync("all moves removed, giving new moves");
                    var move = new LegalityAnalysis(tradeable).GetSuggestedCurrentMoves();
                    tradeable.Moves = move;
                }
                if (tradeable.Move1 == 0)
                { tradeable.Move1_PP = 0; }
                if (tradeable.Move2 == 0)
                { tradeable.Move2_PP = 0; }
                if (tradeable.Move3 == 0)
                { tradeable.Move3_PP = 0; }
                if (tradeable.Move4 == 0)
                { tradeable.Move4_PP = 0; }

                if (LegalityFormatting.GetLegalityReport(new LegalityAnalysis(tradeable)).ToLower().Contains("invalid: static encounter shiny mismatch"))
                {
                    await ReplyAsync("pokemon is shiny locked...changing to non-shiny");
                    tradeable.SetIsShiny(false);

                }

                byte[] yre = tradeable.DecryptedBoxData;
                System.IO.File.WriteAllBytes(temppokewait, yre);
                var la2 = new LegalityAnalysis(tradeable);
                if (!la2.Valid)
                {
                    var sav = TrainerSettings.DefaultFallback(7);
                    ShowdownSet Set = new(tradeable);
                    await FollowupAsync("pokemon is illegal ",ephemeral:true);
                    await FollowupAsync(Set.SetAnalysis(sav, tradeable),ephemeral:true);
                    File.Delete(temppokewait);
                    return;
                }


                await FollowupAsync("yay its legal good job!");

                pokequeue.Enqueue(temppokewait);
                username.Enqueue(Context.User.Id);
                trainername.Enqueue(YourTrainerName);
                pokemonfile.Enqueue(tradeable);
                poketosearch.Enqueue(ptsstr);
                channel.Enqueue(Context.Channel);
                discordname.Enqueue(Context.User);

                await FollowupAsync("added " + Context.User + " to queue");
                await checkstarttrade();
            }

        }

        



       

        




        public async Task checkstarttrade()
        {
           
                if (pokequeue.Count == 1)
                    await FollowupAsync("finishing an ad or wonder trade, be right with you!",ephemeral:true);
                else
                    await FollowupAsync("There are " + pokequeue.Count + " trainers in the queue", ephemeral: true);
            
        }


       

        [SlashCommand("help","a guide for using the bot")]
      
        public async Task help()
        {
            page = 0;
            n = new List<string>();
            embed = new EmbedBuilder();
            embed.Color = new Color(147, 191, 230);
            embed.Title = $"{_client.CurrentUser.Username} Bot Help";
            embed.ThumbnailUrl = "https://www.shinyhunters.com/images/shiny/394.gif";
            embed.AddField("Prinplup is a Gen 7 GTS tradebot for\nSUN / MOON / ULTRA SUN / ULTRA MOON", "hi", false);
            n.Add("__Deposit a pokemon into the Gen 7 GTS__\n__Then use one of these 2 Commands to make the trade:__\n\n:large_blue_diamond:Attached .pk7 file\n```\n!trade DepositPokemon trainerName (and attach the file and hit send)```\n\n:large_blue_diamond:Showdown set\n```\n!trade trainername DepositPokemon ReceivingPokemon (and hit send)\nexample:!t Santa Caterpie Piplup\nShiny: Yes```**__Deposit Pokemon's name must be Capitalized__**");
            n.Add("***Do not deposit or request the following - they will not trade over GTS:*** \n*Mythical Pokemon*\n*Event Pokemon*\n*Fusions*\n *Un-Tradeable Forms*\n*Un-Tradeable Ribbons*\n*Un-Tradeable Moves*\n**If you need a mythical or event from this generation use the wondertrade bot. !wth**\n *Please use quotes around your trainer name, if your trainer name has a space in it*\n```\nex: !trade \"bewear hugs\"");
            n.Add("Pokedex Function (helps you figure out legal moves and other stats for your Pokemon)\n**!dex pokemon**\n```\nex: !dex pidgey\n*works in reverse too*\n!dex 016```");
            n.Add("**!convert**\nMakes you a pk7 file from a showdown set```\nexample: !convert Piplup```");
            n.Add("!settrainer, (**!st**)\nSets your trainer info with the bot permanently so anything you make will have that info!\nThis is also automatically captured if you trade the bot a pokemon you caught or bred\n```Example: !st OT: Santa\nTID: 123456\nSID: 1234```");
            embed.ImageUrl = "https://c.tenor.com/aVgHd6soz1wAAAAC/prinplup-piplup.gif";
            embed.Fields[0].Value = n[0].ToString();
            embed.WithFooter($"Page {page + 1} of {n.Count}");
            IEmote[] reactions = { new Emoji("⬅️"), new Emoji("➡️") };
            await RespondAsync("help");
            var listmsg = await Context.Channel.SendMessageAsync(embed: embed.Build());

            _ = Task.Run(() => listmsg.AddReactionsAsync(reactions).ConfigureAwait(false));

        }

        [SlashCommand("hi","says hi to the bot")]
        public async Task hi()
        {
            await RespondAsync(":middle_finger:",ephemeral:true);
        }
        [SlashCommand("fuckyou","says fuck you to the bot")]
        public async Task fuckyou()
        {
            await RespondAsync(":kissing_heart:",ephemeral:true);
        }
        [SlashCommand("queue","shows the current queue")]
       
        public async Task que()
        {
            Object[] arr = discordname.ToArray();
            var sb = new System.Text.StringBuilder();
            embed = new EmbedBuilder();
            if(arr.Length ==0)
            {
              await  RespondAsync("queue is empty",ephemeral:true);
            }
            int r = 0;
            foreach (object i in arr)
            {
                
                sb.AppendLine((r+1).ToString() +". " + arr[r].ToString());
                r++;
            }
            embed.AddField(x =>
            {
                
                x.Name = "Queue:";
                x.Value = sb.ToString();
                x.IsInline = false;
                
                
            });
            await RespondAsync( embed: embed.Build(),ephemeral:true);
        }
        [SlashCommand("dex","shows pokedex info for a specified pokemon")]
        public async Task dex(string pokemon = "random")

        {
           if(pokemon.ToLower() == "random")
            {
                Random randit = new Random();
                int rannumb = randit.Next(1, 807);
                await dex2(rannumb);
                return;
            }
            EmbedFooterBuilder x = new EmbedFooterBuilder();
            var baseLink = "https://raw.githubusercontent.com/BakaKaito/HomeImages/main/homeimg/128x128/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_');
            var MyArrayLower = Ledybot.Program.PKTable.Species7.Select(s => s.ToLower()).ToArray();
            
            if (Array.IndexOf(MyArrayLower, pokemon.ToLower()) == -1 || Array.IndexOf(MyArrayLower, pokemon.ToLower()) > 807)
            {
                embed = new EmbedBuilder();
                embed.Color = new Color(147, 191, 230);
                embed.Title = "This bot only supports Generation 1-7, dex# 1-807 ";
                await RespondAsync(embed: embed.Build(),ephemeral:true);
                return;
            }

            else

            {
                
                int i = 0;
                i = Array.IndexOf(MyArrayLower, pokemon.ToLower());
                i = i + 1;
                baseLink[2] = i < 10 ? $"000{i}" : i < 100 && i > 9 ? $"00{i}" : $"0{i}";
                baseLink[8] = "r.png";
                var link = string.Join("_", baseLink);
                embed = new EmbedBuilder().WithFooter(x);
                
                embed.Color = new Color(147, 191, 230);
                embed.Title = "National Pokedex #" + i + " " + pokemon;
                embed.ThumbnailUrl = "https://play.pokemonshowdown.com/sprites/ani-shiny/" + pokemon.ToLower() + ".gif";
                System.Text.StringBuilder abil = new System.Text.StringBuilder();

                Stream stra = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.Types.txt");
                StreamReader readera = new StreamReader(stra);
                var typez = readera.ReadToEnd().Split('\n')[i];
                readera.Close();
                stra.Close();

                Stream stri = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.EggGroups.txt");
                StreamReader readeri = new StreamReader(stri);
                var egggr = readeri.ReadToEnd().Split('\n')[i];
                readeri.Close();
                stri.Close();

                embed.AddField("Type: " + typez, "Egg Group(s): " +"\n" + egggr, true);
                Stream strb = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.GenderRatios.txt");
                StreamReader readerb = new StreamReader(strb);
                var genders = readerb.ReadToEnd().Split('\n')[i];
                readerb.Close();
                strb.Close();

                Stream strj = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.Introduced.txt");
                StreamReader readerj = new StreamReader(strj);
                var intro = readerj.ReadToEnd().Split('\n')[i];
                strj.Close();
                readerj.Close();

                embed.AddField("Gender Ratios: " + "\n"+ genders,  intro,true);

                foreach (int d in Ledybot.Program.PKTable.getAbilities7(i, default))
                {

                    string bab = Ledybot.Program.PKTable.Ability7[d - 1];
                    abil.AppendLine(bab);
                }

                Stream strd = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.Height.txt");
                StreamReader readerd = new StreamReader(strd);
                var height = readerd.ReadToEnd().Split('\n')[i];
                readerd.Close();
                strd.Close();
                Stream stre = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.Weight.txt");
                StreamReader readere = new StreamReader(stre);
                var weight = readere.ReadToEnd().Split('\n')[i];
                embed.AddField("Height: " + height, "Weight: " + weight, true);


                Stream strk = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.Catch_Rate.txt");
                StreamReader readerk = new StreamReader(strk);
                var CR = readerk.ReadToEnd().Split('\n')[i];
                readerk.Close();
                strk.Close();

                Stream strc = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.BaseStats.txt");
                StreamReader readerc = new StreamReader(strc);
                var statc = readerc.ReadToEnd().Split('\n')[i];
                var nstatc = statc.Replace('\t', '\n');
                readerc.Close();
                strc.Close();
                embed.AddField("Base Stats", nstatc + "\n" + "**Catch Rate: **" + CR, true);

                embed.AddField("Abilities:", abil,true);

                var quickpk = BuildPokemon(pokemon, 7);
                if (quickpk != null)
                {
                    int[] sugmov = MoveSetApplicator.GetMoveSet(quickpk, true);
                    if (sugmov.Count() != 0)
                    {
                        System.Text.StringBuilder smov = new System.Text.StringBuilder();
                        foreach (int j in sugmov)
                        {
                            string bmov = Ledybot.Program.PKTable.Moves7[j];
                            smov.AppendLine(bmov);
                        }

                        embed.AddField("Suggested Moves:", smov, true);
                    }
                }
                Stream strf = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.DexFlavor.txt");
                StreamReader reader = new StreamReader(strf);
                var entry = reader.ReadToEnd().Split('\n')[i];
                reader.Close();
                strf.Close();


                embed.ImageUrl = link;
                using (WebClient cl = new WebClient())
                { 
                    
                    try
                    {
                       
                        Stream mystream = cl.OpenRead(link);
                        mystream.Close();
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {
                            
                            baseLink[4] = "fo";
                            link = string.Join("_", baseLink);
                            embed.ImageUrl = link;
                        }
                    }
                    try
                    {
                        Stream mystream = cl.OpenRead(link);
                        mystream.Close();
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {

                            baseLink[4] = "mo";
                            link = string.Join("_", baseLink);
                            embed.ImageUrl = link;
                        }
                    }
                    try
                    {
                        Stream mystream = cl.OpenRead(link);
                        mystream.Close();
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {

                            baseLink[4] = "uk";
                            link = string.Join("_", baseLink);
                            embed.ImageUrl = link;
                        }
                    }
                    try
                    {
                        Stream mystream = cl.OpenRead(link);
                        mystream.Close();
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {
                            
                            baseLink[4] = "fd";
                            link = string.Join("_", baseLink);
                            embed.ImageUrl = link;
                        }
                    }
                    try
                    {
                        Stream mystream = cl.OpenRead(link);
                        mystream.Close();
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {

                            baseLink[4] = "md";
                            link = string.Join("_", baseLink);
                            embed.ImageUrl = link;
                        }
                    }
                    try
                    {
                        Stream mystream = cl.OpenRead(link);
                        mystream.Close();
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {
                            
                            baseLink[4] = "mf";
                            link = string.Join("_", baseLink);
                            embed.ImageUrl = link;
                            
                        }
                    }
                    finally
                    {
                       
                        x.Text = "Dex entry: " + entry;
                        await RespondAsync(embed: embed.Build(),ephemeral:true);
                        
                    }
                }
            }
        }

      
        public async Task dex2(int national)

        {
            EmbedFooterBuilder x = new EmbedFooterBuilder();
           
            var baseLink = "https://raw.githubusercontent.com/BakaKaito/HomeImages/main/homeimg/128x128/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_');
            if (national > 807)
            {

                embed = new EmbedBuilder();
                embed.Color = new Color(147, 191, 230);
                embed.Title = "This bot only supports Generation 1-7, dex# 1-807 ";
                await ReplyAsync(embed: embed.Build());
                return;
            }

            else
            {
                baseLink[2] = national < 10 ? $"000{national}" : national < 100 && national > 9 ? $"00{national}" : $"0{national}";
                baseLink[8] = "r.png";
                var link = string.Join("_", baseLink);
                embed = new EmbedBuilder().WithFooter(x);
                
                embed.Color = new Color(147, 191, 230);
                embed.Title = "National Pokedex #" + (national) + " " + Ledybot.Program.PKTable.Species7[national - 1];
                embed.ThumbnailUrl = "https://play.pokemonshowdown.com/sprites/ani-shiny/" + Ledybot.Program.PKTable.Species7[national - 1].ToLower() + ".gif";
                System.Text.StringBuilder abil = new System.Text.StringBuilder();

                Stream stra = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.Types.txt");
                StreamReader readera = new StreamReader(stra);
                var typez = readera.ReadToEnd().Split('\n')[national];
                readera.Close();
                stra.Close();

                Stream stri = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.EggGroups.txt");
                StreamReader readeri = new StreamReader(stri);
                var egggr = readeri.ReadToEnd().Split('\n')[national];
                readeri.Close();
                stri.Close();

                embed.AddField("Type: " + typez, "Egg Group(s): " +"\n" + egggr, true);

                Stream strb = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.GenderRatios.txt");
                StreamReader readerb = new StreamReader(strb);
                var genders = readerb.ReadToEnd().Split('\n')[national];
                readerb.Close();
                strb.Close();
               

                Stream strj = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.Introduced.txt");
                StreamReader readerj = new StreamReader(strj);
                var intro = readerj.ReadToEnd().Split('\n')[national];
                strj.Close();
                readerj.Close();

                embed.AddField("Gender Ratios: " + "\n"+genders,  intro, true);

                Stream strd = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.Height.txt");
                StreamReader readerd = new StreamReader(strd);
                var height = readerd.ReadToEnd().Split('\n')[national];
                readerd.Close();
                strd.Close();
                Stream stre = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.Weight.txt");
                StreamReader readere = new StreamReader(stre);
                var weight = readere.ReadToEnd().Split('\n')[national];
                embed.AddField("Height: " + height, "Weight: " + weight, true);


                Stream strk = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.Catch_Rate.txt");
                StreamReader readerk = new StreamReader(strk);
                var CR = readerk.ReadToEnd().Split('\n')[national];
                readerk.Close();
                strk.Close();

                Stream strc = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.BaseStats.txt");
                StreamReader readerc = new StreamReader(strc);
                var statc = readerc.ReadToEnd().Split('\n')[national];
                var nstatc = statc.Replace('\t', '\n');
                readerc.Close();
                strc.Close();
                embed.AddField("Base Stats", nstatc + "\n" + "**Catch Rate: **" + CR, true);
                foreach (int d in Ledybot.Program.PKTable.getAbilities7(national, default))
                {

                    string bab = Ledybot.Program.PKTable.Ability7[d - 1];
                    abil.AppendLine(bab);
                }
                embed.AddField("Abilities:", abil, true);

                var quickpk = BuildPokemon(Ledybot.Program.PKTable.Species7[national - 1], 7);
                if(quickpk != null) { 
                int[] sugmov = MoveSetApplicator.GetMoveSet(quickpk, true);
                    if (sugmov.Count() != 0)
                    {
                        System.Text.StringBuilder smov = new System.Text.StringBuilder();
                        foreach (int j in sugmov)
                        {
                            string bmov = Ledybot.Program.PKTable.Moves7[j];
                            smov.AppendLine(bmov);
                        }
                        embed.AddField("Suggested Moves:", smov, true);
                    }
                 }


                Stream strf = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ledybot.DexFlavor.txt");
                StreamReader reader = new StreamReader(strf);
                var entry = reader.ReadToEnd().Split('\n')[national];
                reader.Close();
                strf.Close();

              



                embed.ImageUrl = link;
                using (WebClient cl = new WebClient())
                {



                    try
                    {

                        Stream mystream = cl.OpenRead(link);
                        mystream.Close();
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {

                            baseLink[4] = "fo";
                            link = string.Join("_", baseLink);
                            embed.ImageUrl = link;
                        }
                    }
                    try
                    {
                        Stream mystream = cl.OpenRead(link);
                        mystream.Close();
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {

                            baseLink[4] = "mo";
                            link = string.Join("_", baseLink);
                            embed.ImageUrl = link;
                        }
                    }
                    try
                    {
                        Stream mystream = cl.OpenRead(link);
                        mystream.Close();
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {

                            baseLink[4] = "uk";
                            link = string.Join("_", baseLink);
                            embed.ImageUrl = link;
                        }
                    }
                    try
                    {
                        Stream mystream = cl.OpenRead(link);
                        mystream.Close();
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {

                            baseLink[4] = "fd";
                            link = string.Join("_", baseLink);
                            embed.ImageUrl = link;
                        }
                    }
                    try
                    {
                        Stream mystream = cl.OpenRead(link);
                        mystream.Close();
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {

                            baseLink[4] = "md";
                            link = string.Join("_", baseLink);
                            embed.ImageUrl = link;
                        }
                    }
                    try
                    {
                        Stream mystream = cl.OpenRead(link);
                        mystream.Close();
                    }
                    catch (WebException wex)
                    {
                        if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                        {

                            baseLink[4] = "mf";
                            link = string.Join("_", baseLink);
                            embed.ImageUrl = link;

                        }
                    }
                    finally
                    {
                       
                        x.Text = "Dex entry: " + entry;
                        await RespondAsync(embed: embed.Build(),ephemeral:true);

                    }
                }
            }
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
        public static async Task slow()
        {
            IMessageChannel chan = (IMessageChannel)channel.Peek();
            await chan.SendMessageAsync(discordname.Peek() + " I could not find your deposit on the GTS, so the trades been cancelled");
            channel.Dequeue();
            discordname.Dequeue();
            pokequeue.Dequeue();
            username.Dequeue();
            pokemonfile.Dequeue();
            trainername.Dequeue();
            
            
        }
        public static async Task notrade()
        {
            IMessageChannel chan = (IMessageChannel)channel.Peek();
            await chan.SendMessageAsync(discordname.Peek() + " something went wrong with your trade, please try again. if you get this message two times in a row, please ping Santacrab420");
            channel.Dequeue();
            discordname.Dequeue();
            pokequeue.Dequeue();
            username.Dequeue();
            pokemonfile.Dequeue();
            trainername.Dequeue();
            

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
                var pk = sav.GetLegalFromTemplate(tru,re,out _);
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
       

       
        [SlashCommand("convert", "makes a pk7 file from showdown text")]
        public async Task convert(string ShowdownSet)
        {
            try
            {
                string[] pset = ShowdownSet.Split('\n');
                PKM pk = BuildPokemon(ShowdownSet, 7);
                string temppokewait = Path.GetTempFileName().Replace(".tmp", $"{GameInfo.Strings.Species[pk.Species]}.{pk.Extension}").Replace("tmp", "");

                



                if (ShowdownSet.Contains("OT:"))
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
                if (ShowdownSet.Contains("TID:"))
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
                if (ShowdownSet.Contains("SID:"))
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
                if (ShowdownSet.ToLower().Contains("shiny: yes"))
                {
                    pk.SetIsShiny(true);
                }
                if(!new LegalityAnalysis(pk).Valid)
                {
                    await ReplyAsync("I could not legalize that set");
                    File.Delete(temppokewait);
                    return;
                }
                
                
                    byte[] yre = pk.DecryptedBoxData;
                    File.WriteAllBytes(temppokewait, yre);
                   await RespondWithFileAsync(temppokewait, text:"Here is your legalized pk file");
                    File.Delete(temppokewait);
                return;
                
            }
            catch
            { await RespondAsync("I wasn't able to make a file from that set",ephemeral:true);
                 }

        }

        [SlashCommand("settrainer","changes your trainer info stored with the bot")]
  
        public async Task settrainerinfo(string trainerinfo)
        {
            if (!Directory.Exists($"{Directory.GetCurrentDirectory()}//trainerinfo//"))
                Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}//trainerinfo//");
            if (trainerinfo.Contains("OT:") || trainerinfo.Contains("SID:") || trainerinfo.Contains("TID:"))
            {
                File.WriteAllText($"{Directory.GetCurrentDirectory()}//trainerinfo//{Context.User.Id}.txt", trainerinfo);
                await RespondAsync("trainer info saved",ephemeral:true);
            }
            else await RespondAsync("Please use OT: trainername\nTID: XXXXX\nSID: XXXX only",ephemeral:true);
        }
        [SlashCommand("trainerinfo","shows your trainer info saved with the bot")]
       
        public async Task gettrainerinfo()
        {
            if (File.Exists($"{Directory.GetCurrentDirectory()}//trainerinfo//{Context.User.Id}.txt"))
            {
                await RespondAsync(File.ReadAllText($"{Directory.GetCurrentDirectory()}//trainerinfo//{Context.User.Id}.txt"),ephemeral:true);
                return;
            }
            await RespondAsync("no trainer info found",ephemeral:true);
        }
        [SlashCommand("wt","request a pokemon for the wonder trade bot")]
        public async Task wtrequests(string WTRequest)
        {
            bool queued = false;
            if (!TwitchBot.wtuser.Contains(Context.User.Username))
            {
                var files = Directory.GetFiles(Ledybot.Program.f1.wtfolder.Text);

                var converset = ConvertToShowdown(WTRequest);

               
                var comppk = new PK7();
                comppk.ApplySetDetails(converset);
                foreach (string file in files)
                {
                    var temppk = PKMConverter.GetPKMfromBytes(File.ReadAllBytes(file), 7);
                    if (temppk.Species == comppk.Species && temppk.Form == comppk.Form && temppk.IsShiny == comppk.IsShiny)
                    {
                        TwitchBot.wtqueue.Enqueue(temppk);
                        TwitchBot.wtuser.Enqueue(Context.User.Username);
                        await RespondAsync("your request has been added to the queue!",ephemeral:true);
                        queued = true;
                    }

                }
                if (!queued)
                    await RespondAsync( "no file found",ephemeral:true);
                return;
            }
            else
            {
                await RespondAsync("you are already in queue",ephemeral:true);
                return;
            }
        }
        [SlashCommand("wtlist","shows available pokemon for wonder trade")]
        public async Task wtlist()
        {
            page = 0;
            embed = new EmbedBuilder();
            n = new List<string>();
            var wtfiled = Directory.GetFiles(Ledybot.Program.f1.wtfolder.Text);
            
            var sb = new System.Text.StringBuilder();

            foreach (string file in wtfiled)
            {
                var sotemppk = PKMConverter.GetPKMfromBytes(File.ReadAllBytes(file));
                sb.AppendLine((sotemppk.IsShiny ? "★" : "") + (sotemppk.Form == 0 ? $"{(Species)sotemppk.Species}" : $"{(Species)sotemppk.Species}-{ShowdownParsing.GetStringFromForm(sotemppk.Form, GameInfo.Strings, sotemppk.Species, sotemppk.Format)}"));
            }
            var wtfilelist = sb.ToString();
            while (wtfilelist.Length > 0)
            {
                if (wtfilelist.Length > 1000)
                    n.Add(wtfilelist.Substring(0, 1000));
                else
                    n.Add(wtfilelist.Substring(0, wtfilelist.Length));

                if (wtfilelist.Length > 1000)
                    wtfilelist = wtfilelist.Remove(0, 1000);
                else
                    wtfilelist = wtfilelist.Remove(0, wtfilelist.Length);



            }
            embed.Title = $"Wonder Trade List";

            embed.AddField("List", "hi");

            embed.Fields[0].Value = n[0].ToString();

            embed.WithFooter($"Page {page + 1} of {n.Count}");
            IEmote[] reactions = { new Emoji("⬅️"), new Emoji("➡️") };
            await RespondAsync("list");
            var listmsg = await Context.Channel.SendMessageAsync(embed: embed.Build());

            _ = Task.Run(() => listmsg.AddReactionsAsync(reactions).ConfigureAwait(false));
        }
        [SlashCommand("wthelp","a guide for wonder trade mode")]
   
        public async Task wth() 
        {
            embed = new EmbedBuilder();
            embed.AddField("WonderTrade Commands", "!wt pokemon\n!wt Darkrai\nShiny: Yes\n\n!wtlist\nShows all the available pokemon to request. A Star means shiny.\n\n!wtq\nShows the current wonder trade queue");
            await RespondAsync(embed: embed.Build(),ephemeral:true);
        }
        [SlashCommand("wondertradequeue","shows the wonder trade queue")]
       
        public async Task wtque()
        {
            Object[] arr = TwitchBot.wtuser.ToArray();
            var sb = new System.Text.StringBuilder();
            embed = new EmbedBuilder();
            if (arr.Length == 0)
            {
                await RespondAsync("queue is empty",ephemeral:true);
            }
            int r = 0;
            foreach (object i in arr)
            {

                sb.AppendLine((r + 1).ToString() + ". " + arr[r].ToString());
                r++;
            }
            embed.AddField(x =>
            {

                x.Name = "WT Queue:";
                x.Value = sb.ToString();
                x.IsInline = false;


            });
            await RespondAsync(embed: embed.Build(),ephemeral:true);
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


}

    










