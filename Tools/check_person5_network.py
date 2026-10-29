#!/usr/bin/env python3
"""Opt-in real Unity process integration checks. No credentials/network services needed."""
import argparse, json, os, pathlib, subprocess, tempfile, time

ROOT = pathlib.Path(__file__).resolve().parents[1]
UNITY = '/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity'
BINARY = ROOT / 'Builds/Person5Sandbox.app/Contents/MacOS/The Cursed Crown'
parser = argparse.ArgumentParser()
parser.add_argument('--editor', action='store_true', help='Use actual Editor Play Mode for host')
parser.add_argument('--full-duration', action='store_true')
args = parser.parse_args()
OUT = pathlib.Path(tempfile.mkdtemp(prefix='person5-network-'))
print('Reports:', OUT, flush=True)
processes = {}
seq = {}
checks = []
port = str(19000 + os.getpid() % 1000)

def launch(name, mode):
    prefix = OUT/name
    extra = ['-p5Mode',mode,'-p5Port',port,'-p5Report',str(prefix),'-logFile',str(prefix)+'.log']
    if args.editor and name == 'host':
        cmd = [UNITY,'-batchmode','-nographics','-projectPath',str(ROOT),'-executeMethod','CursedCrown.Editor.Person5PlayMode.Run']+extra
    else:
        cmd = [str(BINARY),'-batchmode','-nographics']+extra
    processes[name] = subprocess.Popen(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    seq[name] = 0

def read(name):
    try: return json.loads((OUT/(name+'.json')).read_text())
    except (FileNotFoundError,json.JSONDecodeError): return {}

def wait(label, predicate, timeout=15):
    until=time.monotonic()+timeout
    while time.monotonic()<until:
        if predicate():
            checks.append(label); print('PASS:',label,flush=True); return
        time.sleep(.1)
    raise AssertionError(label+'; snapshots='+str({n:read(n) for n in processes}))

def command(name, action, **kwargs):
    seq[name]+=1
    p=OUT/(name+'.command.json')
    tmp=p.with_suffix('.tmp')
    tmp.write_text(json.dumps(dict(sequence=seq[name], action=action, **kwargs)))
    tmp.replace(p)
    until=time.monotonic()+10
    while time.monotonic()<until:
        r=read(name)
        if r.get('ack',0)>=seq[name]: return r
        time.sleep(.05)
    raise AssertionError('Command not acknowledged: '+name+' '+action)

def match(name='host'): return read(name)
def rows(name='host'): return {r['Id']:r for r in match(name).get('scores',[])}
def phase(value, names=('host','client')): return all(match(n).get('phase')==value for n in names)
def pickup(name='host', rnd=None): command(name,'pickup',round=rnd or match(name)['round'])
def ready(name, value=True, rnd=None): command(name,'ready',value=value,round=rnd or match(name)['round'])
def no_owner(label):
    time.sleep(.4); wait(label,lambda:not match().get('carried') and not match('client').get('carried'))
def move(pid, x=-.25, y=0, z=-.85): command('host','move',player=pid,position=dict(x=x,y=y,z=z)); time.sleep(.25)
def reset():
    old=match()['round']; command('host','restart')
    wait('restart advances round and preserves connections',lambda:phase('Lobby') and match()['round']==old+1 and match('client')['round']==old+1 and all(match(n)['connected'] for n in ('host','client')))
    wait('reset clears score, readiness and ownership',lambda:all(r['Score']==0 and not r['Ready'] for r in rows().values()) and not match()['carried'])

def start(seconds):
    command('host','duration',seconds=seconds)
    ready('host'); ready('client')
    wait('both ready replicated',lambda:all(r['Ready'] for r in rows().values() if r['Connected']) and all(r['Ready'] for r in rows('client').values() if r['Connected']))
    command('host','start'); wait('playing replicated',lambda:phase('Playing'))

try:
    launch('host','host'); wait('host spawns one match',lambda:match().get('matchObjects')==1,120)
    launch('client','client'); wait('client joins and receives lobby',lambda:match('client').get('connected') and len(rows())==2 and len(rows('client'))==2)
    host=match()['localId']; client=match('client')['localId']
    wait('sandbox prefab defaults to 240 seconds',lambda:phase('Lobby') and match().get('roundSeconds')==240)
    command('host','start'); wait('unready start rejected',lambda:not match()['accepted'] and phase('Lobby'))
    ready('host'); command('host','start'); wait('one unready player blocks start',lambda:not match()['accepted'] and phase('Lobby'))
    ready('client'); command('client','start'); wait('client cannot start',lambda:not match('client')['accepted'] and phase('Lobby'))
    start(35)
    wait('shared network deadline',lambda:abs(match()['deadline']-match('client')['deadline'])<.001)
    command('host','start'); wait('duplicate start rejected',lambda:not match()['accepted'])
    pickup(); no_owner('far pickup rejected')
    move(host)
    command('host','trackingRequired',value=True); pickup(); no_owner('untracked desktop pose rejected in production mode')
    command('host','trackingRequired',value=False)
    command('host','services',value=False); pickup(); no_owner('missing real health fails closed with mock disabled')
    command('host','services',value=True)
    command('host','dead',player=host,value=True); pickup(); no_owner('dead player pickup rejected')
    command('host','dead',player=host,value=False)
    command('host','wall',value=True,position=dict(x=0,y=1.3,z=-.25)); pickup(); no_owner('wall blocks reach')
    command('host','wall',value=True,position=dict(x=0,y=1.25,z=-.5)); pickup(); no_owner('hand inside wall rejected')
    command('host','wall',value=False)
    pickup(rnd=999); no_owner('stale pickup round rejected')
    move(client)
    # Both requests are sent through each peer; host serializes acceptance.
    pickup('host'); pickup('client')
    wait('competing requests yield one replicated carrier',lambda:match()['carried'] and match('client')['carried'] and match()['carrier']==match('client')['carrier'])
    carrier=match()['carrier']
    wait('whole-second possession points replicate',lambda:rows()[carrier]['Score']>=2 and rows('client')[carrier]['Score']==rows()[carrier]['Score'])
    command('host','deathEvent',player=carrier,round=999)
    wait('stale death notification rejected',lambda:match()['carried'])
    command('host','deathEvent',player=carrier,round=match()['round'])
    no_owner('current death drops crown')
    wait('invalid placement returns crown to pedestal',lambda:abs(match()['crown']['y']-1.35)<.001 and abs(match()['crown']['z'])<.001)
    score=rows()[carrier]['Score']; time.sleep(1.2)
    wait('unowned crown earns no points',lambda:rows()[carrier]['Score']==score)
    command('host','placement',value=True); pickup('client')
    wait('client RPC claims ownership with sender identity',lambda:match()['carried'] and match()['carrier']==client)
    command('host','dead',player=client,value=True); no_owner('health polling drops dead carrier')
    wait('valid placement service used',lambda:abs(match()['crown']['z']+.1)<.001)
    command('host','dead',player=client,value=False); pickup('client')
    wait('client retakes valid drop',lambda:match()['carried'] and match()['carrier']==client)
    launch('late','client')
    wait('late join receives playing state, crown and scores',lambda:match('late').get('connected') and match('late').get('carried') and match('late')['carrier']==client and match('late')['deadline']==match()['deadline'] and len(rows('late'))==3)
    launch('fourth','client'); launch('fifth','client')
    wait('five desktop peers receive the same crown and deadline',lambda:len([r for r in rows().values() if r['Connected']])==5 and all(match(n).get('connected') and match(n).get('deadline')==match()['deadline'] and match(n).get('carrier')==client for n in ('late','fourth','fifth')))
    command('fourth','leave'); command('fifth','leave')
    command('client','leave')
    wait('carrier disconnect drops and retains participant score',lambda:not match()['carried'] and not rows()[client]['Connected'])
    command('late','leave'); command('client','rejoin')
    wait('client reconnects during round',lambda:match('client').get('connected') and len([r for r in rows().values() if r['Connected']])==2)
    client=match('client')['localId']
    wait('exact deadline finishes on both peers',lambda:phase('Finished'),45)
    wait('finished state observed at or after host deadline',lambda:match()['serverTime']>=match()['deadline'])
    score=sum(r['Score'] for r in rows().values()); pickup()
    time.sleep(.4); wait('finished round rejects pickup and overtime points',lambda:not match()['carried'] and sum(r['Score'] for r in rows().values())==score)
    command('client','restart'); wait('client cannot restart',lambda:not match('client')['accepted'] and phase('Finished'))
    reset()
    wait('departed players removed by reset hook',lambda:len(rows())==2 and match()['resets']==1)
    ready('client',rnd=1); time.sleep(.3); wait('previous-round readiness rejected',lambda:not rows()[client]['Ready'])
    start(8); move(host); pickup()
    wait('fractional scoring possession begins',lambda:match()['carried'])
    time.sleep(.3); command('host','deathEvent',player=host,round=match()['round'])
    wait('subsecond possession earns zero',lambda:not match()['carried'] and rows()[host]['Score']==0)
    pickup(); time.sleep(.3); command('host','deathEvent',player=host,round=match()['round'])
    wait('separate fractional possessions do not accumulate',lambda:not match()['carried'] and rows()[host]['Score']==0)
    pickup(); wait('new continuous second earns exactly one',lambda:rows()[host]['Score']==1)
    command('host','deathEvent',player=host,round=match()['round'])
    wait('fractional scoring round completes',lambda:phase('Finished'),12)
    wait('earned point retained after drop',lambda:rows()[host]['Score']==1 and rows('client')[host]['Score']==1)
    reset()
    # Equal zero scores share victory and repeated rounds do not destroy connections.
    for iteration in range(2):
        start(2); wait('short round reaches finish',lambda:phase('Finished'),6)
        wait('all-zero tie lists both players',lambda:all(r['Score']==0 for r in rows().values()) and all('Player '+str(i) in match()['result'] for i in (host,client)))
        reset()
    # Production duration with possession right through expiry (optional four minutes).
    if args.full_duration:
        start(240); move(host); pickup()
        wait('carrier acquired for full-duration test',lambda:match()['carried'])
        wait('240-second match completes',lambda:phase('Finished'),250)
        wait('full-duration possession score agrees on both peers',lambda:rows()[host]['Score']==rows('client')[host]['Score'] and 238<=rows()[host]['Score']<=240)
        reset()
    # Host session ends then starts again: new match singleton, fresh lobby/round.
    command('host','leave')
    wait('host loss disconnects client',lambda:not match('client')['connected'])
    command('host','rejoin'); time.sleep(.5); command('client','rejoin')
    wait('fresh session has one match per peer',lambda:phase('Lobby') and all(match(n)['matchObjects']==1 and match(n)['round']==1 and match(n)['connected'] for n in ('host','client')))
    wait('zero runtime errors across processes',lambda:all(read(n).get('errors',0)==0 for n in processes))
    summary=dict(passed=True,checks=checks,editorHost=args.editor,fullDuration=args.full_duration,reports=str(OUT))
except Exception as ex:
    summary=dict(passed=False,checks=checks,error=str(ex),reports=str(OUT))
    print('FAIL:',ex,flush=True)
finally:
    for name,p in processes.items():
        if p.poll() is None:
            try: command(name,'quit')
            except Exception: pass
    for p in processes.values():
        try: p.wait(timeout=10)
        except subprocess.TimeoutExpired: p.terminate()
    (OUT/'summary.json').write_text(json.dumps(summary,indent=2))
print(json.dumps(summary,indent=2))
raise SystemExit(0 if summary['passed'] else 1)
