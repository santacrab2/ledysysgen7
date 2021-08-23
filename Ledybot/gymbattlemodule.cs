using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Ledybot
{
     public class gymbattlemodule : ModuleBase<SocketCommandContext>
    {
        public static MoveInfo.MoveInfoRoot MoveRoot = new();
        public static PKM battlebuddy;
        public static PK7 opponentpoke;
        public static EmbedBuilder battleembed;
        public static gleaderpoke leaderpoke;
        public static IUserMessage battlemsg;
        public static IUser battler;
        public static Queue gymbattlequeue = new Queue();
        public static bool gymon = false;
        [Command("gymbattle")]
        [Alias("gb")]
        public async Task gymbattlequeuer()
        {
            gymbattlequeue.Enqueue(Context.User);
            if (gymbattlequeue.Count == 1)
            {
                await ReplyAsync("starting your gym battle now!");
                await gymbattle();
            }
            else
            {
                await ReplyAsync($"There are {gymbattlequeue.Count} trainers in line for a gym battle! Make sure you have your Private Messages turned on!");
              
            }

        }
        public static async Task gymbattle()
        {
            gymon = true;
            battler = (IUser)gymbattlequeue.Peek();
            try
            {
                await battler.SendMessageAsync("gym battle will occur here!");
            }
            catch
            {
                
                gymbattlequeue.Dequeue();
            }
            
         
            if (!File.Exists($"{Directory.GetCurrentDirectory()}//{battler.Id}//Buddy//Buddy"))
            {
                await battler.SendMessageAsync("You don't have a buddy to battle with");
                gymbattlequeue.Dequeue();
                return;
            }
            if (!Directory.Exists($"{Directory.GetCurrentDirectory()}//{battler.Id}//Badges//"))
                Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}//{battler.Id}//Badges//");
            if (!File.Exists($"{Directory.GetCurrentDirectory()}//{battler.Id}//Badges//Badges.txt"))
                File.WriteAllText($"{Directory.GetCurrentDirectory()}//{battler.Id}//Badges//Badges.txt", "\n");
       
            battlebuddy = PKMConverter.GetPKMfromBytes(File.ReadAllBytes($"{Directory.GetCurrentDirectory()}//{battler.Id}//Buddy//Buddy"));
            
            Random GBrng = new Random();
            int natue = GBrng.Next(24);
            var vals = Enum.GetValues(typeof(gleaderpoke));
            leaderpoke = (gleaderpoke)vals.GetValue(GBrng.Next(vals.Length));
            while (File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{battler.Id}//Badges//Badges.txt").Contains($"{(badges)leaderpoke}") || File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{battler.Id}//Badges//Badges.txt").Length >= 60) 
                leaderpoke = (gleaderpoke)vals.GetValue(GBrng.Next(vals.Length));
          
            opponentpoke = new PK7 { Species = (int)leaderpoke, Nature = natue, CurrentLevel = battlebuddy.CurrentLevel };
             int[] sugmov = MoveSetApplicator.GetMoveSet(opponentpoke, true);
            opponentpoke.SetMoves(sugmov);
            opponentpoke.SetRandomIVs();
            opponentpoke.EVs = SetMaxEVs(opponentpoke);
            opponentpoke.Stat_HPCurrent = (int)(0.01 *(2 * opponentpoke.PersonalInfo.HP + opponentpoke.IV_HP + (0.25 * opponentpoke.EV_HP)) * opponentpoke.CurrentLevel) +opponentpoke.CurrentLevel + 10;
            opponentpoke.Stat_HPMax = opponentpoke.Stat_HPCurrent;
            battlebuddy.Stat_HPCurrent = (int)(0.01 * (2 * battlebuddy.PersonalInfo.HP + battlebuddy.IV_HP + (0.25 * battlebuddy.EV_HP)) * battlebuddy.CurrentLevel) + battlebuddy.CurrentLevel + 10;
            battlebuddy.Stat_HPMax = battlebuddy.Stat_HPCurrent;
            battleembed = new EmbedBuilder();
            battleembed.AddField("gym battle", $"Battle between {leaderpoke}'s {(Species)leaderpoke} and {battler.Username}'s {(Species)battlebuddy.Species}");
            battleembed.AddField("Opponent", $"{(Species)opponentpoke.Species}\n HP:{opponentpoke.Stat_HPCurrent}/{opponentpoke.Stat_HPMax}");
            battleembed.AddField($"{battler.Username}", $"{(Species)battlebuddy.Species}\n HP:{battlebuddy.Stat_HPCurrent}/{battlebuddy.Stat_HPMax}");
            battleembed.AddField($"Moves", $":one:{(Move)battlebuddy.Move1}\n:two:{(Move)battlebuddy.Move2}\n:three:{(Move)battlebuddy.Move3}\n:four:{(Move)battlebuddy.Move4}");
            battleembed.ImageUrl = $"https://play.pokemonshowdown.com/sprites/trainers/{leaderpoke.ToString().ToLower()}.png";
            IEmote[] reactions = { new Emoji("1️⃣"), new Emoji("2️⃣"), new Emoji("3️⃣"), new Emoji("4️⃣") };
            battlemsg = await battler.SendMessageAsync(embed: battleembed.Build());
            await battlemsg.AddReactionsAsync(reactions).ConfigureAwait(false);
        }

        public static async Task battle(int move)
        {
            if(opponentpoke.Stat_HPCurrent != 0 && battlebuddy.Stat_HPCurrent != 0)
            {
                string json = File.ReadAllText($"{Directory.GetCurrentDirectory()}//MoveInfo.json");
                MoveRoot = JsonConvert.DeserializeObject<MoveInfo.MoveInfoRoot>(json);
                var yourmove = MoveRoot.Moves.FirstOrDefault(x => x.MoveID == battlebuddy.Moves[move]);
                var yourtypes = new int[] { battlebuddy.PersonalInfo.Type1, battlebuddy.PersonalInfo.Type2 };
                var yourmovepower = Math.Round( WeightedDamage(opponentpoke, (PK7)battlebuddy, yourtypes, yourmove));
                Random GBrng = new Random();
                int opselect = GBrng.Next(4);
                var opmove = MoveRoot.Moves.FirstOrDefault(x => x.MoveID == opponentpoke.Moves[opselect]);
                var optypes = new int[] { opponentpoke.PersonalInfo.Type1, opponentpoke.PersonalInfo.Type2 };
                var opmovepower = Math.Round(WeightedDamage((PK7)battlebuddy, opponentpoke, optypes, opmove));
                if (battlebuddy.Stat_HPCurrent >= opmovepower)
                    battlebuddy.Stat_HPCurrent -= (int)opmovepower;
                else battlebuddy.Stat_HPCurrent = 0;
                if (opponentpoke.Stat_HPCurrent >= yourmovepower)
                    opponentpoke.Stat_HPCurrent -= (int)yourmovepower;
                else opponentpoke.Stat_HPCurrent = 0;
                await battler.SendMessageAsync($"You used {yourmove.Name} and did {yourmovepower} damage");
                await battler.SendMessageAsync($"{leaderpoke} used {opmove.Name} and did {opmovepower} damage");
                battleembed = new EmbedBuilder();
                battleembed.AddField("gym battle", $"Battle between {leaderpoke}'s {(Species)leaderpoke} and {battler.Username}'s {(Species)battlebuddy.Species}");
                battleembed.AddField("Opponent", $"{(Species)opponentpoke.Species}\n HP:{opponentpoke.Stat_HPCurrent}/{opponentpoke.Stat_HPMax}");
                battleembed.AddField($"{battler.Username}", $"{(Species)battlebuddy.Species}\n HP:{battlebuddy.Stat_HPCurrent}/{battlebuddy.Stat_HPMax}");
                battleembed.AddField($"Moves", $":one:{(Move)battlebuddy.Move1}\n:two:{(Move)battlebuddy.Move2}\n:three:{(Move)battlebuddy.Move3}\n:four:{(Move)battlebuddy.Move4}");
                battleembed.ImageUrl = $"https://play.pokemonshowdown.com/sprites/trainers/{leaderpoke.ToString().ToLower()}.png";
                IEmote[] reactions = { new Emoji("1️⃣"), new Emoji("2️⃣"), new Emoji("3️⃣"), new Emoji("4️⃣") };
                var battlemsg = await battler.SendMessageAsync(embed: battleembed.Build());
                await battlemsg.AddReactionsAsync(reactions).ConfigureAwait(false);

            }
            else
            {
                if(opponentpoke.Stat_HPCurrent == 0)
                {
                    if (!File.ReadAllText($"{Directory.GetCurrentDirectory()}//{battler.Id}//Badges//Badges.txt").Contains($"{(badges)leaderpoke}"))
                    {
                        StreamWriter ite = File.AppendText($"{Directory.GetCurrentDirectory()}//{battler.Id}//Badges//Badges.txt");
                        ite.WriteLine((badges)leaderpoke);
                        ite.Close();
                    }
                    
                    battleembed = new EmbedBuilder();
                    battleembed.AddField("You Won!", $"You defeated {leaderpoke} and earned a {(badges)leaderpoke}");
                    if (File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{battler.Id}//Badges//Badges.txt").Length >= 60)
                    {
                        battleembed.AddField("Champion", "With 60 or more Badges you are a Champion of PokéEarth");
                    }
                    await battler.SendMessageAsync(embed: battleembed.Build());
                    gymbattlequeue.Dequeue();
                    if (gymbattlequeue.Count != 0)
                        await gymbattle();

                        
                        
                }
                else
                {
                    battleembed = new EmbedBuilder();
                    battleembed.AddField("You lost!", $"{leaderpoke} just kicked your ass, try again!");
                    await battler.SendMessageAsync(embed: battleembed.Build());
                    gymbattlequeue.Dequeue();
                    if (gymbattlequeue.Count != 0)
                        await gymbattle();
                }
            }
        }
        [Command("badges")]
        public async Task viewbadges()
        {
            if (!File.Exists($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//Badges//Badges.txt"))
            {
                await ReplyAsync("you do not have any badges, type !gymbattle to do a gym battle with your buddy");
                return;
            }
            discordbot.trademodule.embed = new EmbedBuilder();
            discordbot.trademodule.n = new List<string>();
            var yb = new System.Text.StringBuilder();
            if (File.Exists($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//Badges//Badges.txt"))
            {

                var Badges = File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{Context.User.Id}//Badges//Badges.txt");
                foreach (string it in Badges)
                {
                    if (it != "")
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



                discordbot.trademodule.embed.Title = $"{Context.User.Username}'s Badges";

                discordbot.trademodule.embed.AddField("Badges", "hi");
                if (File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{battler.Id}//Badges//Badges.txt").Length >= 60)
                {
                    discordbot.trademodule.embed.AddField("Champion", "With 60 or more Badges you are a Champion of PokéEarth");
                }

                discordbot.trademodule.embed.Fields[0].Value = discordbot.trademodule.n[0].ToString();

                discordbot.trademodule.embed.WithFooter($"Page {discordbot.page + 1} of {discordbot.trademodule.n.Count}");
                IEmote[] reactions = { new Emoji("⬅️"), new Emoji("➡️") };
                var listmsg = await Context.Channel.SendMessageAsync(embed: discordbot.trademodule.embed.Build());

                _ = Task.Run(() => listmsg.AddReactionsAsync(reactions).ConfigureAwait(false));
            }

        }
        [Command("gymqueue")]
        [Alias("gq")]
        public async Task que()
        {
            EmbedBuilder gqembed = new EmbedBuilder();
            Object[] arr = gymbattlequeue.ToArray();
            var sb = new System.Text.StringBuilder();
            
            if (arr.Length == 0)
            {
                await ReplyAsync("queue is empty");
            }
            int r = 0;
            foreach (object i in arr)
            {

                sb.AppendLine((r + 1).ToString() + ". " + arr[r].ToString());
                r++;
            }
            gqembed.AddField(x =>
            {

                x.Name = "Queue:";
                x.Value = sb.ToString();
                x.IsInline = false;


            });
            await ReplyAsync(embed: gqembed.Build());
        }
        public static int[] SetMaxEVs(PKM entity)
        {
            if (entity.Format < 3)
                return Enumerable.Repeat((int)ushort.MaxValue, 6).ToArray();

            var stats = entity.PersonalInfo.Stats;
            var ordered = stats.Select((z, i) => new { Stat = z, Index = i }).OrderByDescending(z => z.Stat).ToArray();

            var result = new int[6];
            result[ordered[0].Index] = 252;
            result[ordered[1].Index] = 252;
            result[ordered[2].Index] = 6;
            return result;
        }
        public enum gleaderpoke
        {
            Brock = (int)Species.Onix,
            Misty = (int)Species.Starmie,
            Surge = (int)Species.Raichu,
            Erika = (int)Species.Vileplume,
            Koga = (int)Species.Weezing,
            Sabrina = (int)Species.Alakazam,
            Blaine = (int)Species.Arcanine,
            Giovanni = (int)Species.Nidoking,
            Falkner = (int)Species.Pidgeotto,
            Bugsy = (int)Species.Scyther,
            Whitney = (int)Species.Miltank,
            Morty = (int)Species.Gengar,
            Chuck = (int)Species.Poliwrath,
            Jasmine = (int)Species.Steelix,
            Pryce = (int)Species.Piloswine,
            Clair = (int)Species.Kingdra,
            Roxanne = (int)Species.Nosepass,
            Brawly = (int)Species.Makuhita,
            Wattson = (int)Species.Magneton,
            Flannery = (int)Species.Torkoal,
            Norman = (int)Species.Slaking,
            Winona = (int)Species.Altaria,
            Liza = (int)Species.Lunatone,
            Tate = (int)Species.Solrock,
            Wallace = (int)Species.Milotic,
            Roark = (int)Species.Cranidos,
            Gardenia = (int)Species.Roserade,
            Maylene = (int)Species.Lucario,
            CrasherWake = (int)Species.Floatzel,
            Fantina = (int)Species.Mismagius,
            Byron = (int)Species.Bastiodon,
            Candice = (int)Species.Abomasnow,
            Volkner = (int)Species.Luxray,
            Cheren = (int)Species.Lillipup,
            Roxie = (int)Species.Whirlipede,
            Burgh = (int)Species.Leavanny,
            Elesa = (int)Species.Zebstrika,
            Clay = (int)Species.Excadrill,
            Skyla = (int)Species.Swanna,
            Drayden = (int)Species.Haxorus,
            Marlon = (int)Species.Jellicent,
            Viola = (int)Species.Vivillon,
            Grant = (int)Species.Amaura,
            Korrina = (int)Species.Hawlucha,
            Ramos = (int)Species.Gogoat,
            Clemont = (int)Species.Heliolisk,
            Valerie = (int)Species.Sylveon,
            Olympia = (int)Species.Meowstic,
            Wulfric = (int)Species.Avalugg,
            Ilima = (int)Species.Gumshoos,
            Lana = (int)Species.Araquanid,
            Kiawe = (int)Species.Salazzle,
            Mallow = (int)Species.Lurantis,
            Sophocles = (int)Species.Togedemaru,
            Acerola = (int)Species.Mimikyu,
            AncientTrial = (int)Species.Kommoo,
            Hapu = (int)Species.Mudsdale,
            Mina = (int)Species.Ribombee,
            Hala = (int)Species.Crabrawler,
            Olivia = (int)Species.Lycanroc,
            Nanu = (int)Species.Krokorok,

        }
        public enum badges
        {
            BoulderBadge = gleaderpoke.Brock,
            CascadeBadge = gleaderpoke.Misty,
            ThunderBadge = gleaderpoke.Surge,
            RainbowBadge = gleaderpoke.Erika,
            SoulBadge = gleaderpoke.Koga,
            MarshBadge = gleaderpoke.Sabrina,
            VolcanoBadge = gleaderpoke.Blaine,
            EarthBadge = gleaderpoke.Giovanni,
            ZephyrBadge = gleaderpoke.Falkner,
            HiveBadge = gleaderpoke.Bugsy,
            PlainBadge = gleaderpoke.Whitney,
            FogBadge = gleaderpoke.Morty,
            StormBadge = gleaderpoke.Chuck,
            MineralBadge = gleaderpoke.Jasmine,
            GlacierBadge = gleaderpoke.Pryce,
            RisingBadge = gleaderpoke.Clair,
            StoneBadge = gleaderpoke.Roxanne,
            KnuckleBadge = gleaderpoke.Brawly,
            DynamoBadge = gleaderpoke.Wattson,
            HeatBadge = gleaderpoke.Flannery,
            BalanceBadge = gleaderpoke.Norman,
            FeatherBadge = gleaderpoke.Winona,
            MindBadge = gleaderpoke.Liza,
            MindBadge2 = gleaderpoke.Tate,
            RainBadge = gleaderpoke.Wallace,
            CoalBadge = gleaderpoke.Roark,
            ForestBadge = gleaderpoke.Gardenia,
            CobalBadge = gleaderpoke.Maylene,
            FenBadge = gleaderpoke.CrasherWake,
            RelicBadge = gleaderpoke.Fantina,
            MineBadge = gleaderpoke.Byron,
            GlaceonBadge = gleaderpoke.Candice,
            BeaconBadge = gleaderpoke.Volkner,
            BasicBadge = gleaderpoke.Cheren,
            ToxicBadge = gleaderpoke.Roxie,
            InsectBadge = gleaderpoke.Burgh,
            BoltBadge = gleaderpoke.Elesa,
            QuakeBadge = gleaderpoke.Clay,
            JetBadge = gleaderpoke.Skyla,
            LegendBadge = gleaderpoke.Drayden,
            WaveBadge = gleaderpoke.Marlon,
            BugBadge = gleaderpoke.Viola,
            CliffBadge = gleaderpoke.Grant,
            RumbleBadge = gleaderpoke.Korrina,
            GrassBadge = gleaderpoke.Ramos,
            VoltageBadge = gleaderpoke.Clemont,
            FairyBadge = gleaderpoke.Valerie,
            PsychicBadge = gleaderpoke.Olympia,
            IcebergBadge= gleaderpoke.Wulfric,
            NormaliumZ = gleaderpoke.Ilima,
            WateriumZ =gleaderpoke.Lana,
            FiriumZ = gleaderpoke.Kiawe,
            GrassiumZ = gleaderpoke.Mallow,
            ElectriumZ = gleaderpoke.Sophocles,
            GhostiumZ = gleaderpoke.Acerola,
            DragoniumZ = gleaderpoke.AncientTrial,
            FairiumZ = gleaderpoke.Mina,
            MeleMeleStamp = gleaderpoke.Hala,
            AkalaStamp = gleaderpoke.Olivia,
            UlaulaStamp = gleaderpoke.Nanu,
            PoniStamp = gleaderpoke.Hapu
        }


    
     
   
            public static double[] TypeDamageMultiplier(int[] types, int moveType)
        {
            double[] effectiveness = { -1, -1 };
            for (int i = 0; i < types.Length; i++)
            {
                effectiveness[i] = moveType switch
                {
                    0 => new double[] { 1.0, 1.0, 1.0, 1.0, 1.0, 0.5, 1.0, 0.0, 0.5, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 }[types[i]],
                    1 => new double[] { 2.0, 1.0, 0.5, 0.5, 1.0, 2.0, 0.5, 0.0, 2.0, 1.0, 1.0, 1.0, 1.0, 0.5, 2.0, 1.0, 2.0, 0.5 }[types[i]],
                    2 => new double[] { 1.0, 2.0, 1.0, 1.0, 1.0, 0.5, 2.0, 1.0, 0.5, 1.0, 1.0, 2.0, 0.5, 1.0, 1.0, 1.0, 1.0, 1.0 }[types[i]],
                    3 => new double[] { 1.0, 1.0, 1.0, 0.5, 0.5, 0.5, 1.0, 0.5, 0.0, 1.0, 1.0, 2.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0 }[types[i]],
                    4 => new double[] { 1.0, 1.0, 0.0, 2.0, 1.0, 2.0, 0.5, 1.0, 2.0, 2.0, 1.0, 0.5, 2.0, 1.0, 1.0, 1.0, 1.0, 1.0 }[types[i]],
                    5 => new double[] { 1.0, 0.5, 2.0, 1.0, 0.5, 1.0, 2.0, 1.0, 0.5, 2.0, 1.0, 1.0, 1.0, 1.0, 2.0, 1.0, 1.0, 1.0 }[types[i]],
                    6 => new double[] { 1.0, 0.5, 0.5, 0.5, 1.0, 1.0, 1.0, 0.5, 0.5, 0.5, 1.0, 2.0, 1.0, 2.0, 1.0, 1.0, 2.0, 0.5 }[types[i]],
                    7 => new double[] { 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0, 1.0, 1.0, 0.5, 1.0 }[types[i]],
                    8 => new double[] { 1.0, 1.0, 1.0, 1.0, 1.0, 2.0, 1.0, 1.0, 0.5, 0.5, 0.5, 1.0, 0.5, 1.0, 2.0, 1.0, 1.0, 2.0 }[types[i]],
                    9 => new double[] { 1.0, 1.0, 1.0, 1.0, 1.0, 0.5, 2.0, 1.0, 2.0, 0.5, 0.5, 2.0, 1.0, 1.0, 2.0, 0.5, 1.0, 1.0 }[types[i]],
                    10 => new double[] { 1.0, 1.0, 1.0, 1.0, 2.0, 2.0, 1.0, 1.0, 1.0, 2.0, 0.5, 0.5, 1.0, 1.0, 1.0, 0.5, 1.0, 1.0 }[types[i]],
                    11 => new double[] { 1.0, 1.0, 0.5, 0.5, 2.0, 2.0, 0.5, 1.0, 0.5, 0.5, 2.0, 0.5, 1.0, 1.0, 1.0, 0.5, 1.0, 1.0 }[types[i]],
                    12 => new double[] { 1.0, 1.0, 2.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0, 0.5, 0.5, 1.0, 1.0, 0.5, 1.0, 1.0 }[types[i]],
                    13 => new double[] { 1.0, 2.0, 1.0, 2.0, 1.0, 1.0, 1.0, 1.0, 0.5, 1.0, 1.0, 1.0, 1.0, 0.5, 1.0, 1.0, 0.0, 1.0 }[types[i]],
                    14 => new double[] { 1.0, 1.0, 2.0, 1.0, 2.0, 1.0, 1.0, 1.0, 0.5, 0.5, 0.5, 2.0, 1.0, 1.0, 0.5, 2.0, 1.0, 1.0 }[types[i]],
                    15 => new double[] { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.5, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0, 1.0, 0.0 }[types[i]],
                    16 => new double[] { 1.0, 0.5, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0, 1.0, 1.0, 0.5, 0.5 }[types[i]],
                    17 => new double[] { 1.0, 2.0, 1.0, 0.5, 1.0, 1.0, 1.0, 1.0, 0.5, 0.5, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0, 2.0, 1.0 }[types[i]],
                    _ => 1.0,
                };
            }
            return effectiveness;
        }
        public static int CalculateEffectiveStat(int statIV, int statEV, int statBase, int level) => ((statIV + (2 * statBase) + (statEV / 4)) * level / 100) + 5;

        public static double WeightedDamage(PK7 lairPk, PK7 pk, int[]types, MoveInfo move)
        {
            var power = Convert.ToDouble(move.Power);
            double typeMultiplier = -1.0;
            double stab = pk.Ability == (int)Ability.Adaptability && (pk.PersonalInfo.Type1 == (int)move.Type || pk.PersonalInfo.Type2 == (int)move.Type) ? 2.0 : pk.PersonalInfo.Type1 == (int)move.Type || pk.PersonalInfo.Type2 == (int)move.Type ? 1.5 : 1.0;
            var typeMulti = TypeDamageMultiplier(types, (int)move.Type);
            if (typeMulti[0] == 0.0 || typeMulti[1] == 0.0)
                typeMultiplier = 0.0;
            else if (typeMulti[0] == 0.5 && typeMulti[1] == 0.5 && types[0] != types[1])
                typeMultiplier = 0.25;
            else if (typeMulti[0] == 0.5 || typeMulti[1] == 0.5)
                typeMultiplier = 0.5;
            else if (typeMulti[0] == 1.0 && typeMulti[1] == 1.0)
                typeMultiplier = 1.0;
            else if (typeMulti[0] == 2.0 && typeMulti[1] == 2.0 && types[0] != types[1])
                typeMultiplier = 4.0;
            else if (typeMulti[0] == 2.0 || typeMulti[1] == 2.0)
                typeMultiplier = 2.0;
            double multiplier =  0.925 * typeMultiplier * stab * (move.Accuracy / 100.0);
            bool physical = move.Category == MoveCategory.Physical;
            bool bodyPress = move.MoveID == (int)Move.BodyPress;
            bool foulPlay = move.MoveID == (int)Move.FoulPlay;
            bool psy = move.MoveID == (int)Move.Psyshock || move.MoveID == (int)Move.Psystrike;
            double effectiveAttack = physical switch
            {
                true => CalculateEffectiveStat(bodyPress ? pk.IV_DEF : foulPlay ? lairPk.IV_ATK : pk.IV_ATK, bodyPress ? pk.EV_DEF : foulPlay ? lairPk.EV_ATK : pk.EV_ATK, bodyPress ? pk.PersonalInfo.DEF : foulPlay ? lairPk.PersonalInfo.ATK : pk.PersonalInfo.ATK, pk.CurrentLevel),
                false => CalculateEffectiveStat(pk.IV_SPA, pk.EV_SPA, pk.PersonalInfo.SPA, pk.CurrentLevel),
            };

            double effectiveDefense = physical switch
            {
                true => CalculateEffectiveStat(lairPk.IV_DEF, lairPk.EV_DEF, lairPk.PersonalInfo.DEF, lairPk.CurrentLevel),
                false => CalculateEffectiveStat(psy ? lairPk.IV_DEF : lairPk.IV_SPD, psy ? lairPk.EV_DEF : lairPk.EV_SPD, psy ? lairPk.PersonalInfo.DEF : lairPk.PersonalInfo.SPD, lairPk.CurrentLevel),
            };
           double dmgCalc = ((((2 * pk.CurrentLevel / 5) + 2) * power * (effectiveAttack / effectiveDefense) / 50) + 2) * multiplier;
            return dmgCalc;
        }
    }
    public class MoveInfo
    {
        public class MoveInfoRoot
        {
            public HashSet<MoveInfo> Moves { get; private set; } = new();
        }

        public int MoveID { get; set; }
        public string Name { get; set; } = string.Empty;
        public MoveType Type { get; set; }
        public MoveCategory Category { get; set; }
        public int Power { get; set; }
        public int Accuracy { get; set; }
        public int Priority { get; set; }
        public int EffectSequence { get; set; }
        public int Recoil { get; set; }
        public int PowerGmax { get; set; }
        public bool Contact { get; set; }
        public bool Charge { get; set; }
        public bool Recharge { get; set; }
        public bool Sound { get; set; }
        public bool Gravity { get; set; }
        public bool Defrost { get; set; }
        public MoveTarget Target { get; set; }
    }
    public enum MoveCategory
    {
        Status,
        Physical,
        Special
    }
  
}
