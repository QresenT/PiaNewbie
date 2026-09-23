using PiaNewbie.Enums;
using PiaNewbie.Services;
using PiaNewbie.Utils;
using PiaNewbie.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;



namespace PiaNewbie.Views;



public partial class PracticePage : System.Windows.Controls.UserControl
{
    private readonly HashSet<Key> _heldMappedKeys = [];

    public PracticePage()

    {

        InitializeComponent();

        Loaded += OnLoaded;

        Unloaded += OnUnloaded;

    }



    private PracticePageViewModel? Vm => DataContext as PracticePageViewModel;



    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)

    {

        if (Vm == null) return;



        await Roll.EnsureInitializedAsync();



        Vm.RollRefreshRequested -= RefreshRoll;
        Vm.PracticeRestartRequested -= OnPracticeRestart;

        Vm.RollRefreshRequested += RefreshRoll;
        Vm.PracticeRestartRequested += OnPracticeRestart;

        Vm.Initialize();

        Roll.LoadSong(Vm.GuideNotes, Vm.BackgroundNotes, Vm.Mode, AppSession.Instance.Theme);

        RefreshRoll();

        Focusable = true;

        Focus();

    }



    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)

    {

        if (Vm == null)

            return;



        Vm.RollRefreshRequested -= RefreshRoll;
        Vm.PracticeRestartRequested -= OnPracticeRestart;

        _heldMappedKeys.Clear();
        AppSession.Instance.Audio.StopPlayback();

    }



    private void RefreshRoll()

    {

        if (Vm == null) return;



        var flow = Vm.GetFlowService();

        Roll.UpdatePlayback(

            Vm.CurrentIndex,

            Vm.CurrentTime,

            Vm.Mode,

            Vm.HighlightPitch,

            Vm.AnchorKey,

            flow?.PreviousNote,

            flow?.CurrentNote,

            Vm.GetPressableKeyIndices(),

            freezePlayback: Vm.FreezeRollPlayback,

            judgmentText: Vm.Mode == PracticeMode.Rhythm ? Vm.JudgmentText : null,

            judgmentSubtext: Vm.Mode == PracticeMode.Rhythm ? Vm.JudgmentSubtext : null,

            judgmentKind: Vm.Mode == PracticeMode.Rhythm ? Vm.JudgmentKind : null);

    }



    private void OnPracticeRestart()
    {
        if (Vm == null)
            return;

        _heldMappedKeys.Clear();
        Roll.LoadSong(Vm.GuideNotes, Vm.BackgroundNotes, Vm.Mode, AppSession.Instance.Theme);
        RefreshRoll();
        Focus();
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (Vm != null)
        {
            if (e.Key == Key.Escape)
            {
                Vm.GoSetupCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F5 && !e.IsRepeat)
            {
                Vm.RestartPracticeCommand.Execute(null);
                e.Handled = true;
                return;
            }
        }

        if (Vm != null && KeyboardLayoutHelper.IsMappedKey(e.Key))
        {
            // 긴 노트 홀드 시 OS 키 반복 → Miss 방지
            if (e.IsRepeat || _heldMappedKeys.Contains(e.Key))
            {
                e.Handled = true;
                return;
            }

            _heldMappedKeys.Add(e.Key);
            Vm.OnKeyDown(e.Key);
            e.Handled = true;
        }

        base.OnPreviewKeyDown(e);
    }

    protected override void OnPreviewKeyUp(System.Windows.Input.KeyEventArgs e)
    {
        if (KeyboardLayoutHelper.IsMappedKey(e.Key))
            _heldMappedKeys.Remove(e.Key);

        base.OnPreviewKeyUp(e);
    }
}


