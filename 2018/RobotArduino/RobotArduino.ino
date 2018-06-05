#include <SoftwareSerial.h>
int bluetoothRx = 6;  // RX-I of bluetooth
int bluetoothTx = 7;  // TX-O of bluetooth

SoftwareSerial mySerial(bluetoothRx, bluetoothTx); // RX, TX

const int SwitchRead = 0;
const int motorB1 = 1;
const int motorB2 = 2;
const int BPWM_mot = 3;
const int motorA1 = 8;
const int motorA2 = 9;
const int APWM_mot = 10;
const int thresh = 900;
String text = "";
double param[15] = {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};
char bufforp[15][10]={"","","","","","","","","","","","","","",""};
char mode[7] = "";
int state = 0;
int intentionpast=0;
int intention = 0;
unsigned long before = 0;
unsigned long now =0;
// 文字列を出力する
void Stringwrite(String txt){
  int i =0;
  for(i=0;i<txt.length();i++){
    mySerial.write(txt.charAt(i));
  }
}
// 文字列を読み込む（文字列が読み込める状態であることを確認してください）
String Stringread(){
  String txt = "";
  while(mySerial.available()){
    delay(50);
    char inputchar = mySerial.read();
    txt = String(txt + inputchar);
  }
  return txt;
}
// タクトスイッチが押されたかを確認する関数
int Switchcheck(){
  if(digitalRead(SwitchRead) == LOW){
    //チャタリング処理
    delay(50);
    if(digitalRead(SwitchRead) == LOW){
      return 1;
    }
  }
  return 0;
}
// フォトトランジスタの値が基準値を超えているか確認
int threshold(int p){
  if(p>thresh){
    return 1;
  }
  else{
    return 0;
  }
}
//フォトトランジスタの値から偏差を返す関数(ただしコースアウトは10,ゴールは100)
int Photocheck(){
  int  right= threshold(analogRead(5));
  int  cent= threshold(analogRead(4));
  int  left= threshold(analogRead(3)); 
  if(right+cent+left==3){
    return 100;
  }
  if(right+cent+left==0){
    return 10;
  }
  if(right ==1  && cent+left == 0){
    return -2;
  }
  if(right+cent == 2 && left == 0){
    return -1;
  }
  if(right+left ==0 && cent == 1){
    return 0;
  }
  if(left == 1 && cent+right == 0){
    return 2;
  }
  if(left+cent == 2 && right == 0){
    return 1;
  }
  if(right+left==2&&cent ==0){
    return 7;
  }
}

