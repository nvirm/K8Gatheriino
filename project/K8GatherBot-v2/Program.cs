using Discore;
using Discore.Http;
using Discore.WebSocket;
using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

namespace K8GatherBotv2
{

/*
Thanks for checking out K8Gatheriino.
Made with love and coffee. More or less the other.
      
   :o`                                      `o:   
   `MN+`                                  `+NM`   
    dMmm+                                +mmMd    
    oMsymm/                            /mmysMo    
    :Mhooymd:                        :dmyoohM:    
     Ndossohmh-                    -hmhossodN     
     hNoyNdsshNy-                -yNhssdNyoNh     
     oMsoMMNdssdmy.            .yNdssdNMMosMo     
     -MhomMMMNhssdmhhhhhhhhhhhhmdsshNMMMmohM-     
      NmodMMMNdooossssssssssssssooodNMMMdomN      
      hNoyMMmsoooooooooooooooooooooosmMMyoNh      
      +MssNyooooooooooooooooooooooooooyNssM+      
      .MhosoooooooooooooooooooooooooooosohM.      
      -MdoooooooooooooooooooooooooooooooodM-      
     :NdooooossoooooooooooooooooooossooooodN:     
    :NdooooooymmdhyssoooooooossyhdmmyoooooodN:    
   /NdoooooooosdMMNdyooooooooydNMMdsoooooooodN/   
  +NhooooooooooosysoooooooooooosysooooooooooohN+  
 +Nhoo+++/:--/oooooooooooooooooooooo/--:/+++oohN+ 
+Md/-.`       .:+oooooooooooooooo+:.       `.-/dM+
./sdhs/.`       `-+oooossssoooo+-`       `./shds/.
    .:ohhy+-`      ./hmNMMNmh/.      `-+yhho:.    
        `:ohdy+-`    .omMMmo.    `-+ydho:`        
            `-+ydyo:`  `//`  `:oydy+-`            
                 -/ydhs/``/shdy/-                 
                     -+ymmy+-
                            
*/

    class Program
    {
        DiscordHttpClient http;

        public static class ProgHelpers
        {
            //NOTE: This program uses both Discord Usernames and UserIDs. 
            //UserIDs are more reliable, because they can not be changed. Keep this in mind when developing.
            public static IConfigurationRoot Configuration { get; set; }    //Initialize Configuration dependency
            public static PersistedData persistedData { get; set; }         //Initialize Persisted Data (Stored data)

            //Queue---------------------------------------------------------------------------------------------
            public static List<string> queue = new List<string>();          //Queue: Discord Usernames (string)
            public static List<string> queueids = new List<string>();       //Queue: Discord UserIDs (string)
            public static List<string> readycheckids = new List<string>();  //Readycheck: Discord UserIDs (string)
            public static List<string> readycheck = new List<string>();     //Readycheck: Discord Usernames (string)
            
            //Team 1--------------------------------------------------------------------------------------------
            public static string captain1 = "";                             //Captain Team1: Discord Username (string)
            public static string captain1id = "";                           //Captain Team1: Discord UserID (string)
            public static List<string> team1 = new List<string>();          //Team1: Discord Usernames (List of string)
            public static List<string> team1ids = new List<string>();       //Team1: Discord UserIDs (List of string)
            
            //Team 2--------------------------------------------------------------------------------------------
            public static string captain2 = "";                             //Captain Team2: Discord Username (string)
            public static string captain2id = "";                           //Captain Team2: Discord UserID (string)
            public static List<string> team2 = new List<string>();          //Team2: Discord Usernames (List of string)
            public static List<string> team2ids = new List<string>();       //Team2: Discord UserIDs (List of string)

            //Draftlist------------------------------------------------------------------------------------------
            //Note: Workaround to keep initial list numbering for the whole draft, show names from another list.
            //Entries to this list are filled when ready check is complete & captains are randomed (all players except captains are in this list)
            //Entries from these lists are removed each time a player is !pick:ed.
            public static List<string> draftchatnames = new List<string>();
            public static List<string> draftchatids = new List<string>();
            public static string pickturn = ""; //Pick turn: Initial value "", team1 is cap1id, team2 is cap2id (this is toggled until teams are full)

            //Technical defaults----------------------------------------------------------------------------------------
            public static int _counter = 0; //Readytimer: Inital Value
            public static int counterlimit = 0; //Readytimer: Max value (Time in seconds after which players who are not ready are removed from queue.)
            public static int qcount = 0; //Queuesize: Max players in queue
            public static string userChannel = ""; //Channel: Channel to listen for messages (Discord debug for ID)
            public static string bottoken = ""; //Bot: Bot token for the bot, Discord developer panel https://discordapp.com/developers/applications/me
            public static string txtversion = ""; //Version: Version txt that is shown in bot embed messages.
            public static string gametxt = ""; //Game: Set a custom "Playing <GAME>" text for bot.
            public static string language = ""; //Language: Set your chosen language (fi = finnish, en = english)
            public static int pickMode = 1; //PickMode: 1-2-2-2-2-1 or 1-1-1-1-1-1-1-1-1-1 (1 or 2)
            public static int newKidThreshold = 10;  //CaptainThreshold: Threshold of games played until you can become captain, defaults at 10, overridden by appsettings.json.
            public static int giveupThreshold = 100; //CaptainThreshold: Threshold to prevent infinite loop, 100 should be sufficient always.
            public static int developmentMode = 0; //DevelopmentMode: 0 false, 1 true. Enables or disables some commands
            public static int additionalAddCmd = 0; //AdditionalAddCmd: 0 false, 1 true. Enables more !add commands (some people like to jest :)), If a more serious venue is needed, can be toggled.
            public static int restrictReset = 0; //RestrictResetCmd: 0 false, 1 true. True restricts !resetbot command to a role specified in restrictResetRole
            public static string restrictResetRole = "";  //RestrictResetCmdRole: String format rolename for user that CAN use !resetbot
            //Languages----------------------------------------------------------------------------------------
            public static Dictionary<string, Dictionary<string, string>> locales = new Dictionary<string, Dictionary<string, string>>();
            public static Dictionary<string, string> locale;
            //Discord gateway----------------------------------------------------------------------------------------
            public static Snowflake channelsnowflake = new Snowflake();
        }

        static void Main(string[] args)
        {
            Console.WriteLine("# K8Gatheriino, PUG arrangement bot for discord #");
            Console.WriteLine("# --------------------------------------------- #");
            //READ SETTINGS----------------------------------------------------------------------------------------
            Console.WriteLine("Reading settings from appsettings.json");
            var settingsfile = File.Exists("appsettings.json");
            if (settingsfile == false)
            {
                //No settings file present, exit application after 5 seconds.
                Console.WriteLine("No appsettings.json found.");
                Console.WriteLine("Seek instructions: https://github.com/kitsun8/K8Gatheriino");
                Thread.Sleep(5000);
                return;
            }

            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
            ProgHelpers.Configuration = builder.Build();
            ProgHelpers.persistedData = new PersistedData();

            Console.WriteLine("START SETTINGS-----------------------------");

                ProgHelpers.developmentMode = Convert.ToInt32(ProgHelpers.Configuration["Settings:DeveloperMode"]);
                Console.WriteLine("DeveloperMode:" + Convert.ToInt32(ProgHelpers.Configuration["Settings:DeveloperMode"]));

                ProgHelpers.additionalAddCmd = Convert.ToInt32(ProgHelpers.Configuration["Settings:AdditionalAddCmd"]);
                Console.WriteLine("AdditionalAddCmd:" + Convert.ToInt32(ProgHelpers.Configuration["Settings:AdditionalAddCmd"]));

                ProgHelpers.qcount = Convert.ToInt32(ProgHelpers.Configuration["Settings:Queuesize"]); 
                Console.WriteLine("Queuesize:" + Convert.ToInt32(ProgHelpers.Configuration["Settings:Queuesize"]));

                ProgHelpers.counterlimit = Convert.ToInt32(ProgHelpers.Configuration["Settings:Readytimer"]);
                Console.WriteLine("Readytimer:" + Convert.ToInt32(ProgHelpers.Configuration["Settings:Readytimer"]));

                ProgHelpers.newKidThreshold = Convert.ToInt32(ProgHelpers.Configuration["Settings:CaptainThreshold"]);
                Console.WriteLine("CaptainThreshold:" + Convert.ToInt32(ProgHelpers.Configuration["Settings:CaptainThreshold"]));

                ProgHelpers.pickMode = Convert.ToInt32(ProgHelpers.Configuration["Settings:PickMode"]);
                Console.WriteLine("PickMode:" + Convert.ToInt32(ProgHelpers.Configuration["Settings:PickMode"]));

                ProgHelpers.restrictReset = Convert.ToInt32(ProgHelpers.Configuration["Settings:RestrictResetCmd"]);
                Console.WriteLine("RestrictResetCmd:" + ProgHelpers.Configuration["Settings:RestrictResetCmd"]);

                ProgHelpers.restrictResetRole = ProgHelpers.Configuration["Settings:RestrictResetCmdRole"];
                Console.WriteLine("RestrictResetCmdRole:" + ProgHelpers.Configuration["Settings:RestrictResetCmdRole"]);

                ProgHelpers.language = ProgHelpers.Configuration["Settings:Language"];
                Console.WriteLine("Language:" + ProgHelpers.Configuration["Settings:Language"]);

                ProgHelpers.userChannel = ProgHelpers.Configuration["Settings:AllowedChannel"];
                Console.WriteLine("AllowedChannel:" + ProgHelpers.Configuration["Settings:AllowedChannel"]);

                ProgHelpers.bottoken = ProgHelpers.Configuration["Settings:BotToken"];
                Console.WriteLine("BotToken:" + ProgHelpers.Configuration["Settings:BotToken"]);

                ProgHelpers.txtversion = ProgHelpers.Configuration["Settings:Version"];
                Console.WriteLine("Version:" + ProgHelpers.Configuration["Settings:Version"]);

                ProgHelpers.gametxt = ProgHelpers.Configuration["Settings:Game"];
                Console.WriteLine("Game:" + ProgHelpers.Configuration["Settings:Game"]);

                ProgHelpers.channelsnowflake.Id = (ulong)Convert.ToInt64(ProgHelpers.Configuration["Settings:AllowedChannel"]);
                Console.WriteLine("Manipulated AllowedChannel ID to Snowflake:" + ProgHelpers.Configuration["Settings:AllowedChannel"]);
                

            Console.WriteLine("END SETTINGS-----------------------------");

            initLocalizations();
            ProgHelpers.locale = ProgHelpers.locales[ProgHelpers.language];

            Program program = new Program();
            program.Run().Wait();
            Console.WriteLine("#! Reached END OF PROGRAM !#");
        }

