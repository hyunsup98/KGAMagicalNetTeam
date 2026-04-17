<p align="center">
  <img alt="Unity" src="https://img.shields.io/badge/Unity-6.0%20(6000.2.10f1)-black?logo=unity" />
  <img alt="URP" src="https://img.shields.io/badge/URP-17.2.0-6aa84f" />
  <img alt="Photon" src="https://img.shields.io/badge/Photon-PUN2-00a3e0" />
  <img alt="Photon Voice" src="https://img.shields.io/badge/Photon-Voice%20PUN-00a3e0" />
  <img alt="Cinemachine" src="https://img.shields.io/badge/Cinemachine-3.1.5-7b68ee" />
  <img alt="Input System" src="https://img.shields.io/badge/Input%20System-1.14.2-333333" />
  <img alt="VFX Graph" src="https://img.shields.io/badge/VFX%20Graph-17.2.0-ff4d4d" />
</p>

---

## EN — KGAMagicalNetTeam (High‑Performance README)

### What this is
A Unity 6 multiplayer prototype built around **Photon PUN2** + **URP**, with a gameplay core that leans on:
- **State machines for player control**
- **ScriptableObject-driven actions/items**
- **Mediator-style interaction orchestration** (Timeline + Cinemachine handoff)
- **Pooling of transient runtime objects** (projectiles/SFX) to avoid `Instantiate/Destroy` spikes

### Tech stack (source of truth)
- **Unity**: 6000.2.10f1 (`ProjectSettings/ProjectVersion.txt`)
- **URP**: 17.2.0 (`Packages/manifest.json`)
- **Photon PUN2 / Photon Voice (PUN)**: code uses `Photon.Pun`, `Photon.Voice.PUN` (`Assets/Scripts/**`)

### Quick start
- Open the project in **Unity 6.0 (6000.2.10f1)**.
- Load a lobby/room scene under `Assets/Scenes/Hyeonyong/` (e.g. `Room.unity`).
- Optional runtime perf HUD: add `SimpleFPS` to a bootstrap object to display **ms + FPS** (`Assets/Scripts/YHG/SimpleFPS.cs`).

### Key Optimization Performance
> Table below uses **repo-backed** “before vs after” deltas from current implementation patterns (pool hits, redundant network writes). For absolute timings (ms, GC alloc), add Unity Profiler captures to `docs/perf/` and update this table.

| Measurement Metric | Before | After | Result (%) |
|---|---:|---:|---:|
| Transient projectile spawns (`FryingPanLogic`) — `Instantiate` per shot after warm-up | 1 / shot | 0 / shot (pool hit) | 100% fewer Instantiates |
| SFX playback (`SoundManager` + `SoundPool`) — `Instantiate` per SFX after warm-up | 1 / play | 0 / play (pool hit) | 100% fewer Instantiates |
| Photon CustomProperty writes — redundant “same value” sets (`NetworkProperties.SetProps`) | 1 / call | 0 / call when unchanged | 100% fewer redundant writes |

Evidence anchors:
- Pooling projectiles: `Assets/Scripts/Sensei/FryingPanLogic.cs`
- Pooling audio sources: `Assets/Scripts/SHS/System/SoundManager.cs`, `Assets/Scripts/SHS/DesignPattern/ObjectPooling/SoundPool.cs`
- No-op property write suppression: `Assets/Scripts/LSB/Utill/NetworkProperties.cs`

### Architecture (modular system diagram)
This codebase already behaves like a modular mediator/event hub in two places:
- **Interaction flow**: `PlayableCharacter` → network-safe request → `InteractionManager` → per-`InteractionType` system → Timeline bindings + Cinemachine camera mode via `ProjectManager`.
- **Action flow**: `PlayerMagicSystem` selects `ActionBase` from `ActionItemDataSO` / `MagicDataSO` and executes across Photon boundaries (RPC / network instantiate).

---

## Mandatory ADRs (Architecture Decision Records)

### ADR-001 — Pool transient runtime objects (projectiles + SFX sources)
- **Context (The problem)**: `Instantiate/Destroy` on hot paths produces spikes (main thread cost + GC pressure), especially under multiplayer fan-out.
- **Decision (The tech chosen)**:
  - Projectile-style reuse (`FryingPanLogic`)
  - Audio source pool (`SoundManager` + `SoundPool`)
- **Consequences (Quantitative result vs trade-off)**:
  - **Result**: after warm-up, **0 instantiates per event** (vs 1 per event) → **100% reduction** in steady-state instantiation frequency for those features.
  - **Trade-off**: higher steady memory footprint + stricter reset discipline to prevent state leakage.

