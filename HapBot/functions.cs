using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Newtonsoft;
using Newtonsoft.Json;

namespace HapBot
{
    partial class Program
    {

        //How many beaten
        public static int Beaten(List<Game> games)
        {
            var total = 0;
            foreach (var g in games)
            {
                if (g.Beaten)
                {
                    total++;
                }
            }
            return total;
        }
        
        //How many left to beat
        public static int LeftToBeat(List<Game> games)
        {
            var total = 0;
            foreach (var g in games)
            {
                if (g.Beaten)
                {
                    total++;
                }
            }
            return games.Count - total;
        }

        //Returns a random game from the list
        public static Game RandomGame(List<Game> games)
        {
            var rand = new Random();
            var randNum = rand.Next(0, games.Count);

            return games[randNum];
        }

        public static Game GetGame(List<Game> games, int game)
        {
            if (game > 0 && game < games.Count+1)
            {
                return games[game-1];
            }

            return null;
        }

        //Gets a count of Genre in list
        public static int GenreCount(List<Game> games, string genre)
        {
            var total = 0;
            foreach (var g in games)
            {
                if (g.Genre==genre)
                {
                    total++;
                }
            }

            return total;
        }

        //Saves the list
        public static void SaveList(List<Game> games)
        {
            using (StreamWriter file = File.CreateText(@"gamelist.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, games);
            }
        }

        public static void SaveVote(List<Game> games)
        {
            var votedGames = VoteList(gameList);
            var limit = 0;
            var names = "";
            var votes = "";
            foreach (Game game in votedGames)
            {
                if (limit < 5)
                {
                    var n = game.Name;
                    if (n.Length > 23)
                    {
                        n = game.Name.Substring(0, 23);
                    }
                    names += n + "\n";
                    votes += game.Votes + "\n";
                    limit++;
                }
            }
            File.WriteAllText("voteList_games.txt",names);
            File.WriteAllText("voteList_votes.txt",votes);
        }

        
        public static string FindGame(List<Game> games, string find)
        {
            Console.WriteLine($"Search for: {find}");
            var g = "";
            var count = 0;
            for(int i = 0; i<games.Count; i++)
            {
                if (games[i].Name.ToLower().Contains(find.ToLower()) && !games[i].Beaten)
                {
                    g += $" {games[i].Name} [ID: {(i).ToString()}] [V:{games[i].Votes}]      ";
                    count++;
                };
                if (count > 4)
                {
                    g += "...";
                    break;
                }
            }

            if (count == 0)
            {
                g = "Nothing!";
            }
            return g;
        }
        public static string FindGameAll(List<Game> games, string find)
        {
            Console.WriteLine($"Search for: {find}");
            var g = "";
            var count = 0;
            for(int i = 0; i<games.Count; i++)
            {
                if (games[i].Name.ToLower().Contains(find.ToLower()))
                {
                    g += $" {games[i].Name} [ID: {(i).ToString()}] [V:{games[i].Votes}] [B:{games[i].Beaten}]      ";
                    count++;
                };
                if (count > 4)
                {
                    g += "...";
                    break;
                }
            }

            if (count == 0)
            {
                g = "Nothing!";
            }
            return g;
        }

        public static int FindGameInt(List<Game> games, string find)
        {
            var j = 0;
            for(int i = 0; i<games.Count; i++)
            {
                if (games[i].Name.ToLower().Contains(find.ToLower()))
                {
                    j = i;
                };
            }
            return j;
        }
        
        public static List<Game> FindGameList(List<Game> games, string find)
        {
            if (find.Length > 0)
            {
                find = find.Remove(find.Length - 1);
            }
            
            Console.WriteLine($"Search for: {find}");
            Console.WriteLine($"Length: {find.Length}");
            List<Game> g = new List<Game>();
            //g.AddRange(games.Where(game => game.Name.ToLower().Contains(find.ToLower())));
            for(int i = 0; i<games.Count; i++)
            {
                Console.WriteLine($"Comparing: {games[i].Name.ToLower()} | {find.ToLower()}");
                if (games[i].Name.ToLower().Contains(find.ToLower()))
                {
                    Console.WriteLine($"GOT ONE");
                    g.Add(games[i]);
                };
            }
            return g;
        }

        public static List<Game> VoteList(List<Game> games)
        {
            var voteList = new List<Game>();
            for(int i = 0; i<games.Count; i++)
            {
                if (!games[i].Beaten && games[i].Votes > 0)
                {
                    voteList.Add(games[i]);
                };
            }
            
            List<Game> SortedList = voteList.OrderByDescending(o=>o.Votes).ToList();
            return SortedList;
        }

        public async static void Vote(List<Game> games, int game, int votes)
        {
            //Check it's a valid int
            if (game >= 0 && game <= 278)
            {
                //check game isn't already beaten!
                if (games[game].Beaten)
                {
                    await twitchBot.SendMessage(channelSet, $"Haplo has already beaten {games[game].Name}, let me know what other game to add votes to!");
                }
                else
                {
                    games[game].Votes += votes;
                    await twitchBot.SendMessage(channelSet,$"({votes}) votes added to {games[game].Name}. Now at ({games[game].Votes}).");
                }
            }
        }
    }
}