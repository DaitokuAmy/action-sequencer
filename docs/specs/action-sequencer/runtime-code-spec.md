# Action Sequencer Runtime Code Spec

## 目的

このドキュメントは `Packages/com.daitokuamy.actionsequencer/Runtime` 配下の実行時コードについて、責務・公開契約・ライフサイクル・制約を整理するための仕様メモです。

Editor 拡張の見た目や操作仕様ではなく、以下を対象にします。

- `SequenceController` による再生制御
- `SequenceClip` / `SequenceTrack` / `SequenceEvent` のデータ構造
- Signal / Range イベントの実行契約
- Handler の登録と実行

## 全体像

Action Sequencer Runtime は、Animator などで進行する Action と並行して、時刻イベントや区間イベントを発火させるための仕組みです。

構成要素は大きく以下です。

- `SequenceClip`
実行対象のアセット。Track 群と include 対象 Clip 群を持つ。
- `SequenceTrack`
複数の `SequenceEvent` を配置するためのコンテナ。
- `SignalSequenceEvent`
単発発火イベント。指定時刻に 1 回だけ発火する。
- `RangeSequenceEvent`
区間イベント。開始時・更新中・終了時・キャンセル時のフックを持つ。
- `SequenceController`
Clip の再生、更新、停止、Handler 登録を担当するランタイム本体。
- `SequenceHandle`
個別再生を停止したり、完了状態を確認するための軽量ハンドル。

## データモデル

### SequenceClip

`SequenceClip` は以下を保持します。

- `tracks`
イベント配置用 Track 一覧。
- `frameRate`
Editor 表示やフレーム変換に使う値。Runtime の更新処理自体は秒ベース。
- `filterData`
イベント種別のフィルター情報。主用途は Editor 側。
- `includeClips`
同時に取り込む別 `SequenceClip` 一覧。

Runtime では `Play` 時に `clip` 自体の Track と `includeClips` の Track を読み込み、待機イベント一覧を構築します。

### SequenceTrack

`SequenceTrack` は表示名と `SequenceEvent[]` を持つ単純なコンテナです。Runtime では Track 自体の順序制御は持たず、各 Event の時刻情報のみを使って処理します。

### SequenceEvent

全 Event の基底クラスです。

- `label`
表示用ラベル。
- `active`
無効化フラグ。`false` の場合、Runtime はその Event を再生対象に含めません。

### SignalSequenceEvent

単発イベントです。

- `time`
発火時刻。
- `ViewDuration`
GUI 表示用の見た目長さ。Runtime の発火条件には直接関与しません。

### RangeSequenceEvent

区間イベントです。

- `enterTime`
開始時刻。
- `exitTime`
終了時刻。
- `Duration`
`exitTime - enterTime`。
- `MustOneFrame`
`enterTime` と `exitTime` が同フレーム処理になっても、最低 1 フレーム維持したい場合に `true` を返す拡張ポイント。

## SequenceController の責務

`SequenceController` は以下を担当します。

- Event と Handler の対応付け定義を保持する
- `Play` 呼び出しごとに再生中情報を生成する
- `Update(deltaTime)` で全再生を進行させる
- 自然終了、明示停止、全停止、Dispose を扱う
- 実行中 Handler の生成と再利用を行う

`SequenceController` は複数の `SequenceClip` を並列再生できます。

## Handler 登録仕様

Handler 登録には local と global の 2 系統があります。

- local
特定 `SequenceController` インスタンスに対する登録。
- global
全 `SequenceController` から参照される静的登録。

解決順は local 優先、global 後勝ちではなく、まず local に該当イベント型の登録が存在するかを見て、存在しなければ global を使います。

### Signal

Signal 系は以下のどちらかで登録します。

- `BindSignalEventHandler<TEvent, THandler>(onInit, onReady)`
- `BindSignalEventHandler<TEvent>(onInvoke)`

`onInit` はハンドラインスタンス生成時に 1 回だけ呼ばれます。  
`onReady` は再生準備時に呼ばれます。  
簡易オーバーロードは `ObserveSignalSequenceEventHandler<TEvent>` を使って `onInvoke` を紐づけます。

### Range

Range 系も同様に以下を持ちます。

- `BindRangeEventHandler<TEvent, THandler>(onInit, onReady)`
- `BindRangeEventHandler<TEvent>(onEnter, onExit, onUpdate, onCancel)`

簡易オーバーロードは `ObserveRangeSequenceEventHandler<TEvent>` を使います。

### 登録単位

登録は「イベント型に対して、どの Handler 定義をぶら下げるか」という意味を持ちます。  
よって、同一イベント型に複数の Handler 型を紐づけることはサポート対象です。

一方で、現在の Handler pool は Handler 型単位で再利用するため、同一 Handler 型を別設定で複数登録する運用はサポート対象外とします。

例:

- 想定内
`MyEvent -> HandlerA, HandlerB`
- 想定外
`MyEvent -> HandlerA(設定A), HandlerA(設定B)`

## 再生ライフサイクル

### Play

`Play(clip, startOffset)` の挙動は以下です。

1. `PlayingInfo` を pool から取得する
2. 一意な `playId` を採番する
3. `clip` と `includeClips` を走査し、active な Event を収集する
4. Event 型に対応する Handler 定義から、実行用 Handler インスタンスを構築する
5. Signal は `time` 降順、Range は `exitTime` 降順で待機一覧をソートする
6. `SequenceHandle(controller, playId)` を返す

`SequenceHandle` は `PlayingInfo` 参照を直接持たず、`SequenceController` と `playId` のみを持ちます。

### Update

`Update(deltaTime)` は全再生に対して以下を行います。

1. `Time += deltaTime`
2. 発火条件を満たした Signal を順に実行する
3. 条件を満たした Range に対して Enter / Update / Exit を実行する
4. 完了した再生を回収する

Signal の発火条件は `signalEvent.time <= playingInfo.Time` です。  
Signal は発火後に待機一覧から除去され、Handler は pool に返却されます。

Range の実行条件は `rangeEvent.enterTime <= playingInfo.Time` です。  
Range は以下の順で処理されます。

- 未 Enter なら `Enter`
- 毎フレーム `Update`
- `exitTime <= currentTime` なら `Exit`

ただし、Enter と Exit が同フレームで重なる場合は `MustOneFrame` が `true` のとき、そのフレームでは Exit しません。

### 自然終了

Signal / Range の待機一覧が両方空になった再生は完了とみなし、`_playingInfos` と `playId` 管理表から除去したうえで `PlayingInfo` を pool に返します。

### 明示停止

`SequenceHandle.Stop()` または内部 `Stop(playId)` では対象再生を停止します。

- pending な Signal は破棄される
- Enter 済み Range は `Cancel` が呼ばれる
- 未 Enter の Range は `Cancel` されず破棄される
- 関連 Handler を解放し、`PlayingInfo` を pool に返す

### StopAll / Dispose

`StopAll()` は全再生を明示停止相当で回収します。  
`Dispose()` は `StopAll()` と local 登録解除を行い、Controller 内部 pool 参照を破棄します。

## SequenceHandle の契約

`SequenceHandle` は以下を提供します。

- `IsDone`
対象 `playId` がまだ `SequenceController` に存在するかで判定する。
- `Stop()`
対象再生がまだ存在する場合のみ停止する。
- `IEnumerator`
`MoveNext()` は `!IsDone` を返すため、Coroutine の待機に使える。
- `Dispose()`
`Stop()` を呼ぶ。

`SequenceHandle` は value type です。コピーされても同じ `playId` を指すだけで、再生情報本体は `SequenceController` 側で一元管理します。

## Handler 実行契約

### SignalSequenceEventHandler

Signal は `Invoke` 1 回のみです。  
基底クラス `SignalSequenceEventHandler<TEvent>` は型変換を担い、実装側は `OnInvoke(TEvent)` を override します。

### RangeSequenceEventHandler

Range は以下の状態を持ちます。

- 未 Enter
- Enter 済み
- 終了済みまたはキャンセル済み

`RangeSequenceEventHandler<TEvent>` は内部に `IsEntered` を持ち、以下のフックを提供します。

- `OnEnter`
- `OnUpdate`
- `OnExit`
- `OnCancel`

`Cancel` は「Enter 済みだが通常終了前に止められた」場合のみ呼ばれます。

## Pool と再利用

Runtime では以下を pool で再利用します。

- `PlayingInfo`
- Signal Handler instance
- Range Handler instance

`PlayingInfo` を release する際は以下を必ず初期化します。

- `Clip`
- `Id`
- `Time`
- active event list
- event-handler dictionary

これにより、自然終了後や強制停止後に古い参照を次回再生へ持ち越さないことを保証します。

## 現時点の制約

- `includeClips` は `Play` 時にその時点の参照を読む。再生中に asset を変更しても進行中再生には反映しない。
- local 登録が存在するイベント型では global 登録は使われない。
- 同一 Handler 型を別設定で複数登録する運用はサポート対象外。
- Runtime の時刻進行は呼び出し側の `Update(deltaTime)` に依存する。Animator の再生状態とは自動同期しない。

## 運用指針

- Action 実行元 1 単位ごとに `SequenceController` を所有し、ライフタイム終了時に `Dispose()` する。
- 毎フレームまたは適切な更新タイミングで `Update(deltaTime)` を必ず呼ぶ。
- 再利用したい共通反応は global bind、個別文脈依存の反応は local bind を使い分ける。
- Handler 実装は副作用と内部状態の初期化責務を明確にし、pool 再利用前提で設計する。
