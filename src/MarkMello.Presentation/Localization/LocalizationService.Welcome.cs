namespace MarkMello.Presentation.Localization;

public sealed partial class LocalizationService
{
    private static void AddWelcomeStrings()
    {
        English["WelcomeTagline"] = "A quiet place to read Markdown.";
        English["WelcomeCreateMd"] = "Create MD";
        English["WelcomeOpenFile"] = "Open file...";
        English["WelcomeOpenFolder"] = "Open Folder…";
        English["WelcomeDropHint"] = "or drop a .md file or a folder here";
        English["WelcomeSupport"] = "Support MarkMello";

        Russian["WelcomeTagline"] = "Тихое место для чтения Markdown.";
        Russian["WelcomeCreateMd"] = "Создать MD";
        Russian["WelcomeOpenFile"] = "Открыть файл...";
        Russian["WelcomeOpenFolder"] = "Открыть папку…";
        Russian["WelcomeDropHint"] = "или перетащите сюда .md файл или папку";
        Russian["WelcomeSupport"] = "Поддержать MarkMello";
    }
}
