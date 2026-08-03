# Soulslike Demo

Unity로 제작한 3인칭 소울라이크 액션 게임 데모입니다. 어둠에 잠긴 아레나에서 보스 하나와 겨루는 단일 전투를 다룹니다.

졸업작품으로 제작되었으며, 넓은 월드를 만드는 대신 **소울라이크의 핵심 감각**(스태미나 관리, 무적 프레임 회피, 사망과 재도전)을 완성도 있게 구현하는 데 집중했습니다.

---

## 게임 개요

플레이어는 검을 든 기사가 되어 거대한 변이체와 싸웁니다. 체력은 100, 보스의 강공격 한 방이 34이므로 **세 대만 맞으면 죽습니다.** 무작정 몰아치는 대신 보스의 공격 동작을 읽고 구르기로 흘려낸 뒤 반격하는 것이 공략의 핵심입니다.

보스는 체력이 절반 이하로 떨어지면 포효와 함께 **2페이즈로 전환**되어 이동 속도와 연속 공격 횟수, 피해량이 모두 증가합니다.

## 조작

| 동작 | 키 |
|---|---|
| 이동 | W A S D |
| 시점 | 마우스 |
| 공격 (2단 콤보) | 마우스 좌클릭 |
| 구르기 (무적 프레임) | Space |
| 락온 | Q / 마우스 휠 클릭 |
| 달리기 | Shift |
| 점프 | F |
| 커서 잠금 해제 | Esc |

게임패드도 지원합니다 (공격 RB, 구르기 B, 락온 오른쪽 스틱 누르기).

## 실행 방법

### 빌드된 실행 파일

```
Builds/SoulslikeDemo/SoulslikeDemo.exe
```

1920x1080 테두리 없는 전체화면으로 실행됩니다. **Esc를 누르면 커서가 풀려** 창을 빠져나올 수 있습니다.

### 에디터에서 실행

`Assets/Scenes/Title.unity`를 열고 Play를 누르면 타이틀부터 전체 흐름을 확인할 수 있습니다. 전투만 바로 보려면 `Assets/Scenes/Arena.unity`를 여세요.

### 직접 빌드

메뉴에서 순서대로 실행합니다.

1. `Tools > Prepare For Build` — 디버그 로그 해제, 임시 컴포넌트 제거, 씬 등록 검증
2. `Tools > Build Windows Player` — `Builds/SoulslikeDemo/`에 출력

---

## 주요 구현

### 전투 시스템

Animator의 전이 조건에 의존하지 않고 **스크립트가 상태 전환을 직접 제어**합니다. 전투 로직이 한 곳에 모여 있어 타이밍 조정이 쉽습니다.

- **2단 콤보** — 공격 중 캔슬 윈도우(55% 지점) 이후 재입력 시 2타로 연결
- **무적 프레임** — 구르기 동작의 3~60% 구간에서 피격 판정 무시
- **선입력 버퍼** — 0.35초간 입력 유지. 동작이 끝나기 직전에 누른 명령이 유실되지 않음
- **스태미나** — 공격 22, 구르기 20, 달리기 초당 12 소모. 고갈 시 행동 불가
- **락온** — 대상을 응시하며 스트레이프 이동, 카메라가 부드럽게 추적

구르기는 클립 길이와 무관하게 **고정 시간(0.62초) 동안 고정 거리(3.2m)**를 이동합니다. 애니메이션 길이에 이동을 연동하면 클립을 교체할 때마다 속도가 달라지기 때문입니다.

### 보스 AI

대기 → 포효 → 추적 → 공격 → 쿨다운을 순환하는 유한 상태 기계입니다.

- NavMesh 기반 추적, 사거리 진입 시 두 패턴(휘두르기 60% / 펀치 40%) 확률 선택
- **연속 공격** — 1~2타(2페이즈 2~4타)를 0.18초 간격으로 이어침
- **전진 공격** — 휘두르며 앞으로 밀고 들어와 거리를 벌리는 회피를 차단
- **추적 회전 감쇠** — 판정 직전까지 추적하되 후반부는 회전 속도를 35%로 낮춰 회피 여지를 남김

### 연출