        //TRANSLATIONS----------------------------------------------------------------------------------------
        public static void initLocalizations()
        {
            Dictionary<string, string> fi = new Dictionary<string, string>();
            Dictionary<string, string> en = new Dictionary<string, string>();
            ProgHelpers.locales.Add("fi", fi);
            ProgHelpers.locales.Add("en", en);

            //Finnish localization
            fi.Add("pickPhase.alreadyInProcess", "Odota kunnes edellinen jono on käsitelty.");
            fi.Add("queuePhase.added", "Lisätty!");
            fi.Add("readyPhase.started", "Jono on nyt täynnä, merkitse itsesi valmiiksi käyttäen ***!ready*** komentoa. \n Aikaa 60 sekuntia!");
            fi.Add("queuePhase.alreadyInQueue", "Olet jo jonossa!");
            fi.Add("pickPhase.cannotRemove", "Liian myöhäistä peruuttaa, odota jonon käsittelyn valmistumista.");
            fi.Add("queuePhase.removed", "Poistettu!");
            fi.Add("queuePhase.notInQueue", "Et ole juuri nyt jonossa");
            fi.Add("queuePhase.notReadyYet", "Jono ei ole vielä valmis!");
            fi.Add("readyPhase.ready", "Valmiina!");
            fi.Add("pickPhase.started", "Readycheck valmis, aloitetaan poimintavaihe! Ensimmäinen poiminta: Team 1 \n - Team 1:n kapteeni:");
            fi.Add("pickPhase.team2Captain", " - Team 2:n kapteeni:");
            fi.Add("pickPhase.instructions", "Poimi pelaajia käyttäen ***!pick NUMERO***");
            fi.Add("pickPhase.pickmodeinst1", "Poimintavuorot: ***1-2-...-2-1***");
            fi.Add("pickPhase.pickmodeinst2", "Poimintavuorot: ***1-1-...-1-1***");
            fi.Add("pickPhase.team2Turn", "Pelaaja lisätty! Poimintavuoro: ");
            fi.Add("pickPhase.team1Turn", "Pelaaja lisätty! Poimintavuoro: ");
            fi.Add("pickPhase.unpicked", "***Poimimatta:***");
            fi.Add("pickPhase.alreadyPicked", "Pelaaja on jo joukkueessa!");
            fi.Add("pickPhase.unknownIndex", "Numerolla ei löytynyt pelaajaa!");
            fi.Add("pickPhase.notYourTurn", "Ei ole vuorosi poimia!");
            fi.Add("pickPhase.notCaptain", "Vain kapteenit voivat poimia pelaajia!");
            fi.Add("queuePhase.emptyQueue", "Jono on tyhjä! Käytä ***!add*** aloittaaksesi jonon!");
            fi.Add("admin.resetSuccessful", "Kaikki listat tyhjennetty onnistuneesti!");
            fi.Add("status.pickedTeams", "Valitut joukkueet");
            fi.Add("status.queueStatus", "Jonon tilanne");
            fi.Add("info.purposeAnswer", "Saada pelaajia keräytymään pelien äärelle!");
            fi.Add("info.funFactAnswer", "Gathut aiheuttavat paljon meemejä :thinking:");
            fi.Add("info.developer", "Kehittäjä");
            fi.Add("info.purpose", "Tarkoitus");
            fi.Add("info.funFact", "Tiesitkö");
            fi.Add("info.commands", "Komennot");
            fi.Add("status.queuePlayers", "Pelaajat");
            fi.Add("status.notReady", "EI VALMIINA");
            fi.Add("readyPhase.timeout", "Kaikki pelaajat eivät olleet valmiita readycheckin aikana. Palataan jonoon valmiina olleiden pelaajien kanssa.");
            fi.Add("readyPhase.alreadyMarkedReady", "Olet jo merkinnyt itsesi valmiiksi!");
            fi.Add("readyPhase.cannotAdd", "Odota poimintavaiheen päättymistä!");
            fi.Add("fatKid.header", "Viimeiseksi valittu");
            fi.Add("fatKid.top10", "Top10 viimeisenä valitut");
            fi.Add("fatKid.statusSingle", "{0} on valittu viimeisenä {1} kertaa ({2}/{3})");
            fi.Add("highScores.header", "Gathuja pelattu");
            fi.Add("highScores.top10", "Top10 gathuLEGENDAT");
            fi.Add("highScores.statusSingle", "{0} on pelannut {1} gathua ({2}/{3})");
            fi.Add("thinKid.header", "1. varaus");
            fi.Add("thinKid.top10", "Top10 ensimmäisenä valitut");
            fi.Add("thinKid.statusSingle", "{0} on valittu ensimmäisenä {1} kertaa ({2}/{3})");
            fi.Add("captain.header", "Kapteeni");
            fi.Add("captain.top10", "Top10 kapteenit");
            fi.Add("captain.statusSingle", "{0} on valittu kapteeniksi {1} kertaa ({2}/{3})");
            fi.Add("player.stats", "pelaajan tiedot");
            fi.Add("relinq.pickPhaseStarted", "Olet jo valinnut pelaajan, liian myöhäistä luopua tehtävästä.");
            fi.Add("relinq.successful", "Luovuit kapteeninhommista onnistuneesti, uusi kapteeni on: ");

            //English localization
            en.Add("pickPhase.alreadyInProcess", "Please wait until the previous queue is handled.");
            en.Add("queuePhase.added", "Added!");
            en.Add("readyPhase.started", "Queue is now full, proceed to mark yourself ready with ***!ready*** \n You have 60 seconds!");
            en.Add("queuePhase.alreadyInQueue", "You're already in the queue!");
            en.Add("pickPhase.cannotRemove", "Too late to back down now! Wait until the queue is handled.");
            en.Add("queuePhase.removed", "Removed!");
            en.Add("queuePhase.notInQueue", "You are not in the queue right now.");
            en.Add("queuePhase.notReadyYet", "Queue is not finished yet!");
            en.Add("readyPhase.ready", "Ready!");
            en.Add("pickPhase.started", "Readycheck complete, starting picking phase! First picking turn: Team 1 \n - Team 1 Captain:");
            en.Add("pickPhase.team2Captain", " - Team 2 Captain:");
            en.Add("pickPhase.instructions", "Pick players using ***!pick NUMBER***");
            en.Add("pickPhase.pickmodeinst1", "Picking turns: ***1-2-...-2-1***");
            en.Add("pickPhase.pickmodeinst2", "Picking turns: ***1-1-...-1-1***");
            en.Add("pickPhase.team2Turn", "Player added! Next pick: ");
            en.Add("pickPhase.team1Turn", "Player added! Next pick: ");
            en.Add("pickPhase.unpicked", "***Remaining players:***");
            en.Add("pickPhase.alreadyPicked", "That player is already in a team!");
            en.Add("pickPhase.unknownIndex", "Couldn't place a player with that index");
            en.Add("pickPhase.notYourTurn", "Not your turn to pick right now!");
            en.Add("pickPhase.notCaptain", "You are not the captain of either teams. Picking is restricted to captains.");
            en.Add("queuePhase.emptyQueue", "Nobody in the queue! use ***!add***  to start queue!");
            en.Add("admin.resetSuccessful", "All lists emptied successfully.");
            en.Add("status.pickedTeams", "Selected teams");
            en.Add("status.queueStatus", "Current queue");
            en.Add("info.purposeAnswer", "Get people to gather and play together");
            en.Add("info.funFactAnswer", "Only a droplet of coffee was used to develop this bot. :thinking:");
            en.Add("info.developer", "Developer");
            en.Add("info.purpose", "Purpose");
            en.Add("info.funFact", "Fun fact");
            en.Add("info.commands", "Commands");
            en.Add("status.queuePlayers", "Players");
            en.Add("status.notReady", "NOT READY YET:");
            en.Add("readyPhase.timeout", "Not all players were ready during the readycheck. Returning to queue with players that were ready.");
            en.Add("readyPhase.cannotAdd", "Wait until the picking phase is over.");
            en.Add("readyPhase.alreadyMarkedReady", "You have already readied!");
            en.Add("fatKid.header", "Fat Kid");
            en.Add("fatKid.top10", "Top 10 Fat Kids");
            en.Add("fatKid.statusSingle", "{0} has been the fat kid {1} times ({2}/{3})");
            en.Add("highScores.header", "Games played");
            en.Add("highScores.top10", "Top 10 Gathering LEGENDS");
            en.Add("highScores.statusSingle", "{0} has played {1} games ({2}/{3})");
            en.Add("thinKid.header", "Thin kid");
            en.Add("thinKid.top10", "Top10 Thin Kids");
            en.Add("thinKid.statusSingle", "{0} has been the thin kid {1} times ({2}/{3}");
            en.Add("captain.header", "Captain");
            en.Add("captain.top10", "Top10 Captains");
            en.Add("captain.statusSingle", "{0} has been selected captain {1} times ({2}/{3}");
            en.Add("player.stats", "Player statistics");
            en.Add("relinq.pickPhaseStarted", "You have already picked a player, too late to drop out of picking phase");
            en.Add("relinq.successful", "Captainship relinquished successfully, new captain is: ");
        }

