// ここで宣言される変数は，''で囲まれたパッケージ及び関数の属性を持つオブジェクトです．
var express = require('express');
var routes = require('./routes/index');
var http = require('http');
var path = require('path');
var mongoose = require('mongoose');
var app = express();

// このへんおまじない
app.configure(function () {
    app.use(express.logger('dev'));
    app.use(express.bodyParser());
});

app.use(express.favicon());
app.set('views', path.join(__dirname, 'views'));
app.set('view engine', 'jade');
app.use('/css', express.static('css'));
app.use('/img', express.static('img'));//?
app.use('/image', express.static('image'));
app.use('/js', express.static('js'));
app.use('/fonts', express.static('fonts'));

// jsonを利用することを宣言しています．
app.use(express.json());

app.use(express.urlencoded());
// <input type="hidden" name="_method" value="put"> などのカスタムリクエストメソッドを定義できる
// 今回は未出。
app.use(express.methodOverride());

// すぐ下のルーティング処理を使うことを宣言しています．
app.use(app.router);

//get命令及びpost命令が特定のホストで呼び出された場合の処理を記述しています．
// localhost:3000/，localhost:3000/index/, localhost:3000/send/がgetで呼び出された場合にはroutes.sendを， localhost:3000/sendがsendで呼び出された場合にはroutes.confを実行するように設定しています．
app.get('/', routes.send);
app.get('/index', routes.send);
app.get('/send', routes.send);
app.post('/send', routes.conf); // /sendにpostが来たら

//httpオブジェクトからcreateServerをappの設定で呼び出しています．3000番ポートを利用し，コールバック関数でログを吐き出します．
http.createServer(app).listen(3000, function () {
    console.log('Express server listening on port ' + 3000);
  // app.get('port'); setできる項目については、getもできる。
});
