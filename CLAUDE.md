# GuildSim — 冒険者ギルド経営シミュレーション

Unity 6 / 2D TopDown / PC

## アーキテクチャ原則

### 型の役割（厳守）

| 型 | 役割 | 命名規則 |
|----|------|----------|
| `ScriptableObject` (Definition) | 不変マスタデータ | `*Definition.cs` |
| `ScriptableObject` (Config) | Inspector チューニング値 | `*Config.cs` |
| `sealed class` (pure C#) | ビジネスロジック・状態管理 | `*Service.cs`, `*State.cs` |
| `static class` (pure C#) | 副作用なし純粋計算 | `*Calculator.cs`, `*System.cs` |
| `readonly struct` | 値オブジェクト・計算結果 | 例: `DispatchResult` |
| `MonoBehaviour` | Unity ライフサイクル・UI・入力のみ | `*Manager.cs`, `*Panel.cs` |

### SerializeField パターン（全 SO で統一）

```csharp
[SerializeField] private int value = 10;
public int Value => value;  // setter なし（Definition は不変）
```

Config の Clamp パターン:
```csharp
public int MaxMembers => Mathf.Max(1, maxMembers);  // getter でバリデーション
```

### ハードコード禁止

- コード内に数値・文字列リテラルを書かない
- 全パラメータは Config SO か Definition SO に移管
- enum を使って分岐条件を型安全に管理

---

## フォルダ構造

```
Assets/
├── _Modules/          各モジュール（= 独立した機能単位）
│   ├── Adventurer/    冒険者管理
│   ├── Quest/         クエスト管理
│   ├── Dispatch/      派遣・成功率計算
│   ├── Economy/       資源（金貨・名声）管理
│   ├── Guild/         ギルド進行・ランク
│   ├── Time/          ゲームクロック
│   └── World/         世界地域定義
├── _Shared/           モジュール共通基盤
│   ├── Core/          EventBus, GameEvents
│   ├── Data/          BaseDefinition, BaseConfig
│   └── Utilities/     RandomUtility, MathUtility
├── _Game/             Bootstrap（全モジュール統合）
└── Resources/Data/    .asset ファイル（実データ）
```

各モジュール内部:
```
{ModuleName}/
├── Data/        *Definition.cs, *Config.cs
├── Runtime/     *Service.cs, *State.cs
├── Components/  *Manager.cs (MonoBehaviour)
└── Prefabs/     プレハブ
```

---

## Assembly Definition 依存ルール

```
GuildSim.Shared
  ↑ GuildSim.Adventurer
  ↑ GuildSim.Quest
  ↑ GuildSim.Economy
  ↑ GuildSim.Time
  ↑ GuildSim.World
  ↑ GuildSim.Dispatch  (Shared + Adventurer + Quest)
  ↑ GuildSim.Guild     (Shared + Economy)
  ↑ GuildSim.Game      (全モジュール)
```

**モジュール間で直接 using してはいけない。** 依存違反はコンパイルエラー。

---

## モジュール間通信

モジュールは EventBus 経由のみで通信する。

```csharp
// 発行
EventBus.Publish(GameEvents.QuestCompleted, result);

// 購読
EventBus.Subscribe<DispatchResult>(GameEvents.QuestCompleted, OnQuestCompleted);
// 必ず Unsubscribe も実装すること（OnDestroy 等で）
```

全イベントキーは `GameEvents.cs` の定数として管理。

---

## Bootstrap パターン

各シーンに Bootstrap MonoBehaviour が 1 つだけある。

1. `[SerializeField] private XxxBootstrapConfig config;` で Config SO を受け取る
2. `Start()` で全 Service を `new` して初期化
3. Service の event に UI 更新メソッドを登録
4. `OnDestroy()` で Unsubscribe / Dispose

Bootstrap は「配線係」。ビジネスロジックを書かない。

---

## Prefab 3層構成（LEGO モデル）

```
Atomic  → 単機能最小 Prefab（StatBar, CoinDisplay, TagChip）
Molecule → Atomic を組み合わせたカード（AdventurerCard, QuestCard）
Organism → パネル単位（GuildRosterPanel, QuestBoardPanel, DispatchPanel）
Scene    → 頂点 Prefab（GuildHQ）
```

Prefab は Inspector から必ず差し替え可能に設計する。

---

## テーマ切替（データ駆動の確認テスト）

Resources/Data/ 以下の .asset ファイルを差し替えるだけで、
コード変更なしにテーマを変更できることを実装の成功基準とする。

例: ファンタジー → SF
- ClassDefinition: 戦士/魔法使い → 傭兵/ハッカー
- RegionDefinition: 魔の森 → 廃棄宇宙ステーション
- QuestDefinition: テキストを SF 風に

---

## AI マルチエージェント並列開発ガイド

各モジュールは独立ブランチで実装可能。

| ブランチ候補 | 担当モジュール |
|-------------|---------------|
| `feature/module-time` | Time |
| `feature/module-economy` | Economy |
| `feature/module-adventurer` | Adventurer |
| `feature/module-quest` | Quest |
| `feature/module-dispatch` | Dispatch (Time + Adventurer + Quest 完了後) |
| `feature/module-guild` | Guild (Economy 完了後) |
| `feature/module-world` | World |

asmdef が依存違反をコンパイルエラーで検知するため、
マージ時のロジック競合は構造的に発生しない。
