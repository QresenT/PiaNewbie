# NeoTheresia와 PiaNewbie

## NeoTheresia를 WPF에 직접 넣을 수 없는 이유

[NeoTheresia](https://github.com/PolyMeilex/Neothesia)는 **Rust + wgpu**로 만든 **독립 실행형** MIDI 비주얼라이저입니다.

- NuGet / .NET 라이브러리 형태로 제공되지 않음
- `neothesia-core` 등은 앱 내부용 크레이트이며 WPF 임베드 API 없음
- **GPL-3.0** — 소스를 합치면 PiaNewbie 전체도 GPL 의무가 생길 수 있음

따라서 NeoTheresia **실행 파일을 창 안에 넣는 방식**은 별도 프로세스·윈도우 핸들 연동이 필요하고, Flow/Rhythm 연습 로직과 통합하기 어렵습니다.
힝 아쉽다
## PiaNewbie에서 쓰는 방식

| 항목 | 내용 |
|------|------|
| 렌더링 | **WebView2** + Canvas (`wwwroot/pianoroll/`) |
| 스타일 | NeoTheresia처럼 **낙하하는 둥근 노트 블록** + 피아노 건반 |
| 성능 | 브라우저 GPU 합성 (SkiaSharp 매 프레임 CPU 그리기 대체) |
| 연동 | `PostWebMessageAsJson`으로 C# ↔ JS 상태 동기화 |

NeoTheresia 앱을 **같이 쓰고 싶다면** [릴리스](https://github.com/PolyMeilex/Neothesia/releases)에서 별도로 실행하면 됩니다. PiaNewbie 연습 화면과는 분리된 프로그램입니다.
이정도면 잘 나온듯
## 요구 사항

- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)