// PIDの実行関数
void PIDexecute(double* Ks){
  //Ks[0]->speed Ks[1]->P Ks[2]->I Ks[3]->D
  unsigned long stime =millis();
  before = millis();
  intentionpast = 0;
  intention = 0;
  now = millis();
  while(1){
    before = now;
    now = millis();
    if(Switchcheck()==1){
      state = 0;
      Stringwrite("Halt");
      digitalWrite(motorA1,LOW);
      digitalWrite(motorA2,LOW);
      digitalWrite(motorB1,LOW);
      digitalWrite(motorB2,LOW);
      delay(100);      

      break; 
    }
    /*
    if(((intentionpast == 2) || (intentionpast == 3)) && intention == 10){
      intentionpast = 3;
      
    }
    else if(((intentionpast == -2) || (intentionpast == -3)) && intention == 10){
      intentionpast = -3;
    }
    */
    /* else{ */
      intentionpast = intention;
    /*}*/
    intention= Photocheck();
    if(intention == 7){
      intention=0;
    }
    if(((intentionpast == 1) || (intentionpast == 2) || (intentionpast == 3)) && intention == 10){
      intention = 3;
      
    }
    else if(((intentionpast == -1) || (intentionpast == -2) || (intentionpast == -3)) && intention == 10){
      intention = -3;
    }
    else if(intention == 10){
      intention = 0;
    }
    if(intention == 100){ //ゴールした場合
      unsigned long etime= millis();
      long resultms = etime - stime;
      long results = resultms / 1000;
      resultms = resultms % 1000;
      long resultmin = results / 60;
      results = results % 60; 
      char timeresult[30];
      state = 0;
      sprintf(timeresult,"0:%ld:%ld.%03d",resultmin,results,resultms);
      Stringwrite(timeresult);
      digitalWrite(motorA1,LOW);
      digitalWrite(motorA2,LOW);
      digitalWrite(motorB1,LOW);
      digitalWrite(motorB2,LOW);
      delay(50);      

      break;       
    }
    /*
    else if(intention == 10){ //コースアウト時
      digitalWrite(motorA1,HIGH);
      digitalWrite(motorA2,LOW);
      analogWrite(APWM_mot,int(Ks[0]/2)); 
      digitalWrite(motorB1,HIGH);
      digitalWrite(motorB2,LOW);
      analogWrite(BPWM_mot,int(Ks[0]/2)); 
      delay(50);      
    }
    */
    else{ //それ以外
      double p = intention;
      double i = (intention + intentionpast) / 2.0;
      double d = (intention - intentionpast);
      double gain = Ks[1]*p + Ks[2]*i + Ks[3]*d;
      int rgain,lgain;
      if(gain>0){
        rgain = int(Ks[0]);
        lgain = int(Ks[0] - gain);
      }
      else{
        rgain = int(Ks[0] + gain);
        lgain = int(Ks[0]);
      }
      if(rgain<0){
        rgain = 0;
      }
      if(lgain<0){
        lgain = 0;
      }
      digitalWrite(motorB1,LOW);
      digitalWrite(motorB2,HIGH);
      digitalWrite(motorA1,LOW);
      digitalWrite(motorA2,HIGH);
      analogWrite(BPWM_mot,rgain); 
      analogWrite(APWM_mot,lgain);
      delay(50);
    }
  }
}
void ONOFFexecute(double* Ks){
  //record.onoffspeed,aaar,aaal, aabr, aabl, abar, abal, baar, baal, abbr, abbl, babr, babl, bbar, bbal
  unsigned long stime =millis();
      digitalWrite(motorB1,LOW);
      digitalWrite(motorB2,HIGH);
      digitalWrite(motorA1,LOW);
      digitalWrite(motorA2,HIGH);
 intention = intentionpast = 0;
  while(1){
    if(Switchcheck()==1){
      state = 0;
      Stringwrite("Halt");
      digitalWrite(motorA1,LOW);
      digitalWrite(motorA2,LOW);
      digitalWrite(motorB1,LOW);
      digitalWrite(motorB2,LOW);
      delay(50);      
      break; 
    }
    intentionpast = intention;
    intention= Photocheck();

    if(intention == 100){ //ゴールした場合
      unsigned long etime= millis();
      long resultms = etime - stime;
      long results = resultms / 1000;
      resultms = resultms % 1000;
      long resultmin = results / 60;
      results = results % 60; 
      char timeresult[30];
      state = 0;
      sprintf(timeresult,"0:%ld:%ld.%03d",resultmin,results,resultms);
      Stringwrite(timeresult);
      digitalWrite(motorA1,LOW);
      digitalWrite(motorA2,LOW);
      digitalWrite(motorB1,LOW);
      digitalWrite(motorB2,LOW);
      delay(100);      

      break;       
    }
    else if(intention == 10){ //コースアウト時
      //aaaseries
      if(Ks[1]>0){
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,HIGH);
        analogWrite(BPWM_mot,int(Ks[0]*Ks[1]/100.0));        
      }
      else if(Ks[1]< 0){
        digitalWrite(motorB1,HIGH);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,int(-1*Ks[0]*Ks[1]/100.0));
      }
      else{
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,0);
        
      }
      if(Ks[2]>0){
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,HIGH);
        analogWrite(APWM_mot,int(Ks[0]*Ks[1]/100.0));        
      }
      else if(Ks[2]< 0){
        digitalWrite(motorA1,HIGH);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,int(-1*Ks[0]*Ks[1]/100.0));
      }
      else{
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,0);
      }
      delay(100);
    }
    else if(intention == 2){
      
      //baaseries 7,8
      if(Ks[7]>0){
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,HIGH);
        analogWrite(BPWM_mot,int(Ks[0]*Ks[7]/100.0));        
      }
      else if(Ks[7]< 0){
        digitalWrite(motorB1,HIGH);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,int(-1*Ks[0]*Ks[7]/100.0));
      }
      else{
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,0);
        
      }
      if(Ks[8]>0){
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,HIGH);
        analogWrite(APWM_mot,int(Ks[0]*Ks[8]/100.0));        
      }
      else if(Ks[8]< 0){
        digitalWrite(motorA1,HIGH);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,int(-1*Ks[0]*Ks[8]/100.0));
      }
      else{
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,0);
      }
      delay(100);
    }
    else if(intention == 1){
      // bba series 13,14
      if(Ks[13]>0){
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,HIGH);
        analogWrite(BPWM_mot,int(Ks[0]*Ks[13]/100.0));        
      }
      else if(Ks[13]< 0){
        digitalWrite(motorB1,HIGH);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,int(-1*Ks[0]*Ks[13]/100.0));
      }
      else{
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,0);
        
      }
      if(Ks[14]>0){
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,HIGH);
        analogWrite(APWM_mot,int(Ks[0]*Ks[14]/100.0));        
      }
      else if(Ks[14]< 0){
        digitalWrite(motorA1,HIGH);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,int(-1*Ks[0]*Ks[14]/100.0));
      }
      else{
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,0);
      }
      delay(100);
    }
    else if(intention == -1){
      // abb series 9,10
      if(Ks[9]>0){
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,HIGH);
        analogWrite(BPWM_mot,int(Ks[0]*Ks[9]/100.0));        
      }
      else if(Ks[9]< 0){
        digitalWrite(motorB1,HIGH);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,int(-1*Ks[0]*Ks[9]/100.0));
      }
      else{
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,0);
        
      }
      if(Ks[10]>0){
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,HIGH);
        analogWrite(APWM_mot,int(Ks[0]*Ks[10]/100.0));        
      }
      else if(Ks[10]< 0){
        digitalWrite(motorA1,HIGH);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,int(-1*Ks[0]*Ks[10]/100.0));
      }
      else{
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,0);
      }
      delay(100);
    }
    else if(intention == -2){
      //aab series 3,4
      if(Ks[3]>0){
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,HIGH);
        analogWrite(BPWM_mot,int(Ks[0]*Ks[3]/100.0));        
      }
      else if(Ks[3]< 0){
        digitalWrite(motorB1,HIGH);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,int(-1*Ks[0]*Ks[3]/100.0));
      }
      else{
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,0);
        
      }
      if(Ks[4]>0){
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,HIGH);
        analogWrite(APWM_mot,int(Ks[0]*Ks[4]/100.0));        
      }
      else if(Ks[4]< 0){
        digitalWrite(motorA1,HIGH);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,int(-1*Ks[0]*Ks[4]/100.0));
      }
      else{
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,0);
      }
      delay(100);
    }
    else if(intention == 0){
      //aba series 5,6
      if(Ks[5]>0){
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,HIGH);
        analogWrite(BPWM_mot,int(Ks[0]*Ks[5]/100.0));        
      }
      else if(Ks[5]< 0){
        digitalWrite(motorB1,HIGH);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,int(-1*Ks[0]*Ks[5]/100.0));
      }
      else{
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,0);
        
      }
      if(Ks[6]>0){
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,HIGH);
        analogWrite(APWM_mot,int(Ks[0]*Ks[6]/100.0));        
      }
      else if(Ks[6]< 0){
        digitalWrite(motorA1,HIGH);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,int(-1*Ks[0]*Ks[6]/100.0));
      }
      else{
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,0);
      }
      delay(100);
    }
    else if(intention == 7){
      //bab series 11,12
      if(Ks[11]>0){
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,HIGH);
        analogWrite(BPWM_mot,int(Ks[0]*Ks[11]/100.0));        
      }
      else if(Ks[11]< 0){
        digitalWrite(motorB1,HIGH);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,int(-1*Ks[0]*Ks[11]/100.0));
      }
      else{
        digitalWrite(motorB1,LOW);
        digitalWrite(motorB2,LOW);
        analogWrite(BPWM_mot,0);
        
      }
      if(Ks[12]>0){
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,HIGH);
        analogWrite(APWM_mot,int(Ks[0]*Ks[12]/100.0));        
      }
      else if(Ks[12]< 0){
        digitalWrite(motorA1,HIGH);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,int(-1*Ks[0]*Ks[12]/100.0));
      }
      else{
        digitalWrite(motorA1,LOW);
        digitalWrite(motorA2,LOW);
        analogWrite(APWM_mot,0);
      }
      delay(100);
    }
  }  
}
void setup()  
{
  // Bluetooth用のシリアルオープン
  mySerial.begin(9600);
  pinMode(motorA1,OUTPUT); //信号用ピン
  pinMode(motorA2,OUTPUT); //信号用ピン
  pinMode(motorB1,OUTPUT); //信号用ピン
  pinMode(motorB2,OUTPUT); //信号用ピン

  pinMode(SwitchRead,INPUT_PULLUP);
  digitalWrite(motorB1,LOW);
  digitalWrite(motorB2,LOW);
  digitalWrite(motorA1,LOW);
  digitalWrite(motorA2,LOW);
}