        //RUN PROGRAM----------------------------------------------------------------------------------------
        public async Task Run()
        {
            

            // Create an HTTP client.
            http = new DiscordHttpClient(ProgHelpers.bottoken); //Use BOT token from settings

            // Create a single shard.
            using (Shard shard = new Shard(ProgHelpers.bottoken, 0, 1))
            {
                // Subscribe to the message creation event.
                shard.Gateway.OnMessageCreated += Gateway_OnMessageCreated;
               
                //2018-04: Shard Connected (Debug reasons)
                shard.OnConnected += Shard_OnConnected;
                //2018-04: Log shard reconnections (Debug reasons, other?)
                shard.OnReconnected += Shard_OnReconnected;
                //2018-04: If shard fails, try making another (after a little while, just in case)
                shard.OnFailure += Shard_OnFailure;

                // Start the shard.
                await shard.StartAsync();
                Console.WriteLine(DateTime.Now + $" -- kitsun8's GatherBot Started \n -------------------------------------------");

                // Wait for the shard to end before closing the program.
                await shard.WaitUntilStoppedAsync();           
            }
        }

        //HANDLE SHARD EVENTS----------------------------------------------------------------------------------------
        private static async void Shard_OnConnected(object sender, ShardEventArgs e)
        {
            Console.WriteLine(DateTime.Now + $" -- #! SHARD CONNECTED !# --");
            //Shard connected.. doesn't need other actions?

            //2018-10 - Update Bot's status
            GameOptions game = new GameOptions(ProgHelpers.gametxt);
            StatusOptions options = new StatusOptions();
            options.SetGame(game);
            e.Shard.Gateway.UpdateStatusAsync(options);
        }
        private static async void Shard_OnReconnected(object sender, ShardEventArgs e)
        {
            Console.WriteLine(DateTime.Now + $" -- #! SHARD RECONNECTED !# --");
            //Shard reconnected.. doesn't need other actions?

            //2018-10 - Update Bot's status
            GameOptions game = new GameOptions(ProgHelpers.gametxt);
            StatusOptions options = new StatusOptions();
            options.SetGame(game);
            e.Shard.Gateway.UpdateStatusAsync(options);

        }
        private static void Shard_OnFailure(object sender, ShardFailureEventArgs e)
        {
            if (e.Reason == ShardFailureReason.Unknown)
            {
                Console.WriteLine(DateTime.Now + $"-- #! SHARD UNKNOWN ERROR !# --");
            }
            if (e.Reason == ShardFailureReason.ShardInvalid)
            {
                Console.WriteLine(DateTime.Now + $"-- #! SHARD INVALID ERROR !# --");
            }
            if (e.Reason == ShardFailureReason.ShardingRequired)
            {
                Console.WriteLine(DateTime.Now + $"-- #! SHARDING REQUIRED ERROR !# --");
            }
            if (e.Reason == ShardFailureReason.AuthenticationFailed)
            {
                Console.WriteLine(DateTime.Now + $"-- #! SHARD AUTH ERROR !# --");
            }
            //Shard has failed, need to Run whole auth again!
            //EXPERIMENTAL! Trying to get the existing shard and stop it first (check if this actually exits the whole program), then run another.. Run.
            Console.WriteLine("-- #! SHARD HAS FAILED, TRYING TO START AGAIN !# --");

            Shard shard = e.Shard;
            shard.StopAsync();
            Console.WriteLine("-- #! SHARD STOPPED ASYNC !# --");

            Program program = new Program();
            Console.WriteLine("-- #! STARTING A NEW INSTANCE !# --");
            program.Run().Wait();
        }


        //NOT READY ANNOUNCE----------------------------------------------------------------------------------------
        public async Task RunNotRdyannounce()
        {
            Console.WriteLine(DateTime.Now + $"-- #! NOT READY ANNOUNCE START !# --");
            DiscordHttpClient http2;
            http2 = new DiscordHttpClient(ProgHelpers.bottoken); //Use BOT token from settings
            // Create a single shard.
            using (Shard shard2 = new Shard(ProgHelpers.bottoken, 0, 1))
            {
                // Start the shard.
                await shard2.StartAsync();
                Console.WriteLine(DateTime.Now + $"-- #! NEW SHARD CREATED !# --");

                // Wait for the shard to end before closing the program.
                await http2.CreateMessage(ProgHelpers.channelsnowflake, ProgHelpers.locale["readyPhase.timeout"]);
                Console.WriteLine(DateTime.Now + $"-- #! SHARD ANNOUNCE ATTEMPT !# --");
                await shard2.StopAsync();
                Console.WriteLine(DateTime.Now + $"-- #! NEW SHARD STOPPED !# --");
                Console.WriteLine(DateTime.Now + $"-- #! NOT READY ANNOUNCE END !# --");
            }
            
        }

        //READYCHECK TIMER----------------------------------------------------------------------------------------
        public static Timer _tm = null;
        public static AutoResetEvent _autoEvent = null;

        public static void StartTimer()
        {
            //timer init

            _autoEvent = new AutoResetEvent(false);
            _tm = new Timer(Checkrdys, _autoEvent, 1000, 1000);

            Console.WriteLine("-- #! RDYCHECK ACTIVATED !# --" + DateTime.Now.ToString());
        }

        public static void Checkrdys(Object stateInfo)
        {
            if (ProgHelpers._counter < ProgHelpers.counterlimit)
            {
                ProgHelpers._counter++;
                return;
            }
            //Timer is up, check who have not readied.
            List<string> notinlist = ProgHelpers.queueids.Except(ProgHelpers.readycheckids).ToList();
            if (notinlist.Count > 0)
            {

                foreach (string item in notinlist)
                {
                    var fndr1 = ProgHelpers.queueids.IndexOf(item); //Get index of Discord UserID, UName can change, ID can not

                    ProgHelpers.queue.RemoveAt(fndr1);
                    ProgHelpers.queueids.Remove(item);
                    Console.WriteLine("-- #! RDYCHECK-REMOVED !# --" + item);
                }
                ProgHelpers.readycheckids.Clear();
                ProgHelpers.readycheck.Clear();

                Program rdyprog = new Program();
                rdyprog.RunNotRdyannounce();

            }

            //Reset timer
            _tm.Dispose();
            ProgHelpers._counter = 0;

            Console.WriteLine("-- #! RDYCHECK EXPIRED !# --" + DateTime.Now.ToString());
        }

