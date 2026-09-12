const {spawn} = require('node:child_process');
const {mkdtempSync} = require('node:fs');
const {tmpdir} = require('node:os');
const {join,resolve} = require('node:path');
const {createInterface} = require('node:readline');
const child = spawn(resolve('dist/app/codex.exe'), ['app-server'], {windowsHide:true, env:{...process.env,CODEX_HOME:mkdtempSync(join(tmpdir(),'codex-widget-test-'))}, stdio:['pipe','pipe','pipe']});
const timer = setTimeout(()=>{ console.error('FAIL protocol timeout'); child.kill(); process.exitCode=1; },30000);
child.stderr.resume();
const send = obj => child.stdin.write(JSON.stringify(obj)+'\n');
createInterface({input:child.stdout}).on('line',line=>{
  const msg=JSON.parse(line);
  if(msg.error) { console.error('FAIL RPC',msg.error.message);clearTimeout(timer); child.kill();process.exitCode=1; }
  if(msg.id===1 && msg.result) { console.log('PASS bundled app-server handshake');send({method:'initialized',params:{}});send({id:2,method:'account/read',params:{refreshToken:false}}); }
  if(msg.id===2 && msg.result) { if(msg.result.account!==null) throw Error('Expected isolated logged-out session');console.log('PASS isolated account requires login');clearTimeout(timer);child.kill(); }
});
send({id:1,method:'initialize',params:{clientInfo:{name:'codex_usage_widget_test',version:'1.0.0'}}});