### ADR-002 — Centralize Photon CustomProperties and block redundant writes
- **Context (The problem)**: re-sending the same CustomProperty value wastes bandwidth and can trigger repeated callbacks even when nothing changed.
- **Decision (The tech chosen)**: use `NetworkProperties.SetProps/GetProps` and skip writes when the requested value equals the current value.
- **Consequences (Quantitative result vs trade-off)**:
  - **Result**: **0 redundant property writes** when unchanged (vs 1 per call) → **100% reduction** in no-op updates.
  - **Trade-off**: still uses string keys + boxing via `Hashtable`; migrating to stricter typing reduces errors but increases refactor cost.

---

## Performance workflow (keep the README measurable)
- Add before/after Unity Profiler captures under `docs/perf/` (e.g. `YYYY-MM-DD_scene_feature_before.data`, `YYYY-MM-DD_scene_feature_after.data`)
- Export marker screenshots / Profile Analyzer summaries beside captures
- Update the performance table with **ms**, **GC alloc**, and **bytes/sec** once captures exist

---

## KO — KGAMagicalNetTeam (고성능 README)

### 이 프로젝트는 무엇인가요?
**Photon PUN2 + URP**를 기반으로 만든 Unity 6 멀티플레이 프로토타입입니다. 핵심 설계는 다음을 전제로 합니다.
- **플레이어 제어는 상태 머신(State Machine) 중심**
- **ScriptableObject 기반 액션/아이템 데이터**
- **상호작용은 Mediator(중재자) 스타일로 오케스트레이션** (Timeline + Cinemachine 전환)
- **런타임 단기 객체(투사체/SFX)를 풀링**하여 `Instantiate/Destroy` 스파이크를 억제

### 기술 스택 (근거 파일)
- **Unity**: 6000.2.10f1 (`ProjectSettings/ProjectVersion.txt`)
- **URP**: 17.2.0 (`Packages/manifest.json`)
- **Photon PUN2 / Photon Voice (PUN)**: `Photon.Pun`, `Photon.Voice.PUN` 사용 (`Assets/Scripts/**`)

### 실행 방법 (Quick start)
- **Unity 6.0 (6000.2.10f1)** 로 프로젝트를 엽니다.
- `Assets/Scenes/Hyeonyong/` 아래 로비/룸 씬(예: `Room.unity`)을 로드합니다.
- 선택: `SimpleFPS`를 부트스트랩 오브젝트에 붙이면 **ms + FPS**가 표시됩니다 (`Assets/Scripts/YHG/SimpleFPS.cs`).

### Key Optimization Performance (핵심 최적화 성능)
> 아래 표는 저장소(Repo)로 증명 가능한 “전/후” 변화(풀 히트, 불필요 네트워크 쓰기 제거)를 기반으로 작성했습니다. 절대 수치(ms, GC alloc)가 필요하면 Profiler 캡처를 `docs/perf/`에 추가하고 갱신하세요.

| Measurement Metric | Before | After | Result (%) |
|---|---:|---:|---:|
| 단기 투사체 스폰(`FryingPanLogic`) — 워밍업 이후 발사당 `Instantiate` | 1 / shot | 0 / shot (pool hit) | Instantiate 100% 감소 |
| SFX 재생(`SoundManager` + `SoundPool`) — 워밍업 이후 재생당 `Instantiate` | 1 / play | 0 / play (pool hit) | Instantiate 100% 감소 |
| Photon CustomProperty 쓰기 — 동일 값 재설정(`NetworkProperties.SetProps`) | 1 / call | 0 / call (unchanged) | 불필요 쓰기 100% 감소 |

근거:
- 투사체 풀링: `Assets/Scripts/Sensei/FryingPanLogic.cs`
- 오디오 소스 풀링: `Assets/Scripts/SHS/System/SoundManager.cs`, `Assets/Scripts/SHS/DesignPattern/ObjectPooling/SoundPool.cs`
- 동일 값 쓰기 차단: `Assets/Scripts/LSB/Utill/NetworkProperties.cs`

---

## 필수 ADR (Architecture Decision Records)

### ADR-001 — 단기 런타임 객체(투사체 + SFX 소스) 풀링
- **Context (문제)**: 핫 패스에서의 `Instantiate/Destroy`는 메인 스레드 스파이크와 GC 압력을 유발하며, 멀티플레이 fan-out 상황에서 비용이 누적됩니다.
- **Decision (선택)**:
  - 투사체: `FryingPanLogic`의 재사용 큐
  - 오디오: `SoundManager` + `SoundPool`의 오디오 소스 재사용
- **Consequences (정량 결과 vs 트레이드오프)**:
  - **결과**: 워밍업 이후 이벤트당 **Instantiate 0회**(기존 1회) → steady-state Instantiate 빈도 **100% 감소**
  - **트레이드오프**: 상시 메모리 점유 증가 + 재사용 객체 상태 리셋(속도/트랜스폼/클립 등) 규율 필요

