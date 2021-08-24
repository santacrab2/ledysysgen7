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
                discordbot.trademodule.embed.Color = new Color(147, 191, 230);
                discordbot.trademodule.embed.Title = "Miss";
                string[] spookyimages = { "https://media.discordapp.net/attachments/873319088775122954/878532498760548392/Yeathoughiwalkthroughthevalleyoftheshadow_ee2df64a7a2664fc69f29c1e4710a11e.png?width=420&height=369", "https://image.pngaaa.com/994/4727994-middle.png", "https://pbs.twimg.com/media/EBYGQCqWsAEsVZb.jpg", "https://media.discordapp.net/attachments/873319088775122954/878526466801958962/newgod.jpg", "https://media.discordapp.net/attachments/873319088775122954/878526890300801094/alsogod.jpg", "https://media.discordapp.net/attachments/873319088775122954/878527168907444244/Togedude.png", "https://cdn.discordapp.com/attachments/873319088775122954/879038118114758726/YQt-qtgcQG7oQYlbpLrxO37Rb2c39fMKer2ACFCNsS4.png", "https://cdn.discordapp.com/attachments/873319088775122954/879237478794551316/68747470733a2f2f73332e616d617a6f6e6177732e636f6d2f776174747061642d6d656469612d736572766963652f53746f.png", "https://cdn.discordapp.com/attachments/873319088775122954/879237588332970014/8775e04b7c658279ca4f1b068c3b2a4d__01.png" };
                int spookyimg = TCrng.Next(spookyimages.Length);
                int missrng = TCrng.Next(806);
                int spookey = TCrng.Next(2);
                if (spookey == 1)
                {
                    discordbot.trademodule.embed.AddField($"{Context.User.Username}", $"You threw a {GameInfo.Strings.balllist[ballrng]} at a wild...whatever that thing is\n\nOne wiggle... Two... It breaks free and stares at you, smiling.You run for dear life.");
                    discordbot.trademodule.embed.WithFooter(x => x.Text = "But deep inside you know there is no escape... ");
                    discordbot.trademodule.embed.ImageUrl = spookyimages[spookyimg];
                    discordbot.trademodule.embed.ThumbnailUrl = $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Ballimg/50x50/{Ledybot.Program.PKTable.Balls7[ballrng - 1].Split(' ')[0].ToLower()}ball.png";
                }
                else
                {
                    
                    discordbot.trademodule.embed.AddField("" + Context.User.Username, "you failed to catch a " + Ledybot.Program.PKTable.Species7[missrng] + " in a " + Ledybot.Program.PKTable.Balls7[ballrng]);
                }
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
                    File.WriteAllText($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt", "\n");
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
                int item = TCrng.Next(4);
                if(item == 1)
                {
                    
                    var vals = Enum.GetValues(typeof(TCItems));
                    TCItems founditem = (TCItems)vals.GetValue(TCrng.Next(vals.Length));
                    if (!Directory.Exists($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//"))
                        Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//");
                    if (!File.Exists($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt"))
                        File.WriteAllText($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt", " ");
                    var itemcountlist = File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt").ToList();
                    var itemcount = itemcountlist.Count(x => x == founditem.ToString());
                    while(itemcount > 2 && itemcountlist.Count() < 78)
                    {
                        founditem = (TCItems)vals.GetValue(TCrng.Next(vals.Length));
                        itemcount = itemcountlist.Count(x => x == founditem.ToString());
                    }
                    StreamWriter ite = File.AppendText($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt");
                    ite.WriteLine(founditem);
                    ite.Close();
                    discordbot.trademodule.embed.AddField("item", $"{GameInfo.Strings.Species[tpk.Species]} dropped a {founditem}. Added {founditem} to your bag!");
                }
                else if((item == 2 || item == 3) && File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt").ToList().Count(x => x == TCItems.RareCandy.ToString()) < 11)
                {
                    
                    if (!Directory.Exists($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//"))
                        Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//");
                    if (!File.Exists($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt"))
                        File.WriteAllText($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt", "\n");
                    StreamWriter ite = File.AppendText($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt");
                    ite.WriteLine(TCItems.RareCandy);
                    ite.Close();
                    discordbot.trademodule.embed.AddField("item", $"{GameInfo.Strings.Species[tpk.Species]} dropped a {TCItems.RareCandy}. Added {TCItems.RareCandy} to your bag!");
                }
                if (File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy"))
                {
                    byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy");
                    var bpk = PKMConverter.GetPKMfromBytes(g, 7);
                    var lvlProgress = (Experience.GetEXPToLevelUpPercentage(bpk.CurrentLevel, bpk.EXP, bpk.PersonalInfo.EXPGrowth) * 100.0).ToString("N1");
               
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
                        bpk.Stat_HPCurrent += tpk.PersonalInfo.EV_HP;
                        bpk.EV_ATK += tpk.PersonalInfo.EV_ATK;
                        bpk.EV_DEF += tpk.PersonalInfo.EV_DEF;
                        bpk.EV_SPA += tpk.PersonalInfo.EV_SPA;
                        bpk.EV_SPD += tpk.PersonalInfo.EV_SPD;
                        bpk.EV_SPE += tpk.PersonalInfo.EV_SPE;
                       
                        

                        File.WriteAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy", bpk.DecryptedBoxData);

                        if (bpk.EXP >= xpMin)
                            discordbot.trademodule.embed.AddField($"{Context.User}'s Buddy {(bpk.IsNicknamed ? bpk.Nickname : GameInfo.Strings.Species[bpk.Species])}", $" gained {xpGet} EXP and leveled up to level {bpk.CurrentLevel}!");
                        else discordbot.trademodule.embed.AddField($"{Context.User}'s Buddy {(bpk.IsNicknamed ? bpk.Nickname : GameInfo.Strings.Species[bpk.Species])}",$" gained {xpGet} EXP!");
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
                var tpk = PKMConverter.GetPKMfromBytes(g, 6);
                var pokespec = (Species)tpk.Species;
                if (!tpk.IsShiny && shiny == "")
                {
                    File.Delete(file);
                }
                else if (shiny == "shiny" && tpk.IsShiny)
                    File.Delete(file);
                else if (shiny == pokespec.ToString())
                    File.Delete(file);
                else if (shiny == "all")
                    File.Delete(file);


            }

            if (shiny == "")
                await ReplyAsync("all non shiny pokemon have been released");
            if (shiny.ToLower() == "shiny")
                await ReplyAsync("all shiny pokemon have been released");
            if (GameInfo.Strings.specieslist.Contains(shiny))
                await ReplyAsync($"all {shiny} have been released");
            if (shiny == "all")
                await ReplyAsync("all Pokemon have been released");
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
            discordbot.page = 0;
            discordbot.trademodule.n = new List<string>();
            discordbot.trademodule.embed = new EmbedBuilder();
            discordbot.trademodule.embed.Color = new Color(147, 191, 230);
            discordbot.trademodule.embed.Title = "Prinplup Tradecord Help";
            discordbot.trademodule.embed.ThumbnailUrl = "https://www.shinyhunters.com/images/shiny/394.gif";
            discordbot.trademodule.embed.AddField("Prinplup tradecord is compatible with: SUN / MOON / ULTRA SUN / ULTRA MOON" + "\n" + "Gen 7 GTS", "hi", false);
            discordbot.trademodule.n.Add($"***Tradecord Commands***\n:large_blue_diamond: **!catch** (***!k***)\n\n*Attempts to catch a random Pokemon*\n\n" +
                ":large_blue_diamond: **!list** (***!l***)\n" +  "\n" + "*Displays a list of you're caught pokemon*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!info #** (***!i #***)" + "\n" + "\n" + "*Replace # with the ID number of the pokemon you want to check (from list command)*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!release #** (***!r #***)" + "\n" + "\n" + "*Replace # with the ID number of the pokemon you want to release (from list command)*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!massrelease** (***!mr***)" + "\n" + "\n" + "*Releases all non-shiny pokemon*" + "\n" + "**!mr shiny|all|species**" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!tradecord** (***!tc***) **trainer-name** **DepositPokemon** **##**(*tradecord-id#*) **trainerinfo**(*optional*) )" + "\n" +  "\n" + "*Trades your caught pokemon to you in the gen 7 GTS (Compatible with SUN / MOON / ULTRA SUN / MOON*");

            discordbot.trademodule.n.Add("***Tradecord Commands Cont.\n***" + ":large_blue_diamond:" + "**!nickname** (***!n***) # nickname" + "\n" + "\n" + "*Replace # with the ID number of the pokemon you want to nickname(from list command)*" + "\n" + "\n" +
                ":large_blue_diamond:" + "**!tradecorddex** (***!tdex***)" + "\n" + "\n" + "Displays how many dex entries you have registered out of 807" + "\n" + "\n" +
                $":large_blue_diamond: **!tdexmissing** (***!tdm***) \n \n Displays what pokemon you are missing from your pokedex \n \n" +
                $":large_blue_diamond: **!BuddySet** (***!bs***) id# \n \n Sets a buddy to go on your adventure, will gain exp with each catch and evolve if it meets level criteria! \n \n" +
                $":large_blue_diamond: **!Buddy** (***!b***) \n \n Displays your buddies information!\n\n"
            + ":large_blue_diamond: **!settrainer**, (**!st**)\n" + "Sets your trainer info with the bot permanently so anything you catch will have that info!\nThis is also automatically captured if you trade the bot a pokemon you caught or bred\n```Example: !st OT: Santa\nTID: 123456\nSID: 1234```\n\n" );
            discordbot.trademodule.n.Add($"***Tradecord Commands cont.***\n:large_blue_diamond: **!evolve**, (**!e**) optional item / timeofday\nEvolves your current Buddy if its able to, if it requires an item like ThunderStone type it with the command\n```example: !evolve ThunderStone```\n\n" +
                 $":large_blue_diamond: **!items**\nDisplays your item bag\n\n" +
             $":large_blue_diamond: **!giveitem**, (**!gi**) item\nGives an item to your buddy to hold, if its a Rare Candy you can specify amount and your buddy will level up\n\n" +
             $":large_blue_diamond: **!takeitem**, (**!ti**)\n takes back the item your buddy is holding\n\n" +
             $":large_blue_diamond: **!dropitem**, (**!di**) item *optional amount*\n drops 1 or the specified amount of the item specified from your bag\n\n" +
             $":large_blue_diamond: **!gift** *@user* id#\ngifts a pokemon to another tradecord user\n\n" +
             $":large_blue_diamond: **!giftitem** *@user* item\ngifts an item to another tradecord user\n\n");
           discordbot.trademodule.n.Add( $":large_blue_diamond: **!gymbattle**, (**!gb**)\nLets you challenge a random gym leader to a 1v1 match with your buddy\n\n"+
            $":large_blue_diamond: **!badges**\nDisplays your badges\n\n"+
            $":large_blue_diamond: **!gymqueue**, (**!gq**)\nDisplays the current queue for gym battles\n\n"+
            $":large_blue_diamond: **!randommoves**, (**!rm**)\nChanges all of your buddies moves to a new legal random set of moves");

            discordbot.trademodule.embed.Fields[0].Value = discordbot.trademodule.n[0].ToString();
            discordbot.trademodule.embed.ImageUrl = "https://c.tenor.com/aVgHd6soz1wAAAAC/prinplup-piplup.gif";
            discordbot.trademodule.embed.WithFooter($"Page {discordbot.page + 1} of {discordbot.trademodule.n.Count}");
            IEmote[] reactions = { new Emoji("⬅️"), new Emoji("➡️") };
            var listmsg = await Context.Channel.SendMessageAsync(embed: discordbot.trademodule.embed.Build());

            _ = Task.Run(() => listmsg.AddReactionsAsync(reactions).ConfigureAwait(false));
            
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
            if(!File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt").Contains(useitem) && useitem != "" && useitem.ToLower() != "night" && useitem.ToLower() != "day" && useitem.ToLower() != "dusk")
            {
                await ReplyAsync("You do not have that item");
                return;
            }    
            if (File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy"))
            {
                bool useditem = false;
                bool heldused = false;
                discordbot.trademodule.embed = new EmbedBuilder();
                int[] levelupevo = { 2, 3, 6, 8, 17, 18, 19, 20,23,24, 32, 33, 37, 38, 40, };
                byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy");
                PKM bpk = PKMConverter.GetPKMfromBytes(g, 7);
                bool shiny = bpk.IsShiny;
                int ogspecies = bpk.Species;
                
                bool savenick = bpk.IsNicknamed;
         
                    var evoltree = EvolutionTree.GetEvolutionTree( 7);
                    
                    var evos = evoltree.GetEvolutions(bpk.Species, bpk.Form);
                    bool hasEvo = evos.Count() > 0;
                    if (!hasEvo)
                    {
                        await ReplyAsync("Your buddy cannot evolve.");
                        return;
                    }
                    
                    
                
                 var temp = new PK7 { Species = evos.First() };
                var preevos = evoltree.GetValidPreEvolutions(temp, 100, 7, true);
                var evoType = (EvolutionType)preevos[1].Method;
               
                int[] Specieslist;
                int[] todspecieslist;
                if (useitem != "" && useitem.ToLower() != "night" && useitem.ToLower() != "day" && useitem.ToLower() != "dusk")
                {

                    Specieslist = useitem switch
                    {
                        "WaterStone" => new int[] { (int)Species.Vaporeon, (int)Species.Poliwrath, (int)Species.Cloyster, (int)Species.Starmie, (int)Species.Ludicolo, (int)Species.Simipour },
                        "ThunderStone" => new int[] { (int)Species.Jolteon, (int)Species.Raichu, (int)Species.Magnezone, (int)Species.Eelektross, (int)Species.Vikavolt },
                        "FireStone" => new int[] { (int)Species.Flareon, (int)Species.Ninetales, (int)Species.Arcanine, (int)Species.Simisear },
                        "LeafStone" => new int[] { (int)Species.Leafeon, (int)Species.Vileplume, (int)Species.Victreebel, (int)Species.Exeggutor, (int)Species.Shiftry, (int)Species.Simisage },
                        "IceStone" => new int[] { (int)Species.Glaceon },
                        "MoonStone" => new int[] { (int)Species.Nidoqueen, (int)Species.Nidoking, (int)Species.Clefable, (int)Species.Wigglytuff, (int)Species.Delcatty, (int)Species.Musharna },
                        "SunStone" => new int[] { (int)Species.Bellossom, (int)Species.Sunflora, (int)Species.Whimsicott, (int)Species.Lilligant, (int)Species.Heliolisk },
                        "ShinyStone" => new int[] { (int)Species.Togekiss, (int)Species.Roserade, (int)Species.Cinccino, (int)Species.Florges },
                        "DuskStone" => new int[] { (int)Species.Honchkrow, (int)Species.Mismagius, (int)Species.Chandelure, (int)Species.Aegislash },
                        "DawnStone" => new int[] { (int)Species.Gallade, (int)Species.Froslass },
                        "KingsRock" => new int[] { (int)Species.Slowking, (int)Species.Politoed },
                        _ => new int[] { }
                    };

                    foreach (int itemevols in Specieslist)
                    {
                        if (evos.Contains(itemevols))
                        {
                            bpk.Species = itemevols;
                        }
                    }
                    var gendercheck = evoltree.GetValidPreEvolutions(bpk, bpk.CurrentLevel, 7, true)[1].Method;
                    if (gendercheck == 17)
                        if (bpk.Gender != 0)
                            bpk.Species = ogspecies;
                    if (gendercheck == 18)
                        if (bpk.Gender != 1)
                            bpk.Species = ogspecies;
                    if (bpk.Species != ogspecies)
                        useditem = true;

                }
                else if (useitem.ToLower() == "night" || useitem.ToLower() == "day" || useitem.ToLower() == "dusk")
                {
                    todspecieslist = useitem switch
                    {
                        "day" => new int[] { (int)Species.Roselia, (int)Species.Lurantis, (int)Species.Lucario, (int)Species.Tyrantrum, (int)Species.Gumshoos, (int)Species.Espeon, (int)Species.Solgaleo },
                        "night" => new int[] { (int)Species.Aurorus, (int)Species.Chimecho, (int)Species.Umbreon, (int)Species.Lunala },
                        _ => null

                    };
                    foreach (int tod in todspecieslist)
                    {
                        if (evos.Contains(tod))
                        {
                            bpk.Species = tod;
                        }
                    }
                    if (bpk.Species == (int)Species.Rockruff)
                    {
                        bpk.Species = (int)Species.Lycanroc;
                        bpk.Form = useitem switch
                        {
                            "day" => 0,
                            "night" => 1,
                            "dusk" => 2,
                            _ => 0
                        };

                    }

                }


                else if (bpk.HeldItem != 0)
                {


                    bpk.Species = Program.PKTable.Item7[bpk.HeldItem] switch
                    {
                        "Dragon Scale" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Dragon Scale") && bpk.Species == (int)Species.Seadra) ? (int)Species.Kingdra : bpk.Species,
                        "Dubious Disc" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Dubious Disc") && bpk.Species == (int)Species.Porygon2) ? (int)Species.PorygonZ : bpk.Species,
                        "Electirizer" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Electirizer") && bpk.Species == (int)Species.Electabuzz) ? (int)Species.Electivire : bpk.Species,
                        "Magmarizer" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Magmarizer") && bpk.Species == (int)Species.Magmar) ? (int)Species.Magmortar : bpk.Species,
                        "Metal Coat" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Metal Coat") && bpk.Species == (int)Species.Onix) ? (int)Species.Steelix : (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Metal Coat") && bpk.Species == (int)Species.Scyther) ? (int)Species.Scizor : bpk.Species,
                        "Oval Stone" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Oval Stone") && bpk.Species == (int)Species.Happiny) ? (int)Species.Chansey : bpk.Species,
                        "Prism Scale" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Prism Scale") && bpk.Species == (int)Species.Feebas) ? (int)Species.Milotic : bpk.Species,
                        "Protector" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Protector") && bpk.Species == (int)Species.Rhydon) ? (int)Species.Rhyperior : bpk.Species,
                        "Razor Claw" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Razor Claw") && bpk.Species == (int)Species.Sneasel) ? (int)Species.Weavile : bpk.Species,
                        "Reaper Cloth" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Reaper Cloth") && bpk.Species == (int)Species.Dusclops) ? (int)Species.Dusknoir : bpk.Species,
                        "Sachet" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Sachet") && bpk.Species == (int)Species.Spritzee) ? (int)Species.Aromatisse : bpk.Species,
                        "Upgrade" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Upgrade") && bpk.Species == (int)Species.Porygon) ? (int)Species.Porygon2 : bpk.Species,
                        "Whipped Dream" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x => x == "Whipped Dream") && bpk.Species == (int)Species.Swirlix) ? (int)Species.Slurpuff : bpk.Species,
                        "Deap Sea Tooth" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x=> x == "Deep Sea Tooth") && bpk.Species == (int)Species.Clamperl)? (int)Species.Huntail : bpk.Species,
                        "Deep Sea Scale" => (bpk.HeldItem == Array.FindIndex(Program.PKTable.Item7, x=> x == "Deep Sea Scale") && bpk.Species == (int)Species.Clamperl)? (int)Species.Gorebyss : bpk.Species,
                        _ => bpk.Species,
                    };
                    if (ogspecies != bpk.Species)
                        heldused = true;

                }
                else if (!levelupevo.Contains(preevos[1].Method) && bpk.Species != (int)Species.Eevee)
                    bpk.Species = evos.First();
                else if (preevos[1].Method == 23 || preevos[1].Method == 24)
                {
                    Specieslist = preevos[1].Method switch {


                        23 => new int[] { (int)Species.Mothim },


                        24 => new int[] { (int)Species.Wormadam, (int)Species.Vespiquen },
                        _ => new int[] { }
                    };
                    foreach (int itemevols in Specieslist)
                    {
                        if (evos.Contains(itemevols))
                        {
                            bpk.Species = itemevols;
                        }
                    }
                }
                else if (bpk.Species == (int)Species.Eevee)
                    bpk.Species = (int)Species.Sylveon;
                string ot = bpk.OT_Name;
                int tid = bpk.TrainerID7;
                int sid = bpk.TrainerSID7;
                try { bpk = bpk.Legalize(); }
                catch { await ReplyAsync("Your buddy can not evolve for some reason"); return; }
                if (!new LegalityAnalysis(bpk).Valid)
                    bpk.Species = ogspecies;
                bpk.OT_Name = ot;
                bpk.TrainerID7 = tid;
                bpk.TrainerSID7 = sid;
                

                if (!savenick)
                        bpk.ClearNickname();
                if (shiny)
                    bpk.SetIsShiny(true);
                if (ogspecies != bpk.Species)
                {
                   
                    if (heldused == true)
                        bpk.HeldItem = 0;
                    if(useditem == true)
                    {
                        var bag = File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt").ToList();
                        bag.Remove(useitem);
                        File.WriteAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt", bag);
                    }
                    var newShowdown = new List<string>();
                    var showdown = ShowdownParsing.GetShowdownText(bpk);
                    foreach (var line in showdown.Split('\n'))
                        newShowdown.Add(line);

                    if (bpk.IsEgg)
                        newShowdown.Add("\nPokémon is an egg");
                    if (bpk.Ball > (int)Ball.None)
                        newShowdown.Insert(newShowdown.FindIndex(z => z.Contains("Nature")), $"Ball: {(Ball)bpk.Ball} Ball");
                    if (bpk.IsShiny)
                    {
                        var index = newShowdown.FindIndex(x => x.Contains("Shiny: Yes"));
                        if (bpk.ShinyXor == 0 || bpk.FatefulEncounter)
                            newShowdown[index] = "Shiny: Square\r";
                        else newShowdown[index] = "Shiny: Star\r";
                    }

                    newShowdown.InsertRange(1, new string[] { $"OT: {bpk.OT_Name}", $"TID: {bpk.TrainerID7}", $"SID: {bpk.TrainerSID7}", $"OTGender: {(Gender)bpk.OT_Gender}", $"Language: {(LanguageID)bpk.Language}" });
                    discordbot.trademodule.embed.ThumbnailUrl = bpk.IsShiny ? "https://play.pokemonshowdown.com/sprites/ani-shiny/" + Ledybot.Program.PKTable.Species7[bpk.Species - 1].ToLower() + ".gif" : "https://play.pokemonshowdown.com/sprites/ani/" + Ledybot.Program.PKTable.Species7[bpk.Species - 1].ToLower() + ".gif";
                    discordbot.trademodule.embed.AddField("Evolution", $"{Context.User.Username}'s {GameInfo.Strings.Species[ogspecies]} evolved into {GameInfo.Strings.Species[bpk.Species]}");
                    discordbot.trademodule.embed.AddField($"{Program.PKTable.Species7[bpk.Species - 1]}'s info", Format.Code(string.Join("\n", newShowdown).TrimEnd()));
                    if (!File.ReadAllLines($"{Directory.GetCurrentDirectory()}//dexs//{Context.User.Id}.txt").Contains(bpk.Species.ToString()))
                    {
                        discordbot.trademodule.embed.AddField("Pokedex", $"Registered {Ledybot.Program.PKTable.Species7[bpk.Species - 1]} to your Pokedex");
                        StreamWriter de = File.AppendText($"{Directory.GetCurrentDirectory()}//dexs///{Context.User.Id}.txt");
                        de.WriteLine(bpk.Species);
                        de.Close();
                    }
                    await ReplyAsync(embed: discordbot.trademodule.embed.Build());
                }
                else
                    await ReplyAsync("Your buddy can not evolve for some reason or another");
                File.WriteAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy", bpk.DecryptedBoxData);


            }
        }
        [Command("items")]
        [Alias("bag")]
        public async Task viewbag()
        {
            discordbot.page = 0;
            discordbot.trademodule.embed = new EmbedBuilder();
            discordbot.trademodule.n = new List<string>();
            var yb = new System.Text.StringBuilder();
            if (File.Exists($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt"))
            {

                var items = File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt");
                foreach (string it in items)
                {
                    if(it != "")
                        yb.Append($"{it} ");
                }


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



                discordbot.trademodule.embed.Title = $"{Context.User.Username}'s Bag";

                discordbot.trademodule.embed.AddField("Items", "hi");

                discordbot.trademodule.embed.Fields[0].Value = discordbot.trademodule.n[0].ToString();

                discordbot.trademodule.embed.WithFooter($"Page {discordbot.page + 1} of {discordbot.trademodule.n.Count}");
                IEmote[] reactions = { new Emoji("⬅️"), new Emoji("➡️") };
                var listmsg = await Context.Channel.SendMessageAsync(embed: discordbot.trademodule.embed.Build());

                _ = Task.Run(() => listmsg.AddReactionsAsync(reactions).ConfigureAwait(false));
            }
            else
                await ReplyAsync("no items found");
        }

        [Command("giveitem")]
        [Alias("gi")]
        public async Task giveitem(string itemtogive, int amount = 1)
        {
            if (File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt").Contains(itemtogive))
            {
                TCItems newheld = (TCItems)Enum.Parse(typeof(TCItems), itemtogive);
                if (File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy"))
                {
                    if (itemtogive != TCItems.RareCandy.ToString())
                    {
                        var bag = File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt").ToList();
                        bag.Remove(itemtogive);
                        File.WriteAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt", bag);
                        byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy");
                        PKM bpk = PKMConverter.GetPKMfromBytes(g, 7);
                        bpk.HeldItem = (int)newheld;
                        File.WriteAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy", bpk.DecryptedBoxData);
                        await ReplyAsync($"{GameInfo.Strings.Species[bpk.Species]} is now holding a {itemtogive}");
                    }
                    else
                    {
                        var bag = File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt").ToList();
                        while (amount != 0 && bag.Contains(itemtogive))
                        {
                            bag.Remove(itemtogive);
                            File.WriteAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt", bag);
                            byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy");
                            PKM bpk = PKMConverter.GetPKMfromBytes(g, 7);
                            if (bpk.CurrentLevel < 100)
                            {
                                bpk.CurrentLevel++;
                                File.WriteAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy", bpk.DecryptedBoxData);
                                await ReplyAsync("Your Buddy just leveled up!");
                            }
                            else
                                await ReplyAsync("you just wasted a rare candy lol");
                            amount--;
                        }
                    }
                }
                else
                    await ReplyAsync("no buddy set");
            }
            else
                await ReplyAsync("you do not have that item");
        }
        [Command("takeitem")]
        [Alias("ti")]
        public async Task takeitem()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy"))
            {
                byte[] g = File.ReadAllBytes(Directory.GetCurrentDirectory() + "//" + Context.User.Id + "//" + "Buddy" + "//" + "Buddy");
                PKM bpk = PKMConverter.GetPKMfromBytes(g, 7);
                if (bpk.HeldItem != 0)
                {
                    StreamWriter ite = File.AppendText($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt");
                    ite.WriteLine((TCItems)bpk.HeldItem);
                    ite.Close();
                    bpk.HeldItem = 0;
                    File.WriteAllBytes($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//Buddy//Buddy", bpk.DecryptedBoxData);
                    await ReplyAsync($"You took {(TCItems)bpk.HeldItem} from your Buddy {GameInfo.Strings.Species[bpk.Species]}");
                }
                else
                    await ReplyAsync("Your buddy is not holding an item");
            }
            else
                await ReplyAsync("You do not have a buddy set");
        }
        [Command("dropitem")]
        [Alias("di")]
        public async Task dropitem(string item, int amount = 1)
        {   while (amount != 0)
            {
                if (File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt").Contains(item))
                {
                    var itemlist = File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt").ToList();


                    itemlist.Remove(item);


                    File.WriteAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt", itemlist);
                    await ReplyAsync($"You dropped {item}");
                }
                else
                {
                    await ReplyAsync("You do not have that item");
                    break;
                }
                amount--;
            }
        }
        [Command("gift")]
        public async Task gift(string user, int giftid)
        {
            var receiver = Context.Message.MentionedUsers.ElementAt(0);
            if (Directory.Exists($"{ Directory.GetCurrentDirectory()}//{receiver.Id}//"))
            {
                if (File.Exists($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//{giftid}"))
                {
                    var temp = PKMConverter.GetPKMfromBytes(File.ReadAllBytes($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//{giftid}"),7);
                    int a = 1;
                    while (File.Exists($"{ Directory.GetCurrentDirectory()}//{receiver.Id}//{a}"))
                        a++;
                    File.Move($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//{giftid}", $"{ Directory.GetCurrentDirectory()}//{receiver.Id}//{a}");
                    if(!File.ReadAllText($"{ Directory.GetCurrentDirectory()}//dexs//{receiver.Id}.txt").Contains(temp.Species.ToString()))
                    {
                        await ReplyAsync($"Registered {(Species)temp.Species} to {receiver.Username}'s Pokedex");
                        StreamWriter de = File.AppendText($"{Directory.GetCurrentDirectory()}//dexs///{Context.User.Id}.txt");
                        de.WriteLine(temp.Species);
                        de.Close();
                    }
                    await ReplyAsync($"{receiver.Username} has been given a {(Species)temp.Species} from {Context.User.Username}");
                }
                else
                    await ReplyAsync("No pokemon with this id# found");
            }
            else
                await ReplyAsync($"{receiver.Username} has not started playing tradecord, tell them to!");
        }
        [Command("giftitem")]
        public async Task giftitem(string user, string giftid)
        {
            var receiver = Context.Message.MentionedUsers.ElementAt(0);
            if (!Directory.Exists($"{ Directory.GetCurrentDirectory()}//{receiver.Id}//items"))
                Directory.CreateDirectory($"{ Directory.GetCurrentDirectory()}//{receiver.Id}//items");
      
                if (File.Exists($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt"))
                {
                    if (File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt").Contains(giftid))
                    {
                        var itemlist = File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt").ToList();
                        itemlist.Remove(giftid);
                        File.WriteAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//items//items.txt", itemlist);
                        StreamWriter receiversfile = File.AppendText($"{Directory.GetCurrentDirectory()}//{receiver.Id}//items//items.txt");
                        receiversfile.WriteLine(giftid);
                        receiversfile.Close();
                    await ReplyAsync($"{receiver.Username} has been gifted {giftid} by {Context.User.Username}");
                    }
                    else await ReplyAsync("You do not have this item");
                }
                else await ReplyAsync("You do not have any items to gift");
           
            
               
        }
        public enum TCItems
        {
            
            // Evolution items
            
            SunStone = 80,
            MoonStone = 81,
            FireStone = 82,
            ThunderStone = 83,
            WaterStone = 84,
            LeafStone = 85,
            ShinyStone = 107,
            DuskStone = 108,
            DawnStone = 109,
            OvalStone = 110,
            KingsRock = 221,
            MetalCoat = 233,
            DragonScale = 235,
            Upgrade = 252,
            Protector = 321,
            Electirizer = 322,
            Magmarizer = 323,
            DubiousDisc = 324,
            ReaperCloth = 325,
            RazorClaw = 326,
            PrismScale = 537,
            WhippedDream = 646,
            Sachet = 647,
            IceStone = 849,
            ExpertBelt = 268,
            LifeOrb = 270,
            RareCandy = 50
        }
        }
}
