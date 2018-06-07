
/*
 * GET home page.
 */
//同ディレクトリ内のdb.jsをdbという変数として宣言しています.
var db = require('./db');
var async = require('async');

// res.renderは''の中の名前を持つviews内の.jadeファイルをレンダリングしてhtmlとしてレスポンスとしてブラウザに送り返します．
exports.index = function (req, res) { //exorts このファイルをオブジェクト化して要素にindexを加える的な
    res.render('send_oni');
};
exports.send = function (req, res) {
    res.render('send_oni');
};

exports.conf = function (req, res) {
    var data;
    // req （htmlリクエスト）のbody部分をrecodeとして宣言しています．(言い訳ですが，recordというdbの変数と区別するためにrecodeという名前で宣言しています．スペルミスじゃないです) <- 言っちゃなんですがこれは最悪だったのでdocBodyに直しました(onitsuka)
    var docBody = req.body;
    // 子供が無入力で送った場合にdbに登録されないようにする処理
    if (docBody.mode == "ON-OFF" &&  docBody.rule1power == "0" && docBody.rule2power == "0" && docBody.rule3power == "0" && docBody.rule4power == "0" && docBody.rule5power == "0" && docBody.rule6power == "0" && docBody.rule7power == "0") {　
        res.render('err'); //自由意思表明を怠ったな！
    }
    // ちゃんと出来てるとき
    else {
        // async.series内の処理は，前のfunctionのcallbackが宣言された段階で次のfunctionが実行されます．
        async.series([
            function (callback) { //次のfunctionをcallback()にします
                // reqとresをdb.addRecordsの引数にし，実行後にfunction内を実行
                db.addRecords(req, res, function (record) {
                    data = record;　
                }
                );
                callback();
            },
            function (callback) {
                // conf.jadeをレンダリング，ただし，conf.jade内の変数dataはこのプログラム中のvar dataとする．
                res.render('conf', { data: data });
            }
        ]);
    }
};