- **히트스톱** — 적중 순간 `Time.timeScale`을 0.05로 낮춰 충격 강조
- **카메라 흔들림** — 펄린 노이즈 기반 회전 흔들림. Cinemachine이 카메라 위치를 확정한 뒤 덮어씀
- **타격 이펙트** — 충돌 지점 불꽃 파티클, 피격 대상 발광
- **절차적 석재 텍스처** — 외부 이미지 없이 벽돌 패턴과 노이즈로 알베도·노멀맵 생성
- **야간 조명** — 방향광을 달빛 수준으로 낮추고 화톳불 8개의 점광원이 주광원 역할
- **포스트프로세싱** — ACES 톤매핑, 블룸, 비네트, 색보정

---

## 프로젝트 구조

```
Assets/
├── Scenes/          Title, Arena, Victory, Defeat
├── Scripts/
│   ├── Combat/      PlayerCombat, BossAI, Health, Stamina,
│   │                LockOnSystem, CombatFeedback, HitFlash
│   ├── UI/          PlayerHUD, BossHealthBar, GameEndScreen,
│   │                TitleMenu, ResultMenu
│   ├── Audio/       GameAudio
│   ├── GameManager.cs    승리/패배 시퀀스와 씬 전환
│   ├── GameSession.cs    씬을 넘나드는 클리어 타임 기록
│   ├── PauseAndCursor.cs Esc 커서 잠금 토글
│   └── TorchFlicker.cs   화톳불 조명 흔들림
├── Editor/          셋업 자동화 스크립트 (아래 참조)
├── Animation/       Mixamo 애니메이션 (Player / Boss)
├── Audio/           효과음, 배경음
└── StarterAssets/   Unity 공식 3인칭 컨트롤러 (일부 수정)
```

### StarterAssets 수정 사항

`ThirdPersonController.cs`에 다음 4개를 추가했습니다. 원본 동작은 유지됩니다.

| 추가 | 용도 |
|---|---|
| `MovementLocked` | 공격·구르기 중 이동 잠금 |
| `SprintBlocked` | 스태미나 고갈 시 달리기 차단 |
| `StrafeTarget` | 락온 시 스트레이프 이동 |
| `SetCameraAngles()` | 락온 카메라 각도 지정 |

---

## 에디터 도구

모든 셋업 작업을 `Tools` 메뉴에서 재실행할 수 있습니다. 수작업 배치 대신 스크립트로 자동화하여 **같은 결과를 언제든 재현**할 수 있게 했습니다.

### 씬 구성

| 메뉴 | 설명 |
|---|---|
| `Build Greybox Arena` | 원형 아레나, 벽, 기둥, 플레이어 배치 |
| `Dress Up Arena` | 석재 텍스처 생성 및 적용, 화톳불·잔해·성가퀴 배치 |
| `Setup Title Scene` | 타이틀 씬 생성 및 빌드 등록 |
| `Setup Victory Scene` | 승리 결산 씬 (클리어 타임) |
| `Setup Defeat Scene` | 패배 결산 씬 |

### 게임 요소

| 메뉴 | 설명 |
|---|---|
| `Setup Player Combat` | 애니메이터 전투 상태 추가, 컴포넌트 부착 |
| `Setup Stamina And LockOn` | 스태미나·락온·HUD 구성 |
| `Setup Boss` | 보스 애니메이터 생성, NavMesh 베이크, 보스 배치 |
| `Setup Game Loop` | GameManager, 보스 체력바, 결산 화면 |
| `Setup Audio` | `Assets/Audio` 파일을 이름으로 인식해 자동 연결 |
| `Apply Hard Boss Balance` | 보스 난이도 상향 수치 일괄 적용 |
| `Set Attack To Stationary` | 공격 중 전진 해제 |

### 애니메이션 / 모델

| 메뉴 | 설명 |
|---|---|
| `Configure Mixamo Imports` | Humanoid 리그 전환, 루프·루트모션 설정 |
| `Swap Player Model` | 로직은 유지한 채 캐릭터 모델만 교체 |
| `Apply New Player Animations` | 전투 클립 교체 (빈 상태 검증 포함) |
| `Extract Textures From Selected FBX` | FBX 내장 텍스처·재질 추출 |
| `Fix Mutant Rig` | 비인간형 모델의 본 매핑 수동 지정 |
| `Fix Boss Grounding` / `Fix Boss Float` | 보스 부유 문제 수정 |
| `Fix Jump Spam Animation` | 점프 연타 시 모션 누락 수정 |

### 조명 / 빌드 / 진단