void loop()
{
  //mySerial.print(state);S
  if(state == 0){ //待機中
    if(Switchcheck()==1){
      state = 1;
    }
  }
  if(state == 1){ //準備が終わったことを連絡
    delay(1000);
    Stringwrite(String("Ready"));
    state = 2;
  }
  if(state == 2){ //パラメータが送られてくるのを待機
    if(Switchcheck()==1){ //エラーが出た場合0に
      state = 0;
    }
    if(mySerial.available()){ //送られてきた場合
      text = Stringread();
      state = 3;
      char str[128] ="";
      strcpy(str,text.c_str());
      int i;
      for(i=0;i<7;i++){
        mode[i]=0;
      }
      if(text.charAt(0) == 'O'){
        sscanf(str,"%s%s%s%s%s%s%s%s%s%s%s%s%s%s%s%s",mode,bufforp[0],bufforp[1],bufforp[2],bufforp[3],bufforp[4],bufforp[5],bufforp[6],bufforp[7],bufforp[8],bufforp[9],bufforp[10],bufforp[11],bufforp[12],bufforp[13],bufforp[14]);
         for(i=0;i<15;i++){
          param[i]=atof(bufforp[i]);
        }

      }
      else if(text.charAt(0) == 'P'){
        sscanf(str,"%s%s%s%s%s",mode,bufforp[0],bufforp[1],bufforp[2],bufforp[3]);
 
        for(i=0;i<4;i++){
          param[i]=atof(bufforp[i]);
        }
      }
    }
  }
  if(state == 3){ //受信が成功したことを知らせます
    delay(1000);
    Stringwrite(String("Success"));
    state = 4;
  }
  if(state == 4){ //実行を開始します
    delay(1000);
    if(strncmp(mode,"PID",3) == 0){
      PIDexecute(param);
    }
    if(strncmp(mode,"ON-OFF",6) == 0){
      ONOFFexecute(param);
    }
    state = 0;
  }
  
  
}
