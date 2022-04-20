using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PKHeX.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Windows.Forms;
using PKHeX.Core;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core.AutoMod;

namespace Ledybot
{
    public class GTSBot7
    {

        //private System.IO.StreamWriter file = new StreamWriter(@"C:\Temp\ledylog.txt");

        public enum gtsbotstates { botstart, startsearch, pressSeek, openpokemonwanted, openwhatpokemon, typepokemon, presssearch, startfind, findfromend, findfromstart, trade, research, botexit, updatecomments, quicksearch, panic, wondertrade };

        public static string consoleName = "Ledybot";

        public static TcpClient syncClient = new TcpClient();
        public static IPEndPoint serverEndPointSync = null;
        public static bool useLedySync = false;

        public static TcpClient tvClient = new TcpClient();
        public static IPEndPoint serverEndPointTV = null;
        private bool useLedybotTV = false;


        public const int SEARCHDIRECTION_FROMBACK = 0;
        public const int SEARCHDIRECTION_FROMBACKFIRSTPAGEONLY = 1;
        public const int SEARCHDIRECTION_FROMFRONT = 2;

        public static uint addr_PageSize; //How many entries are on the current GTS page
        public static uint addr_PageEndStartRecord; //This address holds the address to the last block in the entry-block-list
        public static uint addr_PageStartStartRecord; //This address holds the address to the first block in the entry block-list
        public static uint addr_PageCurrentView; //Current selected entry in the list
        public static uint addr_PageStartingIndex; //To determine on which page we are, 0 = first page, 100 = second page, etc
        public static uint addr_ListOfAllPageEntries; //Startingaddress of all up to 100 trade entries of the current page

        public static uint addr_box1slot1; //To inject the pokemon into box1slot1

        //private uint addr_SearchPokemonNameField = 0x301118D4; //Holds the currently typed in name in the "search pokemon" window

        public static uint addr_currentScreen; //Hopefully a address to tell us in what screen we are (roughly)

        public static uint addr_pokemonToFind;
        public static uint addr_pokemonToFindGender;
        public static uint addr_pokemonToFindLevel;

        public static int val_PlazaScreen;
        public static int val_Quit_SeekScreen;
        public static int val_SearchScreen; //also in the box during selecting etc
        public static int val_WhatPkmnScreen;
        public static int val_GTSListScreen;
        public static int val_BoxScreen;
        public static int val_system; //during error, saving, early sending
        public static int val_duringTrade; //trade is split in several steps, sometimes even 0x00
        public static int val_wondertradeerror; //special pokemon message
        public static int val_WTerror2; //catches if it doesnt even make it to the special pokemon error lol
        public static int val_emptyGTSpage; //since theres now a million bots because im the only one smart enough to realize this was a bad plan. This will catch empty GTS pages after the bots ravage them.
        public static int val_wondertradesearch;

        public static int iPokemonToFind = 0;
        public static int iPokemonToFindGender = 0;
        public static int iPokemonToFindLevel = 0;
        public static int iPID = 0;
        public static bool bBlacklist = false;
        public static bool bReddit = false;
        public static int searchDirection = 0;
        public static int dexnumber = 0;
        public static PKM pokecheck;
        public static string szFC = "";
        public static byte[] principal = new byte[4];

        public static bool botstop = false;
        public static int botState = 0;
        public static int botresult = 0;
        public static int attempts = 0;
        public static Task<bool> waitTaskbool;
        public static int commandtime = 200;
        public static int delaytime = 150;
        public static int o3dswaittime = 1000;

        public static int listlength = 0;
        public static int startIndex = 0;
        public static byte[] block = new byte[256];
        public static int tradeIndex = -1;
        public static uint addr_PageEntry = 0;
        public static bool foundLastPage = false;
        public static string szTrainerName;
        public static string tpfile;
        public static int stupid = 0;
        public static bool distribute = false;
        public static Tuple<string, string, int, int, int, ArrayList> details;
        public static ISocketMessageChannel logchan;
        public static ITextChannel wtchan;
        public static bool wondertrade = false;
        public static int mega;
        public static Stopwatch timeout = new Stopwatch();
        public static async Task<bool> isCorrectWindow(int expectedScreen)
        {
            await Task.Delay(o3dswaittime);
            await Program.helper.waitNTRread(addr_currentScreen);
            int screenID = (int)Program.helper.lastRead;

            //file.WriteLine("Checkscreen: " + expectedScreen + " - " + screenID + " botstate:" + botState);
            //file.Flush();
            return expectedScreen == screenID;
        }

        public static Boolean canThisTrade(byte[] principal, string consoleName, string trainerName, string country, string region, string pokemon, string szFC, string page, string index)
        {
            NetworkStream clientStream = syncClient.GetStream();
            byte[] buffer = new byte[4096];
            byte[] messageID = { 0x00 };
            string szmessage = consoleName + '\t' + trainerName + '\t' + country + '\t' + region + '\t' + pokemon + '\t' + page + "\t" + index + "\t";
            byte[] toSend = Encoding.UTF8.GetBytes(szmessage);

            buffer = messageID.Concat(principal).Concat(toSend).ToArray();
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();
            byte[] message = new byte[4096];
            try
            {
                //blocks until a client sends a message
                int bytesRead = clientStream.Read(message, 0, 4096);
                if (message[0] == 0x02)
                {
                    Program.f1.banlist.Add(szFC);
                }
                return message[0] == 0x01;
            }
            catch
            {
                return false;
                //a socket error has occured
            }
        }



        public GTSBot7(int iP, int iPtF, int iPtFGender, int iPtFLevel, bool bBlacklist, bool bReddit, int iSearchDirection, string waittime, string consoleName, bool useLedySync, string ledySyncIp, string ledySyncPort, int game)
        {
            if (!Directory.Exists($"{Directory.GetCurrentDirectory()}//trainerinfo//"))
                Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}//trainerinfo//");
            if (!ulong.TryParse(Ledybot.Program.f1.log.Text, out var cid))
                Ledybot.Program.f1.ChangeStatus("did not recognize your log channel or its empty");
            if (!ulong.TryParse(Program.f1.wtchannel.Text, out var wid))
                Program.f1.ChangeStatus("did not recognize your wt channel, or its empty.");
            wtchan = (ITextChannel)discordbot._client.GetChannelAsync(wid).Result;
            logchan = (ISocketMessageChannel)discordbot._client.GetChannelAsync(cid).Result;
            iPokemonToFindGender = iPtFGender;
            iPokemonToFindLevel = iPtFLevel;
            iPID = iP;
            bBlacklist = bBlacklist;
            bReddit = bReddit;
            searchDirection = iSearchDirection;
            o3dswaittime = Int32.Parse(waittime);
            if (useLedySync)
            {
                useLedySync = useLedySync;
                int iPort = Int32.Parse(ledySyncPort);
                serverEndPointSync = new IPEndPoint(IPAddress.Parse(ledySyncIp), iPort);
                syncClient.Connect(serverEndPointSync);
            }

            consoleName = consoleName;

            if (game == 0) // Sun and Moon
            {
                addr_PageSize = 0x32A6A1A4;
                addr_PageEndStartRecord = 0x32A6A68C;
                addr_PageStartStartRecord = 0x32A6A690;
                addr_PageCurrentView = 0x305ea384;
                addr_PageStartingIndex = 0x32A6A190;
                addr_ListOfAllPageEntries = 0x32A6A7C4;

                addr_box1slot1 = 0x330D9838;

                addr_currentScreen = 0x00674802;

                addr_pokemonToFind = 0x32A6A180;
                addr_pokemonToFindGender = 0x32A6A184;
                addr_pokemonToFindLevel = 0x32A6A188;

                val_PlazaScreen = 0x00;
                val_Quit_SeekScreen = 0x3F2B;
                val_SearchScreen = 0x4128;
                val_WhatPkmnScreen = 0x4160;
                val_GTSListScreen = 0x4180;
                val_BoxScreen = 0x4120;
                val_system = 0x41A8;
                val_duringTrade = 0x3FD5;
            }
            else if (game == 1) // Ultra Sun and Moon
            {
                addr_PageSize = 0x329921A4;
                addr_PageEndStartRecord = 0x3299268C;
                addr_PageStartStartRecord = 0x32992690;
                addr_PageCurrentView = 0x305CD9F4;
                addr_PageStartingIndex = 0x32992190;
                addr_ListOfAllPageEntries = 0x329927C4;

                addr_box1slot1 = 0x33015AB0;

                addr_currentScreen = 0x006A610A;

                addr_pokemonToFind = 0x32992180;
                addr_pokemonToFindGender = 0x32992184;
                addr_pokemonToFindLevel = 0x32992188;
                val_wondertradesearch = 0x41A8;
                val_PlazaScreen = 0x00;
                val_Quit_SeekScreen = 0x3F2B;
                val_SearchScreen = 0x412A;
                val_WhatPkmnScreen = 0x1040;
                val_GTSListScreen = 0x4180;
                val_BoxScreen = 0x4120;
                val_system = 0x1C848;
                val_duringTrade = 0x3FD5;
                val_wondertradeerror = 0x415A;
                val_emptyGTSpage = 0x40F5;
                val_WTerror2 = 0x41B8;
            }

        }

