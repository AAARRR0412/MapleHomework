<div align="center">

  # 🍁 MapleHomework

  <br>
  메이플스토리 숙제 관리부터 스펙 분석까지, **NEXON Open API** 기반 올인원 매니저

  <br>

  [![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)]()
  [![Platform](https://img.shields.io/badge/Platform-Windows-blue?style=flat-square)]()
  [![Framework](https://img.shields.io/badge/.NET-8.0-purple?style=flat-square)]()
  [![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen?style=flat-square)]()

  <p>
    <a href="#-key-features">Key Features</a> •
    <a href="#-tech-stack">Tech Stack</a> •
    <a href="#-getting-started">Getting Started</a> •
    <a href="#-screenshots">Screenshots</a>
  </p>

</div>

<br>

## ✨ Key Features

### 📅 Smart Routine Management (숙제 관리)
- **일일/주간/월간 자동 리셋**: 메이플스토리 서버 시간을 기준으로 숙제가 자동으로 초기화됩니다.
- **캐릭터별 트래킹**: `CharacterProfile` 모델을 통해 부캐릭터들의 숙제 현황까지 한눈에 파악하세요.
- **Drag & Drop**: 직관적인 드래그 앤 드롭으로 캐릭터 순서를 변경하고 관리할 수 있습니다.

### 🔗 Nexon Open API Integration (실시간 동기화)
- **자동 스펙 갱신**: 캐릭터 닉네임만 입력하면 `MapleApiService`가 전투력, 스탯, 장비 정보를 자동으로 불러옵니다.
- **OCID 기반 조회**: 넥슨 공식 API를 활용하여 정확하고 안전하게 데이터를 처리합니다.

### 💎 Boss Economics & Analytics (보스 결정석 분석)
- **수익 자동 계산**: 클리어한 보스를 체크하면, 난이도별 결정석 가격과 총 수익을 즉시 계산합니다.
- **파티원 분배**: 파티 격파 시 인원수(`PartySize`)에 따른 수익 분배 계산 기능을 제공합니다.
- **시각화된 리포트**: `ReportWindow`를 통해 주간 예상 수익과 달성률을 그래프와 통계로 확인하세요.

### 🖼️ High-Fidelity Tooltip Rendering (고해상도 툴팁)
- **인게임 완벽 구현**: `MapleTooltipRenderer` 엔진이 아이템, 스타포스, 잠재능력, 추옵 색상까지 인게임과 동일하게 렌더링합니다.
- **이미지 변환**: 장비 정보를 깔끔한 이미지로 저장하거나 공유할 수 있습니다.

### 📌 Useful Utilities
- **Overlay Mode**: 게임 화면 위에 띄워놓을 수 있는 미니멀한 오버레이 창을 지원합니다.
- **Theme Support**: 사용자 취향에 맞는 테마 설정이 가능합니다.

---

## 📸 Screenshots

| **Dashboard (Main)** | **Boss Analytics** |
|:---:|:---:|
| <img src="docs/screenshot_main.png" alt="Main Dashboard" width="400"/> | <img src="docs/screenshot_boss.png" alt="Boss Analytics" width="400"/> |
| *직관적인 숙제 체크리스트* | *결정석 수익 및 통계* |

| **Equipment Tooltip** | **Overlay Mode** |
|:---:|:---:|
| <img src="docs/screenshot_tooltip.png" alt="Item Tooltip" width="400"/> | <img src="docs/screenshot_overlay.png" alt="Overlay" width="400"/> |
| *실제 게임과 동일한 렌더링* | *방해되지 않는 미니 모드* |

> *Note: 위 이미지는 예시입니다. 실제 실행 화면을 캡처하여 `docs` 폴더에 넣고 경로를 수정해주세요.*

---

## 🛠 Tech Stack

**Core**
- ![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white) **C# 12**
- ![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white) **.NET 8.0 (WPF)**

**UI & UX**
- **WPF UI Library**: Modern Window style & Controls.
- **GDI+**: Custom Graphics Rendering for MapleStory Assets.
- **MVVM Pattern**: Clean Architecture with `MainViewModel`.

**Data & Connectivity**
- **Newtonsoft.Json**: Data Serialization.
- **HttpClient**: REST API Communication.

---

## 🚀 Getting Started

### Prerequisites
- Windows 10 or 11 (64-bit)
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation
1. [Releases](https://github.com/your-repo/releases) 페이지에서 최신 `MapleHomework.zip`을 다운로드합니다.
2. 원하는 폴더에 압축을 해제합니다.
3. `MapleHomework.exe`를 실행합니다.
4. 설정 창에서 **Nexon API Key**를 입력합니다. (넥슨 개발자 센터에서 발급 필요)

---

## 📝 License

This project is licensed under the **MIT License**. See the `LICENSE` file for details.
