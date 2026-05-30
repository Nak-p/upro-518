# GuildSim.Story モジュール（Yarn Spinner 版）

「かまいたちの夜」的な分岐ストーリーを **Yarn Spinner** でビジュアル（ノード&エッジ）に構築するモジュール。
ストーリー本文は `.yarn` ファイル（純テキスト）に書き、Unity とは分離して編集できる。

---

## ビジュアル編集（ノード&エッジ）

1. **VS Code** に拡張 **「Yarn Spinner」** をインストール
2. `.yarn` ファイルを開き、エディタ上部の **グラフビュー** を開く
3. `title:` ごとが **ノード**、`<<jump XXX>>` が **矢印（エッジ）** として表示される
4. ノードをドラッグして配置・新規ノード追加・接続を視覚的に編集できる

→ `Assets/Resources/Data/Stories/GuildStory.yarn` がサンプル。

---

## Unity 側セットアップ（初回のみ）

1. **Yarn Project を作成**
   - Project ウィンドウで右クリック → Create → Yarn Spinner → Yarn Project
   - 同じフォルダの `.yarn` を自動で取り込む（取り込み対象は Yarn Project の Inspector で設定可）
2. **シーンに DialogueRunner を配置**
   - GameObject → Yarn Spinner → Dialogue Runner（または手動で DialogueRunner + 台詞表示 View を追加）
   - DialogueRunner に上記 Yarn Project を割り当て
3. **StoryDirector を配置**
   - 空の GameObject に `StoryDirector` を追加し、DialogueRunner を割り当て
4. **GameBootstrap に接続**
   - GameBootstrap の `Story Director` 欄に上記 StoryDirector を割り当て
   - GameBootstrapConfig の `Story Config` に StoryConfig アセットを割り当て

---

## アドオン式のストーリー追加

新しい話を足す手順（**コード変更不要**）:

1. `.yarn` に新しい `title:` ノードを追加（既存ノードは触らない）
2. 既存の物語とは **フラグ（Yarn変数 `$flag`）** で連携
3. その話のエントリーにしたい場合のみ、`StoryDefinition.asset` を作り
   - Start Node にそのノード名
   - Triggers に起動条件
4. `StoryConfig.asset` の Stories 配列に追加

1つの YarnProject に全ノードを入れ、`jump` で線をつなぐことで、
小さな話を組み合わせて大きな物語へ育てられる。

---

## .yarn で使えるゲーム状態関数

GuildSim 側（StoryDirector）で登録済み。`.yarn` から直接呼べる:

| 関数 | 戻り値 | Unity側ソース |
|------|--------|--------------|
| `is_quest_completed("QuestId")` | bool | WorldService.IsQuestCompleted |
| `get_gold()` | number | EconomyService.Gold |
| `get_reputation()` | number | EconomyService.Reputation |
| `get_guild_rank()` | number | GuildService.CurrentRankIndex |
| `get_adventurer_count()` | number | AdventurerService.ActiveCount |

使用例:
```yarn
<<if is_quest_completed("Quest_ForestExplore") and get_guild_rank() >= 2>>
    ギルドマスター: 森の件、見事だった。
<<endif>>
```

フラグ（ストーリー間の状態共有）は Yarn 変数で:
```yarn
<<declare $blacksmith_met = false>>
<<set $blacksmith_met to true>>
<<if $blacksmith_met>> ... <<endif>>
```

---

## StoryDefinition の設定項目

| フィールド | 説明 |
|-----------|------|
| Id (BaseDefinition) | ユニークなストーリーID |
| Display Name | 表示名 |
| Yarn Project | ノードを束ねた YarnProject（共通で1つでよい） |
| Start Node | このストーリーの開始ノード名（Yarnの title:） |
| Triggers | 起動する EventBus キー＋任意のクエストID条件 |
| Play Once | クリア後は再起動しない |

---

## トリガー設定例

| 何が起きた時 | EventKey | RequiredPayloadQuestId |
|-------------|----------|------------------------|
| 任意のクエスト完了 | `quest.completed` | （空） |
| 特定クエスト完了 | `quest.completed` | `Quest_GoblinHunt` |
| 1日経過 | `time.day_passed` | （空） |

`quest.completed` のクエストID条件は GameBootstrap が `storyDirector.NotifyEvent` で通知する。
他のペイロード付きイベントを条件にしたい場合は GameBootstrap に同様の通知行を1行足す。

---

## パッケージ

`dev.yarnspinner.unity` 3.2.2 を OpenUPM 経由で導入済み（manifest.json）。
Unity 起動時に自動取得される。Unity 2022.3+ 対応。

VS Code 拡張「Yarn Spinner」:
https://marketplace.visualstudio.com/items?itemName=SecretLab.yarn-spinner