### ADR-002 — Photon CustomProperties를 중앙화하고 동일 값 쓰기 차단
- **Context (문제)**: 같은 값을 반복 설정하면 네트워크 트래픽을 낭비하고, 값이 바뀌지 않았는데도 콜백 처리 비용이 발생할 수 있습니다.
- **Decision (선택)**: `NetworkProperties.SetProps/GetProps`를 통해 동일 값이면 쓰기를 생략합니다.
- **Consequences (정량 결과 vs 트레이드오프)**:
  - **결과**: 값이 같을 때 불필요 쓰기 **0회**(기존 1회) → no-op 업데이트 **100% 제거**
  - **트레이드오프**: 문자열 키 + `Hashtable` 박싱 구조는 유지됩니다(엄격 타입화는 오류를 줄이지만 마이그레이션 비용 증가)

---

## 성능 워크플로 (측정 가능한 상태 유지)
- Profiler 전/후 캡처를 `docs/perf/`에 추가 (예: `YYYY-MM-DD_scene_feature_before.data`, `YYYY-MM-DD_scene_feature_after.data`)
- Top marker 스크린샷/Profile Analyzer 요약을 캡처 옆에 저장
- 캡처가 생기면 표를 **ms / GC alloc / bytes/sec** 중심으로 업데이트

---

## License / attribution
TBD (라이선스를 여기에 추가하세요).

