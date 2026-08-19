using System.Windows;
using Pofus.Core.Settings;

namespace Pofus.Hud.Modules.Settings;

/// <summary>
/// Shown at startup when a known conflicting process is detected and the
/// user hasn't previously chosen to ignore this warning (FR-002).
/// </summary>
public partial class ConflictWarningWindow : Window
{
    private readonly IReadOnlyList<string> _conflictingProcesses;
    private readonly IProcessController _processController;
    private readonly IAppPreferencesStore _preferencesStore;

    public ConflictWarningWindow(
        IReadOnlyList<string> conflictingProcesses,
        IProcessController processController,
        IAppPreferencesStore preferencesStore)
    {
        InitializeComponent();
        _conflictingProcesses = conflictingProcesses;
        _processController = processController;
        _preferencesStore = preferencesStore;

        MessageText.Text =
            $"Le logiciel suivant est en cours d'exécution et peut entrer en conflit avec Pofus : " +
            $"{string.Join(", ", conflictingProcesses)}.";
    }

    private async void OnCloseConflictClick(object sender, RoutedEventArgs e)
    {
        var failures = new List<string>();
        foreach (var processName in _conflictingProcesses)
        {
            if (!_processController.TryKill(processName, out var error))
            {
                failures.Add($"{processName} ({error})");
            }
        }

        if (failures.Count > 0)
        {
            StatusText.Text = $"Échec de fermeture : {string.Join(", ", failures)}.";
            return;
        }

        await PersistIgnoreChoiceIfCheckedAsync();
        Close();
    }

    private async void OnContinueClick(object sender, RoutedEventArgs e)
    {
        await PersistIgnoreChoiceIfCheckedAsync();
        Close();
    }

    private async Task PersistIgnoreChoiceIfCheckedAsync()
    {
        if (IgnoreCheckBox.IsChecked != true)
        {
            return;
        }

        var preferences = await _preferencesStore.LoadAsync();
        preferences.IgnoreConflictWarning = true;
        await _preferencesStore.SaveAsync(preferences);
    }
}
