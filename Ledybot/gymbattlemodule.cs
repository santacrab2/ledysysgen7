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
            while (File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{battler.Id}//Badges//Badges.txt").Contains($"{(badges)leaderpoke}"))
            {
                leaderpoke = (gleaderpoke)vals.GetValue(GBrng.Next(vals.Length));
                if (File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{battler.Id}//Badges//Badges.txt").Length >= 60)
                    break;
                
            }

            opponentpoke = new PK7 { Species = (int)leaderpoke, Nature = natue, CurrentLevel = battlebuddy.CurrentLevel };
            if (File.ReadAllLines($"{Directory.GetCurrentDirectory()}//{battler.Id}//Badges//Badges.txt").Length >= 60)
                opponentpoke.CurrentLevel = 100;
            int[] sugmov = MoveSetApplicator.GetMoveSet(opponentpoke, true);
            opponentpoke.SetMoves(sugmov);
            opponentpoke.SetRandomIVs();
            opponentpoke.EVs = SetMaxEVs(opponentpoke);
            opponentpoke.Stat_HPCurrent = (int)(0.01 * (2 * opponentpoke.PersonalInfo.HP + opponentpoke.IV_HP + (0.25 * opponentpoke.EV_HP)) * opponentpoke.CurrentLevel) + opponentpoke.CurrentLevel + 10;
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

            Random GBrng = new Random();
            string json = File.ReadAllText($"{Directory.GetCurrentDirectory()}//MoveInfo.json");
            MoveRoot = JsonConvert.DeserializeObject<MoveInfo.MoveInfoRoot>(json);
            int opselect = GBrng.Next(4);
            var opmove = MoveRoot.Moves.FirstOrDefault(x => x.MoveID == opponentpoke.Moves[opselect]);
            var yourmove = MoveRoot.Moves.FirstOrDefault(x => x.MoveID == battlebuddy.Moves[move]);
            var yourtypes = new int[] { battlebuddy.PersonalInfo.Type1, battlebuddy.PersonalInfo.Type2 };
            var optypes = new int[] { opponentpoke.PersonalInfo.Type1, opponentpoke.PersonalInfo.Type2 };
            double yourmovepower = 0;
            if (GBrng.Next(3) != 0)
                yourmovepower = Math.Round(WeightedDamage(opponentpoke, (PK7)battlebuddy, yourmove));



            var opmovepower = Math.Round(WeightedDamage((PK7)battlebuddy, opponentpoke, opmove));
            if (opmove.MoveID == (int)Move.Protect)
                yourmovepower = 0;
            if (yourmove.MoveID == (int)Move.Protect)
                opmovepower = 0;
            opponentpoke.Stat_HPCurrent = opmove.MoveID switch
            {
                (int)Move.AquaRing or (int)Move.Ingrain or (int)Move.LeechSeed => opponentpoke.Stat_HPCurrent + (opponentpoke.Stat_HPMax / 16),
                (int)Move.FloralHealing or (int)Move.HealPulse or (int)Move.Wish or (int)Move.HealOrder or (int)Move.MilkDrink or (int)Move.Purify or (int)Move.Recover or (int)Move.Roost or (int)Move.ShoreUp or (int)Move.SlackOff or (int)Move.SoftBoiled => opponentpoke.Stat_HPCurrent + (opponentpoke.Stat_HPMax / 2),
                (int)Move.LifeDew or (int)Move.Moonlight or (int)Move.MorningSun or (int)Move.Synthesis => opponentpoke.Stat_HPCurrent + (opponentpoke.Stat_HPMax / 4),
                _ => opponentpoke.Stat_HPCurrent
            };
            if (opmove.Name == "Rest")
            {
                opponentpoke.Stat_HPCurrent = opponentpoke.Stat_HPMax;
                opponentpoke.Status_Condition = (int)StatusCondition.Asleep;
            }
            if (opponentpoke.Stat_HPCurrent > opponentpoke.Stat_HPMax)
                opponentpoke.Stat_HPCurrent = opponentpoke.Stat_HPMax;
            battlebuddy.Stat_HPCurrent = yourmove.MoveID switch
            {
                (int)Move.AquaRing or (int)Move.Ingrain or (int)Move.LeechSeed => battlebuddy.Stat_HPCurrent + (battlebuddy.Stat_HPMax / 16),
                (int)Move.FloralHealing or (int)Move.HealPulse or (int)Move.Wish or (int)Move.HealOrder or (int)Move.MilkDrink or (int)Move.Purify or (int)Move.Recover or (int)Move.Roost or (int)Move.ShoreUp or (int)Move.SlackOff or (int)Move.SoftBoiled => battlebuddy.Stat_HPCurrent + (battlebuddy.Stat_HPMax / 2),
                (int)Move.LifeDew or (int)Move.Moonlight or (int)Move.MorningSun or (int)Move.Synthesis => battlebuddy.Stat_HPCurrent + (battlebuddy.Stat_HPMax / 4),
                _ => battlebuddy.Stat_HPCurrent
            };
            if (yourmove.Name == "Rest")
            {
                battlebuddy.Stat_HPCurrent = battlebuddy.Stat_HPMax;
                battlebuddy.Status_Condition = (int)StatusCondition.Asleep;
            }
            if (battlebuddy.Stat_HPCurrent > battlebuddy.Stat_HPMax)
                battlebuddy.Stat_HPCurrent = battlebuddy.Stat_HPMax;

            if (battlebuddy.Stat_HPCurrent >= opmovepower)
                battlebuddy.Stat_HPCurrent -= (int)opmovepower;
            else battlebuddy.Stat_HPCurrent = 0;
            if (opponentpoke.Stat_HPCurrent >= yourmovepower)
                opponentpoke.Stat_HPCurrent -= (int)yourmovepower;
            else opponentpoke.Stat_HPCurrent = 0;
            if (battlebuddy.Status_Condition == (int)StatusCondition.Poisoned || battlebuddy.Status_Condition == (int)StatusCondition.Burned)
                if (battlebuddy.Stat_HPCurrent - (battlebuddy.Stat_HPMax / 16) >= 0)
                    battlebuddy.Stat_HPCurrent = battlebuddy.Stat_HPCurrent - (battlebuddy.Stat_HPMax / 16);
                else battlebuddy.Stat_HPCurrent = 0;
            if (opponentpoke.Status_Condition == (int)StatusCondition.Poisoned || opponentpoke.Status_Condition == (int)StatusCondition.Burned)
                if (opponentpoke.Stat_HPCurrent - (opponentpoke.Stat_HPMax / 16) >= 0)
                    opponentpoke.Stat_HPCurrent = opponentpoke.Stat_HPCurrent - (opponentpoke.Stat_HPMax / 16);
                else opponentpoke.Stat_HPCurrent = 0;


            await battler.SendMessageAsync(yourmovepower != 0 ? $"You used {yourmove.Name} and did {yourmovepower} damage" : $"{yourmove.Name} missed and did 0 damage"); ;
            await battler.SendMessageAsync($"{leaderpoke} used {opmove.Name} and did {opmovepower} damage");
            if (opponentpoke.Stat_HPCurrent != 0 && battlebuddy.Stat_HPCurrent != 0)
            {
                if (GBrng.Next(2) == 1)
                {
                    opponentpoke.Status_Condition = yourmove.Name switch
                    {
                        "Body Slam" or "Bolt Strike" or "Bounce" or "Buzzy Buzz" or "Discharge" or "Dragon Breath" or "Fling" or "Force Palm" or "Freeze Shock" or "Glare" or "Lick" or "Nuzzle" or "Secret Power" or "Shadow Bolt" or "Spark" or "Splishy Splash" or "Stoked Sparksurfer" or "Stun Spore" or "Thunder" or "Thunder Fang" or "Thunder Punch" or "Thunder Shock" or "Thunder Wave" or "Thunderbolt" or "Tri Attack" or "Volt Tackle" or "Zap Cannon" => (int)StatusCondition.Paralyzed,
                        "Dark Void" or "Grass Whistle" or "Hypnosis" or "Lovely Kiss" or "Psycho Shift" or "Relic Song" or "Sing" or "Sleep Powder" or "Spore" or "Yawn" => (int)StatusCondition.Asleep,
                        "Blizzard" or "Freeze-Dry" or "Freezing Glare" or "Ice Beam" or "Ice Fang" or "Ice Punch" or "Powder Snow" or "Shadow Chill" => (int)StatusCondition.Frozen,
                        "Beak Blast" or "Blaze Kick" or "Blue Flare" or "Burning Jealousy" or "Ember" or "Fire Blast" or "Fire Fang" or "Fire Punch" or "Flame Wheel" or "Flamethrower" or "Flare Blitz" or "Heat Wave" or "Ice Burn" or "Inferno" or "Lava Plume" or "Pyro Ball" or "Sacred Fire" or "Scald" or "Scorching Sands" or "Searing Shot" or "Shadow Fire" or "Sizzly Slide" or "Steam Eruption" or "Will-O-Wisp" => (int)StatusCondition.Burned,
                        "Baneful Bunker" or "Cross Poison" or "Gunk Shot" or "Poison Fang" or "Poison Gas" or "Poison Jab" or "Poison Powder" or "Poison Sting" or "Poison Tail" or "Sludge" or "Sludge Bomb" or "Sludge Wave" or "Smog" or "Toxic" or "Toxic Spikes" or "Toxic Thread" or "Twineedle" => (int)StatusCondition.Poisoned,
                        _ => 0
                    };
                    battlebuddy.Status_Condition = opmove.Name switch
                    {
                        "Body Slam" or "Bolt Strike" or "Bounce" or "Buzzy Buzz" or "Discharge" or "Dragon Breath" or "Fling" or "Force Palm" or "Freeze Shock" or "Glare" or "Lick" or "Nuzzle" or "Secret Power" or "Shadow Bolt" or "Spark" or "Splishy Splash" or "Stoked Sparksurfer" or "Stun Spore" or "Thunder" or "Thunder Fang" or "Thunder Punch" or "Thunder Shock" or "Thunder Wave" or "Thunderbolt" or "Tri Attack" or "Volt Tackle" or "Zap Cannon" => (int)StatusCondition.Paralyzed,
                        "Dark Void" or "Grass Whistle" or "Hypnosis" or "Lovely Kiss" or "Psycho Shift" or "Relic Song" or "Sing" or "Sleep Powder" or "Spore" or "Yawn" => (int)StatusCondition.Asleep,
                        "Blizzard" or "Freeze-Dry" or "Freezing Glare" or "Ice Beam" or "Ice Fang" or "Ice Punch" or "Powder Snow" or "Shadow Chill" => (int)StatusCondition.Frozen,
                        "Beak Blast" or "Blaze Kick" or "Blue Flare" or "Burning Jealousy" or "Ember" or "Fire Blast" or "Fire Fang" or "Fire Punch" or "Flame Wheel" or "Flamethrower" or "Flare Blitz" or "Heat Wave" or "Ice Burn" or "Inferno" or "Lava Plume" or "Pyro Ball" or "Sacred Fire" or "Scald" or "Scorching Sands" or "Searing Shot" or "Shadow Fire" or "Sizzly Slide" or "Steam Eruption" or "Will-O-Wisp" => (int)StatusCondition.Burned,
                        "Baneful Bunker" or "Cross Poison" or "Gunk Shot" or "Poison Fang" or "Poison Gas" or "Poison Jab" or "Poison Powder" or "Poison Sting" or "Poison Tail" or "Sludge" or "Sludge Bomb" or "Sludge Wave" or "Smog" or "Toxic" or "Toxic Spikes" or "Toxic Thread" or "Twineedle" => (int)StatusCondition.Poisoned,
                        _ => 0
                    };
                }
                if (GBrng.Next(4) == 0)
                    battlebuddy.Status_Condition = 0;
                if (GBrng.Next(4) == 0)
                    opponentpoke.Status_Condition = 0;
                battleembed = new EmbedBuilder();
                battleembed.AddField("gym battle", $"Battle between {leaderpoke}'s {(Species)leaderpoke} and {battler.Username}'s {(Species)battlebuddy.Species}");
                battleembed.AddField("Opponent", $"{(opponentpoke.Status_Condition == 0 ? (Species)opponentpoke.Species : $"{(Species)opponentpoke.Species}({(StatusCondition)opponentpoke.Status_Condition})")}\n HP:{opponentpoke.Stat_HPCurrent}/{opponentpoke.Stat_HPMax}");
                battleembed.AddField($"{battler.Username}", $"{(battlebuddy.Status_Condition == 0 ? (Species)battlebuddy.Species : $"{(Species)battlebuddy.Species}({(StatusCondition)battlebuddy.Status_Condition})")}\n HP:{battlebuddy.Stat_HPCurrent}/{battlebuddy.Stat_HPMax}");
                battleembed.AddField($"Moves", $":one:{(Move)battlebuddy.Move1}\n:two:{(Move)battlebuddy.Move2}\n:three:{(Move)battlebuddy.Move3}\n:four:{(Move)battlebuddy.Move4}");
                battleembed.ImageUrl = $"https://play.pokemonshowdown.com/sprites/trainers/{leaderpoke.ToString().ToLower()}.png";
                IEmote[] reactions = { new Emoji("1️⃣"), new Emoji("2️⃣"), new Emoji("3️⃣"), new Emoji("4️⃣") };
                var battlemsg = await battler.SendMessageAsync(embed: battleembed.Build());
                await battlemsg.AddReactionsAsync(reactions).ConfigureAwait(false);

            }
            else
            {
                if (opponentpoke.Stat_HPCurrent == 0)
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
            discordbot.page = 0;
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
            IcebergBadge = gleaderpoke.Wulfric,
            NormaliumZ = gleaderpoke.Ilima,
            WateriumZ = gleaderpoke.Lana,
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

        public static double WeightedDamage(PK7 lairPk, PK7 pk, MoveInfo move)
        {



            double dmgCalc = 0.0;
            int[] types = { lairPk.PersonalInfo.Type1, lairPk.PersonalInfo.Type2 };
            var encAbility = lairPk.Ability;
            var ourAbility = pk.Ability;


            double typeMultiplier = 0.0;

            var power = Convert.ToDouble(move.Power);
            bool waterImmune = move.Type == MoveType.Water && (lairPk.Ability == (int)Ability.DrySkin || lairPk.Ability == (int)Ability.WaterAbsorb || lairPk.Ability == (int)Ability.StormDrain);
            bool electricImmune = move.Type == MoveType.Electric && (lairPk.Ability == (int)Ability.VoltAbsorb || lairPk.Ability == (int)Ability.LightningRod || lairPk.Ability == (int)Ability.MotorDrive);
            bool groundImmune = move.MoveID != (int)Move.ThousandArrows && move.Type == MoveType.Ground && (lairPk.Ability == (int)Ability.Levitate || types[0] == (int)MoveType.Flying || types[1] == (int)MoveType.Flying);
            bool fireImmune = move.Type == MoveType.Fire && lairPk.Ability == (int)Ability.FlashFire;
            bool grassImmune = move.Type == MoveType.Grass && lairPk.Ability == (int)Ability.SapSipper;
            bool conditioned = pk.Status_Condition == (int)StatusCondition.Asleep || pk.Status_Condition == (int)StatusCondition.Paralyzed || pk.Status_Condition == (int)StatusCondition.Frozen;
            bool ignoreAbility = pk.Ability == (int)Ability.Turboblaze || pk.Ability == (int)Ability.Teravolt || pk.Ability == (int)Ability.MoldBreaker;

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

            bool usefulStatus = (move.MoveID == (int)Move.Toxic && lairPk.Status_Condition != (int)StatusCondition.Poisoned) || move.MoveID == (int)Move.Counter || move.MoveID == (int)Move.LifeDew ||
                move.MoveID == (int)Move.WideGuard || (move.MoveID == (int)Move.Yawn && lairPk.Status_Condition != (int)StatusCondition.Asleep) || (move.MoveID == (int)Move.Protect);
            if ((!ignoreAbility && (waterImmune || electricImmune || fireImmune || grassImmune || groundImmune)) || (move.Category == MoveCategory.Status && !usefulStatus) || (move.MoveID == (int)Move.DreamEater && lairPk.Status_Condition != (int)StatusCondition.Asleep) || conditioned)
                typeMultiplier = 0.0;


            double stab = ourAbility == (int)Ability.Adaptability && (pk.PersonalInfo.Type1 == (int)move.Type || pk.PersonalInfo.Type2 == (int)move.Type) ? 2.0 : pk.PersonalInfo.Type1 == (int)move.Type || pk.PersonalInfo.Type2 == (int)move.Type ? 1.5 : 1.0;
            double multiplier = 1.0;
            multiplier *= encAbility switch // Target ability influence
            {
                (int)Ability.Fluffy => move.Type == MoveType.Fire && !move.Contact ? 2.0 : 0.5,
                (int)Ability.DrySkin => move.Type == MoveType.Fire ? 1.25 : move.Type == MoveType.Water ? -1.25 : 1.0,
                (int)Ability.ThickFat => move.Type == MoveType.Fire || move.Type == MoveType.Ice ? 0.5 : 1.0,
                (int)Ability.Heatproof => move.Type == MoveType.Fire ? 0.5 : 1.0,
                (int)Ability.PrismArmor => typeMultiplier >= 2.0 ? 0.75 : 1.0,
                (int)Ability.PunkRock => move.Sound ? 0.5 : 1.0,
                _ => 1.0,
            };

            multiplier *= ourAbility switch // Our ability influence
            {
                (int)Ability.TintedLens => typeMultiplier < 1.0 ? 2.0 : 1.0,
                (int)Ability.IronFist => move.Name.Contains("Punch") || move.Name.Contains("Hammer") || move.MoveID == (int)Move.MeteorMash || move.MoveID == (int)Move.SkyUppercut ? 1.2 : 1.0,
                (int)Ability.StrongJaw => move.Name.Contains("Fang") || move.MoveID == (int)Move.Bite || move.MoveID == (int)Move.Crunch || move.MoveID == (int)Move.JawLock ? 1.5 : 1.0,
                (int)Ability.Adaptability => (int)move.Type == pk.PersonalInfo.Type1 || (int)move.Type == pk.PersonalInfo.Type2 ? 1.75 : 1.0,
                (int)Ability.PunkRock => move.Sound ? 1.3 : 1.0,
                (int)Ability.Normalize or (int)Ability.Refrigerate or (int)Ability.Aerilate or (int)Ability.Galvanize or (int)Ability.Pixilate => move.Type == MoveType.Normal ? 1.2 : 1.0,
                _ => 1.0,
            };

            multiplier *= pk.HeldItem switch // Held item influence
            {
                268 => typeMultiplier >= 2 ? 1.2 : 1.0, // Expert Belt
                270 => 1.3, // Life Orb
                _ => 1.0,
            };

            multiplier *= move.Type switch
            {
                MoveType.Fairy => (ourAbility == (int)Ability.FairyAura || encAbility == (int)Ability.FairyAura) && (ourAbility == (int)Ability.AuraBreak || encAbility == (int)Ability.AuraBreak) ? 0.75 : ourAbility == (int)Ability.FairyAura || encAbility == (int)Ability.FairyAura ? 1.33 : 1.0,
                MoveType.Dark => (ourAbility == (int)Ability.FairyAura || encAbility == (int)Ability.FairyAura) && (ourAbility == (int)Ability.AuraBreak || encAbility == (int)Ability.AuraBreak) ? 0.75 : ourAbility == (int)Ability.DarkAura || encAbility == (int)Ability.DarkAura ? 1.33 : 1.0,
                _ => 1.0,
            };

            multiplier *= 0.925 * typeMultiplier * stab * (move.Accuracy / 100.0);
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

            power *= move.MoveID switch
            {
                (int)Move.Acrobatics => pk.HeldItem == 0 ? 2.0 : 1.0,
                (int)Move.Hex => lairPk.Status_Condition != (int)StatusCondition.NoCondition ? 2.0 : 1.0,
                (int)Move.Venoshock => lairPk.Status_Condition == (int)StatusCondition.Poisoned ? 2.0 : 1.0,
                (int)Move.DreamEater => lairPk.Status_Condition == (int)StatusCondition.Asleep ? 1.5 : 1.0,
                _ => 1.0,
            };

            double status = pk.Status_Condition switch // Add extra weight based on niche circumstances
            {
                (int)StatusCondition.Burned => move.Category == MoveCategory.Physical && ourAbility != (int)Ability.Guts ? 0.5 : 1.0,
                (int)StatusCondition.Frozen => move.Defrost ? 10.0 : 1.0,
                (int)StatusCondition.Asleep => move.MoveID == (int)Move.Snore || move.MoveID == (int)Move.SleepTalk ? 10.0 : 1.0,
                _ => 1.0,
            };

            power *= status * ((move.Charge || move.Recharge) ? 0.5 : 1.0);



            dmgCalc = ((((2 * pk.CurrentLevel / 5) + 2) * power * (effectiveAttack / effectiveDefense) / 50) + 2) * multiplier;

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
        public enum StatusCondition
        {
            NoCondition,
            Paralyzed,
            Asleep,
            Frozen,
            Burned,
            Poisoned,
        }


    }

