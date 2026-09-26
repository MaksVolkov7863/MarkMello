using MarkMello.Presentation.ViewModels;

namespace MarkMello.Presentation.Tests;

public sealed partial class ShellViewModelTests
{
    [Fact]
    public async Task ApplyQuickDocumentEditInViewerModeUpdatesContentWithoutOpeningTwoPaneEditor()
    {
        var harness = CreateHarness();
        var path = Path.Combine(Path.GetTempPath(), "MarkMello.Tests", "quick_edit.md");
        harness.Loader.Sources[path] = CreateSource(path, "Hello original world");

        await harness.ViewModel.OpenPathAsync(path);

        Assert.False(harness.ViewModel.IsEditMode);
        Assert.False(harness.ViewModel.IsDirty);
        Assert.Same(harness.ViewModel, harness.ViewModel.ActiveDocumentContent);
        Assert.Equal("quick_edit.md", harness.ViewModel.TitleFileDisplayName);

        // Применяем быстрое редактирование инлайн в режиме просмотра
        var sourceTextChanged = false;
        harness.ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShellViewModel.SourceText))
            {
                sourceTextChanged = true;
            }
        };

        harness.ViewModel.ApplyQuickDocumentEdit("Hello edited world");

        Assert.True(sourceTextChanged);

        // Режим остаётся режимом просмотра (две панели НЕ открываются!)
        Assert.False(harness.ViewModel.IsEditMode);
        Assert.Same(harness.ViewModel, harness.ViewModel.ActiveDocumentContent);

        // Документ обновлён и помечен как изменённый
        Assert.True(harness.ViewModel.IsDirty);
        Assert.Equal("quick_edit.md •", harness.ViewModel.TitleFileDisplayName);
        Assert.Equal("Hello edited world", harness.ViewModel.Document?.Content);
        Assert.Equal("Hello edited world", harness.ViewModel.SourceText);
        Assert.True(harness.ViewModel.SaveCommand.CanExecute(null));

        // Вкладка также помечена dirty
        Assert.True(harness.ViewModel.OpenDocuments.ActiveTab?.IsDirty);

        // Сохранение документа
        await harness.ViewModel.SaveCommand.ExecuteAsync(null);

        // После сохранения статус dirty сброшен
        Assert.False(harness.ViewModel.IsDirty);
        Assert.Equal("quick_edit.md", harness.ViewModel.TitleFileDisplayName);
        var save = Assert.Single(harness.DocumentSaver.Saves);
        Assert.Equal(path, save.Path);
        Assert.Equal("Hello edited world", save.Content);
    }

    [Fact]
    public async Task ApplyQuickDocumentEditThenToggleEditModeKeepsUpdatedText()
    {
        var harness = CreateHarness();
        var path = Path.Combine(Path.GetTempPath(), "MarkMello.Tests", "quick_edit_switch.md");
        harness.Loader.Sources[path] = CreateSource(path, "Start text");

        await harness.ViewModel.OpenPathAsync(path);

        harness.ViewModel.ApplyQuickDocumentEdit("Updated text via quick edit");

        // Пользователь решает позже переключиться в полный двухпанельный режим
        await harness.ViewModel.ToggleEditModeCommand.ExecuteAsync(null);

        Assert.True(harness.ViewModel.IsEditMode);
        Assert.NotNull(harness.ViewModel.EditorSession);
        Assert.Equal("Updated text via quick edit", harness.ViewModel.EditorSession.SourceText);
        Assert.True(harness.ViewModel.IsDirty);
    }

    [Fact]
    public async Task CreateNewDocumentHasSaveButtonAndAppMenuAndAllowsToggleEditMode()
    {
        var harness = CreateHarness();

        await harness.ViewModel.CreateNewDocumentCommand.ExecuteAsync(null);

        Assert.True(harness.ViewModel.IsEditMode);
        Assert.True(harness.ViewModel.ShowsAppMenuControl);
        Assert.False(harness.ViewModel.ShowsFloatingAppMenuButton);
        Assert.True(harness.ViewModel.ShowsSaveButton);
        Assert.True(harness.ViewModel.SaveCommand.CanExecute(null));
        Assert.True(harness.ViewModel.ShowsEditToggle);
        Assert.True(harness.ViewModel.ToggleEditModeCommand.CanExecute(null));

        harness.ViewModel.EditorSession!.SourceText = "# My New Document";

        var savePath = Path.Combine(Path.GetTempPath(), "MarkMello.Tests", "my_new_doc.md");
        harness.FilePicker.SavePath = savePath;

        await harness.ViewModel.SaveCommand.ExecuteAsync(null);

        var save = Assert.Single(harness.DocumentSaver.Saves);
        Assert.Equal(savePath, save.Path);
        Assert.Equal("# My New Document", save.Content);
        Assert.False(harness.ViewModel.IsDirty);
        Assert.Equal("my_new_doc.md", harness.ViewModel.TitleFileDisplayName);
    }
}
