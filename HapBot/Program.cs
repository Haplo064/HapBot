using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Newtonsoft;
using Newtonsoft.Json;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace HapBot
{
    partial class Program
    {
        private static TwitchPubSub pubsub;
        public static ITwitchAPI API;
        public static List<Game> gameList;
        public static TwitchBot twitchBot;
        public static string channelSet = "";
        
        async static Task Main(string[] args)
        {
            
            gameList = new List<Game>();
            
            using (StreamReader r = new StreamReader("gameList.json"))
            {
                string json = r.ReadToEnd();
                gameList = JsonConvert.DeserializeObject<List<Game>>(json);
            }
            //new Program().Run();
            
            string password = "xxxx";
            string botUsername = "hap64_bot";

            twitchBot = new TwitchBot(botUsername, password);
            twitchBot.Start().SafeFireAndForget();

            await twitchBot.JoinChannel("haplo064");
            await twitchBot.SendMessage("haplo064", "HapBot is tentatively alive for now. v0.5");

            twitchBot.OnMessage += async (sender, twitchChatMessage) =>
            {
                try
                {
                    Console.WriteLine($"{twitchChatMessage.Sender} said '{twitchChatMessage.Message}'");

                    //Public Commands
                    if (twitchChatMessage.Message.StartsWith("!cheese"))
                    {
                        twitchBot.SendMessage(twitchChatMessage.Channel, $"🧀 🧀 🧀 Classic Haplo. 🧀 🧀 🧀");
                    }


                    if (twitchChatMessage.Message.StartsWith("!searchall"))
                    {
                        twitchBot.SendMessage(twitchChatMessage.Channel,
                            FindGameAll(gameList, twitchChatMessage.Message.Substring(11)));
                    }
                    else if (twitchChatMessage.Message.StartsWith("!search"))
                    {
                        twitchBot.SendMessage(twitchChatMessage.Channel,
                            FindGame(gameList, twitchChatMessage.Message.Substring(8)));
                    }

                    if (twitchChatMessage.Message.StartsWith("!help"))
                    {
                        twitchBot.SendMessage(twitchChatMessage.Channel,
                            "Find games with !search, vote for them with HapBux!");
                    }

                    //TODO: Remake funfact list
                    if (twitchChatMessage.Message.StartsWith("!funfact"))
                    {
                        var rand = new Random();
                        var randNum = rand.Next(0, 2);
                        var randGame = RandomGame(gameList);
                        if (randNum == 0)
                        {
                            twitchBot.SendMessage(twitchChatMessage.Channel,
                                $"Did you know that '{randGame.Name}'s release date was: {randGame.Release}?");
                        }
                        else
                        {
                            twitchBot.SendMessage(twitchChatMessage.Channel,
                                $"Did you know that there are {GenreCount(gameList, randGame.Genre)} English released games in the genre of '{randGame.Genre}'?");
                        }
                    }

                    if (twitchChatMessage.Sender == "haplo064")
                    {
                        if (twitchChatMessage.Message.StartsWith("!set"))
                        {
                            channelSet = twitchChatMessage.Channel;
                        }

                        if (twitchChatMessage.Message.StartsWith("!beat"))
                        {
                            var isInt = int.TryParse(twitchChatMessage.Message.Substring(6), out var gameInt);
                            if (isInt)
                            {
                                gameList[gameInt].Beaten ^= true;
                                Console.WriteLine($"State: {gameList[gameInt].Beaten}");
                                twitchBot.SendMessage(channelSet,
                                    $"I just flagged {gameList[gameInt].Name} status as beaten: {gameList[gameInt].Beaten}.");
                                SaveList(gameList);
                            }
                            else
                            {
                                twitchBot.SendMessage(channelSet, $"Wrong input. Don't fuck it up next time.");
                            }
                        }

                        if (twitchChatMessage.Message.StartsWith("!vote"))
                        {
                            var voteCount = 1;
                            var splits = twitchChatMessage.Message.Substring(6).Split(' ');
                            var isInt = int.TryParse(splits[0], out var gameInt);
                            if (isInt)
                            {
                                if (splits.Length == 2)
                                {
                                    var isInt2 = int.TryParse(splits[1], out voteCount);
                                    if (isInt2)
                                    {
                                        Vote(gameList, gameInt, voteCount);
                                        twitchBot.SendMessage(channelSet,
                                            $"I just added {voteCount} votes to: {gameList[gameInt].Name}. It is now at: {gameList[gameInt].Votes}");

                                    }
                                    else
                                    {
                                        twitchBot.SendMessage(channelSet, $"Wrong input. Don't fuck it up next time.");
                                    }
                                }
                                else
                                {
                                    Vote(gameList, gameInt, 1);
                                    twitchBot.SendMessage(channelSet,
                                        $"I just added {voteCount} votes to: {gameList[gameInt].Name}. It is now at: {gameList[gameInt].Votes}");
                                }

                            }
                            else
                            {
                                twitchBot.SendMessage(channelSet, $"Wrong input. Don't fuck it up next time.");
                            }

                            Task.WaitAll();
                            SaveList(gameList);
                            SaveVote(gameList);
                        }

                        if (twitchChatMessage.Message.StartsWith("!fix_vote"))
                        {
                            var rand = new Random();
                            var x = rand.Next(1, 11);

                            var splits = twitchChatMessage.Message.Substring(10).Split(' ');
                            if (splits.Length >= 2)
                            {
                                var message = "";
                                for (int i = 1; i < splits.Length; i++)
                                {
                                    message += splits[i];
                                    if (i < splits.Length)
                                    {
                                        message += " ";
                                    }
                                }

                                Console.WriteLine(message);
                                int.TryParse(splits[0], out var votes);
                                var shortList = FindGameList(gameList, message);

                                Console.WriteLine("LIST BELOW");
                                foreach (var s in shortList)
                                {
                                    Console.WriteLine(s.Name);
                                }

                                Console.WriteLine("LIST END");

                                var isInt = int.TryParse(message, out var gameInt);

                                if (isInt)
                                {
                                    Console.WriteLine("A");
                                    Vote(gameList, gameInt, votes);
                                    twitchBot.SendMessage(channelSet,
                                        $"I just added {votes} votes to: {gameList[gameInt].Name}. It is now at: {gameList[gameInt].Votes}");
                                    if (x > 5)
                                    {
                                        Vote(gameList, 0, rand.Next(1, 5));
                                    }
                                    Task.WaitAll();
                                    SaveList(gameList);
                                    SaveVote(gameList);
                                }
                                else if (shortList.Count == 1)
                                {
                                    Console.WriteLine("B");
                                    Vote(gameList, FindGameInt(gameList, shortList[0].Name), votes);
                                    twitchBot.SendMessage(channelSet,
                                        $"I just added {votes} votes to: {gameList[gameInt].Name}. It is now at: {gameList[gameInt].Votes}");
                                    if (x > 5)
                                    {
                                        Vote(gameList, 0, rand.Next(1, 5));
                                    }
                                    SaveList(gameList);
                                    SaveVote(gameList);
                                }
                                else
                                {
                                    Console.WriteLine("C");
                                    twitchBot.SendMessage(channelSet,
                                        $"I couldn't parse that as a game name, or the ID of a game. Haplo will add the votes manually shortly!");
                                }

                            }
                        }

                        if (twitchChatMessage.Message.StartsWith("!votelist"))
                        {
                            VoteList(gameList);
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.Write(e.Message);
                    twitchBot.SendMessage(channelSet, "I'm sorry Dave, I'm afraid I can't do that.");
                }

            };
            
            await Task.Delay(-1);
        }

        private async Task Run()
        {
            
            string channelId = "143962928";
            API = new TwitchAPI();
            API.Settings.ClientId = "xxxx";
            API.Settings.Secret = "xxxx";
            pubsub = new TwitchPubSub();
            pubsub.OnPubSubServiceConnected += OnPubSubServiceConnected;
            pubsub.OnPubSubServiceClosed += OnPubSubServiceClosed;
            pubsub.OnPubSubServiceError += OnPubSubServiceError;
            pubsub.OnListenResponse += onListenResponse;
            pubsub.OnStreamUp += onStreamUp;
            pubsub.OnStreamDown += onStreamDown;
            ListenToRewards(channelId);
            Console.WriteLine("Connecting Client");
            pubsub.Connect();
            Console.WriteLine("Client Connected");
            await Task.Delay(-1);

        }
        private void OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            Console.WriteLine("OnPubSubServiceError");
            Console.WriteLine(e.Exception.Source);
            Console.WriteLine(e.Exception.HResult);
            Console.WriteLine(e.Exception.StackTrace);
            Console.WriteLine(e.Exception.ToString());
            Console.WriteLine(e.Exception.Message);
        }

        private void OnPubSubServiceClosed(object sender, EventArgs e)
        {
            Console.WriteLine("Connection closed to pubsub server");
        }

        private void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            Console.WriteLine("Connecting to pubsub server");
            string oauth = "aaq7elkrarb7wyarfm021ox0owblwy";
            pubsub.SendTopics(oauth);
            Console.WriteLine("oauth sent to pubsub server");
        }
        
        private static void onListenResponse(object sender, OnListenResponseArgs e)
        {
            if (!e.Successful)
                throw new Exception($"Failed to listen! Response: {e.Response}");
            if (e.Successful)
                throw new Exception($"Successful listen! Response: {e.Response}");
        }
        
        private void ListenToRewards(string channelId)
        {
            pubsub.OnChannelPointsRewardRedeemed += onChannelPointsRewardRedeemed;
            pubsub.ListenToChannelPoints(channelId);
        }
        
        private async static void onChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            Console.WriteLine("Point redeemed");
            var rand = new Random();
            var x = rand.Next(1, 11);
            
            var gamevotes = 0;
            if (e.RewardRedeemed.Redemption.Reward.Id == "74bac035-afa8-4e20-8ed3-a57a05e99ae4")
            {
                gamevotes = 1;
            }
            if (e.RewardRedeemed.Redemption.Reward.Id == "a62c0254-21ee-4f99-b624-0a4ba112fca0")
            {
                gamevotes = 5;
            }
            if (e.RewardRedeemed.Redemption.Reward.Id == "e36d3f6e-1784-4e6a-9cde-d6ca46b04dfd")
            {
                gamevotes = 10;
            }
            if (gamevotes > 0)
            {
                var isInt = int.TryParse(e.RewardRedeemed.Redemption.UserInput, out var gameInt);
                var shortList = FindGameList(gameList, e.RewardRedeemed.Redemption.UserInput);
                
                if (isInt)
                {
                    Vote(gameList, gameInt, gamevotes);
                    await twitchBot.SendMessage(channelSet,
                        $"I just added {gamevotes} votes to: {gameList[gameInt].Name}. It is now at: {gameList[gameInt].Votes}");
                    if (x > 5)
                    {
                        Vote(gameList, 0, rand.Next(1, 5));
                    }
                    SaveList(gameList);
                    SaveVote(gameList);
                }
                else if (shortList.Count == 1)
                {
                    Vote(gameList, FindGameInt(gameList, shortList[0].Name), gamevotes);
                    await twitchBot.SendMessage(channelSet,
                        $"I just added {gamevotes} votes to: {gameList[gameInt].Name}. It is now at: {gameList[gameInt].Votes}");
                    if (x > 5)
                    {
                        Vote(gameList, 0, rand.Next(1, 5));
                    }
                    SaveList(gameList);
                    SaveVote(gameList);
                }
                else
                {
                    await twitchBot.SendMessage(channelSet, $"I couldn't parse that as a game name, or the ID of a game. Haplo will add the votes manually shortly!");
                }
            }

        }
        
        private static void onStreamUp(object sender, OnStreamUpArgs e)
        {
            Console.WriteLine($"Stream just went up! Play delay: {e.PlayDelay}, server time: {e.ServerTime}");
        }

        private static void onStreamDown(object sender, OnStreamDownArgs e)
        {
            Console.WriteLine($"Stream just went down! Server time: {e.ServerTime}");
        }
        
        public class Game
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("developer")]
            public string Developer { get; set; }

            [JsonProperty("genre")]
            public string Genre { get; set; }

            [JsonProperty("release")]
            public string Release { get; set; }

            [JsonProperty("beaten")]
            public bool Beaten { get; set; }
            
            [JsonProperty("votes")]
            public int Votes { get; set; }
        }
        
    }
}