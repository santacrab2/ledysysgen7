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



public class discordbot
{


    public static DiscordSocketClient _client;
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
        var token = Ledybot.Program.f1.token.Text;
        //var token = File.ReadAllText("token.txt");

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

            newShowdown.InsertRange(1, new string[] { $"OT: {pokme.OT_Name}", $"TID: {pokme.TrainerID7}", $"SID: {pokme.TrainerSID7}", $"OTGender: {(Gender)pokme.OT_Gender}", $"Language: {(LanguageID)pokme.Language}" });
           await msg.Channel.SendMessageAsync(Format.Code(string.Join("\n", newShowdown).TrimEnd()));
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
        public static string distribute = "false";
        public static string trainer;
        public static int[] tradevolvs = { 525, 75, 533, 93, 64, 67, 708, 710, 725 };
        public static int[] mythic = { 151, 251, 385, 386, 490, 491, 492, 493, 494, 646, 647, 648, 649, 719, 720, 721, 801, 802, 807 };
        public static bool distributestart = false;
        
        

        [Command("trade")]
        [Alias("t")]
      
        public async Task stradestringdepo(string trainer, string pts, [Remainder] string set)
        {
            
            int ptsstr = Array.IndexOf(Ledybot.Program.PKTable.Species7, pts);
            if(ptsstr == -1)
            {
                await ReplyAsync("did not recognize your deposit pokemon");
                return;
            }
            ptsstr = ptsstr + 1;
            if (tradevolvs.Contains(ptsstr))
            {
                await ReplyAsync("you almost just broke the bot by depositing a trade evolution, you are a fucking asshole :)");
                return;
            }
            string[] pset = set.Split('\n');
            var l = Legal.ZCrystalDictionary;
            string temppokewait = Path.GetTempFileName();

            PKM pk = BuildPokemon(set, 7);


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
                pk.SetShiny();
            }
            
            if (l.ContainsValue(pk.HeldItem))
            {
                
                    await ReplyAsync("no z-crystals...fixing pokemon");
                    pk.ApplyHeldItem(571, pk.Format);
                


                
            }
            if (!new LegalityAnalysis(pk).Valid)
            {
                await ReplyAsync("Pokemon is illegal dumbass");
                await ReplyAsync(LegalityFormatting.Report(new LegalityAnalysis(pk)));
                File.Delete(temppokewait);
                return;

            }
            await ReplyAsync("yay its legal good job!");

            byte[] g = pk.DecryptedBoxData;
            System.IO.File.WriteAllBytes(temppokewait, g);
            pokequeue.Enqueue(temppokewait);
            username.Enqueue(Context.User.Id);
            trainername.Enqueue(trainer);
            pokemonfile.Enqueue(pk);
            channel.Enqueue(Context.Channel);
            poketosearch.Enqueue(ptsstr);
            discordname.Enqueue(Context.User);
           
