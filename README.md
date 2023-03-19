## 実行方法
名前付きパイプを作成し、そこに対してコマンドを書き込むと
温度と湿度(乱数)が表示されます。

アプリの起動
``` bash
mkfifo testFifo
dotnet run run -input ./testFifo
```
このアプリ終了は、Ctrl+Cで終了できます。


別ターミナルでコマンドを通じて操作します。
表示は、アプリの標準出力に表示されます。
``` bash
cat  > testFifo
```
この状態で、以下を入力すると、アプリに1秒ごとに出力さます。
```
setMonitorInterval --ms 1000
setMonitorStatus --status Running
```
