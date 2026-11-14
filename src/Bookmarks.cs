using System.Text.Json;

namespace CouchBrowser;

public record Bookmark(string Title, string Url);

/// <summary>
/// One shared bookmark list in %LOCALAPPDATA%\CouchBrowser\bookmarks.json.
/// Shared by private and normal tabs: a bookmark is an explicit save action,
/// not passive tracking, so it isn't covered by the private-mode no-persistence rule.
/// </summary>
public static class Bookmarks
{
    static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CouchBrowser", "bookmarks.json");

    public static List<Bookmark> Load()
    {
        try { return JsonSerializer.Deserialize<List<Bookmark>>(File.ReadAllText(Path)) ?? new(); }
        catch { return new(); }
    }

    public static void Add(string title, string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        var list = Load();
        if (list.Any(b => b.Url == url)) return;
        list.Add(new Bookmark(string.IsNullOrWhiteSpace(title) ? url : title, url));
        Save(list);
    }

    public static void Remove(string url)
    {
        var list = Load();
        list.RemoveAll(b => b.Url == url);
        Save(list);
    }

    static void Save(List<Bookmark> list)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.WriteAllText(Path, JsonSerializer.Serialize(list));
        }
        catch { }
    }
}
