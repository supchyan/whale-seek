using System.IO;
using System.Text.Json;

namespace cit_profile_cleaner {
    public class App {
        static readonly Dictionary<string, string> config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText("./properties/config.json"));
        static readonly Dictionary<string, string[]> blackList = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText("./properties/blackList.json"));
        
        public static void Main(string[] args) {
            Console.Clear();
            foreach (string userDir in Directory.GetDirectories(config["UsersPath"]).Select(Path.GetFileName)) {
                if (!IsUserInBlackList(userDir))
                    Console.WriteLine(userDir);
            }
        }
        static bool IsUserInBlackList(string user) {
            foreach (var val in blackList["users"]) {
                if (val == user) return true;
            }
            return false;
        }
    }
}