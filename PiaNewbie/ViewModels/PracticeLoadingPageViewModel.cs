using CommunityToolkit.Mvvm.ComponentModel;
using PiaNewbie.Services;

namespace PiaNewbie.ViewModels;

public partial class PracticeLoadingPageViewModel : ObservableObject
{
    public static int TipCount => Tips.Length;

    private static readonly string[] Tips =
    [
        "1. MISS가 너무 많이 나온다면 다시 플레이해 보세요. 첫 연주는 MIDI 준비 때문에 타이밍이 어긋날 수 있습니다.",
        "2. 히트 라인에 노트가 닿을 때 홈 행 키(A~')를 눌러 보세요.",
        "3. RHYTHM 모드는 반주가 계속 재생됩니다. 판정은 키뷰어 바로 위에 표시됩니다.",
        "4. FLOW 모드는 노래가 조금씩 끊길 수 있습니다.",
        "5. MIDI 파일은 Musescore와 같은 사이트에서 구매 또는 다운로드할 수 있습니다.",
        "6. BPM이 빠른 노래는 연습하기 어려울 수 있습니다.",
        "7. 동시에 눌러야하는 것처럼 보이는 노트는 낮은 음에서 위로 연주하면 됩니다.",
        "8. RHYTHM 모드의 HIT 판정은 0.25초(250ms)입니다.",
        "9. 피아노는 타현악기입니다.",
        "10. 피아노의 범위는 선택한 트랙의 최저음과 최고음에 의존합니다.",
        "11. 등장하는 노트는 선택한 트랙을 따라 등장합니다. 원하지 않는 악기가 나온다면 다른 트랙을 선택해 보세요.",
        "12. 트랙이 너무 많다면 원하는 악기를 찾아 원본 악보를 확인해 보세요.",
        "13. MP3와 WAV같은 파일은 지원하지 못합니다.",
        "14. 실제 피아노 운지법은 신경쓰지 말고 자유롭게 노트를 따라가보세요.",
        "15. 가온다 라고 불리는 음은 C4로, 통상적으로 2옥타브 도에 해당하는 음정입니다.",
        "16. MIDI에서 가온다는 C3로 표기되는데, 컴퓨터는 0부터 세기 때문입니다.",
        "17. 막누르면 막누른만큼 MISS가 올라갑니다. 보고 누르세요.",
        "18. BPM이 120인 노래는 1분에 120박자, 즉 1초에 2박자가 나오는 템포입니다.",
        "19. 더 낮아질 수 없거나 높아질 수 없더라도 당황하지 마세요. 다음 노트는 아무 홈 행 키를 눌러도 진행됩니다.",
        "20. 키보드를 너무 세게 누르지 마세요. 키보드 가격을 생각해요.",
        "21. 맞게 누른것같은데 MISS가 나왔다면, 한번에 두개를 누르진 않았는지 점검해보세요.",
        "22. MISS가 너무 많이 나온다고 좌절하지 마세요. 키보드로는 아르페지오를 치기 어려울 수 있습니다.",
        "23. 글리산도는 피아노에서 손가락을 건반 위에서 미끄러뜨리며 연주하는 기법입니다. 키보드로는 표현하기 어려울 수 있습니다.",
        "24. Musescore에서는 보통 1~5달러 정도에 MIDI 파일을 판매합니다. 무료 MIDI 파일도 많이 있습니다.",
        "25. 착용형 오디오 기기를 사용하면 더 좋은 경험을 하실 수 있습니다.",
        "26. FLOW 모드에서는 반주가 나오지 않습니다.",
        "27. 일반적인 리듬게임의 판정 범위는 0.05초(50ms)입니다.",
        "28. 슈베르트의 마왕은 1초에 약 7~8번 건반을 치면 됩니다. 도전해보세요.",
        "29. MIDI 파일을 돈주고 구매했더라도 재배포는 저작권 문제가 있습니다. 구매한 MIDI 파일은 개인적으로 즐기는 용도로만 사용해주세요.",
        "30. 이런 팁들은 랜덤으로 표시됩니다. 여러 번 플레이하면서 50개의 다양한 팁들을 확인해 보세요.",
        "31. F5는 PC 리듬게임에서 주로 사용하는 리스타트 키입니다. 연습 중에 F5를 눌러보세요.",
        "32. 백스페이스를 눌러 리와인드 하는 기능은 없습니다.",
        "33. 키뷰어는 원래 계획에 없었습니다.",
        "34. 그냥 뭔가 이상하면 F5를 눌러 다시 시작하세요. 아직 불안정합니다.",
        "35. 모차르트의 곡은 선율 구조가 비교적 명확해, 피아노 롤에서 멜로디 흐름을 보기 좋은 편입니다.",
        "36. 베토벤은 고전주의와 낭만주의를 잇는 작곡가로 평가됩니다. 강한 리듬과 극적인 전개가 특징입니다.",
        "37. 슈베르트는 가곡의 왕으로 불립니다. 노래하듯 흐르는 선율이 많아 멜로디 연습에 잘 어울립니다.",
        "38. 쇼팽은 피아노의 시인이라 불립니다. 섬세한 선율과 장식음이 많은 피아노곡으로 유명합니다.",
        "39. 리스트의 곡은 넓은 음역을 자주 사용합니다. 자기 손 크기 자랑하는거에요.",
        "40. 저는 손을 쫙 벌려도 도에서 다음 도까지 간신히 닿아서 피아노를 포기했어요. 아쉽게 됐어요.",
        "41. 바이올린 E현을 조율할 때에는 건드리지 마세요. 아주 신중한 작업입니다.",
        "42. 비발디는 바로크 시대의 작곡가로, 사계 중 봄이 특히 널리 알려져 있습니다.",
        "43. 오르간도 같은 건반 악기지만 피아노와 달리 관악기의 원리를 이용합니다.",
        "44. 클래식 곡의 저작권이 만료되었더라도, MIDI 편곡 파일 자체에는 별도 권리가 있을 수 있습니다.",
        "45. 베토벤 바이러스는 사실 베토벤의 곡이 아닙니다.",
        "46. 학교 근처에는 고양이들이 많습니다.",
        "47. 사람들은 베토벤을 귀머거리라며 비웃었지만 그는 신경쓰지 않았습니다. 안들렸거든요.",
        "48. 높은 음에서 MISS가 자주 나온다면 새끼손가락을 '키에 올려보세요.",
        "49. 모든 음을 한 트랙에 넣어버린 MIDI는 한 레이어에 그려버린 그림과 같습니다. 그래도 이 프로그램은 적절한 음을 골라 연습할 수 있도록 만들어졌습니다.",
        "50. MIDI는 MuseScore Studio, REAPER, FL Studio, Cubase같은 프로그램으로 다룰 수 있습니다."
    ];

    private readonly NavigationService _navigation;
    private readonly PracticePreloadService _preload = new();

    public PracticeLoadingPageViewModel(NavigationService navigation)
    {
        _navigation = navigation;
    }

    [ObservableProperty]
    private string _title = "연습 준비 중";

    [ObservableProperty]
    private string _tipMessage = string.Empty;

    public async Task RunLoadingAsync()
    {
        TipMessage = Tips[Random.Shared.Next(Tips.Length)];

        var session = AppSession.Instance;
        var minDelay = Task.Delay(TimeSpan.FromSeconds(PracticePreloadService.MinimumLoadingSeconds));
        var preload = _preload.PrepareAsync(
            session.SelectedMode,
            session.CurrentSong,
            session.GuideNotes);

        await Task.WhenAll(minDelay, preload);
        _navigation.NavigateTo(AppPage.Practice);
    }
}