```mermaid
flowchart LR
  subgraph Net["Photon PUN2 Boundary"]
    RPC["RPC calls / Room + Player CustomProperties"]
    Inst["PhotonNetwork.Instantiate(...)"]
  end

  subgraph Player["Player Runtime"]
    PC["PlayableCharacter\n(StateMachine + Inventory + MagicSystem)"]
    SM["StateMachine\n(IState: Execute/FixedExecute)"]
    MS["PlayerMagicSystem\n(aim + cooldown + ExecuteAction)"]
  end

  subgraph Interaction["Interaction Orchestration (Mediator)"]
    IM["InteractionManager\n(RequestInteraction)"]
    BIS["BaseInteractSystem\n(per InteractionType)"]
    TL["PlayableDirector + Timeline bindings"]
  end

  subgraph Global["Global Services"]
    PM["ProjectManager (Singleton)\nCinemachineController"]
    AUD["SoundManager\n+ SoundPool"]
    FPS["SimpleFPS HUD\n(ms + fps)"]
  end

  PC --> SM
  PC --> MS
  PC -- "OnTimelinePlay -> RPC_TimelinePlay" --> RPC
  RPC --> IM
  IM --> BIS
  IM --> TL
  TL --> PM

  MS --> Inst
  MS --> RPC
  AUD --> PM
  FPS --> Player


Mandatory ADRs (Architecture Decision Records)
ADR-001 — Pool transient runtime objects (projectiles + SFX sources)
Context (The problem)
Instantiate/Destroy on hot paths produces spikes (main thread cost + GC pressure), especially under multiplayer fan-out.
Decision (The tech chosen)
Pool transient objects:
Projectile-style reuse (FryingPanLogic)
Audio source pool (SoundManager + SoundPool)
Consequences (Quantitative result vs trade-off)
Result: after warm-up, 0 instantiates per event (vs 1 per event) → 100% reduction in steady-state instantiation frequency for those features.
Trade-off: higher steady memory footprint + stricter reset discipline to prevent state leakage.
ADR-002 — Centralize Photon CustomProperties and block redundant writes
Context (The problem)
Re-sending the same CustomProperty value wastes bandwidth and can trigger repeated callbacks even when nothing changed.
Decision (The tech chosen)
Use NetworkProperties.SetProps/GetProps and skip writes when the requested value equals the current value.
Consequences (Quantitative result vs trade-off)
Result: 0 redundant property writes when unchanged (vs 1 per call) → 100% reduction in no-op updates.
Trade-off: still uses string keys + boxing via Hashtable; migrating to stricter typing reduces errors but increases refactor cost.
Performance workflow (keep the README measurable)
Add before/after Unity Profiler captures under docs/perf/ (e.g. YYYY-MM-DD_scene_feature_before.data, after.data)
Export marker screenshots / Profile Analyzer summaries beside captures
Update the table with ms, GC alloc, and bytes/sec when captures exist
KO — KGAMagicalNetTeam (고성능 README)
이 프로젝트는 무엇인가요?
Photon PUN2 + URP를 기반으로 만든 Unity 6 멀티플레이 프로토타입입니다. 핵심 설계는 다음을 전제로 합니다.

플레이어 제어는 상태 머신(State Machine) 중심
ScriptableObject 기반 액션/아이템 데이터
상호작용은 Mediator(중재자) 스타일로 오케스트레이션 (Timeline + Cinemachine 전환)
런타임 단기 객체(투사체/SFX)를 풀링하여 Instantiate/Destroy 스파이크를 억제
기술 스택 (근거 파일)
Unity: 6000.2.10f1 (ProjectSettings/ProjectVersion.txt)
URP: 17.2.0 (Packages/manifest.json)
Photon PUN2 / Photon Voice (PUN): Photon.Pun, Photon.Voice.PUN 사용 (Assets/Scripts/**)
실행 방법 (Quick start)
Unity 6.0 (6000.2.10f1) 로 프로젝트를 엽니다.
Assets/Scenes/Hyeonyong/ 아래 로비/룸 씬(예: Room.unity)을 로드합니다.
선택: SimpleFPS를 부트스트랩 오브젝트에 붙이면 ms + FPS가 표시됩니다 (Assets/Scripts/YHG/SimpleFPS.cs).
Key Optimization Performance (핵심 최적화 성능)
아래 표는 현재 코드에서 저장소(Repo)로 증명 가능한 “전/후” 변화(풀 히트, 불필요 네트워크 쓰기 제거)를 기반으로 작성했습니다. 절대 수치(ms, GC alloc)가 필요하면 Profiler 캡처를 docs/perf/에 추가하고 갱신하세요.

Measurement Metric	Before	After	Result (%)
단기 투사체 스폰(FryingPanLogic) — 워밍업 이후 발사당 Instantiate
1 / shot
0 / shot (pool hit)
Instantiate 100% 감소
SFX 재생(SoundManager + SoundPool) — 워밍업 이후 재생당 Instantiate
1 / play
0 / play (pool hit)
Instantiate 100% 감소
Photon CustomProperty 쓰기 — 동일 값 재설정(NetworkProperties.SetProps)
1 / call
0 / call (unchanged)
불필요 쓰기 100% 감소
근거:

투사체 풀링: Assets/Scripts/Sensei/FryingPanLogic.cs
오디오 소스 풀링: Assets/Scripts/SHS/System/SoundManager.cs, Assets/Scripts/SHS/DesignPattern/ObjectPooling/SoundPool.cs
동일 값 쓰기 차단: Assets/Scripts/LSB/Utill/NetworkProperties.cs
아키텍처 (모듈 구조 다이어그램)
현재 구조는 두 축에서 “모듈 + 중재자” 형태로 동작합니다.

상호작용 흐름: PlayableCharacter → 네트워크 안전 요청 → InteractionManager → InteractionType별 시스템 → Timeline 바인딩 + ProjectManager를 통한 Cinemachine 카메라 모드 전환
액션 흐름: PlayerMagicSystem이 ActionItemDataSO / MagicDataSO에서 ActionBase를 선택하고 Photon 경계(RPC/Instantiate)를 넘겨 실행
(상단 EN 섹션의 Mermaid 다이어그램과 동일)

필수 ADR (Architecture Decision Records)
ADR-001 — 단기 런타임 객체(투사체 + SFX 소스) 풀링
Context (문제)
핫 패스에서의 Instantiate/Destroy는 메인 스레드 스파이크와 GC 압력을 유발하며, 멀티플레이 fan-out 상황에서 비용이 누적됩니다.
Decision (선택)
단기 객체를 풀링합니다.
투사체: FryingPanLogic의 재사용 큐
오디오: SoundManager + SoundPool의 오디오 소스 재사용
Consequences (정량 결과 vs 트레이드오프)
결과: 워밍업 이후 이벤트당 Instantiate 0회(기존 1회) → 해당 기능의 steady-state Instantiate 빈도 100% 감소
트레이드오프: 상시 메모리 점유 증가 + 재사용 객체 상태 리셋(속도/트랜스폼/클립 등) 규율 필요
ADR-002 — Photon CustomProperties를 중앙화하고 동일 값 쓰기 차단
Context (문제)
같은 값을 반복 설정하면 네트워크 트래픽을 낭비하고, 값이 바뀌지 않았는데도 콜백 처리 비용이 발생할 수 있습니다.
Decision (선택)
NetworkProperties.SetProps/GetProps를 통해 동일 값이면 쓰기를 생략합니다.
Consequences (정량 결과 vs 트레이드오프)
결과: 값이 같을 때 불필요 쓰기 0회(기존 1회) → no-op 업데이트 100% 제거
트레이드오프: 문자열 키 + Hashtable 박싱 구조는 유지됩니다(더 엄격한 타입화는 오류를 줄이지만 마이그레이션 비용이 증가)
성능 워크플로 (측정 가능한 상태 유지)
Profiler 전/후 캡처를 docs/perf/에 추가 (예: YYYY-MM-DD_scene_feature_before.data, after.data)
Top marker 스크린샷/Profile Analyzer 요약을 캡처 옆에 저장
캡처가 생기면 표를 ms / GC alloc / bytes/sec 중심으로 업데이트
License / attribution
TBD (라이선스를 여기에 추가하세요).