        public static async Task<int> RunBot()
        {
            try {
                byte[] pokemonIndex = new byte[2];
                byte pokemonGender = 0x0;
                byte pokemonLevel = 0x0;
                int panicAttempts = 0;
                if (wondertrade == true)
                {
                    botState = (int)gtsbotstates.wondertrade;
                }
                else
                    botState = (int)gtsbotstates.botstart;
                while (!botstop && timeout.ElapsedMilliseconds < 1200_000)
                {
                    if (botState != (int)gtsbotstates.panic)
                    {
                        panicAttempts = 0;
                    }
                    switch (botState)
                    {

                        case (int)gtsbotstates.botstart:
                            while (MainForm.combo_distri.SelectedIndex == 1 && discordbot.trademodule.pokequeue.Count == 0)
                                await Task.Delay(25);
                            if (discordbot.trademodule.pokequeue.Count == 0)
                                distribute = true;
                            if (MainForm.combo_distri.SelectedIndex == 0 && distribute == true)
                            {
                                int pts = 4321;
                                discordbot.trademodule.poketosearch.Enqueue(pts);
                                discordbot.trademodule.trainername.Enqueue("");


                            }
                            if (distribute == false)
                            {
                                try
                                {
                                    IMessageChannel chan = (IMessageChannel)discordbot.trademodule.channel.Peek();
                                
                                    await chan.SendMessageAsync("<@" + discordbot.trademodule.username.Peek() + ">" + " searching for you now. Deposit your pokemon if you haven't already.");
                                }catch { TwitchBot.client.SendMessage(TwitchBot.Channel, discordbot.trademodule.discordname.Peek() + " searching for you now. Deposit your pokemon if you haven't already."); }
                            }
                            if ((int)discordbot.trademodule.poketosearch.Peek() == 4321)
                            {
                                iPokemonToFind = MainForm.combo_pkmnList.SelectedIndex + 1;
                                if(iPokemonToFind == 808)
                                {
                                    Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                                    await Task.Delay(1000);
                                    Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                                    await Task.Delay(5000);
                                    Program.helper.quicktouch(180, 70, commandtime);
                                    await Task.Delay(5000);
                                    Program.helper.quicktouch(180, 160, commandtime);
                                    await Task.Delay(1000);
                                    botState = (int)gtsbotstates.wondertrade;
                                    discordbot.trademodule.poketosearch.Dequeue();
                                    discordbot.trademodule.trainername.Dequeue();
                                    break;
                                }
                                discordbot.trademodule.poketosearch.Dequeue();
                               
                            }
                            else
                            {
                                iPokemonToFind = (int)discordbot.trademodule.poketosearch.Peek();
                                discordbot.trademodule.poketosearch.Dequeue();
                            }

                            new discordbot.trademodule();
                            bool correctScreen = true;
                            pokemonIndex = new byte[2];
                            pokemonGender = 0x0;
                            pokemonLevel = 0x0;
                            byte[] full = BitConverter.GetBytes(iPokemonToFind);
                            pokemonIndex[0] = full[0];
                            pokemonIndex[1] = full[1];
                            full = BitConverter.GetBytes(iPokemonToFindGender);
                            pokemonGender = full[0];
                            full = BitConverter.GetBytes(iPokemonToFindLevel);
                            pokemonLevel = full[0];
                            panicAttempts = 0;
                            botState = 0;
                            dexnumber = 0;
                            stupid = 0;
                            if (bReddit)
                                Program.f1.updateJSON();
                            botState = (int)gtsbotstates.startsearch;
                            break;
                        case (int)gtsbotstates.updatecomments:
                            Program.f1.updateJSON();
                            botState = (int)gtsbotstates.research;
                            break;
                        case (int)gtsbotstates.startsearch:
                            Program.f1.ChangeStatus("Setting Pokemon to find");
                            waitTaskbool = Program.helper.waitNTRwrite(addr_pokemonToFind, pokemonIndex, iPID);
                            waitTaskbool = Program.helper.waitNTRwrite(addr_pokemonToFindGender, pokemonGender, iPID);
                            waitTaskbool = Program.helper.waitNTRwrite(addr_pokemonToFindLevel, pokemonLevel, iPID);
                            botState = (int)gtsbotstates.pressSeek;
                            break;
                        case (int)gtsbotstates.pressSeek:
                            if (stupid == 5)
                            {
                                startIndex = 0;
                                tradeIndex = -1;
                                listlength = 0;
                                addr_PageEntry = 0;
                                foundLastPage = false;
                                botresult = 8;



                                if (distribute == false)
                                {
                                    await logchan.SendMessageAsync($"{discordbot.trademodule.discordname.Peek()} did not complete their trade");
                                    try { await discordbot.trademodule.slow(); } catch { await TwitchBot.slow(); }
                                    botState = (int)gtsbotstates.botstart;
                                    break;
                                }
                                if (distribute == true)
                                {
                                    discordbot.trademodule.trainername.Dequeue();



                                    if (MainForm.combo_pkmnList.SelectedIndex < 805)
                                        MainForm.combo_pkmnList.SelectedIndex += 1;
                                    else
                                        MainForm.combo_pkmnList.SelectedIndex = 0;
                                   // while (discordbot.trademodule.tradevolvs.Contains(MainForm.combo_pkmnList.SelectedIndex + 1) || discordbot.trademodule.mythic.Contains(MainForm.combo_pkmnList.SelectedIndex + 1) || MainForm.combo_pkmnList.SelectedIndex == 587)
                                    //    MainForm.combo_pkmnList.SelectedIndex += 1;

                                    distribute = false;
                                    botState = (int)gtsbotstates.botstart;
                                }

                                break;
                            }
                            Program.f1.ChangeStatus("Pressing seek button");
                            //Seek/Deposite pokemon screen
                            correctScreen = await isCorrectWindow(val_Quit_SeekScreen);
                            if (!correctScreen)
                            {
                                botState = (int)gtsbotstates.panic;
                                break;
                            }
                            await Program.helper.waittouch(160, 80);
                            await Task.Delay(commandtime + delaytime);
                            botState = (int)gtsbotstates.presssearch;
                            break;
                        case (int)gtsbotstates.presssearch:
                            Program.f1.ChangeStatus("Press search button");
                            correctScreen = await isCorrectWindow(val_SearchScreen);
                            if (!correctScreen)
                            {
                                botState = (int)gtsbotstates.panic;
                                break;
                            }
                            //Pokemon wanted screen again, this time with filled out information
                            await Task.Delay(2000);

                            waitTaskbool = Program.helper.waittouch(160, 185);


                            if (await waitTaskbool)
                            {
                                botState = (int)gtsbotstates.findfromstart;
                                await Task.Delay(2250);
                            }
                            else
                            {
                                attempts++;
                                botresult = 6;
                                botState = (int)gtsbotstates.startsearch;
                            }
                            break;
                        case (int)gtsbotstates.findfromstart:
                            timeout.Restart();
                            correctScreen = await isCorrectWindow(val_GTSListScreen);
                            if (!correctScreen)
                            {
                                if (Program.helper.lastRead == val_emptyGTSpage)
                                {
                                    stupid = 5;
                                    while (!await isCorrectWindow(val_Quit_SeekScreen))
                                    {
                                        Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                                        await Task.Delay(500);
                                    }

                                    botState = (int)gtsbotstates.pressSeek;
                                }
                                //Hotfix for Only one Pokemon on List
                                if (Program.helper.lastRead == 0x40F5)
                                {
                                    // No Entries found.
                                    botState = (int)gtsbotstates.panic;
                                    break;
                                }
                                else if (Program.helper.lastRead == 0x40C0)
                                {
                                    // Only one Pokemon on List, ignore.
                                }
                                else
                                {
                                    botState = (int)gtsbotstates.panic;
                                    break;
                                }
                            }
                            //GTS entry list screen, cursor at position 1
                            await Program.helper.waitNTRread(addr_PageSize);

                            attempts = 0;
                            listlength = (int)Program.helper.lastRead;

                            if (listlength == 100 && !foundLastPage && searchDirection == SEARCHDIRECTION_FROMBACK)
                            {
                                Program.f1.ChangeStatus("Moving to last page");
                                waitTaskbool = Program.helper.waitNTRread(addr_PageStartingIndex);
                                if (await waitTaskbool)
                                {
                                    startIndex = (int)Program.helper.lastRead;
                                    waitTaskbool = Program.helper.waitNTRwrite(addr_PageStartingIndex, (uint)(startIndex + 200), iPID);
                                    if (await waitTaskbool)
                                    {
                                        startIndex += 100;
                                        Program.helper.quickbuton(Program.PKTable.DpadRIGHT, commandtime);
                                        await Task.Delay(commandtime + delaytime);
                                        Program.helper.quickbuton(Program.PKTable.DpadLEFT, commandtime);
                                        await Task.Delay(commandtime + delaytime);
                                        Program.helper.quickbuton(Program.PKTable.DpadLEFT, commandtime);
                                        await Task.Delay(commandtime + delaytime);
                                        //prevent potential loop by going left once more before the page is actually loaded
                                        Program.helper.quickbuton(Program.PKTable.DpadLEFT, commandtime);
                                        await Task.Delay(commandtime + delaytime);
                                        await Task.Delay(3000);
                                        Program.helper.quicktouch(10, 10, commandtime);
                                        await Task.Delay(commandtime + delaytime + 250);
                                        Program.helper.quicktouch(10, 10, commandtime);
                                        await Task.Delay(commandtime + delaytime + 250);
                                        Program.helper.quicktouch(10, 10, commandtime);
                                        await Task.Delay(commandtime + delaytime + 250);
                                        Program.helper.quicktouch(10, 10, commandtime);
                                        await Task.Delay(commandtime + delaytime + 250);
                                        await Program.helper.waitNTRread(addr_PageStartingIndex);
                                        if (Program.helper.lastRead == 0)
                                        {
                                            foundLastPage = true;
                                        }
                                        else
                                        {
                                            botState = (int)gtsbotstates.findfromend;
                                        }

                                    }
                                }
                            }
                            else
                            {
                                Program.f1.ChangeStatus("Looking for a pokemon to trade");
                                foundLastPage = true;
                                attempts = 0;
                                await Program.helper.waitNTRread(addr_PageSize);
                                listlength = (int)Program.helper.lastRead;
                                if (distribute == false)
                                    pokecheck = (PKM)discordbot.trademodule.pokemonfile.Peek();
                                if (distribute == true)
                                    pokecheck = PKMConverter.GetBlank(7);
                                dexnumber = 0;
                                if (searchDirection == SEARCHDIRECTION_FROMBACK || searchDirection == SEARCHDIRECTION_FROMBACKFIRSTPAGEONLY)
                                {
                                    await Program.helper.waitNTRread(addr_PageEndStartRecord);
                                }
                                else
                                {
                                    await Program.helper.waitNTRread(addr_PageStartStartRecord);
                                }
                                addr_PageEntry = Program.helper.lastRead;
                                await Program.helper.waitNTRread(addr_ListOfAllPageEntries, (uint)(256 * 100));
                                byte[] blockBytes = Program.helper.lastArray;
                                int iStartIndex, iEndIndex, iDirection, iNextPrevBlockOffest;
                                if (searchDirection == SEARCHDIRECTION_FROMBACK || searchDirection == SEARCHDIRECTION_FROMBACKFIRSTPAGEONLY)
                                {
                                    iStartIndex = listlength;
                                    iEndIndex = 0;
                                    iDirection = -1;
                                    iNextPrevBlockOffest = 0;
                                }
                                else
                                {
                                    iStartIndex = 1;
                                    iEndIndex = listlength + 1;
                                    iDirection = 1;
                                    iNextPrevBlockOffest = 4;
                                }
                                for (int i = iStartIndex; i * iDirection < iEndIndex; i += iDirection)
                                {
                                    Array.Copy(blockBytes, addr_PageEntry - addr_ListOfAllPageEntries, block, 0, 256);
                                    dexnumber = BitConverter.ToInt16(block, 0xC);
                                    szTrainerName = Encoding.Unicode.GetString(block, 0x4C, 24).Trim('\0');
                                    int gender = block[0xE];
                                    int level = block[0xF];
                                    int levels;
                                    if (level == 1)
                                    {
                                        levels = 10;
                                    }
                                    else if (level == 2)
                                    {
                                        levels = 20;
                                    }
                                    else if (level == 3)
                                    {
                                        levels = 30;
                                    }
                                    else if (level == 4)
                                    {
                                        levels = 40;
                                    }
                                    else if (level == 5)
                                    {
                                        levels = 50;
                                    }
                                    else if (level == 6)
                                    {
                                        levels = 60;
                                    }
                                    else if (level == 7)
                                    {
                                        levels = 70;
                                    }
                                    else if (level == 8)
                                    {
                                        levels = 80;
                                    }
                                    else if (level == 9)
                                    {
                                        levels = 90;
                                    }
                                    else
                                    {
                                        levels = 100;
                                    }
                                    int preprou = 0;
                                    if (gender == 0)
                                        preprou = 0;
                                    else if (gender == 1)
                                        preprou = 0;
                                    else if (gender == 2)
                                        preprou = 1;
                                    else
                                    {
                                        addr_PageEntry = BitConverter.ToUInt32(block, 0);
                                        continue;
                                    }
                                    if ((szTrainerName.ToLower() == discordbot.trademodule.trainername.Peek().ToString().ToLower() || (string)discordbot.trademodule.trainername.Peek() == "") && (dexnumber != 29 || dexnumber != 32)) {
                                        if (pokecheck.Species != dexnumber && distribute == false)
                                        {

                                            addr_PageEntry = BitConverter.ToUInt32(block, 0);
                                            continue;
                                        }

                                        else
                                        {
                                            if (distribute == true)
                                            {

                                                try
                                                {

                                                    pokecheck = discordbot.trademodule.BuildPokemon(Ledybot.Program.PKTable.Species7[dexnumber - 1], 7);
                                                    if (pokecheck.Species == 29 || pokecheck.Species == 32 || szTrainerName.ToLower() == "funkygamer26")
                                                    {
                                                        addr_PageEntry = BitConverter.ToUInt32(block, 0);
                                                        continue;
                                                    }
                                                    pokecheck.SetIsShiny(true);
                                                    if (new LegalityAnalysis(pokecheck).Report().Contains("Static Encounter shiny mismatch"))
                                                        pokecheck.SetIsShiny(false);
                                                    pokecheck.CurrentLevel = levels;
                                                    pokecheck.Gender = preprou;

                                                    if (new LegalityAnalysis(pokecheck).Report().Contains("Genderless"))
                                                        pokecheck.Gender = 2;
                                                    int[] sugmov = MoveSetApplicator.GetMoveSet(pokecheck, true);
                                                    pokecheck.SetMoves(sugmov);
                                                    Random nat = new Random();
                                                    int natue = nat.Next(24);
                                                    pokecheck.Nature = natue;
                                                    pokecheck.SetRandomIVs();
                                                    Random megastone = new Random();

                                                    pokecheck.HeldItem = megastone.Next(656, 683);
                                                    pokecheck = pokecheck.Legalize();
                                                    pokecheck.OT_Name = "Piplup.net";

                                                    if (!new LegalityAnalysis(pokecheck).Valid)
                                                    {
                                                        addr_PageEntry = BitConverter.ToUInt32(block, 0);
                                                        continue;
                                                    }
                                                }
                                                catch
                                                {
                                                    addr_PageEntry = BitConverter.ToUInt32(block, 0);
                                                    continue;
                                                }
                                            }
                                            Array.Copy(block, 0x48, principal, 0, 4);
                                            byte checksum = Program.f1.calculateChecksum(principal);
                                            byte[] fc = new byte[8];
                                            Array.Copy(principal, 0, fc, 0, 4);
                                            fc[4] = checksum;
                                            long iFC = BitConverter.ToInt64(fc, 0);
                                            szFC = iFC.ToString().PadLeft(12, '0');




                                            int level2 = pokecheck.CurrentLevel;
                                            int levelcheck;
                                            if (level2 < 11)
                                            {
                                                levelcheck = 1;
                                            }
                                            else if (level2 < 21)
                                            {
                                                levelcheck = 2;
                                            }
                                            else if (level2 < 31)
                                            {
                                                levelcheck = 3;
                                            }
                                            else if (level2 < 41)
                                            {
                                                levelcheck = 4;
                                            }
                                            else if (level2 < 51)
                                            {
                                                levelcheck = 5;
                                            }
                                            else if (level2 < 61)
                                            {
                                                levelcheck = 6;
                                            }
                                            else if (level2 < 71)
                                            {
                                                levelcheck = 7;
                                            }
                                            else if (level2 < 81)
                                            {
                                                levelcheck = 8;
                                            }
                                            else if (level2 < 91)
                                            {
                                                levelcheck = 9;
                                            }
                                            else
                                            {
                                                levelcheck = 10;
                                            }

                                            int prepro = 0;
                                            if (pokecheck.Gender == 0)
                                                prepro = 1;
                                            else if (pokecheck.Gender == 1)
                                                prepro = 2;
                                            else if (pokecheck.Gender == 2)
                                                prepro = 0;

                                            if ((gender == prepro || gender == 0) && (level == 0 || level == levelcheck))
                                            {

                                                int countryIndex = BitConverter.ToInt16(block, 0x68);
                                                string country = "-";
                                                Program.f1.countries.TryGetValue(countryIndex, out country);
                                                Program.f1.getSubRegions(countryIndex);
                                                int subRegionIndex = BitConverter.ToInt16(block, 0x6A);
                                                string subregion = "-";
                                                Program.f1.regions.TryGetValue(subRegionIndex, out subregion);
                                                int ipage = Convert.ToInt32(Math.Floor(startIndex / 100.0)) + 1;
                                                if (useLedySync && !Program.f1.banlist.Contains(szFC) && canThisTrade(principal, consoleName, szTrainerName, country, subregion, Program.PKTable.Species7[dexnumber - 1], szFC, ipage + "", (i - 1) + ""))
                                                {
                                                    Program.f1.ChangeStatus("Found a pokemon to trade");
                                                    tradeIndex = i - 1;
                                                    botState = (int)gtsbotstates.trade;
                                                    break;
                                                }
                                                else if (!useLedySync)
                                                {
                                                    if (!bReddit && !Program.f1.commented.Contains(szFC) && !Program.f1.banlist.Contains(szFC))
                                                    {
                                                        Program.f1.ChangeStatus("Found a pokemon to trade");
                                                        tradeIndex = i - 1;

                                                        botState = (int)gtsbotstates.trade;
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        startIndex = 0;
                                                        tradeIndex = -1;
                                                        listlength = 0;
                                                        addr_PageEntry = 0;
                                                        foundLastPage = false;
                                                        botresult = 8;
                                                        distribute = false;
                                                        await discordbot.trademodule.ban();
                                                        botState = (int)gtsbotstates.botstart;
                                                        break;
                                                    }

                                                }


                                            }

                                        }

                                    }


                                    addr_PageEntry = BitConverter.ToUInt32(block, iNextPrevBlockOffest);
                                }
                                if (tradeIndex == -1)
                                {
                                    if (startIndex == 0)
                                    {
                                        Program.f1.ChangeStatus("No pokemon to trade found");
                                        Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                                        await Task.Delay(commandtime + delaytime + 500);
                                        Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                                        await Task.Delay(commandtime + delaytime + 500);
                                        if (bReddit)
                                        {
                                            botState = (int)gtsbotstates.updatecomments;
                                        }
                                        else
                                        {
                                            botState = (int)gtsbotstates.pressSeek;

                                            stupid++;
                                        }
                                    }
                                    else
                                    {
                                        Program.f1.ChangeStatus("No pokemon to trade on this page, try previous page");
                                        startIndex -= 100;
                                        Program.helper.quickbuton(Program.PKTable.DpadLEFT, commandtime);
                                        await Task.Delay(commandtime + delaytime);
                                        await Task.Delay(2250);
                                        botState = (int)gtsbotstates.findfromend;
                                    }
                                }

                            }

                            break;
                        case (int)gtsbotstates.findfromend:
                            correctScreen = await isCorrectWindow(val_GTSListScreen);
                            {
                                //Hotfix for Only one Pokemon on List
                                if (Program.helper.lastRead == 0x40F5)
                                {
                                    // No Entries found.
                                    botState = (int)gtsbotstates.panic;
                                    break;
                                }
                                else if (Program.helper.lastRead == 0x40C0)
                                {
                                    // Only one Pokemon on List, ignore.
                                }
                                else
                                {
                                    botState = (int)gtsbotstates.panic;
                                    break;
                                }
                            }
                            //also GTS entry list screen, but cursor is at the end of the list in this case
                            await Program.helper.waitNTRread(addr_PageSize);

                            attempts = 0;
                            listlength = (int)Program.helper.lastRead;
                            if (listlength == 100 && !foundLastPage)
                            {
                                Program.f1.ChangeStatus("Moving to last page");
                                startIndex += 100;
                                Program.helper.quickbuton(Program.PKTable.DpadRIGHT, commandtime);
                                await Task.Delay(commandtime + delaytime);
                                await Task.Delay(3000);
                                Program.helper.quicktouch(10, 10, commandtime);
                                await Task.Delay(commandtime + delaytime + 250);
                                Program.helper.quicktouch(10, 10, commandtime);
                                await Task.Delay(commandtime + delaytime + 250);
                                Program.helper.quicktouch(10, 10, commandtime);
                                await Task.Delay(commandtime + delaytime + 250);
                                Program.helper.quicktouch(10, 10, commandtime);
                                await Task.Delay(commandtime + delaytime + 250);
                                await Program.helper.waitNTRread(addr_PageStartingIndex);
                                if (Program.helper.lastRead == 0)
                                {
                                    foundLastPage = true;
                                }
                                botState = (int)gtsbotstates.findfromstart;
                            }
                            else
                            {
                                foundLastPage = true;
                                attempts = 0;
                                listlength = (int)Program.helper.lastRead;
                                dexnumber = 0;
                                await Program.helper.waitNTRread(addr_PageEndStartRecord);
                                addr_PageEntry = Program.helper.lastRead;
                                await Program.helper.waitNTRread(addr_ListOfAllPageEntries, (uint)(256 * 100));
                                byte[] blockBytes = Program.helper.lastArray;
                                for (int i = listlength; i > 0; i--)
                                {
                                    Program.f1.ChangeStatus("Looking for a pokemon to trade");
                                    Array.Copy(blockBytes, addr_PageEntry - addr_ListOfAllPageEntries, block, 0, 256);
                                    dexnumber = BitConverter.ToInt16(block, 0xC);
                                    if (pokecheck.Species != dexnumber && discordbot.trademodule.distribute == "false")
                                    {

                                        addr_PageEntry = BitConverter.ToUInt32(block, 0);
                                        continue;
                                    }

                                    else
                                    {



                                        Array.Copy(block, 0x48, principal, 0, 4);
                                        byte checksum = Program.f1.calculateChecksum(principal);
                                        byte[] fc = new byte[8];
                                        Array.Copy(principal, 0, fc, 0, 4);
                                        fc[4] = checksum;
                                        long iFC = BitConverter.ToInt64(fc, 0);
                                        szFC = iFC.ToString().PadLeft(12, '0');
                                        int gender = block[0xE];
                                        int level = block[0xF];
                                        int level2 = pokecheck.CurrentLevel;
                                        int levelcheck;
                                        if (level2 < 11)
                                        {
                                            levelcheck = 1;
                                        }
                                        else if (level2 < 21)
                                        {
                                            levelcheck = 2;
                                        }
                                        else if (level2 < 31)
                                        {
                                            levelcheck = 3;
                                        }
                                        else if (level2 < 41)
                                        {
                                            levelcheck = 4;
                                        }
                                        else if (level2 < 51)
                                        {
                                            levelcheck = 5;
                                        }
                                        else if (level2 < 61)
                                        {
                                            levelcheck = 6;
                                        }
                                        else if (level2 < 71)
                                        {
                                            levelcheck = 7;
                                        }
                                        else if (level2 < 81)
                                        {
                                            levelcheck = 8;
                                        }
                                        else if (level2 < 91)
                                        {
                                            levelcheck = 9;
                                        }
                                        else
                                        {
                                            levelcheck = 10;
                                        }
                                        int prepro = 0;
                                        if (pokecheck.Gender == 0)
                                            prepro = 1;
                                        if (pokecheck.Gender == 1)
                                            prepro = 2;
                                        if (pokecheck.Gender == 2)
                                            prepro = 1;
                                        if ((prepro == gender || gender == 0) && (level == 0 || level == levelcheck))
                                        {
                                            string szTrainerName = Encoding.Unicode.GetString(block, 0x4C, 24).Trim('\0');
                                            int countryIndex = BitConverter.ToInt16(block, 0x68);
                                            string country = "-";
                                            Program.f1.countries.TryGetValue(countryIndex, out country);
                                            Program.f1.getSubRegions(countryIndex);
                                            int subRegionIndex = BitConverter.ToInt16(block, 0x6A);
                                            string subregion = "-";
                                            Program.f1.regions.TryGetValue(subRegionIndex, out subregion);
                                            int ipage = Convert.ToInt32(Math.Floor(startIndex / 100.0)) + 1;
                                            if (useLedySync && !Program.f1.banlist.Contains(szFC) && canThisTrade(principal, consoleName, szTrainerName, country, subregion, Program.PKTable.Species7[dexnumber - 1], szFC, ipage + "", (i - 1) + ""))
                                            {
                                                Program.f1.ChangeStatus("Found a pokemon to trade");
                                                tradeIndex = i - 1;
                                                botState = (int)gtsbotstates.trade;
                                                break;
                                            }
                                            else if (!useLedySync)
                                            {
                                                if ((!bReddit || Program.f1.commented.Contains(szFC)) && !Program.f1.banlist.Contains(szFC))
                                                {
                                                    Program.f1.ChangeStatus("Found a pokemon to trade");
                                                    tradeIndex = i - 1;
                                                    botState = (int)gtsbotstates.trade;
                                                    break;
                                                }
                                                else
                                                {
                                                    startIndex = 0;
                                                    tradeIndex = -1;
                                                    listlength = 0;
                                                    addr_PageEntry = 0;
                                                    foundLastPage = false;
                                                    botresult = 8;
                                                    botState = (int)gtsbotstates.botexit;
                                                    Ledybot.MainForm.btn_Stop_Click(null, EventArgs.Empty);
                                                    await discordbot.trademodule.ban();
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    addr_PageEntry = BitConverter.ToUInt32(block, 0);

                                }
                                if (tradeIndex == -1)
                                {
                                    if (listlength < 100 && startIndex >= 200)
                                    {
                                        Program.f1.ChangeStatus("No pokemon to trade on this page, try previous page");
                                        for (int i = 0; i < listlength; i++)
                                        {
                                            Program.helper.quickbuton(Program.PKTable.DpadLEFT, commandtime);
                                            await Task.Delay(commandtime + delaytime);
                                        }
                                        startIndex -= 100;
                                        await Task.Delay(2250);
                                        botState = (int)gtsbotstates.findfromend; //hope this is right
                                    }
                                    else if (startIndex >= 200)
                                    {
                                        Program.f1.ChangeStatus("No pokemon to trade on this page, try previous page");
                                        waitTaskbool = Program.helper.waitNTRwrite(addr_PageStartingIndex, (uint)(startIndex - 200), iPID);
                                        if (await waitTaskbool)
                                        {
                                            await Program.helper.waitNTRwrite(addr_PageSize, 0x64, iPID);
                                            startIndex -= 100;
                                            Program.helper.quickbuton(Program.PKTable.DpadLEFT, commandtime);
                                            await Task.Delay(commandtime + delaytime);
                                            Program.helper.quickbuton(Program.PKTable.DpadRIGHT, commandtime);
                                            await Task.Delay(commandtime + delaytime);
                                            Program.helper.quickbuton(Program.PKTable.DpadRIGHT, commandtime);
                                            await Task.Delay(commandtime + delaytime);
                                            await Task.Delay(3000);
                                            Program.helper.quicktouch(10, 10, commandtime);
                                            await Task.Delay(commandtime + delaytime + 250);
                                            Program.helper.quicktouch(10, 10, commandtime);
                                            await Task.Delay(commandtime + delaytime + 250);
                                            Program.helper.quicktouch(10, 10, commandtime);
                                            await Task.Delay(commandtime + delaytime + 250);
                                            Program.helper.quicktouch(10, 10, commandtime);
                                            await Task.Delay(commandtime + delaytime + 250);
                                            botState = (int)gtsbotstates.findfromstart;
                                        }
                                    }
                                    else if (startIndex == 0)
                                    {
                                        Program.f1.ChangeStatus("No pokemon to trade found");
                                        Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                                        await Task.Delay(commandtime + delaytime + 500);
                                        Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                                        await Task.Delay(commandtime + delaytime + 500);
                                        Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                                        await Task.Delay(commandtime + delaytime + 500);
                                        if (bReddit)
                                        {
                                            botState = (int)gtsbotstates.updatecomments;
                                        }
                                        else
                                        {
                                            botState = (int)gtsbotstates.pressSeek;
                                        }
                                    }
                                    else if (startIndex < 200)
                                    {
                                        Program.f1.ChangeStatus("No pokemon to trade found");
                                        botState = (int)gtsbotstates.pressSeek;
                                    }
                                }
                            }

                            break;
                        case (int)gtsbotstates.trade:
                            //still in GTS list screen
                            //write index we want to trade
                            int page = Convert.ToInt32(Math.Floor(startIndex / 100.0)) + 1;
                            Program.f1.ChangeStatus("Trading pokemon on page " + page + " index " + tradeIndex + "");

                            waitTaskbool = Program.helper.waitNTRwrite(addr_PageCurrentView, BitConverter.GetBytes(tradeIndex), iPID);
                            if (await waitTaskbool)
                            {




                                mega = pokecheck.HeldItem;
                                pokecheck.HeldItem = 1;
                                byte[] pkmEncrypted = pokecheck.DecryptedBoxData;
                                byte[] cloneshort = PKHeX.encryptArray(pkmEncrypted.Take(232).ToArray());
                                string ek7 = BitConverter.ToString(cloneshort).Replace("-", ", 0x");
                                pokecheck.HeldItem = mega;
                                bool shiny = false;
                                if (pokecheck.IsShiny == true)
                                    shiny = true;
                                if (pokecheck.Nickname.ToLower() == "egg")
                                {
                                    pokecheck.IsNicknamed = true;
                                    switch (pokecheck.Language)
                                    {
                                        case 1: pokecheck.Nickname = "タマゴ"; break;
                                        case 3: pokecheck.Nickname = "Œuf"; break;
                                        case 4: pokecheck.Nickname = "Uovo"; break;
                                        case 5: pokecheck.Nickname = "Ei"; break;
                                        case 7: pokecheck.Nickname = "Huevo"; break;
                                        case 8: pokecheck.Nickname = "알"; break;
                                        case 9: pokecheck.Nickname = "蛋"; break;
                                        case 10: pokecheck.Nickname = "蛋"; break;
                                        default: pokecheck.Nickname = "Egg"; break;


                                    }


                                    pokecheck.IsEgg = true;
                                    pokecheck.Egg_Location = 60002;
                                    pokecheck.MetDate = DateTime.Parse("2020/04/20");
                                    pokecheck.EggMetDate = pokecheck.MetDate;
                                    pokecheck.HeldItem = 0;
                                    pokecheck.CurrentLevel = 1;
                                    pokecheck.EXP = 0;

                                    pokecheck.Met_Level = 1;
                                    pokecheck.Met_Location = 30002;
                                    pokecheck.CurrentHandler = 0;
                                    pokecheck.OT_Friendship = 1;
                                    pokecheck.HT_Name = "";
                                    pokecheck.HT_Friendship = 0;

                                    pokecheck.HT_Gender = 0;



                                    pokecheck.StatNature = pokecheck.Nature;
                                    pokecheck.EVs = new int[] { 0, 0, 0, 0, 0, 0 };
                                    pokecheck.Markings = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };
                                    
                                    var la = new LegalityAnalysis(pokecheck);
                                    if (la.Report().ToLower().Contains("illegal move")) {
                                        pokecheck.ClearRelearnMoves();
                                        pokecheck.Moves = new int[] { 0, 0, 0, 0 };
                                        pokecheck.RelearnMoves = MoveBreed.GetExpectedMoves(pokecheck.RelearnMoves, la.EncounterMatch);
                                        pokecheck.Moves = pokecheck.RelearnMoves;
                                    }
                                    pokecheck.Move1_PPUps = pokecheck.Move2_PPUps = pokecheck.Move3_PPUps = pokecheck.Move4_PPUps = 0;
                                    pokecheck.SetMaximumPPCurrent(pokecheck.Moves);
                                    pokecheck.SetSuggestedHyperTrainingData();
                                    pokecheck.SetSuggestedRibbons(la.EncounterMatch);
                                    if (shiny == true)
                                        pokecheck.SetIsShiny(true);

                                }
                                byte[] megaencrypted = pokecheck.DecryptedBoxData;
                                byte[] megashort = PKHeX.encryptArray(megaencrypted.Take(232).ToArray());
                                //optional: grab some trainer data
                                string szTrainerName = Encoding.Unicode.GetString(block, 0x4C, 24).Trim('\0');
                                int countryIndex = BitConverter.ToInt16(block, 0x68);
                                string country = "-";
                                Program.f1.countries.TryGetValue(countryIndex, out country);
                                Program.f1.getSubRegions(countryIndex);
                                int subRegionIndex = BitConverter.ToInt16(block, 0x6A);
                                string subregion = "-";
                                Program.f1.regions.TryGetValue(subRegionIndex, out subregion);

                                Program.f1.AppendListViewItem(szTrainerName, pokecheck.Nickname, country, subregion, Program.PKTable.Species7[dexnumber - 1], szFC, page + "", tradeIndex + "");


                                try
                                {
                                    await logchan.SendMessageAsync($"Deposited Pokemon: {Ledybot.Program.PKTable.Species7[iPokemonToFind - 1]}\n Discord: {(distribute ? "ad trade" : discordbot.trademodule.discordname.Peek())}\n Trainer: {szTrainerName}\n Nickname: {pokecheck.Nickname}\n Country: {country}\n Subregion: {subregion}\n Pokemon: {Program.PKTable.Species7[dexnumber - 1]}\n FC: {szFC}\n Page: {page}\n Index: {tradeIndex}");
                                }
                                catch
                                {
                                    Program.f1.ChangeStatus("Log Channel Broken...idk");
                                }
                                //Inject the Pokemon to box1slot1
                                Program.scriptHelper.write(addr_box1slot1, cloneshort, iPID);
                                //spam a to trade pokemon
                                Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                                await Task.Delay(commandtime + delaytime + 2500 + o3dswaittime);
                                await Task.Delay(1000 + o3dswaittime);
                                Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                                await Task.Delay(commandtime + delaytime);
                                await Task.Delay(1000 + o3dswaittime);
                                Program.helper.quickbuton(Program.PKTable.keyA, commandtime);

                                await Task.Delay(commandtime + delaytime);
                                await Task.Delay(1000 + o3dswaittime);
                                Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                                Program.scriptHelper.write(addr_box1slot1, megashort, iPID);

                                await Task.Delay(commandtime + delaytime);
                                await Task.Delay(5000);
                               
                                if (await isCorrectWindow(val_duringTrade) || await isCorrectWindow(val_system))
                                {
                                    while ((await isCorrectWindow(val_duringTrade) || await isCorrectWindow(val_system)) && timeout.ElapsedMilliseconds < 1200_000)
                                    {
                                        Program.f1.ChangeStatus("handling trade evolution");
                                        Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                                        Program.helper.quicktouch(180, 180, commandtime);
                                        await Task.Delay(1000);
                                        continue;
                                    }
                                }
                                if (await isCorrectWindow(val_BoxScreen) || await isCorrectWindow(val_SearchScreen))
                                {
                                    Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                                    await Task.Delay(commandtime + delaytime);
                                    await Task.Delay(1000);

                                }
                                if (await isCorrectWindow(val_duringTrade) || await isCorrectWindow(val_system))
                                {
                                    while ((await isCorrectWindow(val_duringTrade) || await isCorrectWindow(val_system)) && timeout.ElapsedMilliseconds < 1200_000)
                                    {
                                        Program.f1.ChangeStatus("handling trade evolution");
                                        Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                                        Program.helper.quicktouch(180, 180, commandtime);

                                        continue;
                                    }
                                }
                                if (timeout.ElapsedMilliseconds >= 1200_000)
                                {
                                    if (wtchan.Name.ToString().Contains("✅"))
                                    {
                                        await wtchan.ModifyAsync(prop => prop.Name = wtchan.Name.Replace("✅", "❌"));
                                        var offembed = new EmbedBuilder();
                                        offembed.AddField($"{discordbot._client.CurrentUser.Username} Bot Announcement", "Wonder Trade Bot is Offline");
                                        await wtchan.SendMessageAsync(embed: offembed.Build());

                                    }
                                    var bcidses = Ledybot.Program.f1.BotChannels.Text.Split(',');
                                    foreach (string ids in bcidses)
                                    {
                                        ulong.TryParse(ids, out var bcid);
                                        var botchan = (ITextChannel)discordbot._client.GetChannelAsync(bcid).Result;
                                        if (botchan.Name.Contains("✅"))
                                        {
                                             var role = botchan.Guild.EveryoneRole;
                        await botchan.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny));
                                            await botchan.ModifyAsync(prop => prop.Name = botchan.Name.ToString().Replace("✅", "❌"));
                                            var offembed = new EmbedBuilder();
                                            offembed.AddField($"{discordbot._client.CurrentUser.Username} Bot Announcement", "GTS Trade Bot is Offline");
                                            await botchan.SendMessageAsync(embed: offembed.Build());
                                        }

                                    }
                                    var cordchan4 = (ITextChannel)discordbot._client.GetChannelAsync(873613944273641503).Result;
                                    await cordchan4.ModifyAsync(x => x.Name = cordchan4.Name.Replace("✅", "❌"));
                                    timeout.Reset();
                                    return 8;
                                }
                                   
                                //during the trade spam a/b to get back to the start screen in case of "this pokemon has been traded"
                                while (!await isCorrectWindow(val_Quit_SeekScreen))
                                {
                                    Program.f1.ChangeStatus("handling trade evolution");
                                    Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                                    await Task.Delay(500);
                             
                                }
                                if (distribute == false)
                                {
                                    await Program.helper.waitNTRread(addr_box1slot1, 260);

                                    byte[] pokebytes = Program.helper.lastArray;
                                    PKM tradedpoke = PKMConverter.GetPKMfromBytes(pokebytes, 7);
                                    PKM checker = (PKM)discordbot.trademodule.pokemonfile.Peek();
                                    if (tradedpoke.PID == checker.PID)
                                    {
                                        startIndex = 0;
                                        tradeIndex = -1;
                                        listlength = 0;
                                        addr_PageEntry = 0;
                                        foundLastPage = false;
                                        botresult = 8;
                                        distribute = false;
                                        try { await discordbot.trademodule.notrade(); } catch { await TwitchBot.notrade(); }
                                        botState = (int)gtsbotstates.botstart;
                                        break;
                                    }
                                    tradedpoke.ClearNickname();
                                    tradedpoke.IsNicknamed = false;
                                    byte[] writepoke = tradedpoke.DecryptedBoxData;
                                    tpfile = Path.GetTempFileName().Replace(".tmp", "." + tradedpoke.Extension);
                                    tpfile = tpfile.Replace("tmp", tradedpoke.FileNameWithoutExtension);
                                    System.IO.File.WriteAllBytes(tpfile, writepoke);
                                    if (tradedpoke.OT_Name == (string)discordbot.trademodule.trainername.Peek())
                                    {
                                        if (!File.Exists($"{Directory.GetCurrentDirectory()}//trainerinfo//{discordbot.trademodule.username.Peek()}.txt"))
                                        {

                                            File.WriteAllText($"{Directory.GetCurrentDirectory()}//trainerinfo//{discordbot.trademodule.username.Peek()}.txt", $"OT: {tradedpoke.OT_Name}\nTID: {tradedpoke.TrainerID7}\nSID: {tradedpoke.TrainerSID7}");
                                        }
                                    }
                                    discordbot.trademodule.retpoke.Enqueue(tpfile);
                                    discordbot.trademodule.username.Dequeue();
                                    discordbot.trademodule.pokequeue.Dequeue();
                                    discordbot.trademodule.pokemonfile.Dequeue();


                                }

                                if (discordbot.trademodule.retpoke.Count != 0)
                                {
                                    try
                                    {
                                        IMessageChannel t = (IMessageChannel)discordbot.trademodule.channel.Peek();
                                        await t.SendFileAsync((string)discordbot.trademodule.retpoke.Peek(), discordbot.trademodule.discordname.Peek() + " here is the pokemon you traded me ");
                                       
                                    }catch { TwitchBot.client.SendMessage(TwitchBot.Channel, $"{discordbot.trademodule.discordname.Dequeue()} Your trade has been completed"); };
                                    discordbot.trademodule.channel.Dequeue();
                                    discordbot.trademodule.retpoke.Dequeue();
                                    discordbot.trademodule.discordname.Dequeue();
                                    if (Ledybot.MainForm.game == 0 || Ledybot.MainForm.game == 1)
                                        File.Delete(Ledybot.GTSBot7.tpfile);
                                    else
                                        File.Delete(Ledybot.GTSBot6.tpfile);

                                }




                                bool cont = false;



                                if (discordbot.trademodule.pokequeue.Count == 0)
                                {
                                    startIndex = 0;
                                    tradeIndex = -1;
                                    listlength = 0;
                                    addr_PageEntry = 0;
                                    foundLastPage = false;
                                    botresult = 8;
                                    distribute = false;


                                    discordbot.trademodule.trainername.Dequeue();

                                    botState = (int)gtsbotstates.botstart;


                                    break;
                                }

                                startIndex = 0;
                                tradeIndex = -1;
                                listlength = 0;
                                addr_PageEntry = 0;
                                foundLastPage = false;
                                distribute = false;


                                discordbot.trademodule.trainername.Dequeue();

                                botState = (int)gtsbotstates.botstart;



                            }
                            break;
                        case (int)gtsbotstates.quicksearch:
                            //end of list reach, press b and "search" again to reach GTS list again
                            Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                            await Task.Delay(commandtime + delaytime + 500);
                            await Program.helper.waittouch(160, 185);
                            await Task.Delay(2250);
                            botState = (int)gtsbotstates.findfromstart;
                            break;
                        case (int)gtsbotstates.research:
                            //press a and "search" again to reach GTS list again
                            Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                            await Task.Delay(commandtime + delaytime + 1000);
                            await Program.helper.waittouch(160, 185);
                            await Task.Delay(2250);
                            botState = (int)gtsbotstates.findfromstart;
                            break;
                        case (int)gtsbotstates.botexit:
                            Program.f1.ChangeStatus("Stopped");
                            File.Delete(discordbot.trademodule.temppokecurrent);
                            botstop = true;
                            wondertrade = false;
                            break;
                        case (int)gtsbotstates.wondertrade:
                            timeout.Restart();
                            if (wtchan.Name.Contains("❌"))
                            {
                                await wtchan.ModifyAsync(prop => prop.Name = wtchan.Name.Replace("❌", "✅"));
                                var offembed = new EmbedBuilder();
                                offembed.AddField($"{discordbot._client.CurrentUser.Username} bot Announcement", "Wondertrade Bot is Online");
                                await wtchan.SendMessageAsync(embed: offembed.Build());
                            }
                      
                            Program.f1.ChangeStatus("wonder trading");
                            if (!await isCorrectWindow(val_Quit_SeekScreen))
                            {
                                botState = (int)gtsbotstates.botexit;
                            }
                            var wtfiles = Directory.GetFiles(Program.f1.wtfolder.Text);
                            Random wtrand = new Random();
                            //  var piptwitch = new TwitchBot();
                            pokecheck = discordbot.trademodule.BuildPokemon("Piplup.net (Piplup)", 7);
                            pokecheck.OT_Name = "Piplup.net";
                            byte[] wonderfodder = pokecheck.DecryptedBoxData;
                            byte[] wondershort = PKHeX.encryptArray(wonderfodder.Take(232).ToArray());
                            var wtfile = wtfiles[wtrand.Next(wtfiles.Length)];
                            pokecheck = PKMConverter.GetPKMfromBytes(File.ReadAllBytes(wtfile));
                            if (TwitchBot.wtqueue.Count != 0)
                            {
                                pokecheck = (PKM)TwitchBot.wtqueue.Peek();
                                TwitchBot.wtqueue.Dequeue();
                                TwitchBot.wtuser.Dequeue();
                            }
                            byte[] wtreal = pokecheck.DecryptedBoxData;
                            byte[] wtrealshort = PKHeX.encryptArray(wtreal.Take(232).ToArray());
                            Program.scriptHelper.write(addr_box1slot1, wondershort, iPID);
                            Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                            await Task.Delay(10000);
                            Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                            await Task.Delay(2000);
                            Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                            await Task.Delay(2000);
                            Program.scriptHelper.write(addr_box1slot1, wtrealshort, iPID);
                            await Task.Delay(500);
                            try
                            {
                                EmbedBuilder embed = new EmbedBuilder();
                                embed.ThumbnailUrl = pokecheck.IsShiny ? $"https://play.pokemonshowdown.com/sprites/ani-shiny/{Program.PKTable.Species7[pokecheck.Species - 1].ToLower().Replace(" ", "")}.gif" : $"https://play.pokemonshowdown.com/sprites/ani/{Program.PKTable.Species7[pokecheck.Species - 1].ToLower().Replace(" ", "")}.gif";
                                var newShowdown = new List<string>();
                                var showdown = ShowdownParsing.GetShowdownText(pokecheck);
                                foreach (var line in showdown.Split('\n'))
                                    newShowdown.Add(line);

                                if (pokecheck.IsEgg)
                                    newShowdown.Add("\nPokémon is an egg");
                                if (pokecheck.Ball > (int)Ball.None)
                                    newShowdown.Insert(newShowdown.FindIndex(z => z.Contains("Nature")), $"Ball: {(Ball)pokecheck.Ball} Ball");
                                if (pokecheck.IsShiny)
                                {
                                    var index = newShowdown.FindIndex(x => x.Contains("Shiny: Yes"));
                                    if (pokecheck.ShinyXor == 0 || pokecheck.FatefulEncounter)
                                        newShowdown[index] = "Shiny: Square\r";
                                    else newShowdown[index] = "Shiny: Star\r";
                                }
                                
                                newShowdown.InsertRange(1, new string[] { $"OT: {pokecheck.OT_Name}", $"TID: {pokecheck.TrainerID7}", $"SID: {pokecheck.TrainerSID7}", $"OTGender: {(Gender)pokecheck.OT_Gender}", $"Language: {(LanguageID)pokecheck.Language}" });
                                embed.AddField("Wonder trading in 15 seconds", Format.Code(string.Join("\n", newShowdown).TrimEnd()));
                                if (!File.Exists($"{Directory.GetCurrentDirectory()}//wondertrade.txt"))
                                    File.Create($"{Directory.GetCurrentDirectory()}//wondertrade.txt");
                                File.WriteAllText($"{Directory.GetCurrentDirectory()}//wondertrade.txt", $"Gen 7 Wonder trading:{Program.PKTable.Species7[pokecheck.Species - 1]}");
                                var tempsprite = SpriteUtil.GetSprite(pokecheck.Species, pokecheck.Form, pokecheck.Gender, FormArgumentUtil.GetFormArgumentMax(pokecheck.Species, pokecheck.Form, pokecheck.Generation), 0, false, pokecheck.IsShiny, pokecheck.Generation, false, pokecheck.IsShiny);
                                tempsprite.Save($"{Directory.GetCurrentDirectory()}//wondertradesprite.png");
                                await wtchan.SendMessageAsync(embed: embed.Build());
                                TwitchBot.client.SendMessage(TwitchBot.Channel, $"wonder trading {(pokecheck.IsShiny ? "Shiny" : "")} {(Species)pokecheck.Species}{(pokecheck.Form == 0 ? "" : "-" + ShowdownParsing.GetStringFromForm(pokecheck.Form, GameInfo.Strings, pokecheck.Species, pokecheck.Format))} in 15 seconds");
                                //  piptwitch.StartingDistribution(pokecheck);
                            }
                            catch { await Task.Delay(1); }
                            await Task.Delay(12000);
                            try
                            {
                                await wtchan.SendMessageAsync("3");
                                TwitchBot.client.SendMessage(TwitchBot.Channel, "3");
                            }
                            catch { await Task.Delay(1); }
                            await Task.Delay(1000);
                            try
                            {
                                await wtchan.SendMessageAsync("2");
                                TwitchBot.client.SendMessage(TwitchBot.Channel, "2");
                            }
                            catch { await Task.Delay(1); }
                            await Task.Delay(1000);
                            try
                            {
                                await wtchan.SendMessageAsync("1");
                                TwitchBot.client.SendMessage(TwitchBot.Channel, "1");
                            }
                            catch { await Task.Delay(1); }
                            await Task.Delay(1000);
                            try
                            {
                                await wtchan.SendMessageAsync("wonder trade now!");
                                TwitchBot.client.SendMessage(TwitchBot.Channel, "wonder trade now!");
                            }
                            catch { await Task.Delay(1); }
                            Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                            await Task.Delay(2000);

                            if (!await isCorrectWindow(val_wondertradesearch))
                            {

                                while (!await isCorrectWindow(val_Quit_SeekScreen))
                                {
                                    Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                                    await Task.Delay(1000);
                                    if (timeout.ElapsedMilliseconds > 600_000)
                                    {
                                        await wtchan.ModifyAsync(prop => prop.Name = wtchan.Name.Replace("✅", "❌"));
                                        var offembed = new EmbedBuilder();
                                        offembed.AddField($"{discordbot._client.CurrentUser.Username} Bot Announcement", "Wonder Trade Bot is Offline");
                                        await wtchan.SendMessageAsync(embed: offembed.Build());

                                        ulong.TryParse(Program.f1.BotChannels.Text, out var bcid);
                                        var botchan = (ITextChannel)discordbot._client.GetChannelAsync(bcid).Result;
                                        var role = botchan.Guild.EveryoneRole;
                                        await botchan.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny));
                                        await botchan.ModifyAsync(prop => prop.Name = botchan.Name.Replace("✅", "❌"));
                                        var offembed2 = new EmbedBuilder();
                                        offembed2.AddField($"{discordbot._client.CurrentUser.Username} Bot Announcement", "GTS Trade Bot is Offline");
                                        await botchan.SendMessageAsync(embed: offembed2.Build());
                                        var cordchan3 = (ITextChannel)discordbot._client.GetChannelAsync(873613944273641503).Result;
                                        await cordchan3.ModifyAsync(x => x.Name = cordchan3.Name.Replace("✅", "❌"));
                                        break;
                                    }

                                }
                            }
                            while (!await isCorrectWindow(val_Quit_SeekScreen) && timeout.ElapsedMilliseconds < 600_000)
                            {
                                if(timeout.ElapsedMilliseconds> 600_000)
                                {
                                    await wtchan.ModifyAsync(prop => prop.Name = wtchan.Name.Replace("✅", "❌"));
                                    var offembed = new EmbedBuilder();
                                    offembed.AddField($"{discordbot._client.CurrentUser.Username} Bot Announcement", "Wonder Trade Bot is Offline");
                                    await wtchan.SendMessageAsync(embed: offembed.Build());
                                    
                                    ulong.TryParse(Program.f1.BotChannels.Text, out var bcid);
                                    var botchan = (ITextChannel)discordbot._client.GetChannelAsync(bcid).Result;
                                    var role = botchan.Guild.EveryoneRole;
                                    await botchan.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny));
                                    await botchan.ModifyAsync(prop => prop.Name = botchan.Name.Replace("✅", "❌"));
                                    var offembed2 = new EmbedBuilder();
                                    offembed2.AddField($"{discordbot._client.CurrentUser.Username} Bot Announcement", "GTS Trade Bot is Offline");
                                    await botchan.SendMessageAsync(embed: offembed2.Build());
                                    var cordchan2 = (ITextChannel)discordbot._client.GetChannelAsync(873613944273641503).Result;
                                    await cordchan2.ModifyAsync(x => x.Name = cordchan2.Name.Replace("✅", "❌"));
                                    break;
                                }
                                await Task.Delay(25);
                                
                             

                            }
                            if (timeout.ElapsedMilliseconds > 600_000)
                            {
                                botState =(int) gtsbotstates.botexit;
                                break;
                            }
                                
                            try
                            {
                                await wtchan.SendMessageAsync("starting the next wonder trade in 42 seconds");
                                TwitchBot.client.SendMessage(TwitchBot.Channel, "starting the next wonder trade in 42 seconds");
                            }
                            catch { await Task.Delay(1); }
                            await Task.Delay(42000);
                            if (MainForm.wondertrade.Enabled == true && discordbot.trademodule.pokequeue.Count != 0)
                            {
                                try { await wtchan.SendMessageAsync("switching to GTS mode, wonder trades will return soon");
                                    TwitchBot.client.SendMessage(TwitchBot.Channel, "switching to GTS mode, wonder trades will return soon");  }
                                catch { await Task.Delay(1); }

                                Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                                await Task.Delay(5000);
                                Program.helper.quicktouch(180, 70, commandtime);
                                await Task.Delay(5000);
                                Program.helper.quicktouch(180, 115, commandtime);
                                await Task.Delay(10000);
                                distribute = false;
                                botState = (int)gtsbotstates.botstart;
                                break;
                            }
                            botState = (int)gtsbotstates.wondertrade;
                            break;


                        case (int)gtsbotstates.panic:
                            Program.f1.ChangeStatus("Recovery mode!");
                            //recover from weird state here
                            await Program.helper.waitNTRread(addr_currentScreen);
                            int screenID = (int)Program.helper.lastRead;

                            if (screenID == val_PlazaScreen)
                            {
                                await Program.helper.waittouch(200, 120);
                                await Task.Delay(1000);
                                await Program.helper.waittouch(200, 120);
                                await Task.Delay(8000);
                                correctScreen = await isCorrectWindow(val_Quit_SeekScreen);
                                if (correctScreen)
                                {
                                    botState = (int)gtsbotstates.startsearch;
                                    break;
                                }
                                else
                                {
                                    botState = (int)gtsbotstates.botexit;
                                    break;
                                }
                            }
                            else if (screenID == val_Quit_SeekScreen)
                            {
                                //press b, press where seek button would be, press b again -> guaranteed seek screen
                                Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                                await Task.Delay(commandtime + delaytime + 500);
                                await Program.helper.waittouch(160, 80);
                                await Task.Delay(2250);
                                Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                                await Task.Delay(commandtime + delaytime + 500);
                                botState = (int)gtsbotstates.startsearch;
                                break;
                            }
                            else if (screenID == val_WhatPkmnScreen)
                            {
                                //can only exit this one by pressing the ok button
                                waitTaskbool = Program.helper.waitbutton(Program.PKTable.keySTART);
                                if (await waitTaskbool)
                                {
                                    waitTaskbool = Program.helper.waitbutton(Program.PKTable.keyA);
                                    if (await waitTaskbool)
                                    {
                                        botState = (int)gtsbotstates.panic;
                                        break;
                                    }
                                }
                            }
                            else // if(screenID == val_SearchScreen || screenID == val_BoxScreen || screenID == val_GTSListScreen)
                            {
                                //spam b a lot and hope we get to val_quit_seekscreen like this
                                for (int i = 0; i < 5; i++)
                                {
                                    Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                                    await Task.Delay(commandtime + delaytime + 500);
                                    await Task.Delay(1000);
                                }
                                correctScreen = await isCorrectWindow(val_Quit_SeekScreen);
                                if (correctScreen)
                                {
                                    botState = (int)gtsbotstates.panic;
                                    break;
                                }
                                else
                                {
                                    Program.helper.quickbuton(Program.PKTable.keyA, commandtime);
                                    await Task.Delay(commandtime + delaytime + 500);
                                    for (int i = 0; i < 5; i++)
                                    {
                                        Program.helper.quickbuton(Program.PKTable.keyB, commandtime);
                                        await Task.Delay(commandtime + delaytime + 500);
                                        await Task.Delay(1000);
                                    }
                                    correctScreen = await isCorrectWindow(val_Quit_SeekScreen);
                                    if (correctScreen)
                                    {
                                        botState = (int)gtsbotstates.panic;
                                        break;
                                    }
                                    else
                                    {
                                        if (panicAttempts == 0)
                                        {
                                            panicAttempts++;
                                            botState = (int)gtsbotstates.panic;
                                            break;
                                        }
                                        botState = (int)gtsbotstates.botexit;
                                        break;
                                    }
                                }
                            }
                            break;
                        default:
                            botresult = -1;
                            botstop = true;
                            break;

                    }
                }
                
                if (wtchan.Name.ToString().Contains("✅"))
                {
                    await wtchan.ModifyAsync(prop => prop.Name = wtchan.Name.Replace("✅", "❌"));
                    var offembed = new EmbedBuilder();
                    offembed.AddField($"{discordbot._client.CurrentUser.Username} Bot Announcement", "Wonder Trade Bot is Offline");
                    await wtchan.SendMessageAsync(embed: offembed.Build());

                }
                var bcids = Ledybot.Program.f1.BotChannels.Text.Split(',');
                foreach (string ids in bcids)
                {
                    ulong.TryParse(ids, out var bcid);
                    var botchan = (ITextChannel)discordbot._client.GetChannelAsync(bcid).Result;
                    if (botchan.Name.Contains("✅"))
                    {
                        var role = botchan.Guild.EveryoneRole;
                        await botchan.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny));
                        await botchan.ModifyAsync(prop => prop.Name = botchan.Name.ToString().Replace("✅", "❌"));
                        var offembed = new EmbedBuilder();
                        offembed.AddField($"{discordbot._client.CurrentUser.Username} Bot Announcement", "GTS Trade Bot is Offline");
                        await botchan.SendMessageAsync(embed: offembed.Build());
                    }

                }
                var cordchan = (ITextChannel)discordbot._client.GetChannelAsync(873613944273641503).Result;
                await cordchan.ModifyAsync(x => x.Name = cordchan.Name.Replace("✅", "❌"));
                timeout.Reset();
                return 8;

                if (serverEndPointSync != null)
                {
                    syncClient.Close();
                }
                if (serverEndPointTV != null)
                {
                    tvClient.Close();
                }

            }
            catch 
            {
                if (wtchan.Name.ToString().Contains("✅"))
                {
                    await wtchan.ModifyAsync(prop => prop.Name = wtchan.Name.ToString().Replace("✅", "❌"));
                    var offembed = new EmbedBuilder();
                    offembed.AddField($"{discordbot._client.CurrentUser.Username} Bot Announcement", "Wonder Trade Bot is Offline");
                    await wtchan.SendMessageAsync(embed: offembed.Build());
                }
                var bcids = Ledybot.Program.f1.BotChannels.Text.Split(',');
                foreach (string ids in bcids)
                {
                    ulong.TryParse(ids, out var bcid);
                    var botchan = (ITextChannel)discordbot._client.GetChannelAsync(bcid).Result;
                    if (botchan.Name.Contains("✅"))
                    {
                        var role = botchan.Guild.EveryoneRole;
                        await botchan.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny));
                        await botchan.ModifyAsync(prop => prop.Name = botchan.Name.ToString().Replace("✅", "❌"));
                        var offembed = new EmbedBuilder();
                        offembed.AddField($"{discordbot._client.CurrentUser.Username} Bot Announcement", "GTS Trade Bot is Offline");
                        await botchan.SendMessageAsync(embed: offembed.Build());
                    }

                }
                var cordchan = (ITextChannel)discordbot._client.GetChannelAsync(873613944273641503).Result;
                await cordchan.ModifyAsync(x => x.Name = cordchan.Name.Replace("✅", "❌"));
                return 8;
            }
        }

        public void RequestStop()
        {
            botstop = true;
        }


    }
}

