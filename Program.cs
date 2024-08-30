using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace cit_profile_cleaner {
    public class App {
        const string UsersPath = @"C:\Users\";
        const string RegistryLocalMachine = @"HKEY_LOCAL_MACHINE\";
        const string RegistryProfilesPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\";
        
        static readonly Dictionary<string, string[]>? BlackList = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(@".\config\black_list.json"));
        
        static string? DirsClearCommand = @"echo '[ info ] Попытка отчистки оставшихся директорий в C:\Users...'";
        
        static bool deleteProfessors = true;

        public static void Main(string[] args) {
            Console.Clear();

            Print(@"            '||              '||                                 '||      ");
            Print(@"... ... ...  || ..    ....    ||    ....   ....    ....    ....   ||  ..  ");
            Print(@" ||  ||  |   ||' ||  '' .||   ||  .|...|| ||. '  .|...|| .|...||  || .'   ");
            Print(@"  ||| |||    ||  ||  .|' ||   ||  ||      . '|.. ||      ||       ||'|.   ");
            Print(@"   |   |    .||. ||. '|..'|' .||.  '|...' |'..|'  '|...'  '|...' .||. ||. ");

            Print(@"Удалить профили преподавателей [ y / n ]:");

            var input = Console.ReadLine();
            if (input == "N" || input == "n" || input == "Т" || input == "т") {
                deleteProfessors = false;
            }

            Console.Clear();

            FlushRegistry();
            FlushUserDirs();
            DeleteUserDirs();

            Console.ReadKey();
        }
        static void FlushRegistry() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Print("# PART 1", ConsoleColor.Gray);
                Print("[ info ] Отчистка данных в регистре...", ConsoleColor.DarkGreen);
                
                try {
                    var PROFILES_LIST = Registry.LocalMachine.OpenSubKey(RegistryProfilesPath);
                    
                    if(PROFILES_LIST == null) {
                        Print("[ error ] PROFILES_LIST не существует.", ConsoleColor.Red);
                        return;
                    }

                    foreach (var profileSubKey in PROFILES_LIST.GetSubKeyNames()) {
                        // путь до параметра пути к profileimage
                        // через него можно определить владельца ключа в регистре

                        var profileImagePath = Registry.GetValue($"{RegistryLocalMachine}{RegistryProfilesPath}{profileSubKey}", "ProfileImagePath", null)?.ToString();
                        
                        // фильтрует все ключи внутри профилей,
                        // чтобы найти только созданных вручную пользователей
                        if(profileImagePath != null) {
                            var profileUsername = profileImagePath.ToString().Contains(UsersPath) ? profileImagePath.ToString().Replace(UsersPath, "") : null;
                            if (profileUsername != null) {
                                if (!BlackList["users"].Contains(profileUsername))
                                {
                                    if (!IsUserProfessor(profileUsername))
                                    {
                                        Registry.LocalMachine.DeleteSubKey($"{RegistryProfilesPath}{profileSubKey}");
                                        Print($"[ removed ] {profileUsername} | {profileSubKey}", ConsoleColor.DarkGreen);
                                    }
                                    else
                                    {
                                        Print($"[ skipping ] PrU | {profileUsername} | {profileSubKey}", ConsoleColor.DarkGray);
                                    }
                                    
                                }
                                else {
                                    Print($"[ skipping ] BLU | {profileUsername} | {profileSubKey}", ConsoleColor.DarkGray);
                                }
                            }
                        }
                        
                    }
                    PROFILES_LIST.Close();

                    Print("[ success ] Готово.", ConsoleColor.Green);
                }
                catch (Exception e) {
                    Print($"[ error ] {e}", ConsoleColor.Red);
                    Console.ReadKey();
                }
            }
        }
        static void FlushUserDirs() {
            Print("");
            Print("# PART 2", ConsoleColor.Gray);
            Print($"[ info ] Отчистка данных в {UsersPath}...", ConsoleColor.DarkGreen);

            try {
                foreach (string? userDir in Directory.GetDirectories(UsersPath).Select(Path.GetFileName)) {
                    if(userDir == null) {
                        Print($"[ error ] {UsersPath} не существует.", ConsoleColor.Red);
                        return;
                    }
                    if (!IsUserInBlackList(userDir))
                    {
                        if (!IsUserProfessor(userDir))
                        {
                            Print($"[ info ] Отчистка | {userDir}", ConsoleColor.DarkGreen);

                            DirectoryInfo profileDir = new DirectoryInfo($"{UsersPath}{userDir}");
                            SetAttributesNormal(profileDir);
                            try
                            {
                                profileDir.Delete(true);
                            }
                            catch (Exception e)
                            {
                                Print($"[ warning ] {e.Message}", ConsoleColor.Yellow);
                                DirsClearCommand += $"; rm -r -force {profileDir}";
                            }
                            Print($"[ success ] Готово | {userDir}", ConsoleColor.Green);
                        }
                        else
                        {
                            Print($"[ skipping ] PrU | {userDir}", ConsoleColor.DarkGray);
                        }
                        
                    }
                    else {
                        Print($"[ skipping ] BLU | {userDir}", ConsoleColor.DarkGray);
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
            foreach (var val in BlackList["users"]) {
                if (val == user) return true;
            }

            return false;
        }
        static bool IsUserProfessor(string user) {
            var parts = user.Split('.');

            if (parts[0].ToCharArray().Length <= 3)
            {
                if (!deleteProfessors)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
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