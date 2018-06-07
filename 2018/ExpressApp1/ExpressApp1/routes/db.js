var async = require('async');
var mongoose = require('mongoose');
//var timespan = require('timespan');
//var autoIncrement = require('mongoose-auto-increment');
// Default Schemaを取得
var Schema = mongoose.Schema;

// Defaultのスキーマから新しいスキーマを定義
var RequestTable = new Schema({
    Username: String,
    Type: String,
    Reserve: Date,
    State: String,
    Param : [Number],
    Rec: { type: String , default: "1:00:00.0000000" }
},
    {versionKey : false}
);

// ドキュメント保存時にフックして処理したいこと
RequestTable.pre('save', function (next) {
    this.date = new Date();
    next();
});


// mongodb://[hostname]/[dbname]
var connection = mongoose.createConnection('mongodb://localhost/Record');
//autoIncrement.initialize(connection);

var Rec;

// mongoDB接続時のエラーハンドリング
connection.on('error', console.error.bind(console, 'connection error:'));
connection.once('open', function () {
    console.log("Connected to 'record' database");
    // RecordでautoIncrementを使用することを定義
    //RequestTable.plugin(autoIncrement.plugin, 'record');
    // 定義したときの登録名で呼び出し
    Rec = connection.model('record',RequestTable);
});

exports.addRecords = function (req, res, callback) {
    var record = req.body; //document の　bodyを取得
    console.log('Adding record: ' + JSON.stringify(record));
    delete record.__proto__;
    record.date = new Date();
    record.state = "Waiting";
    var addrec;
    if (record.mode == "PID") {
        addrec = new Rec({ Username : record.name , Type : record.mode , Reserve : record.date, State : record.state , Param : [record.SPEED, record.P, record.I, record.D] });
    }
    else {///a = □ b = ■
        var aaar = 0, aaal = 0, aabr = 0, aabl = 0, abar = 0, abal = 0, baar = 0, baal = 0, abbr = 0, abbl = 0, babr = 0, babl = 0, bbar = 0, bbal = 0;


        (function(){ //クロージャ
          var zero2hundred = function(val) {
            if (val > 100) return 100;
            else if (val < 0) return 0;
            else return parseInt(val);
          }

          var rGain = function(fb,power,bar){//右車輪のゲイン
            var handle = parseInt(power) - parseInt(bar) + 50; //なんかマイナス値だとdbが成功しない
            if (fb === "for") return zero2hundred(handle);
            else if (fb === "back") return -zero2hundred(handle);
          };
          var lGain = function(fb,power,bar){//右車輪のゲイン
            var handle = parseInt(power) + parseInt(bar) - 50; //なんかマイナス値だとdbが成功しない
            if (fb === "for") return zero2hundred(handle);
            else if (fb === "back") return -zero2hundred(handle);
          };

          //rule1 010
          abar = rGain(record.rule1fb,record.rule1power,record.rule1bar);
          abal = lGain(record.rule1fb,record.rule1power,record.rule1bar);

          //rule2 110
          bbar = rGain(record.rule2fb,record.rule2power,record.rule2bar);
          bbal = lGain(record.rule2fb,record.rule2power,record.rule2bar);

          //rule3 100
          baar = rGain(record.rule3fb,record.rule3power,record.rule3bar);
          baal = lGain(record.rule3fb,record.rule3power,record.rule3bar);

          //rule4 011
          abbr = rGain(record.rule4fb,record.rule4power,record.rule4bar);
          abbl = lGain(record.rule4fb,record.rule4power,record.rule4bar);

          //rule5 001
          aabr = rGain(record.rule5fb,record.rule5power,record.rule5bar);
          aabl = lGain(record.rule5fb,record.rule5power,record.rule5bar);

          //rule6 101
          babr = rGain(record.rule6fb,record.rule6power,record.rule6bar);
          babl = lGain(record.rule6fb,record.rule6power,record.rule6bar);

          //rule7 000
          aaar = rGain(record.rule7fb,record.rule7power,record.rule7bar);
          aaal = lGain(record.rule7fb,record.rule7power,record.rule7bar);
        })();

        addrec = new Rec({ Username : record.name , Type : record.mode , Reserve : record.date, State : record.state , Param : [record.onoffspeed,aaar,aaal, aabr, aabl, abar, abal, baar, baal, abbr, abbl, babr, babl, bbar, bbal] });

        console.log('Adding record: ' + JSON.stringify(
          { Username : record.name , Type : record.mode , Reserve : record.date, State : record.state , Param : [record.onoffspeed,aaar,aaal, aabr, aabl, abar, abal, baar, baal, abbr, abbl, babr, babl, bbar, bbal] }
        ));
    }

    addrec.save(function (err, result) { //保存結果がresultに入る
        if (err) {
            res.send({ 'error': 'An error has occurred' });
        } else {
            console.log('Success: ' + JSON.stringify(result));
            res.json(result);
        }
    });
    callback(JSON.stringify(record)); //index.jsのrecordにrecordを返す
};
