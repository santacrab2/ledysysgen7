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


namespace Ledybot
{
   public class tradecordmodule : ModuleBase<SocketCommandContext>
    {
        public static int[] tradevolvs = { 525, 75, 533, 93, 64, 67, 708, 710, 61, 79, 95, 123, 117, 137, 366, 112, 125, 126, 233, 356, 684, 682, 349 };
        discordbot.trademodule dbot = new discordbot.trademodule();
        public static int[] mythic = { 151, 251, 385, 386, 490, 491, 492, 493, 494, 646, 647, 648, 649, 719, 720, 721, 801, 802, 807 };
        
        [Command("catch")]
        [Alias("k")]
        public async Task tradecordcatch()
        {

            discordbot.trademodule.embed = new EmbedBuilder();
            string direct;
            Random TCrng = new Random();
            int ballrng = TCrng.Next(24);
            while (ballrng == 15)
            {
                ballrng = TCrng.Next(24);
            }

            int farng = TCrng.Next(2);
            if (farng != 1)
            {

                int missrng = TCrng.Next(806);

                discordbot.trademodule.embed.Color = new Color(147, 191, 230);
                discordbot.trademodule.embed.Title = "Miss";
                discordbot.trademodule.embed.AddField("" + Context.User, "you failed to catch a " + Ledybot.Program.PKTable.Species7[missrng] + " in a " + Ledybot.Program.PKTable.Balls7[ballrng]);
                await ReplyAsync(embed: discordbot.trademodule.embed.Build());
                return;
            }
            try
            {

                int catchrng = TCrng.Next(806);
                if (!File.Exists($"{Directory.GetCurrentDirectory()}//rolls.txt"))
                    File.Create($"{Directory.GetCurrentDirectory()}//rolls.txt");
                while (File.ReadAllLines($"{Directory.GetCurrentDirectory()}//rolls.txt").Contains(catchrng.ToString()))
                    catchrng = TCrng.Next(806);
                while (mythic.Contains(catchrng))
                    catchrng = TCrng.Next(806);

                StreamWriter catches = File.AppendText($"{Directory.GetCurrentDirectory()}//rolls.txt");
                catches.WriteLine(catchrng);
                catches.Close();
                if (File.ReadAllLines($"{Directory.GetCurrentDirectory()}//rolls.txt").Count() > 800)
                {

                    File.WriteAllText($"{Directory.GetCurrentDirectory()}//rolls.txt", string.Empty);

                }
                var tpk = PKMConverter.GetBlank(7);
                var shinymessage = "non-shiny";

                tpk = discordbot.trademodule.BuildPokemon(Ledybot.Program.PKTable.Species7[catchrng], 7);


                tpk.Ball = BallApplicator.ApplyBallLegalRandom(tpk);


                tpk.CurrentLevel = TCrng.Next(100);
                tpk = tpk.Legalize();
                while (new LegalityAnalysis(tpk).Report().Contains("Evolution not valid"))
                {
                    tpk.CurrentLevel = TCrng.Next(100);
                    tpk = tpk.Legalize();
                }
                int[] sugmov = MoveSetApplicator.GetMoveSet(tpk, true);
                tpk.SetMoves(sugmov);

                int natue = TCrng.Next(24);
                tpk.Nature = natue;
                tpk.SetRandomIVs();
                tpk = tpk.Legalize();


                if (File.Exists($"{Directory.GetCurrentDirectory()}//trainerinfo//{Context.User.Id}.txt"))
                {
                    string[] trsplit = File.ReadAllText($"{Directory.GetCurrentDirectory()}//trainerinfo//{Context.User.Id}.txt").Split('\n');
                    int q = 0;
                    foreach (string b in trsplit)
                    {
                        if (trsplit[q].Contains("OT:"))
                            tpk.OT_Name = trsplit[q].Replace("OT: ", "");
                        q++;
                    }
                    int h = 0;
                    foreach (string v in trsplit)
                    {
                        if (trsplit[h].Contains("TID:"))
                        {
                            int trid7 = Convert.ToInt32(trsplit[h].Replace("TID: ", ""));
                            tpk.TrainerID7 = trid7;

                        }
                        h++;
                    }
                    int hd = 0;
                    foreach (string v in trsplit)
                    {
                        if (trsplit[hd].Contains("SID:"))
                        {
                            int trsid7 = Convert.ToInt32(trsplit[hd].Replace("SID: ", ""));
                            tpk.TrainerSID7 = trsid7;

                        }
                        hd++;
                    }
                }
                new LegalityAnalysis(tpk);


                int shinyrng = TCrng.Next(100);
                if (shinyrng <= 75)
                    tpk.SetIsShiny(true);
                if (new LegalityAnalysis(tpk).Report().Contains("Static Encounter shiny mismatch"))
                    tpk.SetIsShiny(false);
                if (!new LegalityAnalysis(tpk).Valid)
                    tpk.SetIsShiny(false);
                if (!new LegalityAnalysis(tpk).Valid)
                {
                    tradecordcatch();
                    return;
                }
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
                if (!File.ReadAllLines($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt").Contains(tpk.Species.ToString()) || File.ReadAllText($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt") == null)
                {
                    discordbot.trademodule.embed.AddField("Pokedex", $"Registered {Ledybot.Program.PKTable.Species7[tpk.Species - 1]} to your Pokedex");
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

                discordbot.trademodule.embed.Color = new Color(147, 191, 230);
                discordbot.trademodule.embed.ThumbnailUrl = $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Ballimg/50x50/{Ledybot.Program.PKTable.Balls7[tpk.Ball - 1].Split(' ')[0].ToLower()}ball.png";
                discordbot.trademodule.embed.AddField(Context.User + "'s catch!", " you threw a " + Ledybot.Program.PKTable.Balls7[tpk.Ball - 1] + " at a " + shinymessage + " " + Ledybot.Program.PKTable.Species7[tpk.Species - 1] + "...");
                discordbot.trademodule.embed.AddField("Results", "It put up a fight, but you caught " + shinymessage + "  " + Ledybot.Program.PKTable.Species7[tpk.Species - 1]);
                baseLink[8] = tpk.IsShiny ? "r.png" : "n.png";
                var baseLink2 = string.Join("_", baseLink);
                EmbedFooterBuilder x = new EmbedFooterBuilder();
                x.Text = "Id number: " + a;
                discordbot.trademodule.embed.Footer = x;
                discordbot.trademodule.embed.ImageUrl = baseLink2;

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
                        var testpk = discordbot.trademodule.BuildPokemon(Ledybot.Program.PKTable.Species7[bpk.Species + 1], 7);
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
                                    discordbot.trademodule.embed.AddField("Pokedex", $"Registered {Ledybot.Program.PKTable.Species7[bpk.Species - 1]} to your pokedex");
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
                            discordbot.trademodule.embed.AddField($"\n{Context.User}'s Buddy " + (evolve ? bpk.IsNicknamed ? bpk.Nickname : Ledybot.Program.PKTable.Species7[currentspec - 1] : bpk.Nickname), evolve ? $" gained {xpGet} EXP and leveled up to level {bpk.CurrentLevel} and evolved to {(Species)bpk.Species}!" : $" gained {xpGet} EXP and leveled up to level {bpk.CurrentLevel}!");
                        else discordbot.trademodule.embed.AddField($"\n{Context.User}'s Buddy " + (evolve ? bpk.IsNicknamed ? bpk.Nickname : Ledybot.Program.PKTable.Species7[currentspec - 1] : bpk.Nickname), evolve ? $" gained {xpGet} EXP and evolved to {(Species)bpk.Species}!" : $" gained {xpGet} EXP!");
                    }

                }
                await ReplyAsync(embed: discordbot.trademodule.embed.Build());
            }
            catch
            {
                await ReplyAsync("something went wrong with this catch, try again!");
                return;

            }
        }
        [Command("tradecord")]
        [Alias("tc")]
        public async Task tradecordtrade(string trainer, int pts, int idnumb, [Remainder] string trainerinfo = "")
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
            discordbot.trademodule.pokequeue.Enqueue(temppokewait);
            discordbot.trademodule.username.Enqueue(Context.User.Id);
            discordbot.trademodule.trainername.Enqueue(trainer);
            discordbot.trademodule.pokemonfile.Enqueue(tpk);
            discordbot.trademodule.channel.Enqueue(Context.Channel);
            discordbot.trademodule.poketosearch.Enqueue(pts);
            discordbot.trademodule.discordname.Enqueue(Context.User);
            await ReplyAsync("added " + Context.User + " to tradecord queue");
            await dbot.checkstarttrade();
        }
        [Command("tradecord")]
        [Alias("tc")]
        public async Task tradecordstrtrade(string trainer, string pts, int idnumb, [Remainder] string trainerinfo = "")
        {
            int ptsstr = Array.IndexOf(Ledybot.Program.PKTable.Species6, pts);
            if (ptsstr == -1)
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
            discordbot.trademodule.pokequeue.Enqueue(temppokewait);
            discordbot.trademodule.username.Enqueue(Context.User.Id);
            discordbot.trademodule.trainername.Enqueue(trainer);
            discordbot.trademodule.pokemonfile.Enqueue(tpk);
            discordbot.trademodule.channel.Enqueue(Context.Channel);
            discordbot.trademodule.poketosearch.Enqueue(ptsstr);
            discordbot.trademodule.discordname.Enqueue(Context.User);
            await ReplyAsync("added " + Context.User + " to tradecord queue");
            await dbot.checkstarttrade();
        }
        [Command("list")]
        [Alias("l")]
        public async Task pokelist()
        {
            discordbot.page = 0;
            discordbot.trademodule.embed = new EmbedBuilder();
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
            discordbot.trademodule.n = new List<string>();
            int q = 0;
            string yb = y.ToString();

            while (yb.Length > 0)
            {
                if (yb.Length > 1000)
                    discordbot.trademodule.n.Add(yb.Substring(0, 1000));
                else
                    discordbot.trademodule.n.Add(yb.Substring(0, yb.Length));

                if (yb.Length > 1000)
                    yb = yb.Remove(0, 1000);
                else
                    yb = yb.Remove(0, yb.Length);


                q++;
            }



            discordbot.trademodule.embed.Title = $"{Context.User.Username}'s pokemon Box";

            discordbot.trademodule.embed.AddField("Box", "hi");

            discordbot.trademodule.embed.Fields[0].Value = discordbot.trademodule.n[0].ToString();

            discordbot.trademodule.embed.WithFooter($"Page {discordbot.page + 1} of {discordbot.trademodule.n.Count}");
            IEmote[] reactions = { new Emoji("⬅️"), new Emoji("➡️") };
            var listmsg = await Context.Channel.SendMessageAsync(embed: discordbot.trademodule.embed.Build());

            _ = Task.Run(() => listmsg.AddReactionsAsync(reactions).ConfigureAwait(false));






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
                discordbot.trademodule.embed = new EmbedBuilder().WithFooter(Ledybot.Program.PKTable.Balls7[tpk.Ball - 1], $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Ballimg/50x50/{Ledybot.Program.PKTable.Balls7[tpk.Ball - 1].Split(' ')[0].ToLower()}ball.png");
                discordbot.trademodule.embed.ThumbnailUrl = tpk.IsShiny ? "https://play.pokemonshowdown.com/sprites/ani-shiny/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif" : "https://play.pokemonshowdown.com/sprites/ani/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif";
                var newShowdown = new List<string>();
                var showdown = ShowdownParsing.GetShowdownText(tpk);
                foreach (var line in showdown.Split('\n'))
                    newShowdown.Add(line);

                if (tpk.IsEgg)
                    newShowdown.Add("\nPokémon is an egg");
                if (tpk.Ball > (int)Ball.None)
                    newShowdown.Insert(newShowdown.FindIndex(z => z.Contains("Nature")), $"Ball: {(Ball)tpk.Ball} Ball");
                if (tpk.IsShiny)
                {
                    var index = newShowdown.FindIndex(x => x.Contains("Shiny: Yes"));
                    if (tpk.ShinyXor == 0 || tpk.FatefulEncounter)
                        newShowdown[index] = "Shiny: Square\r";
                    else newShowdown[index] = "Shiny: Star\r";
                }

                newShowdown.InsertRange(1, new string[] { $"OT: {tpk.OT_Name}", $"TID: {tpk.TrainerID7}", $"SID: {tpk.TrainerSID7}", $"OTGender: {(Gender)tpk.OT_Gender}", $"Language: {(LanguageID)tpk.Language}" });
                discordbot.trademodule.embed.AddField($"{Context.User}'s {Ledybot.Program.PKTable.Species7[tpk.Species - 1]}'s info", Format.Code(string.Join("\n", newShowdown).TrimEnd()));
                await ReplyAsync(embed: discordbot.trademodule.embed.Build());
                return;
            }
            await ReplyAsync("no pokemon with this id number was found");
        }
        [Command("cordhelp")]
        [Alias("ch")]
        public async Task HelpTC()
        {
            discordbot.trademodule.embed = new EmbedBuilder();
            discordbot.trademodule.embed.Color = new Color(147, 191, 230);
            discordbot.trademodule.embed.Title = "Piplup Tradecord Help";
            discordbot.trademodule.embed.ThumbnailUrl = "https://www.shinyhunters.com/images/shiny/393.gif";
            discordbot.trademodule.embed.AddField("Piplup tradecord is compatible with:", "SUN / MOON / ULTRA SUN / ULTRA MOON" + "\n" + "Gen 7 GTS", false);
            discordbot.trademodule.embed.AddField("***Tradecord Commands***", "⠀" + "\n" + ":large_blue_diamond:" + "**!catch** (***!k***)" + "\n" + "⠀" + "\n" + "*Attempts to catch a random Pokemon*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!list** (***!l***)" + "\n" + "⠀" + "\n" + "*Displays a list of you're caught pokemon*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!info #** (***!i #***)" + "\n" + "⠀" + "\n" + "*Replace # with the ID number of the pokemon you want to check (from list command)*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!release #** (***!r #***)" + "\n" + "⠀" + "\n" + "*Replace # with the ID number of the pokemon you want to release (from list command)*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!massrelease** (***!mr***)" + "\n" + "⠀" + "\n" + "*Releases all non-shiny pokemon*" + "\n" + "**!mr shiny will release ALL pokemon**" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!tradecord (***!tc***) trainer-name ###**(*natdex#-of-deposit*) **##**(*tradecord-id#*) **trainerinfo**(*optional*) )" + "\n" + "⠀" + "\n" + "*Trades your caught pokemon to you in the gen 7 GTS (Compatible with SUN / MOON / ULTRA SUN / MOON*"
                , true);
            discordbot.trademodule.embed.AddField("extras", ":large_blue_diamond:" + "**!nickname** (***!n***) # nickname" + "\n" + "\n" + "*Replace # with the ID number of the pokemon you want to nickname(from list command)*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!tradecorddex** (***!tdex***)" + "\n" + "\n" + "Displays how many dex entries you have registered out of 807" + "\n" + "\n" +
                $":large_blue_diamond: **!tdexmissing** (***!tdm***) \n \n Displays what pokemon you are missing from your pokedex \n \n" +
                $":large_blue_diamond: **!BuddySet** (***!bs***) \n \n Sets a buddy to go on your adventure, will gain exp with each catch and evolve if it meets level criteria! \n \n" +
                $":large_blue_diamond: **!Buddy** (***!b***) \n \n Displays your buddies information!", true);
            discordbot.trademodule.embed.AddField("!settrainer, (**!st**)", "Sets your trainer info with the bot permanently so anything you catch will have that info!\nThis is also automatically captured if you trade the bot a pokemon you caught or bred\n```Example: !st OT: Santa\nTID: 123456\nSID: 1234```");
            discordbot.trademodule.embed.ImageUrl = "https://cdn.discordapp.com/attachments/733454651227373579/848772777641377832/piplup.gif";
            await ReplyAsync(embed: discordbot.trademodule.embed.Build());
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
                discordbot.trademodule.embed = new EmbedBuilder();
                discordbot.trademodule.embed.WithColor(147, 191, 230);
                tpk.SetNickname(nicky);
                File.WriteAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb, tpk.DecryptedBoxData);
                discordbot.trademodule.embed.AddField($"{Ledybot.Program.PKTable.Species7[tpk.Species - 1]}'s info", ShowdownParsing.GetShowdownText(tpk) + "\n" + "Ball: " + Ledybot.Program.PKTable.Balls7[tpk.Ball - 1]);
                await ReplyAsync(embed: discordbot.trademodule.embed.Build());
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
            if (File.Exists(Directory.GetCurrentDirectory() + "//" + "//dexs//" + Context.User.Id + ".txt"))
            {
                discordbot.trademodule.embed = new EmbedBuilder();
                discordbot.trademodule.embed.Title = $"{Context.User}'s Pokedex Progress";
                int count = File.ReadAllLines($"{Directory.GetCurrentDirectory()}//dexs///{Context.User.Id}.txt").Count();
                discordbot.trademodule.embed.AddField("You've caught: ", count.ToString() + "/807");
                if (count == 807)
                    discordbot.trademodule.embed.AddField("Master", "You've caught em all! You're a Pokemon Master!");
                await ReplyAsync(embed: discordbot.trademodule.embed.Build());
                return;
            }
            await ReplyAsync("no user found, catch some pokemon with !k");

        }
        [Command("tdexmissing")]
        [Alias("tdm")]
        public async Task TCdexmissing()
        {
            discordbot.page = 0;
            discordbot.trademodule.embed = new EmbedBuilder();
            if (!File.Exists($"{Directory.GetCurrentDirectory()}////dexs//{Context.User.Id}.txt"))
            {
                await ReplyAsync("no user found, catch some pokemon with !k");
                return;
            }
            var tdextxt = File.ReadAllLines($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt");
            var natdex2 = Ledybot.Program.PKTable.Species7;

            foreach (string w in tdextxt)
            {


                natdex2[Convert.ToInt32(w) - 1] = "";



            }
            var yb = new System.Text.StringBuilder();
            foreach (string f in natdex2)
            {
                if (f != "")
                {
                    yb.Append($"{f} ");
                }
                continue;
            }
            discordbot.trademodule.n = new List<string>();
            int q = 0;
            string ybc = yb.ToString();
            while (ybc.Length > 0)
            {
                if (ybc.Length > 1000)
                    discordbot.trademodule.n.Add(ybc.Substring(0, 1000));
                else
                    discordbot.trademodule.n.Add(ybc.Substring(0, ybc.Length));

                if (ybc.Length > 1000)
                    ybc = ybc.Remove(0, 1000);
                else
                    ybc = ybc.Remove(0, ybc.Length);


                q++;
            }



            discordbot.trademodule.embed.Title = $"{Context.User.Username}'s Missing Pokemon";

            discordbot.trademodule.embed.AddField("Missing Entries", "hi");

            discordbot.trademodule.embed.Fields[0].Value = discordbot.trademodule.n[0].ToString();

            discordbot.trademodule.embed.WithFooter($"Page {discordbot.page + 1} of {discordbot.trademodule.n.Count}");
            IEmote[] reactions = { new Emoji("⬅️"), new Emoji("➡️") };
            var listmsg = await Context.Channel.SendMessageAsync(embed: discordbot.trademodule.embed.Build());

            _ = Task.Run(() => listmsg.AddReactionsAsync(reactions).ConfigureAwait(false));



        }
        [Command("Buddy")]
        [Alias("buddy", "b", "B")]
        public async Task Buddy()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy"))
            {
                byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy");
                var tpk = PKMConverter.GetPKMfromBytes(g, 7);
                discordbot.trademodule.embed = new EmbedBuilder().WithFooter(Ledybot.Program.PKTable.Balls7[tpk.Ball - 1], $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Ballimg/50x50/{Ledybot.Program.PKTable.Balls7[tpk.Ball - 1].Split(' ')[0].ToLower()}ball.png");
                discordbot.trademodule.embed.Color = new Color(88, 163, 73);
                discordbot.trademodule.embed.Title = Context.User.ToString() + "'s Buddy";
                var newShowdown = new List<string>();
                var showdown = ShowdownParsing.GetShowdownText(tpk);
                foreach (var line in showdown.Split('\n'))
                    newShowdown.Add(line);

                if (tpk.IsEgg)
                    newShowdown.Add("\nPokémon is an egg");
                if (tpk.Ball > (int)Ball.None)
                    newShowdown.Insert(newShowdown.FindIndex(z => z.Contains("Nature")), $"Ball: {(Ball)tpk.Ball} Ball");
                if (tpk.IsShiny)
                {
                    var index = newShowdown.FindIndex(x => x.Contains("Shiny: Yes"));
                    if (tpk.ShinyXor == 0 || tpk.FatefulEncounter)
                        newShowdown[index] = "Shiny: Square\r";
                    else newShowdown[index] = "Shiny: Star\r";
                }

                newShowdown.InsertRange(1, new string[] { $"OT: {tpk.OT_Name}", $"TID: {tpk.TrainerID7}", $"SID: {tpk.TrainerSID7}", $"OTGender: {(Gender)tpk.OT_Gender}", $"Language: {(LanguageID)tpk.Language}" });
                discordbot.trademodule.embed.AddField($"{Ledybot.Program.PKTable.Species7[tpk.Species - 1]}'s info", Format.Code(string.Join("\n", newShowdown).TrimEnd()));
                discordbot.trademodule.embed.ThumbnailUrl = tpk.IsShiny ? "https://play.pokemonshowdown.com/sprites/ani-shiny/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif" : "https://play.pokemonshowdown.com/sprites/ani/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif";
                await ReplyAsync(embed: discordbot.trademodule.embed.Build());
                return;
            }
            else
            {
                discordbot.trademodule.embed = new EmbedBuilder();
                discordbot.trademodule.embed.Color = new Color(88, 163, 73);
                discordbot.trademodule.embed.Title = Context.User.ToString() + " has no assigned Buddy, use !bs id# to assign a pokemon as your buddy";
                await ReplyAsync(embed: discordbot.trademodule.embed.Build());
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
                discordbot.trademodule.embed = new EmbedBuilder();
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

                    discordbot.trademodule.embed.WithColor(88, 163, 73);
                    discordbot.trademodule.embed.Color = new Color(88, 163, 73);
                    discordbot.trademodule.embed.Title = Context.User.ToString() + " has set " + tpk.Nickname.ToString() + " to be there buddy pokemon";
                    var newShowdown = new List<string>();
                    var showdown = ShowdownParsing.GetShowdownText(tpk);
                    foreach (var line in showdown.Split('\n'))
                        newShowdown.Add(line);

                    if (tpk.IsEgg)
                        newShowdown.Add("\nPokémon is an egg");
                    if (tpk.Ball > (int)Ball.None)
                        newShowdown.Insert(newShowdown.FindIndex(z => z.Contains("Nature")), $"Ball: {(Ball)tpk.Ball} Ball");
                    if (tpk.IsShiny)
                    {
                        var index = newShowdown.FindIndex(x => x.Contains("Shiny: Yes"));
                        if (tpk.ShinyXor == 0 || tpk.FatefulEncounter)
                            newShowdown[index] = "Shiny: Square\r";
                        else newShowdown[index] = "Shiny: Star\r";
                    }

                    newShowdown.InsertRange(1, new string[] { $"OT: {tpk.OT_Name}", $"TID: {tpk.TrainerID7}", $"SID: {tpk.TrainerSID7}", $"OTGender: {(Gender)tpk.OT_Gender}", $"Language: {(LanguageID)tpk.Language}" });
                    discordbot.trademodule.embed.AddField($"{Ledybot.Program.PKTable.Species7[tpk.Species - 1]}'s info", Format.Code(string.Join("\n", newShowdown).TrimEnd()));
                    discordbot.trademodule.embed.ThumbnailUrl = tpk.IsShiny ? "https://play.pokemonshowdown.com/sprites/ani-shiny/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif" : "https://play.pokemonshowdown.com/sprites/ani/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif";
                    discordbot.trademodule.embed.WithFooter(Ledybot.Program.PKTable.Balls7[tpk.Ball - 1], $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Ballimg/50x50/{Ledybot.Program.PKTable.Balls7[tpk.Ball - 1].Split(' ')[0].ToLower()}ball.png");
                }
                if (!Directory.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy"))
                {
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy");
                    File.WriteAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy", tpk.DecryptedBoxData);
                    File.Delete(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + idnumb);
                    discordbot.trademodule.embed.ThumbnailUrl = tpk.IsShiny ? "https://play.pokemonshowdown.com/sprites/ani-shiny/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif" : "https://play.pokemonshowdown.com/sprites/ani/" + Ledybot.Program.PKTable.Species7[tpk.Species - 1].ToLower() + ".gif";
                    discordbot.trademodule.embed.WithColor(88, 163, 73);
                    discordbot.trademodule.embed.Color = new Color(88, 163, 73);
                    discordbot.trademodule.embed.Title = Context.User.ToString() + " has set " + tpk.Nickname + " to be their buddy pokemon";
                    discordbot.trademodule.embed.AddField($"{Ledybot.Program.PKTable.Species7[tpk.Species - 1]}'s info", ShowdownParsing.GetShowdownText(tpk));
                    discordbot.trademodule.embed.WithFooter(Ledybot.Program.PKTable.Balls7[tpk.Ball - 1], $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Ballimg/50x50/{Ledybot.Program.PKTable.Balls7[tpk.Ball - 1].Split(' ')[0].ToLower()}ball.png");
                }


                await ReplyAsync(embed: discordbot.trademodule.embed.Build());


            }

        }
        [Command("evolve")]
        [Alias("evo", "e")]
        public async Task evolve([Remainder]string useitem = "")
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy"))
            {

                byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy");
                PKM bpk = PKMConverter.GetPKMfromBytes(g, 7);
               ;
                
                bool savenick = bpk.IsNicknamed;
         
                    var evoltree = EvolutionTree.GetEvolutionTree(bpk, 7);
                    
                    var evos = evoltree.GetEvolutions(bpk.Species, bpk.Form);
                    bool hasEvo = evos.Count() > 0;
                    if (!hasEvo)
                    {
                        await ReplyAsync("Your buddy cannot evolve.");
                        return;
                    }

                    
                var preevos = evoltree.GetValidPreEvolutions(bpk, 100, 7, true);
                 var temp = new PK7 { Species = evos.First() };
                if(evos.Count() >= 2 && preevos.Count >= 2)
                {
                    var reqitem = bpk.species switch { }

                    
                }
              preevos = evoltree.GetValidPreEvolutions(temp, 100, 7, true);

                //var evoType = (EvolutionType)preevos[1].Method;
                
                await ReplyAsync(evos.Count().ToString());
                    if (!savenick)
                        bpk.ClearNickname();
                    File.WriteAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy", bpk.DecryptedBoxData);

                
             
            
            }
        }
    }
}
