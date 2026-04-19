# Action Sequencer Editor Architecture

## 目的

このドキュメントは `Packages/com.daitokuamy.actionsequencer/Editor` 配下の大規模リファクタリングに向けて、Editor 拡張の責務分離と依存方向を整理するための設計メモです。

今回の主眼は以下です。

- 中途半端になっている MVP の責務を明確にする
- `Subject` / `ReactiveProperty` ベースの通知を、単純な Service event に寄せる
- `SerializedObject` / `AssetDatabase` / `Undo` への書き込みを Repository に隔離する
- Asset を唯一の正本として扱い、Model は編集用キャッシュにする
- EditorWindow 直下の処理を Session にまとめる

## この設計で先に固定する前提

今回の方針では、以下を設計上の前提として固定します。

- Asset が唯一の正本
- Model は pure C# の編集用キャッシュ
- UI 向け通知は Service が発火する
- Model は UI 向け event を持たない
- 外部要因で Asset が変化したら Session が Model を再構築する
- Service は複数に分割し、Service から他 Service へ依存させない
- ID は編集中セッション内で安定していればよい
- Repository は責務単位でまとめ、細かく分けすぎない

## 現状の課題

現状の Editor コードは MVP 風の構成を持っていますが、責務が各層にまたがっています。

- `SequenceClipModel` / `SequenceTrackModel` / `SequenceEventModel` が `SerializedObject` を直接保持している
- Model が値の保持、通知、Unity Asset への永続化を同時に担当している
- Presenter が Model の通知監視だけでなく、実質的なユースケース実行の入口にもなっている
- `Subject` と `ReactiveProperty` が混在し、通知契約が型ごとに異なる
- EditorWindow が session 的なライフサイクル管理まで抱えている

特に問題なのは、Model が「Editor の内部状態」と「ScriptableObject への書き込み窓口」を兼ねている点です。  
この構造だと、UI の都合で Model を更新したいだけなのか、Asset を保存したいのかがコード上で区別しづらくなります。

## 目標アーキテクチャ

```text
EditorWindow
  -> EditorSession
    -> Models
    -> Services
    -> Repository

View <-> Presenter -> Service
                      |
                      +-> Session
                           |- Models
                           |- Repository
                           \- Asset
```

依存方向の意図は以下です。

- View は Presenter だけを知る
- Presenter は担当 Service と View だけを知る
- Service は Session と Model と Repository を知る
- Service 同士は直接依存しない
- Repository だけが `SerializedObject`, `AssetDatabase`, `Undo`, `EditorUtility.SetDirty` を知る

## 各要素の責務

### EditorWindow

EditorWindow は UI ツリーの起点です。

- UXML / USS の読み込み
- Session の生成と破棄
- View の生成
- Presenter の組み立て

EditorWindow 自体はアプリケーションロジックを持ちません。  
現在の `SequenceEditorWindow` が持っている session 的責務は、できるだけ `EditorSession` に移します。

### EditorSession

EditorSession は Editor の編集中セッション全体を管理する上位オブジェクトです。

- 各種インスタンスの生成、保持、破棄
- 各種インスタンスへの依存関係注入
- セッションの開始処理、終了処理、再読込処理の一元管理
- 現在開いている Asset の保持
- Asset から構築した Model 群の保持
- Repository の保持
- 各 Service の生成と所有
- セッション内 ID の管理

EditorWindow が実質 session っぽく振る舞う状態は避け、Session を明示的に置きます。
`Open(...)`, `Close()`, `Reload()` の起点は Session にまとめます。

一方で、Session はユースケースの本体を持ちません。  
Track 編集や Event 編集の具体的な処理は各 Service に委譲し、Session は「構築、接続、開始、終了、再構築」に責務を限定します。

### View

View は `VisualElement` を中心とした UI ラッパーです。

- 現在値の表示
- UI イベントの公開
- レイアウトや見た目の責務

View は Model を直接触りません。  
また、View は保存や Asset 編集の知識を持ちません。

### Presenter

Presenter は View と Service の橋渡しです。

- 初期表示時に Service から Model を読み、View に反映する
- Service event を監視して View を更新する
- View の操作 event を Service の command に変換する
- 複数 View の協調更新を行う

Presenter は Model を直接変更しません。  
更新は必ず Service 経由で行います。

