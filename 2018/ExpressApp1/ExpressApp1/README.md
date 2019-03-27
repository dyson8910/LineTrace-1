# ExpressAndMongo

このプログラムはlocalhost:3000を利用してクライアントとの情報をやりとりし、そのデータをmongodbに格納するプログラムです。
まず，mongodbをインストールしてください．

## 立ち上げ
最初に同ディレクトリのdbをmongoDBのdbpathにするため、cmdを起動し、このプログラムのあるディレクトリの上位ディレクトリまで移動し、(~/hogehoge/LTGP_programs)
$ mongod --nojournal --noprealloc --dbpath db
を入力してください。
mongodがmongodbをローカルに立ち上げるcommandです．--dbpath db がdbのフォルダをデータベース用のフォルダとするコマンドで，dbがないとうまく立ち上がりません．ない場合はmkdirしてください．
あとはおまじないです．
正常にmongoDBが起動したら、別のcmdを立ち上げて、
$ mongo
を入力し、データベースにアクセスしてください。
$ show dbs
と入力してlocal(初回以降はRecordも出るかも)が出たらOKです。

その状態でExpressApp1のソリューションを開き，デバッグを実行(F5)すると，コンソールが立ち上がり，自動的にデータベースに接続します．ビルドの方法はよくわからないので（多分できない？）デバッグで実行してください．
デフォルトの設定ではその状態でブラウザを開き，
http://localhost:3000/　を開くとライントレースグランプリのページが出て来るはずです．

## jadeについて
jadeはnode.jsがhtmlにレンダリングすることができるhtmlの記述形式の一つです．
htmlは基本的に静的ですが，これを利用することで変数を参照してhtmlを動的に生成することができます．
記述は基本的にpythonと同様のインデントを利用した形式ですが，日本語をそのままレンダリングできないため，日本語を使いたい場合は，「&#x16進;コード」で表現する必要があります．気に入らないならjson等に変えても良いでしょう．

##mongodb の中身をcmdでいじる
$ mongo
でcmd上から立ち上がっているデータベースにクエリを送ることができます
$ use Record
$ show collections
と入力してrecordsがあったら、
$ db.records.find();
で、中身があるかを確認してください。
主な命令は
$ db.records.remove({Query});
$ db.records.insert({Query});
$ db.records.find({Query});   //{}省略可
でことたりるはずです。本番環境下でremoveを使用しないこと。



