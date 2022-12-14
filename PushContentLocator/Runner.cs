using System.Net.Http.Headers;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;

namespace PushContentLocator;

internal class Runner
{
    private readonly FileInfo _sourceVioletsRoot;
    private readonly FileInfo _publishVioletsRoot;
    private readonly string _token;
    private readonly FileInfo _solutionPath;
    private DateTime _lastPush;
    private HttpClient _client;

    public Runner(FileInfo sourceVioletsRoot, FileInfo publishVioletsRoot, string token, FileInfo solutionPath)
    {
        _sourceVioletsRoot = sourceVioletsRoot;
        _publishVioletsRoot = publishVioletsRoot;
        _token = token;
        _solutionPath = solutionPath;
        
        InitHttpClient();
    }

    private void InitHttpClient()
    {
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Add("User-Agent", "request");
        _client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    public async Task Run()
    {
        while (true)
        {
            Console.WriteLine("Check");
            TimeSpan delay = TimeSpan.FromMinutes(1);

            HttpRequestMessage request = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://api.github.com/users/efimovnikita/events"),
            };

            using HttpResponseMessage response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode == false)
            {
                await Task.Delay(delay);
                continue;
            }
            string body = await response.Content.ReadAsStringAsync();

            List<PushEvent> pushEvents = null;
            try
            {
                pushEvents = JsonSerializer.Deserialize<List<PushEvent>>(body);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
            
            if (pushEvents == null)
            {
                await Task.Delay(delay);
                continue;
            }
            
            PushEvent pushEvent = pushEvents
                .Where(root => root.repo.id.Equals(545448538))
                .FirstOrDefault(root => root.type.Equals("PushEvent"));
            if (pushEvent == null)
            {
                await Task.Delay(delay);
                continue;
            }

            if (_lastPush < pushEvent.created_at)
            {
                Console.WriteLine("New push event found");

                BufferedCommandResult result = await Cli
                    .Wrap("git")
                    .WithWorkingDirectory(_solutionPath.FullName)
                    .WithValidation(CommandResultValidation.None)
                    .WithArguments("pull")
                    .ExecuteBufferedAsync();

                Console.WriteLine(result.StandardError);
                Console.WriteLine(result.StandardOutput);

                DeleteAllInViolets(_publishVioletsRoot.FullName);
                CopyFilesRecursively(_sourceVioletsRoot.FullName, _publishVioletsRoot.FullName);

                _lastPush = pushEvent.created_at;
            }
            
            await Task.Delay(delay);
        }
        // ReSharper disable once FunctionNeverReturns
    }
    
    private static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        //Now Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }

    private static void DeleteAllInViolets(string path)
    {
        DirectoryInfo info = new(path);
        foreach (FileInfo file in info.GetFiles())
        {
            file.Delete();
        }

        foreach (DirectoryInfo dir in info.GetDirectories())
        {
            dir.Delete(true);
        }
    }
}