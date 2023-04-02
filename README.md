# Seristerll
[Serister](https://www.vector.co.jp/soft/cmt/win95/hardware/se423507.html "Serister") のOSS、.net 実装クローン。

本家ソフトで放置されている以下の問題を改善しています。
- 送受信データ内にnull(0x00) があると、それ以降のデータが破棄される。
- 2桁以上のポート番号に対応していない。