        //PARSE GATEWAY MESSAGES----------------------------------------------------------------------------------------
        private async void Gateway_OnMessageCreated(object sender, MessageEventArgs e)
        {
            Shard shard = e.Shard;
            DiscordMessage message = e.Message;


            if (message.Author.Id == shard.UserId)
            {
                return; //Disregard Bot's own messages
            }

            if (message.ChannelId.Id.ToString() != ProgHelpers.userChannel) //Prevent DM abuse, only react to messages sent on a set channel.
            {
                return;
            }

            string msgBody = message.Content.ToLower().Split(' ')[0];

            switch (msgBody)
            {
                case "!abb":    //haHAA!
                case "!asd":    //haHAA!
                case "!fap":    //haHAA!
                case "!ab":     //haHAA!
                case "!kohta":  //haHAA!
                case "!dab":    //haHAA!
                case "!sad":    //haHAA!
                case "!abs":    //hahaa!
                case "!bad":    //haHAA!
                case "!ad":     //haHAA!
                case "!mad":    //haHAA!
                case "!grand":  //haHAA!
                    if (ProgHelpers.additionalAddCmd == 1)
                    {
                        //Only run in AdditionalAddCmd = 1
                        await CmdAdd(shard, message);
                    }
                    break;
                case "!add":
                    await CmdAdd(shard, message);
                    break;
                case "!remove":
                case "!rm":
                    await CmdRemove(shard, message);
                    break;
                case "!ready":
                case "!r":
                    await CmdReady(shard, message);
                    break;
                case "!pick":
                case "!p":
                    await CmdPick(shard, message);
                    break;
                case "!pstats":
                    await CmdPlayerStats(shard, message);
                    break;
                case "!fatkid":
                    await CmdFatKid(shard, message);
                    break;
                case "!f10":
                case "!fat10":
                    await CmdFatTopTen(shard, message);
                    break;
                case "!highscore":
                case "!hs":
                    await CmdHighScore(shard, message);
                    break;
                case "!topten":
                case "!top10":
                    await CmdTopTen(shard, message);
                    break;
                case "!thinkid":
                    await CmdThinKid(shard, message);
                    break;
                case "!tk10":
                    await CmdThinTopTen(shard, message);
                    break;
                case "!captain":
                    await CmdCaptain(shard, message);
                    break;
                case "!c10":
                    await CmdCaptainTopTen(shard, message);
                    break;
                case "!gstatus":
                case "!gs":
                    await CmdGStatus(shard, message);
                    break;
                case "!resetbot":
                    if (ProgHelpers.restrictReset == 1)
                    {
                        //Only restricted roles can use the command (appsettings)

                        Console.WriteLine("#! Checking permissions for !resetbot !#");
                        //NOTE: The following function is made with a HTTP API call. 
                        //HTTP API calls are restricted to be used only seldomly. 
                        //This command, however, shouldn't be called upon very often.

                        DiscordGuildTextChannel textChannel = await http.GetChannel<DiscordGuildTextChannel>(message.ChannelId); //Get the textchannel
                        IReadOnlyList<DiscordRole> roles = await http.GetGuildRoles(textChannel.GuildId); //Get all roles in the textchannel
                        //Get Role ID of the Role Name specified in appsettings.json
                        var selRole = roles.FirstOrDefault(x => x.Name == ProgHelpers.restrictResetRole);
                        if (selRole != null)
                        {
                            Console.WriteLine("-- #! Found a match for role from appsettings.json !# --");
                            // Get the message author member and match role Id against the resolved role Id
                            DiscordGuildMember member = await http.GetGuildMember(textChannel.GuildId, message.Author.Id);
                            var selRoleString = selRole.Id.ToString();
                            var memberRole = member.RoleIds.Where(x => x.Id.ToString() == selRoleString);
                            if (memberRole.Any())
                            {
                                Console.WriteLine("-- #! User has permission to !resetbot !# --");
                                await CmdResetBot(shard, message);
                            }
                            else
                            {
                                Console.WriteLine("-- #! User does not have permission to !resetbot !# --");
                            }
                        }
                        else
                        {
                            Console.WriteLine("-- #! No match for role from appsettings.json !# --");
                        }  
                    }
                    else
                    {
                            //Anyone is allowed to use the command (appsettings)
                            await CmdResetBot(shard, message);
                    }
                    break;
                case "!gatherinfo":
                case "!ginfo":
                case "!gi":
                    await CmdGatherInfo(shard, message);
                    break;
                case "#test_add":
                    if (ProgHelpers.developmentMode == 1)
                    {
                        //Only run in developmentmode = 1
                        CmdFakeriino(shard, message);
                    }
                    break;
                //Note: Relinquish feature disabled as Work-In-Progress, threshold took the place of this.
                //Not removing the code however, might prove useful in something else.
                    //case "!relinquish":
                    //await CmdRelinquishCaptainship(shard, message);
                    //break;
            }

        }

        private async Task CmdPlayerStats(Shard shard, DiscordMessage message)
        {
            try
            {
                string msg = message.Content;
                Tuple<string, string> idUsername = ParseIdAndUsername(message);
                Console.WriteLine("-- #! Getting stats for user !# --" + idUsername);
                string highScoreStats = ProgHelpers.persistedData.GetHighScoreInfo(idUsername, ProgHelpers.locale["highScores.statusSingle"]);
                string fatkidStats = ProgHelpers.persistedData.GetFatKidInfo(idUsername, ProgHelpers.locale["fatKid.statusSingle"]);
                string captainStats = ProgHelpers.persistedData.GetCaptainInfo(idUsername, ProgHelpers.locale["captain.statusSingle"]);
                string thinkidStats = ProgHelpers.persistedData.GetThinKidInfo(idUsername, ProgHelpers.locale["thinKid.statusSingle"]);
                 

                await http.CreateMessage(ProgHelpers.channelsnowflake, new CreateMessageOptions()
                     .SetEmbed(new EmbedOptions()
                     .SetTitle($"kitsun8's GatherBot, " + ProgHelpers.locale["player.stats"] + ": " + idUsername.Item2)
                              .SetFooter("K8Gatheriino, " + ProgHelpers.txtversion)
                              .SetColor(DiscordColor.FromHexadecimal(0xff9933))
                              .AddField(ProgHelpers.locale["highScores.header"], highScoreStats, true)
                              .AddField(ProgHelpers.locale["captain.header"], captainStats, true)
                              .AddField(ProgHelpers.locale["thinKid.header"], thinkidStats, true)
                              .AddField(ProgHelpers.locale["fatKid.header"], fatkidStats, true)
                              )
                              );
            }
            catch (Exception e)
            {
                Console.WriteLine($"!gatherinfo - EX -" + message.Author.Username + "-" + message.Author.Id + " --- " + DateTime.Now);
                Console.WriteLine("!# DEBUG INFO FOR ERROR: " + e.ToString() + " #! --");
            }
        }

        private async Task CmdGatherInfo(Shard shard, DiscordMessage message)
        {
            try
            {
                await http.CreateMessage(ProgHelpers.channelsnowflake, new CreateMessageOptions()
                    .SetEmbed(new EmbedOptions()
                    .SetTitle($"kitsun8's GatherBot")
                    .SetFooter("K8Gatheriino, " + ProgHelpers.txtversion)
                    .SetColor(DiscordColor.FromHexadecimal(0xff9933))
                    .AddField(ProgHelpers.locale["info.developer"] + " ", "kitsun8 & pirate_patch", false)
                    .AddField(ProgHelpers.locale["info.purpose"] + " ", ProgHelpers.locale["info.purposeAnswer"], false)
                    .AddField(ProgHelpers.locale["info.funFact"] + " ", ProgHelpers.locale["info.funFactAnswer"], false)
                    .AddField(ProgHelpers.locale["info.commands"] + " ", "!add, !remove/rm, !ready/r, !pick/p, !ginfo/gi/gatherinfo, !gstatus/gs, !f10/fat10, !fatkid, !top10/topten, !hs/highscore, !tk10, !thinkid, !c10, !captain, !resetbot", false)
                              )
                              );

                Console.WriteLine($"!gatherinfo - " + message.Author.Username + "-" + message.Author.Id + " --- " + DateTime.Now);
            }
            catch (Exception)
            {
                Console.WriteLine($"!gatherinfo - EX -" + message.Author.Username + "-" + message.Author.Id + " --- " + DateTime.Now);
            }
        }

        private async Task CmdResetBot(Shard shard, DiscordMessage message)
        {
            try
            {
                ResetQueue();
                await http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + ProgHelpers.locale["admin.resetSuccessful"]);
                Console.WriteLine("!resetbot" + " --- " + DateTime.Now);
            }
            catch (Exception)
            {
                Console.WriteLine("EX-!resetbot" + " --- " + DateTime.Now);
            }
        }