### Model

Model は Editor 上の編集用キャッシュです。  
ここでいう Model は `SerializedObjectModel` のような Unity API 依存オブジェクトではなく、pure C# のデータオブジェクトです。

例:

- `SequenceEditorModel`
- `SequenceClipModel`
- `SequenceTrackModel`
- `SignalSequenceEventModel`
- `RangeSequenceEventModel`

Model の責務は以下です。

- UI が読むための現在値を保持する
- Service が扱いやすいようにデータを整理する
- 最小限の整合性を保つ

Model は以下を持ちません。

- `SerializedObject`
- `AssetDatabase`
- `Undo`
- UI 向け event

### Service

Service は Editor のユースケース層です。

- Track の追加、削除、並び替え
- Event の追加、削除、複製、移動
- Label / time / active の変更
- 選択状態変更
- 時間表示モード変更
- Session reload
- UI 向け通知の発火

Service は「何を変更したいか」を受け取り、必要に応じて

1. Model を更新し
2. Repository に永続化を依頼し
3. Presenter 向けの event を発火します

### Repository

Repository は ScriptableObject 永続化境界です。

- Asset から Model を構築する
- Model の変更を Asset に反映する
- SubAsset の追加、削除、複製を行う
- `Undo.RecordObject`, `AssetDatabase.AddObjectToAsset`, `Undo.DestroyObjectImmediate` などを集約する

Repository は永続化の都合を吸収する層であり、UI 向け通知責務は持ちません。

## 正本と再読込

### 正本

Asset を唯一の正本とします。  
Model は Asset を編集しやすくするためのキャッシュです。

したがって、永続化されるべき変更は最終的に必ず Repository を通ります。

### 外部変更

以下のような外部要因で Asset が変わる可能性があります。

- Inspector からの編集
- Undo / Redo
- Asset の再読み込み
- IncludeClip の切り替え

この場合は差分同期よりも、Session が Model を再構築する方針を基本とします。

- 部分同期を頑張りすぎない
- `Reload()` を明示的なユースケースとして持つ
- reload 後は Service が粗い通知を出して Presenter が再描画する

## 通知設計

通知は `Subject` を廃止し、Service event に統一します。

### 基本ルール

- UI 向け通知は Service が発火する
- Model は UI 向け event を持たない
- Presenter は Model ではなく Service event を監視する
- event は細かくしすぎず、ユースケース単位で粗く出す

### event を粗くする理由

今回の用途では、プロパティごとの `XChanged` を大量に作ると次の問題が出やすいです。

- 連続ドラッグで通知嵐になる
- Undo 粒度と通知粒度がずれる
- 複数プロパティ変更時に一時的不整合が見える
- Presenter が細かい購読だらけになる

そのため、通知は「値変更そのもの」より「ユースケースの完了」を表す event を優先します。

### 想定する通知例

- `SessionOpened`
- `SessionReloaded`
- `SelectionChanged`
- `TimelineSettingsChanged`
- `TrackListChanged`
- `TrackChanged`
- `EventListChanged`
- `EventChanged`

必要なら payload に ID や対象 Model を含めますが、通知の意味はあくまで「何を再描画すべきか」が分かる粒度に留めます。

## Session と Service の関係

Service を相互依存させないため、各 Service の所有者として `EditorSession` を置きます。

```text
EditorSession
  |- SelectionService
  |- TimelineViewService
  |- TrackEditingService
  \- EventEditingService
```

この形にすると、

- Service は Session 経由で共通の Model / Repository にアクセスできる
- Service 同士の直接依存を避けられる
- reload や ID 管理を Session に一元化できる
- Session がライフサイクル管理の起点になれる

## Service 分割方針

Service は最初から 1 クラスにまとめません。  
ユースケース単位で分割し、互いに直接依存させない方針を取ります。

候補:

- `SequenceEditorSessionService`
- `SelectionService`
- `TimelineViewService`
- `TrackEditingService`
- `EventEditingService`

共通処理の扱いは以下の優先順とします。

1. まず Model の関数として自然に置けるか考える
2. それでも共通化が必要なら Utility / Helper を使う
3. Service 同士の依存で共通処理を使い回さない

## ID 設計

ID は永続 ID ではなく、編集中セッション内で安定していれば十分です。

- `TrackId`
- `EventId`

