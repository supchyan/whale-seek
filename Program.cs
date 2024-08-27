using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace cit_profile_cleaner {
    public class App {
        static readonly Dictionary<string, string>? config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText("./properties/config.json"));
        static readonly Dictionary<string, string[]>? blackList = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText("./properties/blackList.json"));

        static string? DirsClearCommand = "echo '[ info ] Попытка отчистки оставшихся директорий в C:\\Users...'";
        public static void Main(string[] args) {
            Console.Clear();
            
            Console.ForegroundColor = ConsoleColor.DarkGreen;

            Console.WriteLine(@"            '||              '||                                 '||      ");
            Console.WriteLine(@"... ... ...  || ..    ....    ||    ....   ....    ....    ....   ||  ..  ");
            Console.WriteLine(@" ||  ||  |   ||' ||  '' .||   ||  .|...|| ||. '  .|...|| .|...||  || .'   ");
            Console.WriteLine(@"  ||| |||    ||  ||  .|' ||   ||  ||      . '|.. ||      ||       ||'|.   ");
            Console.WriteLine(@"   |   |    .||. ||. '|..'|' .||.  '|...' |'..|'  '|...'  '|...' .||. ||. ");

            FlushRegistry();
            FlushUserDirs();
            DeleteUserDirs();

            Console.ReadKey();
        }
        static void FlushRegistry() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Print("");
                Print("# PART 1", ConsoleColor.Gray);
                Print("[ info ] Выполняется отчистка данных в регистре...", ConsoleColor.DarkGreen);
                
                try {
                    var PROFILES_LIST = Registry.LocalMachine.OpenSubKey(config["RegistryProfilesPath"]);
                    
                    if(PROFILES_LIST == null) {
                        Print("[ error ] PROFILES_LIST не существует.", ConsoleColor.Red);
                        return;
                    }

                    foreach (var profileSubKey in PROFILES_LIST.GetSubKeyNames()) {
                        // путь до параметра пути к profileimage
                        // через него можно определить владельца ключа в регистре

                        var profileImagePath = Registry.GetValue($"{config["RegistryLocalMachine"]}{config["RegistryProfilesPath"]}{profileSubKey}", "ProfileImagePath", null)?.ToString();
                        
                        // фильтрует все ключи внутри профилей,
                        // чтобы найти только созданных вручную пользователей
                        if(profileImagePath != null) {
                            var profileUsername = (profileImagePath.ToString().Contains(config["UsersPath"])) ? profileImagePath.ToString().Replace(config["UsersPath"], "") : null;
                            if (profileUsername != null) {
                                if (!blackList["users"].Contains(profileUsername)) {
                                    Registry.LocalMachine.DeleteSubKey($"{config["RegistryProfilesPath"]}{profileSubKey}");
                                    Print($"[ removed ] {profileUsername} | {profileSubKey}", ConsoleColor.DarkGreen);
                                }
                                else {
                                    Print($"[ skipping ] BlackList | {profileUsername} | {profileSubKey}", ConsoleColor.DarkGray);
                                }
                            }
                        }
                        
                    }
                    PROFILES_LIST.Close();

                    Print("[ success ] Готово.", ConsoleColor.Green);
                }
                catch (Exception e) {
                    Print($"[ error ] {e.Message}", ConsoleColor.Red);
                    Console.ReadKey();
                }
            }
        }
        static void FlushUserDirs() {
            Print("");
            Print("# PART 2", ConsoleColor.Gray);
            Print($"[ info ] Выполняется отчистка данных в {config["UsersPath"]}...", ConsoleColor.DarkGreen);

            try {
                foreach (string? userDir in Directory.GetDirectories(config["UsersPath"]).Select(Path.GetFileName)) {
                    if(userDir == null) {
                        Print($"[ error ] {config["UsersPath"]} не существует.", ConsoleColor.Red);
                        return;
                    }
                    if (!IsUserInBlackList(userDir)) {
                        Print($"[ info ] Отчистка | {userDir}", ConsoleColor.DarkGreen);
                        
                        DirectoryInfo profileDir = new DirectoryInfo($"{config["UsersPath"]}{userDir}");
                        SetAttributesNormal(profileDir);
                        try
                        {
                            profileDir.Delete(true);
                        }
                        catch(Exception e)
                        {
                            Print($"[ warning ] {e.Message}", ConsoleColor.Yellow);
                            DirsClearCommand += $"; rm -r -force {profileDir}";
                        }
                        Print($"[ success ] Готово | {userDir}", ConsoleColor.Green);
                    }
                    else {
                        Print($"[ skipping ] BlackList | {userDir}", ConsoleColor.DarkGray);
                    }
                }

                Print($"[ success ] Готово.", ConsoleColor.Green);
            }
            catch (Exception e) {
                Print($"[ error ] {e.Message}", ConsoleColor.Red);
                Console.ReadKey();
            }
        }
        static void DeleteUserDirs()
        {
            Print("");
            Print("# PART 3", ConsoleColor.Gray);
            DirsClearCommand += "; echo '[ info ] Готово. Any key to exit...'";
            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo
            {
                FileName = @"powershell",
                Arguments = DirsClearCommand
            };
            proc.Start();
        }
        static bool IsUserInBlackList(string user) {
            foreach (var val in blackList["users"]) {
                if (val == user) return true;
            }

            return false;
        }
        static void SetAttributesNormal(DirectoryInfo dir) {
            foreach (var file in dir.GetFiles()) {
                file.Attributes = FileAttributes.Normal;
            }
        }
        static void Print(string msg, ConsoleColor color = ConsoleColor.DarkGreen) {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
        }
    }
}