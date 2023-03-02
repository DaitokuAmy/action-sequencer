# action-sequencer
![image](https://user-images.githubusercontent.com/6957962/209446627-82463af7-83de-44a2-87d4-d4c024f9a0b3.png)

## 概要
#### 特徴
* Timelineではなく、Animatorを使って動かす物(Actionと定義)にタイミングや区画を指定するためのツール
* ツールその物は疎結合に出来ているので、使用者の環境に合わせて使い分けが可能
* タイミングを指定するためのイベントの拡張がかなり簡素なので、手軽に使える

#### 背景
Unityに搭載されている「Timeline」「AnimationEvent/AnimationCurve」では、Animator制御による行動に対して演出(Effect, Soundなど)を付け辛い状態でした。  
そのため、行動実行と並列してタイミングとそのパラメータの通知を担う役割が必要になったため作成したツールです。

## セットアップ
#### インストール
1. Window > Package ManagerからPackage Managerを開く
2. 「+」ボタン > Add package from git URL
3. 以下を入力してインストール
   * https://github.com/DaitokuAmy/action-sequencer.git?path=/Assets/ActionSequencer
   ![image](https://user-images.githubusercontent.com/6957962/209446846-c9b35922-d8cb-4ba3-961b-52a81515c808.png)

あるいはPackages/manifest.jsonを開き、dependenciesブロックに以下を追記します。

```json
{
    "dependencies": {
        "com.daitokuamy.actionsequencer": "https://github.com/DaitokuAmy/action-sequencer.git?path=/Assets/ActionSequencer"
    }
}
```
バージョンを指定したい場合には以下のように記述します。

https://github.com/DaitokuAmy/action-sequencer.git?path=/Assets/ActionSequencer#1.0.0

## 機能
#### ライフサイクル
初期化はSequenceControllerというクラス(Sequenceを実行するためのクラス)を作成する所から始まります
```C#
_sequenceController = new SequenceController();
```
これだけでは更新が行われないため、適切なイベント発火タイミングを行いたい場所で更新を呼び出します
```C#
_sequenceController.Update(Time.deltaTime);
```
終了時はDisposeを行います（もし実行中の区間イベントがあればキャンセルが発火されます）
```C#
_sequenceController.Dispose();
```
#### シーケンスの実行
SequenceClipをAssetとして読み込んだ状態で、SequenceControllerに渡す事でシーケンスが始まります  
また、Sequence自体は並列再生可能なのでSequenceController一つで様々な用途のイベント管理が可能です
```C#
_sequenceController.Play(sequenceClip);

// 開始時間をずらす事も可能(この例は1.0sec)
// _sequenceController.Play(sequenceClip, 1.0f);
```
#### シーケンスの停止
基本的には全部流れ終わると自動で止まりますが、止めたい場合はPlayの戻り値に来たHandleを利用して停止します
```C#
_sequenceHandle = _sequenceController.Play(sequenceClip);
  :
_sequenceController.Stop(_sequenceHandle);
```
#### イベントクラスの作成
基本機能のままだと何も出来ないため、アプリケーション固有のイベントクラスを以下の様に作成します
* SignalEventの場合(任意のタイミングでの処理を定義したい場合)
```C#
public class LogSignalSequenceEvent : SignalSequenceEvent
{
    [Tooltip("出力用のログ")]
    public string text = "";
}
```
* RangeEventの場合(任意の範囲での処理を定義したい場合)
```C#
public class TimerRangeSequenceEvent : RangeSequenceEvent
{
    [Tooltip("出力用のフォーマット")]
    public string format = "Time:{0.000}";
}
```
ちなみに、SequenceEventAttributeは以下の様な指定が可能です
- displayName
  - SequenceClip編集Windowでのイベント表示名
- colorCode
  - SequenceClip編集Windowでのイベント表示色

![image](https://user-images.githubusercontent.com/6957962/209447528-0f084136-b448-4051-9788-a7d1ba144835.png)
#### イベントのハンドリング(実行処理の記述)
* SignalEventの場合
```C#
public class LogSignalSequenceEventHandler : SignalSequenceEventHandler<LogSignalSequenceEvent>
{
    /// <summary>
    /// タイミング発火時の処理
    /// </summary>
    protected override void OnInvoke(LogSignalSequenceEvent signalSequenceEvent)
    {
        Debug.Log(signalSequenceEvent.text);
    }
}
```
* RangeEventの場合
```C#
public class TimerRangeSequenceEventHandler : RangeSequenceEventHandler<TimerRangeSequenceEvent>
{
    private Stopwatch _stopwatch = new Stopwatch();
    
    /// <summary>
    /// 開始位置に到達した時の処理
    /// </summary>
    protected override void OnEnter(TimerRangeSequenceEvent sequenceEvent)
    {
        _stopwatch.Restart();
    }

    /// <summary>
    /// 終了位置に到達した時の処理
    /// </summary>
    protected override void OnExit(TimerRangeSequenceEvent sequenceEvent)
    {
        _stopwatch.Stop();
        Debug.Log(string.Format(sequenceEvent.format, _stopwatch.Elapsed.TotalSeconds));
    }

    /// <summary>
    /// 終了する前にキャンセルされた時の処理
    /// </summary>
    protected override void OnCancel(TimerRangeSequenceEvent sequenceEvent)
    {
        OnExit(sequenceEvent);
    }
}
```
#### イベントのBind
イベントの作成、イベントハンドリング処理の記述だけでは動かせず、それらを紐づけ(Bind)する事で機能が動作するようになります  
イベントのハンドリングが必要ないシーンなどでは<b>Bindを行わない、もしくは違うHandlerをBindするなど</b>といった使い分けが可能です
```C#
_sequenceController.BindSignalEventHandler<LogSignalSequenceEvent, LogSignalSequenceEventHandler>();
_sequenceController.BindRangeEventHandler<TimerRangeSequenceEvent, TimerRangeSequenceEventHandler>();
```
#### SequenceClipの作成
Projectウィンドウにて、<b>「Create > Action Sequencer > Sequence Clip」</b>と選択し、SequenceClipのアセットを作成します
![image](https://user-images.githubusercontent.com/6957962/209447801-2ea14eff-e088-403a-a16c-d2f049e57c5b.png)
#### SequenceClipの編集
作成されたSequenceClipアセットをダブルクリックする事で以下のようなEditorWindowが開かれるので、そこで必要なイベントの追加や編集を行います
![ActionSequencer Memo](https://user-images.githubusercontent.com/6957962/209448449-fe28c091-dea3-40dd-8414-c81f5ae47645.jpg)