        private async Task CmdGStatus(Shard shard, DiscordMessage message)
        {
            try
            {

                if (ProgHelpers.queue.Count != 0)
                {
                    if (ProgHelpers.queue.Count == ProgHelpers.qcount)
                    {

                        //compare readycheck list to queue, print out those who are not ready
                        List<string> notinlist = ProgHelpers.queue.Except(ProgHelpers.readycheck).ToList();

                        //full queue, ready phase

                        await http.CreateMessage(ProgHelpers.channelsnowflake, new CreateMessageOptions()
                        .SetEmbed(new EmbedOptions()
                        .SetTitle($"kitsun8's GatherBot, readycheck  " + "(" + ProgHelpers.readycheckids.Count.ToString() + "/" + ProgHelpers.qcount.ToString() + ")")
                        .SetFooter("K8Gatheriino, " + ProgHelpers.txtversion)
                        .SetColor(DiscordColor.FromHexadecimal(0xff9933))
                        .AddField(ProgHelpers.locale["status.queuePlayers"] + " ", string.Join("\n", notinlist.Cast<string>().ToArray()), false)
                              )
                              );

                    }
                    else
                    {
                        if (ProgHelpers.team1ids.Count + ProgHelpers.team2ids.Count > 0)
                        {
                            //picking phase
                            await http.CreateMessage(ProgHelpers.channelsnowflake, new CreateMessageOptions()
                            .SetEmbed(new EmbedOptions()
                            .SetTitle($"kitsun8's GatherBot, " + ProgHelpers.locale["status.pickedTeams"])
                            .SetFooter("K8Gatheriino, " + ProgHelpers.txtversion)
                            .SetColor(DiscordColor.FromHexadecimal(0xff9933))
                            .AddField("Team1: ", string.Join("\n", ProgHelpers.team1.Cast<string>().ToArray()), true)
                            .AddField("Team2: ", string.Join("\n", ProgHelpers.team2.Cast<string>().ToArray()), true)
                            ));
                        }
                        else
                        {
                            //queue phase
                            await http.CreateMessage(ProgHelpers.channelsnowflake, new CreateMessageOptions()
                            .SetEmbed(new EmbedOptions()
                            .SetTitle($"kitsun8's GatherBot, " + ProgHelpers.locale["status.queueStatus"] + " " + "(" + ProgHelpers.queue.Count.ToString() + "/" + ProgHelpers.qcount.ToString() + ")")
                            .SetFooter("K8Gatheriino, " + ProgHelpers.txtversion)
                            .SetColor(DiscordColor.FromHexadecimal(0xff9933))
                            .AddField(ProgHelpers.locale["status.queueStatus"] + " ", string.Join("\n", ProgHelpers.queue.Cast<string>().ToArray()), false)
                            ));
                        }

                    }

                    Console.WriteLine("!status");
                }
                else
                {
                    await http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + ProgHelpers.locale["queuePhase.emptyQueue"]);
                    Console.WriteLine("!status" + " --- " + DateTime.Now);
                }

            }
            catch (Exception)
            {
                Console.WriteLine("EX-!status" + " --- " + DateTime.Now);
            }
        }

        private async Task sendTop10Message(string textKey, string top10List)
        {
            await http.CreateMessage(ProgHelpers.channelsnowflake, new CreateMessageOptions()
                .SetEmbed(new EmbedOptions()
                .SetTitle($"kitsun8's Gatheriino")
                .SetFooter("K8Gatheriino, " + ProgHelpers.txtversion)
                .SetColor(DiscordColor.FromHexadecimal(0xff9933))
                .AddField(ProgHelpers.locale[textKey], top10List)));
        }

        private async Task CmdTopTen(Shard shard, DiscordMessage message)
        {
            try
            {
                string highScoreTop10 = ProgHelpers.persistedData.GetHighScoreTop10();
                await sendTop10Message("highScores.top10", highScoreTop10);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EX-!top10 --- " + DateTime.Now);
                Console.WriteLine("!#DEBUG INFO FOR ERROR: " + ex.ToString());
            }
        }

        private async Task CmdFatTopTen(Shard shard, DiscordMessage message)
        {
            try
            {
                string fatKidTop10 = ProgHelpers.persistedData.GetFatKidTop10();
                await sendTop10Message("fatKid.top10", fatKidTop10);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EX-!f10 --- " + DateTime.Now);
                Console.WriteLine("!#DEBUG INFO FOR ERROR: " + ex.ToString());
            }
        }

        private async Task CmdCaptainTopTen(Shard shard, DiscordMessage message)
        {
            try
            {
                string captainTop10 = ProgHelpers.persistedData.GetCaptainTop10();
                await sendTop10Message("captain.top10", captainTop10);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EX-!c10 --- " + DateTime.Now);
                Console.WriteLine("!#DEBUG INFO FOR ERROR: " + ex.ToString());
            }
        }

        private async Task CmdCaptain(Shard shard, DiscordMessage message)
        {
            try
            {
                Tuple<string, string> idUsername = ParseIdAndUsername(message);
                Console.WriteLine("captain name split resulted in " + idUsername);
                string captainInfo = ProgHelpers.persistedData.GetCaptainInfo(idUsername, ProgHelpers.locale["captain.statusSingle"]);
                await http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + captainInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine("EX-!captain" + " --- " + DateTime.Now);
                Console.WriteLine("!#DEBUG INFO FOR ERROR: " + e.ToString());
            }
        }

        private async Task CmdHighScore(Shard shard, DiscordMessage message)
        {
            try
            {
                Tuple<string, string> idUsername = ParseIdAndUsername(message);
                Console.WriteLine("fatkid name split resulted in " + idUsername);
                string hsInfo = ProgHelpers.persistedData.GetHighScoreInfo(idUsername, ProgHelpers.locale["highScores.statusSingle"]);
                await http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + hsInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine("EX-!hs" + " --- " + DateTime.Now);
                Console.WriteLine("!#DEBUG INFO FOR ERROR: " + e.ToString());
            }
        }

        private async Task CmdFatKid(Shard shard, DiscordMessage message)
        {
            try
            {
                Tuple<string, string> idUsername = ParseIdAndUsername(message);
                Console.WriteLine("fatkid name split resulted in " + idUsername);
                string fatKidInfo = ProgHelpers.persistedData.GetFatKidInfo(idUsername, ProgHelpers.locale["fatKid.statusSingle"]);
                await http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + fatKidInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine("EX-!fatkid" + " --- " + DateTime.Now);
                Console.WriteLine("!#DEBUG INFO FOR ERROR: " + e.ToString());
            }
        }

        private async Task CmdThinTopTen(Shard shard, DiscordMessage message)
        {
            try
            {
                string thinKidTop10 = ProgHelpers.persistedData.GetThinKidTop10();
                await http.CreateMessage(ProgHelpers.channelsnowflake, new CreateMessageOptions()
                    .SetEmbed(new EmbedOptions()
                    .SetTitle($"kitsun8's Gatheriino")
                    .SetFooter("K8Gatheriino, " + ProgHelpers.txtversion)
                    .SetColor(DiscordColor.FromHexadecimal(0xff9933))
                    .AddField(ProgHelpers.locale["thinKid.top10"], thinKidTop10)));
            }
            catch (Exception ex)
            {
                Console.WriteLine("EX-!tk10 --- " + DateTime.Now);
                Console.WriteLine("!#DEBUG INFO FOR ERROR: " + ex.ToString());
            }
        }

        private async Task CmdThinKid(Shard shard, DiscordMessage message)
        {
            try
            {
                Tuple<string, string> idUsername = ParseIdAndUsername(message);
                Console.WriteLine("thinkid name split resulted in " + idUsername);
                string thinKidInfo = ProgHelpers.persistedData.GetThinKidInfo(idUsername, ProgHelpers.locale["thinKid.statusSingle"]);
                await http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + thinKidInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine("EX-!thinkid" + " --- " + DateTime.Now);
                Console.WriteLine("!#DEBUG INFO FOR ERROR: " + e.ToString());
            }
        }

        //DeveloperCommand - Test Adds
        private void CmdFakeriino(Shard shard, DiscordMessage message)
        {
            
            HandleAdd(message, "010", "User X");
            Thread.Sleep(100);
            HandleAdd(message, "111", "User A");
            Thread.Sleep(100);
            HandleAdd(message, "222", "User B");
            Thread.Sleep(100);
            HandleAdd(message, "333", "User C");
            Thread.Sleep(100);
            HandleAdd(message, "444", "User D");
            Thread.Sleep(100);
            HandleAdd(message, "555", "User E");
            Thread.Sleep(100);
            HandleAdd(message, "666", "User F");
            Thread.Sleep(100);
            HandleAdd(message, "777", "User G");
            Thread.Sleep(100);
            HandleAdd(message, "888", "User H");
            Thread.Sleep(100);
            HandleAdd(message, "999", "User I");
            Thread.Sleep(100);
            HandleAdd(message, "000", "User J");
            Thread.Sleep(100);
            HandleAdd(message, "101", "User K");
            Thread.Sleep(100);

            HandleReady(message, "010", "User X");
            Thread.Sleep(100);
            HandleReady(message, "111", "User A");
            Thread.Sleep(100);
            HandleReady(message, "222", "User B");
            Thread.Sleep(100);
            HandleReady(message, "333", "User C");
            Thread.Sleep(100);
            HandleReady(message, "444", "User D");
            Thread.Sleep(100);
            HandleReady(message, "555", "User E");
            Thread.Sleep(100);
            HandleReady(message, "666", "User F");
            Thread.Sleep(100);
            HandleReady(message, "777", "User G");
            Thread.Sleep(100);
            HandleReady(message, "888", "User H");
            Thread.Sleep(100);
            HandleReady(message, "999", "User I");
            Thread.Sleep(100);
            HandleReady(message, "000", "User J");
            Thread.Sleep(100);
            HandleReady(message, "101", "User K");
            Thread.Sleep(100);

        }

        //Note: Relinquish feature disabled as Work-In-Progress, threshold took the place of this.
        //Not removing the code however, might prove useful in something else.
        private async Task CmdRelinquishCaptainship(Shard shard, DiscordMessage message)
        {

            var authorId = message.Author.Id.Id.ToString();
            var authorUserName = message.Author.Username.ToString();

            if (ProgHelpers.captain1id == "" && ProgHelpers.captain2id == "")
            {
                // no captains
                return;
            }
            string team = ""; //27.1.2018 - default value was "team1", testing empty variable as default might prevent possible bugs involving a non-captain doing !wimp.
            if (ProgHelpers.captain1id.Equals(authorId))
            {
                team = "team1";
            }
            else if (ProgHelpers.captain2id.Equals(authorId))
            {
                team = "team2";
            }
            if (!team.Equals(""))
            {
                ChangeCaptain(team, authorId, authorUserName);
            }
            else
            {
                return; //27.1.2018 Just return if !wimp team variable doesn't result to anything, we don't need to announce anything because of non-captains doing !wimp
            }
        }

        private void ChangeCaptain(String team, string authorId, string authorUseName)
        {
            if ((team.Equals("team1") && ProgHelpers.team1ids.Count > 1)
                || (team.Equals("team2") && ProgHelpers.team2ids.Count > 1))
            {
                http.CreateMessage(ProgHelpers.channelsnowflake, $"<@{authorId}> " + ProgHelpers.locale["relinq.pickPhaseStarted"]);
                return;
            }
            Random rnd = new Random(); //Random a new captain
            int newCap = rnd.Next(ProgHelpers.queueids.Count); //Rnd index from the current playerpool (-2 of total players)
            string c1n = "";
            string c1i = "";
            c1n = ProgHelpers.queue[newCap];
            c1i = ProgHelpers.queueids[newCap];

            ProgHelpers.queue[newCap] = authorUseName; //Place the old captain in place of the new captain in the playerpool
            ProgHelpers.queueids[newCap] = authorId; //Place the old captain in place of the new captain in the playerpool
            int draftIndex = ProgHelpers.draftchatids.IndexOf(c1i);
            ProgHelpers.draftchatnames[draftIndex] = newCap + " - " + authorUseName;
            ProgHelpers.draftchatids[draftIndex] = authorId;

            string nextTeam = team;
            if (ProgHelpers.pickturn == authorId)
            {
                nextTeam = team.Equals("team1") ? "team2" : "team1"; //Find out which team is picking
                //ProgHelpers.pickturn = authorId; -- This was most likely a false statement
                ProgHelpers.pickturn = c1i; //Place new captain in the picking turn
            }

            ProgHelpers.persistedData.RemoveCaptain(authorId, authorUseName); //Statistics manipulation, old cap stats removed
            ProgHelpers.persistedData.AddCaptain(c1i, c1n); //Statistics manipulation, new cap gets stats

            if (team.Equals("team1"))
            {
                ProgHelpers.captain1 = c1n;
                ProgHelpers.captain1id = c1i;
                ProgHelpers.team1.Clear();
                ProgHelpers.team1ids.Clear();
                ProgHelpers.team1.Add(c1n);
                ProgHelpers.team1ids.Add(c1i);
            }
            else
            {
                ProgHelpers.captain2 = c1n;
                ProgHelpers.captain2id = c1i;
                ProgHelpers.team2.Clear();
                ProgHelpers.team2ids.Clear();
                ProgHelpers.team2.Add(c1n);
                ProgHelpers.team2ids.Add(c1i);
            }
                http.CreateMessage(ProgHelpers.channelsnowflake, $"<@{authorId}> " + ProgHelpers.locale["relinq.successful"] + $" <@{c1i}>");
            if (ProgHelpers.team1ids.Count > 2)
            {
                http.CreateMessage(ProgHelpers.channelsnowflake, $"<@{authorId}> " + ProgHelpers.locale["pickPhase." + (nextTeam) + "Turn"] + " <@" + ProgHelpers.pickturn + "> \n " + ProgHelpers.locale["pickPhase.unpicked"] + " \n" + string.Join("\n", ProgHelpers.draftchatnames.Cast<string>().ToArray()));
            }
            else
            {
                List<string> phlist = new List<string>();
                int count = 0;
                foreach (string item in ProgHelpers.queue)
                {
                    phlist.Add(count.ToString() + " - " + item);
                    count++;
                }

                var pickmodeStr = "";
                if (ProgHelpers.pickMode == 1)
                {
                    pickmodeStr = ProgHelpers.locale["pickPhase.pickmodeinst1"];
                }
                else
                {
                    pickmodeStr = ProgHelpers.locale["pickPhase.pickmodeinst2"];
                }

                http.CreateMessage(ProgHelpers.channelsnowflake, ProgHelpers.locale["pickPhase.started"] + " " + "<@" + ProgHelpers.captain1id + ">" + "\n"
                                          + ProgHelpers.locale["pickPhase.team2Captain"] + " " + "<@" + ProgHelpers.captain2id + ">" + "\n" + ProgHelpers.locale["pickPhase.instructions"]+"\n"+ pickmodeStr
                                          + "\n \n" + string.Join("\n", phlist.Cast<string>().ToArray()));
            }


        }

        private bool PickTeamMember(DiscordUser author, string msg, List<string> teamIds, List<string> team, string nextCaptain)
        {
            string[] msgsp = msg.Split(null);
            int selectedIndex = 0;
            var selectedPlayerId = "";
            var selectedPlayerName = "";
            if (!Int32.TryParse(msgsp[1].Trim(), out selectedIndex))
            {
                // ignore since it was garbage   
            }
            if (ProgHelpers.queueids.ElementAtOrDefault(selectedIndex) == null)
            {
                http.CreateMessage(ProgHelpers.channelsnowflake, $"<@{author.Id}> " + ProgHelpers.locale["pickPhase.unknownIndex"]);
                return false;
            }

            selectedPlayerId = ProgHelpers.queueids.ElementAtOrDefault(selectedIndex);
            selectedPlayerName = ProgHelpers.queue.ElementAtOrDefault(selectedIndex);

            if (ProgHelpers.team1ids.IndexOf(selectedPlayerId) > -1 || ProgHelpers.team2ids.IndexOf(selectedPlayerId) > -1)
            {
                http.CreateMessage(ProgHelpers.channelsnowflake, $"<@{author.Id}> " + ProgHelpers.locale["pickPhase.alreadyPicked"]);
                return false;
            }

            teamIds.Add(selectedPlayerId);
            team.Add(selectedPlayerName);

            int finderremover = ProgHelpers.draftchatids.IndexOf(selectedPlayerId);
            ProgHelpers.draftchatnames.RemoveAt(finderremover);
            ProgHelpers.draftchatids.RemoveAt(finderremover);

            Console.WriteLine(ProgHelpers.draftchatnames.Cast<string>().ToArray());

            // add thin kid (the first pick)
            if (ProgHelpers.team1.Count + ProgHelpers.team2.Count == 3)
            {
                ProgHelpers.persistedData.AddThinKid(selectedPlayerId, selectedPlayerName);
                Console.WriteLine("Thin kid selected (" + selectedPlayerName + ")");
            }

            ProgHelpers.pickturn = nextCaptain;

            return true;
        }

        private void PickFatKid(List<string> teamids, List<string> teamNames)
        {
            var remainingPlayer = ProgHelpers.draftchatids.FirstOrDefault();
            var remainingPlayerIndex = ProgHelpers.queueids.IndexOf(remainingPlayer);
            var remainingPlayername = ProgHelpers.queue.ElementAtOrDefault(remainingPlayerIndex);

            //Put remaining player in picking team (pickturn has already gotten a new value above)
            teamNames.Add(remainingPlayername);
            teamids.Add(remainingPlayer);
            ProgHelpers.persistedData.AddFatKid(remainingPlayer, remainingPlayername);
            Console.WriteLine("Fat kid selected (" + remainingPlayername + ")");

            //Clear draftchat names (you could say this is redundant but..)
            ProgHelpers.draftchatnames.Clear();
            ProgHelpers.draftchatids.Clear();
        }

        private async Task CmdPick(Shard shard, DiscordMessage message)
        {
            if (ProgHelpers.pickMode == 1)
            {
                try
                {
                    DiscordUser author = message.Author;
                    string messageAuthorId = author.Id.Id.ToString();

                    // verify message sender
                    if (messageAuthorId != ProgHelpers.pickturn)
                    {
                        if (messageAuthorId == ProgHelpers.captain2id || messageAuthorId == ProgHelpers.captain1id)
                        {
                            Console.WriteLine("!pick -- Not your turn (" + author.Username + ")");
                            await http.CreateMessage(message.ChannelId, $"<@{author.Id}> " + ProgHelpers.locale["pickPhase.notYourTurn"]);
                        }
                        else
                        {
                            Console.WriteLine("!pick -- Not Captain (" + author.Username + ")");
                            await http.CreateMessage(message.ChannelId, $"<@{author.Id}> " + ProgHelpers.locale["pickPhase.notCaptain"]);
                        }
                        return;
                    }

                    //Execute team pick, declare helpers
                    string nextTeam = null;
                    bool pickSuccessful = false;

                    if (ProgHelpers.pickturn == ProgHelpers.captain1id)
                    {
                        //TEAM 1 PICKING PHASE

                        if (ProgHelpers.team1ids.Count + ProgHelpers.team2ids.Count == 2)
                        {
                            //FIRSTPICK

                            //Only pick one because it is the first player pick
                            //Team 1 is always the first picker
                            nextTeam = "team2";
                            pickSuccessful = PickTeamMember(author, message.Content, ProgHelpers.team1ids, ProgHelpers.team1, ProgHelpers.captain2id);
                        }
                        else 
                        {
                            //NORMAL PICK

                            //If team has already picked one in the turn, move the next turn, else proceed with current team
                            if (ProgHelpers.team1ids.Count == ProgHelpers.team2ids.Count)
                            {
                                Console.WriteLine("-- !# Team 1 Picked 2/2 of turn --> Next team #! --");
                                nextTeam = "team2";
                                pickSuccessful = PickTeamMember(author, message.Content, ProgHelpers.team1ids, ProgHelpers.team1, ProgHelpers.captain2id);
                            }
                            else
                            {
                                Console.WriteLine("-- !# Team 1 Picked 1/2 of turn --> Same team #! --");
                                nextTeam = "team1";
                                pickSuccessful = PickTeamMember(author, message.Content, ProgHelpers.team1ids, ProgHelpers.team1, ProgHelpers.captain1id);
                            }

                        }
                    }
                    else
                    {
                        //TEAM 2 PICKING PHASE

                        //If team has already picked one in the turn, move the next turn, else proceed with current team
                        if (ProgHelpers.team2ids.Count == ProgHelpers.team1ids.Count)
                        {
                            Console.WriteLine("-- !# Team 2 Picked 2/2 of turn --> Next team #! --");
                            nextTeam = "team1";
                            pickSuccessful = PickTeamMember(author, message.Content, ProgHelpers.team2ids, ProgHelpers.team2, ProgHelpers.captain1id);
                        }
                        else
                        {
                            Console.WriteLine("-- !# Team 2 Picked 1/2 of turn --> Same team #! --");
                            nextTeam = "team2";
                            pickSuccessful = PickTeamMember(author, message.Content, ProgHelpers.team2ids, ProgHelpers.team2, ProgHelpers.captain2id);
                        }
                    }
                    if (!pickSuccessful) return;

                    // automatically pick the fat kid
                    if (ProgHelpers.team1ids.Count + ProgHelpers.team2ids.Count == (ProgHelpers.qcount - 1))
                    {
                        if (ProgHelpers.pickturn == ProgHelpers.captain1id)
                        {
                            PickFatKid(ProgHelpers.team1ids, ProgHelpers.team1);
                        }
                        else
                        {
                            PickFatKid(ProgHelpers.team2ids, ProgHelpers.team2);
                        }
                    }
                    else
                    {
                        await http.CreateMessage(message.ChannelId, $"<@{author.Id}> " + ProgHelpers.locale["pickPhase." + nextTeam + "Turn"] + " <@" + ProgHelpers.pickturn + "> \n " + ProgHelpers.locale["pickPhase.unpicked"] + " \n" + string.Join("\n", ProgHelpers.draftchatnames.Cast<string>().ToArray()));
                    }

                    // if all players have been picked show the teams and reset bot status
                    if (ProgHelpers.team1ids.Count + ProgHelpers.team2ids.Count == ProgHelpers.qcount)
                    {

                        ProgHelpers.persistedData.AddHighScores(ProgHelpers.team1ids.Concat(ProgHelpers.team2ids).ToList(), ProgHelpers.team1.Concat(ProgHelpers.team2).ToList());
                        await http.CreateMessage(ProgHelpers.channelsnowflake, new CreateMessageOptions()
                         .SetEmbed(new EmbedOptions()
                         .SetTitle($"kitsun8's Gatheriino, " + ProgHelpers.locale["status.pickedTeams"])
                         .SetFooter("K8Gatheriino, " + ProgHelpers.txtversion)
                         .SetColor(DiscordColor.FromHexadecimal(0xff9933))
                         .AddField("Team1: ", string.Join("\n", ProgHelpers.team1.Cast<string>().ToArray()), true)
                         .AddField("Team2: ", string.Join("\n", ProgHelpers.team2.Cast<string>().ToArray()), true)
                          ));

                        //clear other lists as well, resetting bot to default values, ready for another round!
                        ResetQueue();
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("!# Error in CmdPick: " + e.ToString());
                }
            }
            else
            {
                // ORIGINAL PICKPHASE MODE
                try
                {
                    DiscordUser author = message.Author;
                    string messageAuthorId = author.Id.Id.ToString();

                    // verify message sender
                    if (messageAuthorId != ProgHelpers.pickturn)
                    {
                        if (messageAuthorId == ProgHelpers.captain2id || messageAuthorId == ProgHelpers.captain1id)
                        {
                            Console.WriteLine("!pick -- Not your turn (" + author.Username + ")");
                            await http.CreateMessage(message.ChannelId, $"<@{author.Id}> " + ProgHelpers.locale["pickPhase.notYourTurn"]);
                        }
                        else
                        {
                            Console.WriteLine("!pick -- Not Captain (" + author.Username + ")");
                            await http.CreateMessage(message.ChannelId, $"<@{author.Id}> " + ProgHelpers.locale["pickPhase.notCaptain"]);
                        }
                        return;
                    }

                    // execute team pick
                    string nextTeam = null;
                    bool pickSuccessful = false;
                    if (ProgHelpers.pickturn == ProgHelpers.captain1id)
                    {
                        nextTeam = "team2";
                        pickSuccessful = PickTeamMember(author, message.Content, ProgHelpers.team1ids, ProgHelpers.team1, ProgHelpers.captain2id);
                    }
                    else
                    {
                        nextTeam = "team1";
                        pickSuccessful = PickTeamMember(author, message.Content, ProgHelpers.team2ids, ProgHelpers.team2, ProgHelpers.captain1id);
                    }
                    if (!pickSuccessful) return;

                    // automatically pick the fat kid
                    if (ProgHelpers.team1ids.Count + ProgHelpers.team2ids.Count == (ProgHelpers.qcount - 1))
                    {
                        if (ProgHelpers.pickturn == ProgHelpers.captain1id)
                        {
                            PickFatKid(ProgHelpers.team1ids, ProgHelpers.team1);
                        }
                        else
                        {
                            PickFatKid(ProgHelpers.team2ids, ProgHelpers.team2);
                        }
                    }
                    else
                    {
                        await http.CreateMessage(message.ChannelId, $"<@{author.Id}> " + ProgHelpers.locale["pickPhase." + nextTeam + "Turn"] + " <@" + ProgHelpers.pickturn + "> \n " + ProgHelpers.locale["pickPhase.unpicked"] + " \n" + string.Join("\n", ProgHelpers.draftchatnames.Cast<string>().ToArray()));
                    }

                    // if all players have been picked show the teams and reset bot status
                    if (ProgHelpers.team1ids.Count + ProgHelpers.team2ids.Count == ProgHelpers.qcount)
                    {

                        ProgHelpers.persistedData.AddHighScores(ProgHelpers.team1ids.Concat(ProgHelpers.team2ids).ToList(), ProgHelpers.team1.Concat(ProgHelpers.team2).ToList());
                        await http.CreateMessage(ProgHelpers.channelsnowflake, new CreateMessageOptions()
                         .SetEmbed(new EmbedOptions()
                         .SetTitle($"kitsun8's Gatheriino, " + ProgHelpers.locale["status.pickedTeams"])
                         .SetFooter("K8Gatheriino, " + ProgHelpers.txtversion)
                         .SetColor(DiscordColor.FromHexadecimal(0xff9933))
                         .AddField("Team1: ", string.Join("\n", ProgHelpers.team1.Cast<string>().ToArray()), true)
                         .AddField("Team2: ", string.Join("\n", ProgHelpers.team2.Cast<string>().ToArray()), true)
                          ));

                        //clear other lists as well, resetting bot to default values, ready for another round!
                        ResetQueue();
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("!# Error in CmdPick: " + e.ToString());
                }
            }

        }


        private static void ResetQueue()
        {
            ProgHelpers.team1.Clear();
            ProgHelpers.team1ids.Clear();
            ProgHelpers.team2.Clear();
            ProgHelpers.team2ids.Clear();
            ProgHelpers.captain1 = "";
            ProgHelpers.captain2 = "";
            ProgHelpers.captain1id = "";
            ProgHelpers.captain2id = "";
            ProgHelpers.pickturn = "";
            ProgHelpers.draftchatids.Clear();
            ProgHelpers.draftchatnames.Clear();
            ProgHelpers.queue.Clear();
            ProgHelpers.queueids.Clear();
            ProgHelpers.readycheckids.Clear();
            ProgHelpers.readycheck.Clear();
        }

        private async Task CmdReady(Shard shard, DiscordMessage message)
        {
            
            try
            {
                var authorId = message.Author.Id.Id.ToString();
                var authorUserName = message.Author.Username.ToString();

                HandleReady(message, authorId, authorUserName);
            }
            catch (Exception e)
            {
                Console.WriteLine("EX-!ready" + " --- " + DateTime.Now);
                Console.WriteLine("!#DEBUG INFO FOR ERROR: " + e.ToString());
            }
        }

        private void HandleReady(DiscordMessage message, string authorId, string authorUserName)
        {
            if (ProgHelpers.queueids.IndexOf(authorId) != -1)
            {
                if (ProgHelpers.draftchatids.Count > 0)
                {
                    http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + ProgHelpers.locale["pickPhase.alreadyInProcess"] + " " + ProgHelpers.queue.Count.ToString() + "/" + ProgHelpers.qcount.ToString());
                    return;
                }
                //check if person has added himself in the queue
                if (ProgHelpers.queue.Count != ProgHelpers.qcount)
                {
                    http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + ProgHelpers.locale["queuePhase.notReadyYet"] + " " + ProgHelpers.queue.Count.ToString() + "/" + ProgHelpers.qcount.ToString());
                    return;
                }
                //01.08.2017 Check if person has ALREADY readied...
                var checkExists = ProgHelpers.readycheckids.FirstOrDefault(x => x == authorId);
                if (checkExists != null)
                {
                    //Person has already readied
                    http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + ProgHelpers.locale["readyPhase.alreadyMarkedReady"] + " " + ProgHelpers.readycheckids.Count.ToString() + "/" + ProgHelpers.qcount.ToString());
                    return;
                }
                //Proceed

                //place person in readycheck queue
                ProgHelpers.readycheckids.Add(authorId);
                ProgHelpers.readycheck.Add(authorUserName);
                http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + ProgHelpers.locale["readyPhase.ready"] + " " + ProgHelpers.readycheckids.Count.ToString() + "/" + ProgHelpers.qcount.ToString());
                //if readycheck completes the queue, start captainpick phase, clear readycheck queue in process
                if (ProgHelpers.readycheckids.Count == ProgHelpers.qcount)
                {
                    //dispose of the currenttimer
                    _tm.Dispose();
                    ProgHelpers._counter = 0;

                    //Dispose readychecks
                    ProgHelpers.readycheckids.Clear();
                    ProgHelpers.readycheck.Clear();

                    bool notAllNewKids = !ProgHelpers.persistedData.AreAllNewKids(ProgHelpers.queueids, ProgHelpers.newKidThreshold);
                    int retryCount = 0;

                    //Random captain 1
                    Random rnd = new Random();
                    int c1 = rnd.Next( ProgHelpers.queueids.Count );
                    if (notAllNewKids)
                    {
                        while (ProgHelpers.persistedData.IsNewKid(ProgHelpers.queueids[c1], ProgHelpers.newKidThreshold) &&
                            retryCount < ProgHelpers.giveupThreshold)
                        {
                            c1 = rnd.Next( ProgHelpers.queueids.Count );
                            retryCount++;
                        }
                    }
                    string c1n = "";
                    string c1i = "";

                    c1n = ProgHelpers.queue[c1];
                    c1i = ProgHelpers.queueids[c1];
                    ProgHelpers.queue.RemoveAt(c1);
                    ProgHelpers.queueids.RemoveAt(c1);
                    ProgHelpers.captain1 = c1n;
                    ProgHelpers.captain1id = c1i;
                    ProgHelpers.team1.Add(c1n);
                    ProgHelpers.team1ids.Add(c1i);

                    //Random captain 2
                    Random rnd2 = new Random();
                    int c2 = rnd2.Next(ProgHelpers.queueids.Count);
                    if (notAllNewKids)
                    {
                        while (ProgHelpers.persistedData.IsNewKid(ProgHelpers.queueids[c2], ProgHelpers.newKidThreshold ) &&
                            retryCount < ProgHelpers.giveupThreshold)
                        {
                            c2 = rnd.Next(ProgHelpers.queueids.Count);
                            retryCount++;
                        }
                    }
                    string c2n = "";
                    string c2i = "";

                    c2n = ProgHelpers.queue[c2];
                    c2i = ProgHelpers.queueids[c2];
                    ProgHelpers.queue.RemoveAt(c2);
                    ProgHelpers.queueids.RemoveAt(c2);
                    ProgHelpers.captain2 = c2n;
                    ProgHelpers.captain2id = c2i;
                    ProgHelpers.team2.Add(c2n);
                    ProgHelpers.team2ids.Add(c2i);

                    ProgHelpers.persistedData.AddCaptains(c1i, c1n, c2i, c2n);

                    //Workaround to keep the initial numbering active for whole draft
                    ProgHelpers.draftchatids.AddRange(ProgHelpers.queueids);
                    List<string> draftlist = new List<string>();
                    int qcount = 0;
                    foreach (string item in ProgHelpers.queue)
                    {
                        draftlist.Add(qcount.ToString() + " - " + item);
                        qcount++;
                    }
                    ProgHelpers.draftchatnames.AddRange(draftlist);

                    //2018-10: Type the actual name to log..
                    var txtdraftnames = String.Join(" , ", ProgHelpers.draftchatnames.ToArray());
                    Console.WriteLine("!-- STARTING PICKING PHASE --!");
                    Console.WriteLine("Pick phase, Captain Team 1: " + c1n + ", ID " + c1i);                //Team 1 Captain name and id
                    Console.WriteLine("Pick phase, Captain Team 2: " + c2n + ", ID " + c2i);                //Team 2 Captain name
                    Console.WriteLine("Pick phase, available players: " + txtdraftnames);                   //Available players and their pick #

                    ProgHelpers.pickturn = ProgHelpers.captain1id; //initial pickturn


                    List<string> phlist = new List<string>();
                    int count = 0;
                    foreach (string item in ProgHelpers.queue)
                    {
                        phlist.Add(count.ToString() + " - " + item);
                        count++;
                    }

                    http.CreateMessage(message.ChannelId, ProgHelpers.locale["pickPhase.started"] + " " + "<@" + c1i + ">" + "\n"
                                              + ProgHelpers.locale["pickPhase.team2Captain"] + " " + "<@" + c2i + ">" + "\n" + ProgHelpers.locale["pickPhase.instructions"]
                                              + "\n \n" + string.Join("\n", phlist.Cast<string>().ToArray()));
                }
            }
            else
            {
                http.CreateMessage(message.ChannelId, $"<@{message.Author.Id}> " + ProgHelpers.locale["pickPhase.alreadyInProcess"]);
            }
            Console.WriteLine("!ready" + " --- " + DateTime.Now);
        }

        private async Task CmdRemove(Shard shard, DiscordMessage message)
        {
            try
            {
                if (ProgHelpers.queue.Count == ProgHelpers.qcount)
                {
                    //too late to bail out
                    await http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + ProgHelpers.locale["pickPhase.cannotRemove"]);
                }
                else
                {
                    if (ProgHelpers.team1ids.Count + ProgHelpers.team2ids.Count > 0)
                    {
                        await http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + ProgHelpers.locale["readyPhase.cannotAdd"]);
                    }
                    else
                    {
                        //remove player from list
                        var aa = message.Author.Id.Id.ToString();
                        var bb = message.Author.Username.ToString();

                        if (ProgHelpers.queueids.IndexOf(aa) != -1)
                        {
                            var inx = ProgHelpers.queueids.IndexOf(aa); //Get index because discord name can change, id can not
                            ProgHelpers.queueids.Remove(aa);
                            ProgHelpers.queue.RemoveAt(inx);
                            //queue.Remove(message.Author.Username);

                            await http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + ProgHelpers.locale["queuePhase.removed"] + " " + ProgHelpers.queue.Count.ToString() + "/" + ProgHelpers.qcount.ToString());

                            Console.WriteLine("!remove - " + ProgHelpers.queue.Count.ToString() + "/" + ProgHelpers.qcount.ToString() + " --- " + DateTime.Now);
                        }
                        else
                        {
                            await http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + ProgHelpers.locale["queuePhase.notInQueue"]);
                           
                            Console.WriteLine("!remove - " + ProgHelpers.queue.Count.ToString() + "/" + ProgHelpers.qcount.ToString() + " --- " + DateTime.Now);
                        }
                    }


                }

            }
            catch (Exception)
            {
                Console.WriteLine("EX-!remove" + " --- " + DateTime.Now);
            }
        }

        private async Task CmdAdd(Shard shard, DiscordMessage message)
        {
            var authorId = message.Author.Id.Id.ToString();
            var authorUserName = message.Author.Username.ToString();
            try
            {
                HandleAdd(message, authorId, authorUserName);
            }
            catch (Exception e)
            {
                Console.WriteLine("EX-!add" + " --- " + DateTime.Now);
                Console.WriteLine("!#DEBUG INFO FOR ERROR: " + e.ToString());
            }
        }

        private void HandleAdd(DiscordMessage message, string authorId, string authorUserName)
        {
            if (ProgHelpers.queue == null)
            {
                return;
            }
            if (ProgHelpers.queue.Count == ProgHelpers.qcount)
            {
                //readycheck in process, cant add anymore
                http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + ProgHelpers.locale["pickPhase.alreadyInProcess"]);
                return;
            }
            //Additional check, check if the picking phase is in progress...
            if (ProgHelpers.team1ids.Count + ProgHelpers.team2ids.Count > 0)
            {
                http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + ProgHelpers.locale["readyPhase.cannotAdd"]);
                return;
            }
            var findx = ProgHelpers.queueids.Find(item => item == authorId);
            if (findx == null)
            {

                //add player to queue
                ProgHelpers.queueids.Add(authorId);
                ProgHelpers.queue.Add(authorUserName);
                http.CreateMessage(message.ChannelId, $"<@!{message.Author.Id}> " + ProgHelpers.locale["queuePhase.added"] + " " + ProgHelpers.queue.Count.ToString() + "/" + ProgHelpers.qcount.ToString());
                Console.WriteLine("!add - " + ProgHelpers.queue.Count.ToString() + "/" + ProgHelpers.qcount.ToString());


                //check if queue is full
                if (ProgHelpers.queue.Count == ProgHelpers.qcount)
                {
                    List<string> phlist = new List<string>();
                    foreach (string item in ProgHelpers.queueids)
                    {
                        phlist.Add("<@" + item + ">");
                    }
                    //if queue complete, announce readychecks
                    http.CreateMessage(message.ChannelId, ProgHelpers.locale["readyPhase.started"] + " \n" + string.Join("\t", phlist.Cast<string>().ToArray()));
                    StartTimer();
                }
            }
            else
            {
                //Player is already in queue
                http.CreateMessage(message.ChannelId, $"<@{message.Author.Id}> " + ProgHelpers.locale["queuePhase.alreadyInQueue"] + " " + ProgHelpers.queue.Count.ToString() + "/" + ProgHelpers.qcount.ToString());
                Console.WriteLine("!add - " + ProgHelpers.queue.Count.ToString() + "/" + ProgHelpers.qcount.ToString() + " --- " + DateTime.Now);
            }

        }

        private Tuple<string, string> ParseIdAndUsername(DiscordMessage message)
        {
            string id = null;
            string userName = null;

            var msg = message.Content;
            if (message.Mentions.Count > 0)
            {
                id = message.Mentions[0].Id.Id.ToString();
            }
            else if (msg.Split(null).Length < 2)
            {
                id = message.Author.Id.Id.ToString();
            }

            if (msg.Split(' ').Length > 1 && id == null)
            {
                userName = msg.Substring(msg.Split(' ')[0].Length + 1);
            }
            return Tuple.Create(id, userName);
        }

    }


}
