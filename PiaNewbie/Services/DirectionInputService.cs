using PiaNewbie.Models;
using PiaNewbie.Utils;
using System.Windows.Input;

namespace PiaNewbie.Services;

public class DirectionInputService
{
    public bool IsMappedKey(Key key) => KeyboardLayoutHelper.IsMappedKey(key);

    /// <summary>
    /// 히트 라인의 <paramref name="toNote"/>를 칠 때,
    /// <paramref name="fromNote"/> → <paramref name="toNote"/> 음정 방향과 키 방향이 맞는지 검사합니다.
    /// (다음 음표가 아닌, 지금 맞춰야 할 음표 기준)
    /// </summary>
    public bool TryValidateInput(
        Key? anchorKey,
        Key pressedKey,
        GuideNote? fromNote,
        GuideNote toNote,
        out string message)
    {
        message = string.Empty;

        if (!KeyboardLayoutHelper.IsMappedKey(pressedKey))
        {
            message = "홈 행(A S D F G H J K L ; ')만 사용할 수 있습니다.";
            return false;
        }

        if (fromNote == null)
            return true;

        if (!anchorKey.HasValue || !KeyboardLayoutHelper.IsMappedKey(anchorKey.Value))
            return true;

        var dir = PianoKeyboardMap.GetPitchDirection(fromNote.Pitch, toNote.Pitch);
        if (!KeyboardLayoutHelper.IsPressAllowed(anchorKey.Value, pressedKey, dir))
        {
            message = $"방향 불일치. 허용: {KeyboardLayoutHelper.GetAllowedKeysHint(anchorKey.Value, dir)}";
            return false;
        }

        return true;
    }
}