のような軽量 ID を用意し、UI 層に `UnityEngine.Object` をそのまま流さないようにします。

ID の生成と対応表は Session が持ちます。

## Repository の分割方針

Repository は細かく分けすぎません。  
責務単位でまとまっていれば十分です。

現時点では `SequenceClipRepository` を中心に据え、Track / Event の永続化もそこで扱う想定です。

この方が以下の理由で扱いやすくなります。

- `Undo.RecordObject` の境界をまとめやすい
- SubAsset 操作を一箇所に閉じ込めやすい
- `SequenceClip` を集約ルートとして扱いやすい

## 命名規約

通知と操作の命名は以下で揃えます。

- Model property: `Label`, `IsActive`, `EnterTime`
- Model mutation: `SetLabel`, `SetActive`, `SetEnterTime`
- Service command: `RenameTrack`, `MoveTrack`, `CreateEvent`, `DeleteEvent`
- Service event: `TrackChanged`, `EventListChanged`, `SessionReloaded`

`ChangedLabelSubject` のような名前は新規設計では使いません。

## 導入したい主要インターフェース

想定している入口は以下です。

```csharp
public interface ISequenceEditorSession
{
    SequenceClip CurrentAsset { get; }
    SequenceEditorModel Model { get; }

    TrackEditingService TrackEditing { get; }
    EventEditingService EventEditing { get; }
    SelectionService Selection { get; }
    TimelineViewService TimelineView { get; }

    void Open(SequenceClip clip, int includeClipIndex);
    void Reload();
}
```

```csharp
public interface ITrackEditingService
{
    event Action TrackListChanged;
    event Action<TrackId> TrackChanged;

    void RenameTrack(TrackId trackId, string label);
    void MoveTrack(TrackId trackId, int targetIndex);
    TrackId CreateTrack();
    void DeleteTrack(TrackId trackId);
}
```

```csharp
public interface IEventEditingService
{
    event Action<TrackId> EventListChanged;
    event Action<EventId> EventChanged;

    EventId CreateEvent(TrackId trackId, Type eventType);
    void DeleteEvent(EventId eventId);
    void MoveEvent(EventId eventId, TrackId targetTrackId, int targetIndex);
    void SetSignalTime(EventId eventId, float time);
    void SetRangeTime(EventId eventId, float enterTime, float exitTime);
}
```

## この設計で改善したい点

この構造にすると、以下が改善されます。

- Presenter が `AssetDatabase` や `SerializedObject` を意識しなくてよくなる
- Model と永続化の責務が分かれる
- `Subject` / `ReactiveProperty` の独自ルールを覚えなくてよくなる
- EditorWindow の責務を Session に逃がせる
- unit test で Service / Repository / Presenter を分けて検証しやすくなる

## 段階的移行案

### Phase 1

- `docs/architecture/` に設計メモを整備する
- `Subject` / `ReactiveProperty` の新規利用を止める
- 新しい命名規約を決める

### Phase 2

- `EditorSession` を導入する
- Asset から構築する pure C# Model を追加する
- `SequenceClipRepository` を導入する

### Phase 3

- Track / Event / Selection / Timeline の Service を分離する
- View 操作を Service command に寄せる
- Presenter が Service event を監視するようにする

### Phase 4

- `SerializedObjectModel` から永続化責務を剥がす
- `Subject` / `ReactiveProperty` 依存を削除する
- 外部変更時の reload フローを Session に集約する

### Phase 5

- 旧 Model 実装を段階的に置き換える
- テストを Service / Repository / Presenter の単位で整理する

## 結論

今回のリファクタリング方針としては、純粋な MVP に固執するよりも、

- View
- Presenter
- Session
- Model
- Service
- Repository

の 6 要素で考える方が整理しやすいです。

特に重要なのは次の 4 点です。

- Asset が唯一の正本
- Model は pure C# の編集用キャッシュ
- UI 通知は Service event に限定する
- 外部変更時は Session が Model を再構築する

この方針なら、ユーザー操作に対する UI 更新の見通しを保ちつつ、Unity Editor 固有の保存処理も安全に分離できます。

責務を短く言い切ると以下です。

- Session = 構築、接続、開始、終了、再構築
- Service = ユースケース実行
- Repository = 永続化
- Model = 編集用データ