| 메뉴 | 설명 |
|---|---|
| `Setup Visuals And Feel` | 타격감 컴포넌트, 텍스처 추출, 포스트프로세싱 |
| `Apply Night Lighting` | 야간 조명·안개·포스트프로세싱 값 적용 |
| `Fix Lighting Determinism` | 자동 GI 베이크 해제 (플레이마다 조명이 달라지는 문제) |
| `Configure Build Settings` | 씬 등록, 해상도, 제품명 설정 |
| `Prepare For Build` | 빌드 전 정리 및 검증 |
| `Build Windows Player` | Windows 빌드 |
| `Diagnose Boss` / `Diagnose Lighting` | 읽기 전용 진단 (안전) |

> 이름이 `Diagnose`, `Inspect`, `Dump`으로 시작하는 메뉴는 **읽기 전용**이라 언제 실행해도 안전합니다. 나머지는 씬이나 에셋을 변경합니다.

---

## 개발 중 해결한 기술적 문제

기록해둘 만한 것들입니다. 대부분 추측이 아니라 **실행 중 수치를 측정해서** 원인을 특정했습니다.

**보스 애니메이션이 재생되지 않음** — Mutant 모델은 왼손이 갈퀴 형태라 손가락 본이 없어 Humanoid 자동 매핑이 `LeftHand not found`로 실패했습니다. 22개 본을 명시적으로 매핑하고 skeleton 배열까지 직접 채워 해결했습니다. 이후 기본 포즈가 T-포즈가 아니어서 자세가 뒤틀린 문제는 에디터에서 Enforce T-Pose로 잡았습니다.

**보스가 공중에 떠서 이동** — 측정 결과 NavMesh가 실제 바닥보다 8.3cm 높게 생성되어 있었습니다(복셀 크기 기본값 문제). 복셀을 0.05로 줄여 재베이크하고 남은 오차를 `baseOffset`으로 상쇄했습니다. 여기에 Mixamo 클립의 루트 Y 기준이 Original로 되어 있어 체형이 다른 모델에서 어긋난 것도 함께 수정했습니다(Feet 기준으로 변경).

**카메라 흔들림이 체감되지 않음** — 카메라 추적점을 흔들고 있었는데 Cinemachine의 Damping(0.1~0.3)이 고주파 진동을 거의 전부 걸러내고 있었습니다. `DefaultExecutionOrder(10000)`으로 CinemachineBrain 이후에 실행되도록 하여 **카메라 트랜스폼을 직접 덮어쓰는** 방식으로 변경하고, 위치 대신 회전 흔들림 위주로 바꿨습니다.

**플레이할 때마다 배경 밝기가 다름** — 그레이박스 빌더가 NavMesh용으로 Static 플래그를 전부 켜면서 ContributeGI까지 활성화되어, 결과물도 나오지 않는 자동 GI 베이크가 백그라운드에서 돌고 있었습니다. GI 관련 플래그만 해제하여 해결했습니다.

**보스가 밀착한 플레이어를 못 때림** — 판정 로그를 남겨 확인한 결과, 판정 구체가 정면 1.8m에 반경 1.5m로 잡혀 있어 **밀착 시 사각지대**가 생겼고, 공격 동작의 앞 30%에서만 회전해 긴 클립(2.67초)에서는 판정 전에 플레이어가 측면으로 빠져나갔습니다. 판정 구체를 앞으로 당겨 키우고(1.1m / 1.9m) 추적 지속 시간을 늘려 해결했습니다.

**공격 시 캐릭터가 앞으로 밀려남** — 클립의 Root Transform Position XZ에서 Bake Into Pose를 켠 것이 원인이었습니다. 이 옵션은 수평 이동을 **포즈 안에 남기기 때문에** GameObject는 제자리인데 메시만 전진합니다. 끄면 루트 모션으로 분리되고, Apply Root Motion이 꺼져 있으므로 그대로 폐기되어 완전한 제자리 동작이 됩니다.

---

## 개발 환경

- Unity 6000.3.12f1
- Universal Render Pipeline 17.3
- Input System 1.19 / Cinemachine 3.1.4 / AI Navigation 2.0.11
- 대상 플랫폼: Windows (Direct3D11)

## 사용 에셋

- **Starter Assets - ThirdPerson** (Unity Technologies, 무료) — 3인칭 컨트롤러 기반
- **Mixamo** (Adobe, 무료) — Paladin J Nordstrom(플레이어), Mutant(보스), 전투 애니메이션
- 효과음 및 배경음 — Freesound, Pixabay 무료 음원

환경 텍스처(석재 알베도·노멀맵)는 외부 에셋 없이 프로젝트 내에서 절차적으로 생성됩니다.