            await ReplyAsync("added " + Context.User + " to queue");
            await checkstarttrade();


        }

        


        [Command("trade")]
        [Alias("t")]
     
        public async Task stradenotidpts(string trainer, int pts,[Remainder] string set)
        {
            
            if (tradevolvs.Contains(pts))
            {
                await ReplyAsync("you almost just broke the bot by depositing a trade evolution, you are a fucking asshole :)");
                return;
            }
            string[] pset = set.Split('\n');
            var l = Legal.ZCrystalDictionary;
            string temppokewait = Path.GetTempFileName();

            PKM pk = BuildPokemon(set, 7);
        

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
                pk.SetShiny();
            }
         
            if (l.ContainsValue(pk.HeldItem))
            {

                await ReplyAsync("no z-crystals...fixing pokemon");
                pk.ApplyHeldItem(571, pk.Format);




            }
            if (!new LegalityAnalysis(pk).Valid)
            {
                await ReplyAsync("Pokemon is illegal dumbass");
                await ReplyAsync(LegalityFormatting.Report(new LegalityAnalysis(pk)));
                File.Delete(temppokewait);
                return;

            }
            await ReplyAsync("yay its legal good job!");

            byte[] g = pk.DecryptedBoxData;
            System.IO.File.WriteAllBytes(temppokewait, g);
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
        [Alias("t")]
      
        public async Task pstrtrade([Summary("poke to search")] string pts, [Remainder] string trainer)
        {
            
            int ptsstr = Array.IndexOf(Ledybot.Program.PKTable.Species7, pts);
            if (ptsstr == -1)
            {
                await ReplyAsync("Deposit pokemon not recognized");
                return;
            }
            ptsstr = ptsstr + 1;
            string temppokewait = Path.GetTempFileName();
            if (tradevolvs.Contains(ptsstr))
            {
                await ReplyAsync("you almost just broke the bot by depositing a trade evolution, you are a fucking asshole :)");
                return;
            }
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
                await ReplyAsync("pokemon is illegal dumbass");
                File.Delete(temppokewait);
                return;
            }
           
            if (l.ContainsValue(tradeable.HeldItem))
            {

                await ReplyAsync("no z-crystals...fixing pokemon");
                tradeable.ApplyHeldItem(571, tradeable.Format);




            }






            await ReplyAsync("yay its legal good job!");
            pokequeue.Enqueue(temppokewait);
            username.Enqueue(Context.User.Id);
            trainername.Enqueue(trainer);
            pokemonfile.Enqueue(tradeable);
            poketosearch.Enqueue(ptsstr);
            channel.Enqueue(Context.Channel);
            discordname.Enqueue(Context.User);
           
            await ReplyAsync("added " + Context.User + " to queue");
            await checkstarttrade();



        }



        [Command("trade")]
        [Alias("t")]
      
        public async Task ptrade([Summary("poke to search")] int pts, [Remainder] string trainer)
        {
            
            string temppokewait = Path.GetTempFileName();
            if (tradevolvs.Contains(pts))
            {
                await ReplyAsync("you almost just broke the bot by depositing a trade evolution, you are a fucking asshole :)");
                return;
            }
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
                await ReplyAsync("pokemon is illegal dumbass");
                File.Delete(temppokewait);
                return;
            }
            
            if (l.ContainsValue(tradeable.HeldItem))
            {

                await ReplyAsync("no z-crystals...fixing pokemon");
                tradeable.ApplyHeldItem(571, tradeable.Format);




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

        




        public async Task checkstarttrade()
        {
           
                if (pokequeue.Count == 1)
                    await ReplyAsync("finishing an ad trade, be right with you!");
                else
                    await ReplyAsync("There are " + pokequeue.Count + " trainers in the queue");
            
        }
        [Command("start")]
        [RequireOwner]
        public async Task startdistribute()
        {
            await ReplyAsync("starting distribution");
            Ledybot.MainForm.combo_distri.SelectedIndex = 0;
            if (Ledybot.MainForm.btn_Start.Enabled == true)
                Ledybot.MainForm.btn_Start_Click(null, EventArgs.Empty);
        }
   
           
            

        
        [Command("stop")]
        [RequireOwner]
        public async Task stop()
        {

            await ReplyAsync("stopping distribution");
            Ledybot.MainForm.combo_distri.SelectedIndex = 1;
            if (Ledybot.MainForm.btn_Start.Enabled == false)
                Ledybot.MainForm.btn_Stop_Click(null, EventArgs.Empty);
        }

        [Command("queueclear")]
        [Alias("rq")]
        [RequireOwner]
        public async Task queueclear()
        {
         
                pokequeue.Dequeue();
                username.Dequeue();
                pokemonfile.Dequeue();
                trainername.Dequeue();
                channel.Dequeue();
                discordname.Dequeue();
           
                await ReplyAsync("the first person in line has been removed");
              
            
     

        }

        [Command("clear")]
        [RequireOwner]
        public async Task clqueue()
        {
            
                pokequeue.Clear();
                username.Clear();
                pokemonfile.Clear();
                trainername.Clear();
                channel.Clear();
                discordname.Clear();
                poketosearch.Clear();
                await ReplyAsync("the entire queue has been cleared");
                
         
        }

        [Command("help")]
        [Alias("h")]
        public async Task help()
        {
            var embed = new EmbedBuilder();
            embed.Color = new Color(147, 191, 230);
            embed.Title = "Piplup Bot Help";
            embed.ThumbnailUrl = "https://www.shinyhunters.com/images/shiny/393.gif";
            embed.AddField("Piplup is a Gen 7 GTS Sysbot for" + "\n", "SUN / MOON / ULTRA SUN / ULTRA MOON", false);
            embed.AddField("⠀", "__Deposit a pokemon into the Gen 7 GTS__" + "\n" + "__Then use one of these 2 Commands to make the trade:__" + "⠀", false);
            embed.AddField(":large_blue_diamond:Attached .pk7 file" + "\n", "```" + "\n" + "!trade DepositPokemon trainerName (and attach the file and hit send)```", true);
            embed.AddField(":large_blue_diamond:Showdown set" + "\n", "```" + "\n" + "!trade trainername DepositPokemon showdownset (and hit send)```"+"\n"+"Deposit Pokemon's name must be Capitalized", true);
            embed.AddField("***Do not deposit or request the following - they will not trade over GTS and may break the bot:***",
                "*Mythical Pokemon*" + "\n" + "*Event Pokemon*" + "\n" + "*Special Pokemon*" + "\n" + "*Fusions*" + "\n" + " *Un-Tradeable Forms*" + "\n" + "*Un-Tradeable Ribbons*" + "\n" + "*Un-Tradeable Moves*" + "\n" + "*Special Items (Megastone/Z-Crystal)*" + "\n" + "⠀", false);
            embed.ImageUrl = "https://cdn.discordapp.com/attachments/733454651227373579/848772777641377832/piplup.gif";
            embed.AddField("*Showdown sets now accept batch commands!*" + "\n" + "⠀", " *Please use quotes around your trainer name, if your trainer name has a space in it*" + "\n" + "```" + "\n" + "ex: !trade \"bewear hugs\"" + "```", false);
            embed.AddField("Pokedex Function (helps you figure out legal moves and other stats for your Pokemon)" + "\n" + "**!dex pokemon**" + "\n", "```" + "\n" + "ex: !dex pidgey" + "\n" + "*works in reverse too*" + "\n" + "!dex 016" + "```" + "\n", true);
            await ReplyAsync(embed: embed.Build());
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
        [Command("Queue")]
        [Alias("q")]
        public async Task que()
        {
            Object[] arr = discordname.ToArray();
            var sb = new System.Text.StringBuilder();
            var embed = new EmbedBuilder();
            if(arr.Length ==0)
            {
              await  ReplyAsync("queue is empty");
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
            await ReplyAsync( embed: embed.Build());
        }
        [Command("dex")]
        public async Task dex([Remainder]string pokemon)

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
                var embed = new EmbedBuilder();
                embed.Color = new Color(147, 191, 230);
                embed.Title = "This bot only supports Generation 1-7, dex# 1-807 ";
                await ReplyAsync(embed: embed.Build());
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
                var embed = new EmbedBuilder().WithFooter(x);
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
                        await ReplyAsync(embed: embed.Build());
                        
                    }
                }
            }
        }

        [Command("dex")]
        public async Task dex2(int national)

        {
            EmbedFooterBuilder x = new EmbedFooterBuilder();
           
            var baseLink = "https://raw.githubusercontent.com/BakaKaito/HomeImages/main/homeimg/128x128/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_');
            if (national > 807)
            {

                var embed = new EmbedBuilder();
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
                var embed = new EmbedBuilder().WithFooter(x);
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
                        await ReplyAsync(embed: embed.Build());

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
            await chan.SendMessageAsync(discordname.Peek() + " you were too slow or too stupid, one of those, so the trades been cancelled");
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
                ShowdownSet set = new ShowdownSet(Set);
                IBattleTemplate re = new RegenTemplate(set, 7);
                // Generate a Blank Savefile


                var sav = TrainerSettings.GetSavedTrainerData(GameVersion.US, 7);
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
        [Command("catch")]
        [Alias("k")]
        public async Task tradecordcatch()
        {
            
            var embed = new EmbedBuilder();
            string direct;
            Random balrng = new Random();
            int ballrng = balrng.Next(24);
            while (ballrng == 15)
            {
                ballrng = balrng.Next(24);
            }
            Random frng = new Random();
            int farng = frng.Next(1, 3);
            if (farng != 1)
            {
                Random misrng = new Random();
                int missrng = misrng.Next(806);

                embed.Color = new Color(147, 191, 230);
                embed.Title = "Miss";
                embed.AddField("" + Context.User, "you failed to catch a " + Ledybot.Program.PKTable.Species7[missrng] + " in a " + Ledybot.Program.PKTable.Balls7[ballrng]);
                await ReplyAsync(embed: embed.Build());
                return;
            }
            try
            {
                Random carng = new Random();
            int catchrng = carng.Next(806);
            if (!File.Exists($"{Directory.GetCurrentDirectory()}//rolls.txt"))
                File.Create($"{Directory.GetCurrentDirectory()}//rolls.txt");
            while (File.ReadAllLines($"{Directory.GetCurrentDirectory()}//rolls.txt").Contains(catchrng.ToString()))
                catchrng = carng.Next(806);
           while(mythic.Contains(catchrng))
                catchrng = carng.Next(806);
           
            StreamWriter catches = File.AppendText($"{Directory.GetCurrentDirectory()}//rolls.txt");
            catches.WriteLine(catchrng);
            catches.Close();
            if (File.ReadAllLines($"{Directory.GetCurrentDirectory()}//rolls.txt").Count() > 800)
            {

                File.WriteAllText($"{Directory.GetCurrentDirectory()}//rolls.txt", string.Empty);

            }
            var tpk = PKMConverter.GetBlank(7);
            var shinymessage = "non-shiny";
           
                 tpk = BuildPokemon(Ledybot.Program.PKTable.Species7[catchrng], 7);


                tpk.Ball = BallApplicator.ApplyBallLegalRandom(tpk);

                Random level = new Random();
                tpk.CurrentLevel = level.Next(100);
                tpk = tpk.Legalize();
                while (!new LegalityAnalysis(tpk).Valid)
                {
                    tpk.CurrentLevel = level.Next(100);
                    tpk = tpk.Legalize();
                }
                int[] sugmov = MoveSetApplicator.GetMoveSet(tpk, true);
                tpk.SetMoves(sugmov);
                Random nat = new Random();
                int natue = nat.Next(24);
                tpk.Nature = natue;
                tpk.SetRandomIVs();

                Random shinrng = new Random();
                int shinyrng = shinrng.Next(4);
                if (shinyrng != 1)
                    tpk.SetIsShiny(true);
                if (new LegalityAnalysis(tpk).Report().Contains("Static Encounter shiny mismatch"))
                    tpk.SetIsShiny(false);
                tpk = tpk.Legalize();
                
                if (tpk.IsShiny)
                    shinymessage = "shiny";
       
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + Context.User.Id);
            }
            direct = Directory.GetCurrentDirectory() + "//" + Context.User.Id;
            string directfile;
            int a = 1;
            while (File.Exists(direct + "//" + a))
                a++;
            directfile = direct + "//" + a;
            File.WriteAllBytes(directfile, tpk.EncryptedBoxData);
            if (!Directory.Exists($"{Directory.GetCurrentDirectory()}//dexs//"))
                Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}//dexs//");
            if (!File.Exists($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt"))
                File.Create($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt");
            if (!File.ReadAllLines($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt").Contains(tpk.Species.ToString())|| File.ReadAllText($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt")== null){
                embed.AddField("Pokedex", $"Registered {Ledybot.Program.PKTable.Species7[tpk.Species - 1]} to your Pokedex");
               StreamWriter de = File.AppendText($"{Directory.GetCurrentDirectory()}//dexs///{Context.User.Id}.txt");
                de.WriteLine(tpk.Species);
                de.Close();
            }

            var baseLink = "https://raw.githubusercontent.com/BakaKaito/HomeImages/main/homeimg/128x128/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_');
            bool md = false;
            bool fd = false;



            if (Enum.IsDefined(typeof(Ledybot.LookupTable.GenderDependent), tpk.Species) && tpk.Form == 0)
            {
                if (tpk.Gender == 0)
                    md = true;
                else fd = true;
            }

            baseLink[2] = tpk.Species < 10 ? $"000{tpk.Species}" : tpk.Species < 100 && tpk.Species > 9 ? $"00{tpk.Species}" : $"0{tpk.Species}";
            baseLink[3] = tpk.Form < 10 ? $"00{tpk.Form}" : $"0{tpk.Form}";
            baseLink[4] = tpk.PersonalInfo.OnlyFemale ? "fo" : tpk.PersonalInfo.OnlyMale ? "mo" : tpk.PersonalInfo.Genderless ? "uk" : fd ? "fd" : md ? "md" : "mf";

            embed.Color = new Color(147, 191, 230);
            embed.ThumbnailUrl = $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Ballimg/50x50/{Ledybot.Program.PKTable.Balls7[tpk.Ball - 1].Split(' ')[0].ToLower()}ball.png";
            embed.AddField(Context.User + "'s catch!", " you threw a " + Ledybot.Program.PKTable.Balls7[tpk.Ball - 1] + " at a " + shinymessage + " " + Ledybot.Program.PKTable.Species7[tpk.Species - 1] + "...");
            embed.AddField("Results", "It put up a fight, but you caught " + shinymessage + "  " + Ledybot.Program.PKTable.Species7[tpk.Species - 1]);
            baseLink[8] = tpk.IsShiny ? "r.png" : "n.png";
            var baseLink2 = string.Join("_", baseLink);
            EmbedFooterBuilder x = new EmbedFooterBuilder();
            x.Text = "Id number: " + a;
            embed.Footer = x;
            embed.ImageUrl = baseLink2;
            
            if (File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy"))
            {
                byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy");
                var bpk = PKMConverter.GetPKMfromBytes(g, 7);
                var lvlProgress = (Experience.GetEXPToLevelUpPercentage(bpk.CurrentLevel, bpk.EXP, bpk.PersonalInfo.EXPGrowth) * 100.0).ToString("N1");
                int currentspec = 0;
                bool evolve = false;
                if (bpk.CurrentLevel < 100 && bpk.Species != 0)
                {
                    var xpMin = Experience.GetEXP(bpk.CurrentLevel + 1, bpk.PersonalInfo.EXPGrowth);
                    var xpGet = (uint)Math.Round(Math.Pow(bpk.CurrentLevel / 5.0 * ((2.0 * bpk.CurrentLevel + 10.0) / (bpk.CurrentLevel + bpk.CurrentLevel + 10.0)), 2.5) * (bpk.IsShiny ? 1.3 : 1.0), 0, MidpointRounding.AwayFromZero);
                    if (xpGet < 100)
                        xpGet = 175;
                    
                    bpk.EXP += xpGet;
                    while (bpk.EXP >= Experience.GetEXP(bpk.CurrentLevel + 1, bpk.PersonalInfo.EXPGrowth) && bpk.CurrentLevel < 100)
                    {
                        bpk.CurrentLevel++;
                      
                    }
             
                    var b = EvolutionTree.GetEvolutionTree(bpk, 7).GetBaseSpeciesForm(bpk.Species, bpk.Form);
                    var testpk = BuildPokemon(Ledybot.Program.PKTable.Species7[bpk.Species + 1], 7);
                    var sug = EncounterSuggestion.GetSuggestedMetInfo(testpk);
                    if (bpk.CurrentLevel >= sug.LevelMin && EvolutionTree.GetEvolutionTree(7).GetEvolutions(b, 0).Last() != bpk.Species)
                    {
                        evolve = true;
                        bool savenick = bpk.IsNicknamed;
                        currentspec = bpk.Species;
                        if (bpk.Species == b)
                            bpk.Species = EvolutionTree.GetEvolutionTree(7).GetEvolutions(b, 0).First();
                        else
                            bpk.Species = EvolutionTree.GetEvolutionTree(7).GetEvolutions(b, 0).Last();
                        if (savenick == false)
                            bpk.ClearNickname();
                        if (new LegalityAnalysis(bpk).Report().Contains("Evolution not valid"))
                        {
                            if (bpk.Species == EvolutionTree.GetEvolutionTree(7).GetEvolutions(b, 0).First())
                                bpk.Species = b;
                            else
                                bpk.Species = EvolutionTree.GetEvolutionTree(7).GetEvolutions(b, 0).First();
                            evolve = false;
                            if (savenick == false)
                                bpk.ClearNickname();
                        }
                        else
                        {
                            if (!File.Exists($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt"))
                                File.Create($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt");
                            if (!File.ReadAllLines($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt").Contains(bpk.Species.ToString()) || File.ReadAllText($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt") == null)
                            {
                                embed.AddField("Pokedex", $"Registered {Ledybot.Program.PKTable.Species7[bpk.Species - 1]} to your pokedex");
                                StreamWriter de = File.AppendText($"{Directory.GetCurrentDirectory()}//dexs///{Context.User.Id}.txt");
                                de.WriteLine(bpk.Species);
                                de.Close();
                            }
                        }
                    }
                    if (bpk.CurrentLevel == 100)
                        bpk.EXP = xpMin;
                    
                        File.WriteAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy", bpk.DecryptedBoxData);
                    
                    if (bpk.EXP >= xpMin)
                        embed.AddField($"\n{Context.User}'s Buddy " + (evolve ? bpk.IsNicknamed ? bpk.Nickname : Ledybot.Program.PKTable.Species7[currentspec-1] : bpk.Nickname ), evolve? $" gained {xpGet} EXP and leveled up to level {bpk.CurrentLevel} and evolved to {(Species)bpk.Species}!" : $" gained {xpGet} EXP and leveled up to level {bpk.CurrentLevel}!");
                    else embed.AddField($"\n{Context.User}'s Buddy " + (evolve ? bpk.IsNicknamed ? bpk.Nickname : Ledybot.Program.PKTable.Species7[currentspec - 1] : bpk.Nickname), evolve  ? $" gained {xpGet} EXP and evolved to {(Species)bpk.Species}!" :$" gained {xpGet} EXP!" );
                }
               
            }
            await ReplyAsync(embed: embed.Build());
            }
            catch
            {
                tradecordcatch();
                return;

            }
        }
        [Command("tradecord")]
        [Alias("tc")]
        public async Task tradecordtrade(string trainer, int pts, int idnumb,[Remainder]string trainerinfo="")
        {
            string[] tset = trainerinfo.Split('\n');
            string temppokewait = Path.GetTempFileName();
            if (!File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb))
            {
                await ReplyAsync("no pokemon assigned that id number");
                return;
            }
            byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb);
            var tpk = PKMConverter.GetPKMfromBytes(g, 7);
            if (trainerinfo.Contains("OT:"))
            {
                int q = 0;
                foreach (string b in tset)
                {
                    if (tset[q].Contains("OT:"))
                        tpk.OT_Name = tset[q].Replace("OT: ", "");
                    q++;
                }
            }
            if (trainerinfo.Contains("TID:"))
            {
                int h = 0;
                foreach (string v in tset)
                {
                    if (tset[h].Contains("TID:"))
                    {
                        int trid7 = Convert.ToInt32(tset[h].Replace("TID: ", ""));
                        tpk.TrainerID7 = trid7;

                    }
                    h++;
                }
            }
            if (trainerinfo.Contains("SID:"))
            {
                int h = 0;
                foreach (string v in tset)
                {
                    if (tset[h].Contains("SID:"))
                    {
                        int trsid7 = Convert.ToInt32(tset[h].Replace("SID: ", ""));
                        tpk.TrainerSID7 = trsid7;

                    }
                    h++;
                }
            }
            if (trainerinfo.Contains("OTGender: male"))
            {
                tpk.OT_Gender = 0;
            }

            if (trainerinfo.Contains("OTGender: Female"))
            {
                tpk.OT_Gender = 1;
            }
            byte[] pc = tpk.DecryptedBoxData;
            File.WriteAllBytes(temppokewait, pc);
            pokequeue.Enqueue(temppokewait);
            username.Enqueue(Context.User.Id);
            trainername.Enqueue(trainer);
            pokemonfile.Enqueue(tpk);
            channel.Enqueue(Context.Channel);
            poketosearch.Enqueue(pts);
            discordname.Enqueue(Context.User);
            await ReplyAsync("added " + Context.User + " to tradecord queue");
            await checkstarttrade();
        }
        [Command("list")]
        [Alias("l")]
        public async Task pokelist()
        {

           
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id))
                await ReplyAsync("no pokemon found");
            if (Directory.GetFiles(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//").Count() == 0)
                await ReplyAsync("no pokemon found");
            System.Text.StringBuilder y = new System.Text.StringBuilder();
            int h = 1;
            int k = 1;
           
            while (Directory.GetFiles(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//").Count() >= k)
            {
                
                if (!File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + h))
                {
                    h++;
                    
                   
                    continue;
                }
                byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + h);
                var tpk = PKMConverter.GetPKMfromBytes(g, 7);
                y.Append(h + "." + (tpk.IsShiny ? "★" : "") + Ledybot.Program.PKTable.Species7[tpk.Species - 1] + " ");
                k++;
                h++;
            }
            string[] n = new string[24];
            int q = 0;
            string yb = y.ToString();
            
            while (yb.Length > 0)
            {
                if (yb.Length > 1000)
                    n[q] = yb.Substring(0, 1000);
                else
                    n[q] = yb.Substring(0, yb.Length);

                if (yb.Length > 1000)
                    yb = yb.Remove(0, 1000);
                else
                    yb = yb.Remove(0, yb.Length);
              
                
                q++;
            }

            int r = 0;
            EmbedBuilder embed = new EmbedBuilder();
            embed.Title = "Your pokemon Box";
            foreach (string i in n)
            {
                if(i != null)
                    embed.AddField($"page {r + 1} ", n[r].ToString());
                
                r++;
            }
            await ReplyAsync(embed: embed.Build());





        }
        [Command("release")]
        [Alias("r")]
        public async Task pokerelease(int idnumb)
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb))
            {
                byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb);
                var tpk = PKMConverter.GetPKMfromBytes(g, 7);
                await ReplyAsync(Ledybot.Program.PKTable.Species7[tpk.Species - 1] + " has been released");
                File.Delete(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb);
                return;
            }
            await ReplyAsync("no pokemon with that id number found");
        }
        [Command("massrelease")]
        [Alias("mr")]
        public async Task massrelease(string shiny = "")
        {

            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//");
            foreach (string file in files)
            {
                
                byte[] g = File.ReadAllBytes(file);
                var tpk = PKMConverter.GetPKMfromBytes(g, 7);
                if (!tpk.IsShiny || shiny.ToLower() == "shiny")
                {
                    
                    

                        File.Delete(file);
                       

                }
                else
                    continue;


            }

            if (shiny == "")
                await ReplyAsync("all non shiny pokemon have been released");
            if (shiny.ToLower() == "shiny")
                await ReplyAsync("all pokemon have been released");

        }


        [Command("info")]
        [Alias("i")]
        public async Task info(int idnumb)
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb))
            {
                byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb);
                var tpk = PKMConverter.GetPKMfromBytes(g, 7);
                EmbedBuilder embed = new EmbedBuilder().WithFooter(Ledybot.Program.PKTable.Balls7[tpk.Ball-1], $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Ballimg/50x50/{Ledybot.Program.PKTable.Balls7[tpk.Ball - 1].Split(' ')[0].ToLower()}ball.png");
                embed.ThumbnailUrl = tpk.IsShiny ? "https://play.pokemonshowdown.com/sprites/ani-shiny/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif" : "https://play.pokemonshowdown.com/sprites/ani/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif";
                embed.AddField($"{Context.User} {Ledybot.Program.PKTable.Species7[tpk.Species - 1]}'s info", ShowdownParsing.GetShowdownText(tpk));
                await ReplyAsync(embed: embed.Build());
                return;
            }
            await ReplyAsync("no pokemon with this id number was found");
        }
        [Command("cordhelp")]
        [Alias("ch")]
        public async Task HelpTC()
        {
            var embed = new EmbedBuilder();
            embed.Color = new Color(147, 191, 230);
            embed.Title = "Piplup Tradecord Help";
            embed.ThumbnailUrl = "https://www.shinyhunters.com/images/shiny/393.gif";
            embed.AddField("Piplup tradecord is compatible with:", "SUN / MOON / ULTRA SUN / ULTRA MOON" + "\n" + "Gen 7 GTS", false);
            embed.AddField("***Tradecord Commands***", "⠀" + "\n" + ":large_blue_diamond:" + "**!catch** (***!k***)" + "\n" + "⠀" + "\n" + "*Attempts to catch a random Pokemon*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!list** (***!l***)" + "\n" + "⠀" + "\n" + "*Displays a list of you're caught pokemon*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!info #** (***!i #***)" + "\n" + "⠀" + "\n" + "*Replace # with the ID number of the pokemon you want to check (from list command)*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!release #** (***!r #***)" + "\n" + "⠀" + "\n" + "*Replace # with the ID number of the pokemon you want to release (from list command)*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!massrelease** (***!mr***)" + "\n" + "⠀" + "\n" + "*Releases all non-shiny pokemon*" + "\n" + "**!mr shiny will release ALL pokemon**" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!tradecord (***!tc***) trainer-name ###**(*natdex#-of-deposit*) **##**(*tradecord-id#*) **trainerinfo**(*optional*) )" + "\n" + "⠀" + "\n" + "*Trades your caught pokemon to you in the gen 7 GTS (Compatible with SUN / MOON / ULTRA SUN / MOON*" 
                , true);
            embed.AddField("extras", ":large_blue_diamond:" + "**!nickname** (***!n***) # nickname" + "\n" + "\n" + "*Replace # with the ID number of the pokemon you want to nickname(from list command)*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!tradecorddex** (***!tdex***)" + "\n" + "\n" + "Displays how many dex entries you have registered out of 807" + "\n" + "\n" + 
                $":large_blue_diamond: **!tdexmissing** (***!tdm***) \n \n Displays what pokemon you are missing from your pokedex \n \n" +
                $":large_blue_diamond: **!BuddySet** (***!bs***) \n \n Sets a buddy to go on your adventure, will gain exp with each catch and evolve if it meets level criteria! \n \n" +
                $":large_blue_diamond: **!Buddy** (***!b***) \n \n Displays your buddies information!", true);
            embed.ImageUrl = "https://cdn.discordapp.com/attachments/733454651227373579/848772777641377832/piplup.gif";
            await ReplyAsync(embed: embed.Build());
            return;
        }
        [Command("nickname")]
        [Alias("n")]
        public async Task nick(int idnumb, string nicky)

        {

            if (File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb))

            {
                byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb);
                var tpk = PKMConverter.GetPKMfromBytes(g, 7);
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithColor(147, 191, 230);
                tpk.SetNickname(nicky);
                File.WriteAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb, tpk.DecryptedBoxData);
                embed.AddField($"{Ledybot.Program.PKTable.Species7[tpk.Species - 1]}'s info", ShowdownParsing.GetShowdownText(tpk) + "\n" + "Ball: " + Ledybot.Program.PKTable.Balls7[tpk.Ball - 1]);
                await ReplyAsync(embed: embed.Build());
            }
            else
            {
                EmbedBuilder embeda = new EmbedBuilder();
                embeda.WithColor(147, 191, 230);
                embeda.WithTitle("No Pokemon with matching ID # found");
                await ReplyAsync(embed: embeda.Build());
            }
        }
        [Command("tradecorddex")]
        [Alias("tdex")]
        public async Task TCdex()
        {
            if(File.Exists(Directory.GetCurrentDirectory() + "//"+"//dexs//" +Context.User.Id+".txt"))
            {
                EmbedBuilder embed = new EmbedBuilder();
                embed.Title = $"{Context.User}'s Pokedex Progress";
                int count = File.ReadAllLines($"{Directory.GetCurrentDirectory()}//dexs///{Context.User.Id}.txt").Count();
                embed.AddField("You've caught: ", count.ToString() + "/807");
                if (count == 807)
                    embed.AddField("Master","You've caught em all! You're a Pokemon Master!");
                await ReplyAsync(embed: embed.Build());
                return;
            }
            await ReplyAsync("no user found, catch some pokemon with !k");

        }
        [Command("tdexmissing")]
        [Alias("tdm")]
        public async Task TCdexmissing()
        {
            if(!File.Exists($"{Directory.GetCurrentDirectory()}////dexs//{Context.User.Id}.txt"))
            {
                await ReplyAsync("no user found, catch some pokemon with !k");
                return;
            }
            var tdextxt = File.ReadAllLines($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt");
            var natdex2 = Ledybot.Program.PKTable.Species7;
            
            foreach(string w in tdextxt)
            {


                natdex2[Convert.ToInt32(w)-1] = "" ;
                    
                
                
            }
            var yb = new System.Text.StringBuilder();
            foreach(string f in natdex2)
            {
                if(f != "")
                {
                    yb.Append($"{f} ");
                }
                continue;
            }
            string[] n = new string[24];
            int q = 0;
            string ybc = yb.ToString();
            while (ybc.Length > 0)
            {
                if (ybc.Length > 1000)
                    n[q] = ybc.Substring(0, 1000);
                else
                    n[q] = ybc.Substring(0, ybc.Length);

                if (ybc.Length > 1000)
                    ybc = ybc.Remove(0, 1000);
                else
                    ybc = ybc.Remove(0, ybc.Length);


                q++;
            }
            
            int r = 0;
          
          await ReplyAsync("missing pokemon: ");
            foreach(string i in n)
            {
                if (i != null)
                {
                    await ReplyAsync($"page {r+1}: {i}");
                    
                }
                r++;

            }
            
            
           
        }
        [Command("Buddy")]
        [Alias("buddy", "b", "B")]
        public async Task Buddy()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy"))
            {
                byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy");
                var tpk = PKMConverter.GetPKMfromBytes(g, 7);
                var embed = new EmbedBuilder().WithFooter(Ledybot.Program.PKTable.Balls7[tpk.Ball - 1], $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Ballimg/50x50/{Ledybot.Program.PKTable.Balls7[tpk.Ball - 1].Split(' ')[0].ToLower()}ball.png"); 
                embed.Color = new Color(88, 163, 73);
                embed.Title = Context.User.ToString() + "'s Buddy";
                embed.AddField($"{Ledybot.Program.PKTable.Species7[tpk.Species - 1]}'s info", ShowdownParsing.GetShowdownText(tpk));
                embed.ThumbnailUrl = tpk.IsShiny ? "https://play.pokemonshowdown.com/sprites/ani-shiny/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif" : "https://play.pokemonshowdown.com/sprites/ani/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif";
                await ReplyAsync(embed: embed.Build());
                return;
            }
            else
            {
                var embed = new EmbedBuilder();
                embed.Color = new Color(88, 163, 73);
                embed.Title = Context.User.ToString() + " has no assigned Buddy, use !bs id# to assign a pokemon as your buddy";
                await ReplyAsync(embed: embed.Build());
                return;
            }
        }

        [Command("BuddySet")]
        [Alias("bs", "BS", "sb")]
        public async Task SetBuddy(int idnumb)
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb))
            {
                byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb);
                var tpk = PKMConverter.GetPKMfromBytes(g, 7);
                EmbedBuilder embed = new EmbedBuilder();
                if (Directory.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy"))
                {
                    File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy");
                    byte[] i = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy");
                    var bpk = PKMConverter.GetPKMfromBytes(i, 7);
                    var direct = Directory.GetCurrentDirectory() + "//" + Context.User.Id;
                    string directfile;
                    int a = 1;
                    while (File.Exists(direct + "//" + a))
                        a++;
                    directfile = direct + "//" + a;
                    File.WriteAllBytes(directfile, bpk.EncryptedBoxData);
                    File.Delete(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb);
                    File.WriteAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy", tpk.DecryptedBoxData);

                    embed.WithColor(88, 163, 73);
                    embed.Color = new Color(88, 163, 73);
                    embed.Title = Context.User.ToString() + " has set " + tpk.Nickname.ToString() + " to be there buddy pokemon";
                    embed.AddField($"{Ledybot.Program.PKTable.Species7[tpk.Species - 1]}'s info", ShowdownParsing.GetShowdownText(tpk));
                    embed.ThumbnailUrl = tpk.IsShiny ? "https://play.pokemonshowdown.com/sprites/ani-shiny/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif" : "https://play.pokemonshowdown.com/sprites/ani/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif";
                    embed.WithFooter(Ledybot.Program.PKTable.Balls7[tpk.Ball - 1], $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Ballimg/50x50/{Ledybot.Program.PKTable.Balls7[tpk.Ball - 1].Split(' ')[0].ToLower()}ball.png");
                }
                if (!Directory.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy"))
                {
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy");
                    File.WriteAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy", tpk.DecryptedBoxData);
                    File.Delete(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb);
                    embed.ThumbnailUrl = tpk.IsShiny ? "https://play.pokemonshowdown.com/sprites/ani-shiny/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif" : "https://play.pokemonshowdown.com/sprites/ani/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif";
                    embed.WithColor(88, 163, 73);
                    embed.Color = new Color(88, 163, 73);
                    embed.Title = Context.User.ToString() + " has set " + tpk.Nickname + " to be there buddy pokemon";
                    embed.AddField($"{Ledybot.Program.PKTable.Species7[tpk.Species - 1]}'s info", ShowdownParsing.GetShowdownText(tpk));
                    embed.WithFooter(Ledybot.Program.PKTable.Balls7[tpk.Ball - 1], $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Ballimg/50x50/{Ledybot.Program.PKTable.Balls7[tpk.Ball - 1].Split(' ')[0].ToLower()}ball.png");
                }

            
                await ReplyAsync(embed: embed.Build());
                
              
                }

            }

        }


    }

    










