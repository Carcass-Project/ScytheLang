using System;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using Pastel;
using Octokit;

public class Program
{
    public static async void UpgradeThread()
    {
       
    }
    public static void ProgressBar(int min, int max, int timeBetween)
    {
        if(timeBetween > 0)
        {
            Console.WriteLine("");
            Console.Write("[");
            int r, g, b;
            for(int i = min; i < max; i++)
            {
                

                Console.Write('■');
                Task.Delay(timeBetween);
            }
            Console.ResetColor();
            Console.Write(']');
        }
    }

    public static String Rainbow(Int32 numOfSteps, Int32 step)
    {
        var r = 0.0;
        var g = 0.0;
        var b = 0.0;
        var h = (Double)step / numOfSteps;
        var i = (Int32)(h * 6);
        var f = h * 6.0 - i;
        var q = 1 - f;

        switch (i % 6)
        {
            case 0:
                r = 1;
                g = f;
                b = 0;
                break;
            case 1:
                r = q;
                g = 1;
                b = 0;
                break;
            case 2:
                r = 0;
                g = 1;
                b = f;
                break;
            case 3:
                r = 0;
                g = q;
                b = 1;
                break;
            case 4:
                r = f;
                g = 0;
                b = 1;
                break;
            case 5:
                r = 1;
                g = 0;
                b = q;
                break;
        }
        return "#" + ((Int32)(r * 255)).ToString("X2") + ((Int32)(g * 255)).ToString("X2") + ((Int32)(b * 255)).ToString("X2");
    }

    public static void DeleteDirectory(string target_dir)
    {
        string[] files = Directory.GetFiles(target_dir);
        string[] dirs = Directory.GetDirectories(target_dir);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            DeleteDirectory(dir);
        }

        Directory.Delete(target_dir, false);
    }
    public static void Main(string[] args)
    {
        if(args.Length > 0)
        {
            if(args[0] == "upgrade")
            {
                var client = new GitHubClient(new ProductHeaderValue("Carcass-Project"));
                var latestRelease = client.Repository.Release.GetLatest(466837999).Result;

                Console.WriteLine("Got latest release, " + latestRelease.Name + " from Github.");
                Console.WriteLine("Downloading Release....");


                Console.WriteLine("");
               
                int min = 0, max = 100;
                int r = 255, g = 255, b = 255;

                using (var hclient = new System.Net.WebClient())
                {
                    Console.WriteLine("");
                    hclient.Headers.Add("user-agent", "Carcass-Project");
                    hclient.DownloadFile("https://api.github.com/repos/Carcass-Project/ScytheLang/zipball/", "scythe-latest.zip");
                }

                Console.Write("[");
                for (int i = min; i < max; i++)
                {
                    Thread.Sleep(50);
                   
                    
                    Console.Write("■".Pastel(ColorTranslator.FromHtml(Rainbow(100, i))));
                    
                }
                Console.ResetColor();
                Console.Write(']');
                Console.WriteLine("");
                Console.WriteLine("Extracting Files...");

                System.IO.Compression.ZipFile.ExtractToDirectory("scythe-latest.zip", "Scythe-Latest");

                Console.Write("[");
                for (int i = min; i < max; i++)
                {
                    Thread.Sleep(20);


                    Console.Write("■".Pastel(ColorTranslator.FromHtml(Rainbow(100, i))));

                }
                Console.ResetColor();
                Console.Write(']');
                Console.WriteLine("");
                Console.WriteLine("Building Scythe from Source...");

                string dir = Directory.GetDirectories("Scythe-Latest").First();

                var proc = new Process();
                proc.StartInfo = new ProcessStartInfo("dotnet", "publish --configuration Release " + Path.Combine(dir, "Scythe.csproj"));
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();


                Console.Write("[");
                for (int i = min; i < max; i++)
                {
                    Thread.Sleep(50);


                    Console.Write("■".Pastel(ColorTranslator.FromHtml(Rainbow(100, i))));

                }
                Console.ResetColor();
                Console.Write(']');
                Console.WriteLine("");

                while(!proc.HasExited)
                {
                    ;
                }

    

                Thread.Sleep(1000);

                Console.WriteLine("Installing Scythe...");

                DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(Directory.GetDirectories(Path.Combine(dir, "bin", "Release", "net6.0")).First(), "publish"));
                directoryInfo.Attributes -= FileAttributes.ReadOnly;
                if(Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Scythe")))
                    DeleteDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Scythe"));

                Directory.Move(Path.Combine(Directory.GetDirectories(Path.Combine(dir, "bin", "Release", "net6.0")).First(), "publish"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Scythe"));
                File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Scythe", "_VERSION"), latestRelease.TagName);
                
                var name = "PATH";
                var scope = EnvironmentVariableTarget.Machine; // or User
                var oldValue = Environment.GetEnvironmentVariable(name, scope);
                var newValue = oldValue + ";" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Scythe");
                Environment.SetEnvironmentVariable(name, newValue, scope);

                Console.Write("[");
                for (int i = min; i < max; i++)
                {
                    Thread.Sleep(20);


                    Console.Write("■".Pastel(ColorTranslator.FromHtml(Rainbow(100, i))));

                }
                Console.ResetColor();
                Console.Write(']');
                Console.WriteLine("");

                Console.WriteLine("Cleaning Up..");

                File.Delete("scythe-latest.zip");
                DeleteDirectory("Scythe-Latest");

                Console.Write("[");
                for (int i = min; i < max; i++)
                {
                    Thread.Sleep(20);


                    Console.Write("■".Pastel(ColorTranslator.FromHtml(Rainbow(100, i))));

                }
                Console.ResetColor();
                Console.Write(']');
                Console.WriteLine("");

                Console.WriteLine("Scythe is now installed!");
            }
            if(args[0] == "version")
            {
                Console.WriteLine("Current Scythe Version: " + File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Scythe", "_VERSION")));
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("------ Syckle Usage Guide ------");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("upgrade -> Upgrades to the latest Scythe version based on your selected stream(default: stable).");
            //Console.WriteLine("stream <stable/nightly> -> Changes your Scythe version stream(default: stable).");
            //Console.WriteLine("remove -> Uninstalls Scythe from your system.");
            Console.WriteLine("version -> Shows your current Scythe and Syckle version.");
            Console.ResetColor();
        }
    }
}