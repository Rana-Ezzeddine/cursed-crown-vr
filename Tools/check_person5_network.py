#!/usr/bin/env python3
"""Opt-in real Unity process integration checks. No credentials/network services needed."""
import argparse, json, os, pathlib, subprocess, tempfile, time
from concurrent.futures import ThreadPoolExecutor

ROOT = pathlib.Path(__file__).resolve().parents[1]
UNITY = '/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity'
BINARY = ROOT / 'Builds/Person5Sandbox.app/Contents/MacOS/The Cursed Crown'
parser = argparse.ArgumentParser()
parser.add_argument('--editor', action='store_true', help='Use actual Editor Play Mode for host')
parser.add_argument('--full-duration', action='store_true')
parser.add_argument('--real-health', action='store_true')
parser.add_argument('--arena', action='store_true', help='Real P2 services and P4 health in combined arena')
args = parser.parse_args()
if args.arena: args.real_health = True
if args.real_health:
    if args.editor: parser.error("--real-health uses its isolated standalone scene")
    BINARY = ROOT / "Builds/Person5HealthIntegration.app/Contents/MacOS/The Cursed Crown"
if args.arena: BINARY = ROOT / 'Builds/Person5ArenaIntegration.app/Contents/MacOS/The Cursed Crown'
OUT = pathlib.Path(tempfile.mkdtemp(prefix='person5-network-'))
print('Reports:', OUT, flush=True)
processes = {}
seq = {}
checks = []
port = str(19000 + os.getpid() % 1000)

def launch(name, mode):
    prefix = OUT/name
    extra = ['-p5Mode',mode,'-p5Port',port,'-p5Report',str(prefix),'-logFile',str(prefix)+'.log']
    if args.real_health: extra += ['-p5RealHealth','1']
    if args.arena: extra += ['-p5Arena','1']
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
    if args.real_health:
        launch('host','host'); wait('real-health host spawns',lambda:match().get('matchObjects')==1,120)
        launch('client','client'); wait('real health replicated on both player objects',lambda:len(match().get('health',[]))==2 and len(match('client').get('health',[]))==2)
        host=match()['localId']; client=match('client')['localId']
        def hp(pid, name='host'): return next(r for r in match(name)['health'] if r['id']==pid)
        def hit(count, shot, rnd=None):
            return command('host','hit',player=client,shooter=host,count=count,shot=shot,round=rnd or match()['round'])
        wait('real health integration disables mock alive',lambda:match().get('mockAlive') is False)
        hit(1,1); wait('lobby hit rejected by round/phase bridge',lambda:not match()['accepted'] and hp(client)['hp']==100)
        if args.arena:
            wait('production arena services registered',lambda:match().get('realArenaServices') is True)
            wait('host health rounds synchronized',lambda:all(h['round']==match()['round'] for h in match()['health']))
        start(18); move(client)
        if args.arena:
            pickup('client'); no_owner('arena rejects untracked pickup by default')
            command('host','trackingRequired',value=False) # Explicit desktop-only test override.
        pickup('client')
        wait('real living player carries crown',lambda:match()['carried'] and match()['carrier']==client)
        hit(1,10,rnd=999); wait('stale validated hit rejected',lambda:not match()['accepted'] and hp(client)['hp']==100)
        hit(1,20); wait('real damage replicates 100 to 75',lambda:hp(client)['hp']==75 and hp(client,'client')['hp']==75)
        hit(1,20); wait('real duplicate hit rejected',lambda:not match()['accepted'] and hp(client)['hp']==75)
        wait('real carrier scores a full second',lambda:rows()[client]['Score']>=1)
        hit(3,21)
        wait('real death drops crown on both peers',lambda:not hp(client)['alive'] and not hp(client,'client')['alive'] and not match()['carried'] and not match('client')['carried'])
        pickup('client'); no_owner('real dead player cannot pick up crown')
        if not args.arena: wait('real death uses invalid-placement pedestal fallback',lambda:abs(match()['crown']['z'])<.001)
        else: wait('production crown drop is safe and replicated',lambda:match().get('safeArenaDrop') is True and match()['crown']==match('client')['crown'])
        wait('real respawn restores replicated alive and health with '+('P2 spawn service' if args.arena else 'mock spawn service'),lambda:hp(client)['alive'] and hp(client,'client')['alive'] and hp(client)['hp']==100,8)
        if not args.arena:
            move(client); pickup('client'); wait('respawned real player can retake crown',lambda:match()['carried'] and match()['carrier']==client)
        wait('real-health round finishes',lambda:phase('Finished'),20)
        reset(); wait('real reset restores health',lambda:hp(client)['hp']==100 and hp(client)['alive'])
        if args.arena:
            wait('restart updates all health rounds',lambda:all(h['round']==match()['round'] for h in match()['health']))
            launch('late','client')
            wait('late join health receives current round',lambda:len(match()['health'])==3 and all(h['round']==match()['round'] for h in match()['health']))
            command('late','leave')
            wait('late player leaves cleanly',lambda:len(match()['health'])==2)
        hit(1,100,rnd=1); wait('previous-round health action rejected after reset',lambda:not match()['accepted'] and hp(client)['hp']==100)
        start(2); hit(4,200)
        wait('second real death observed',lambda:not hp(client)['alive'])
        wait('match finishes before pending respawn',lambda:phase('Finished'),5)
        reset(); wait('reset cancels pending death and restores alive state',lambda:hp(client)['alive'] and hp(client,'client')['alive'] and hp(client)['hp']==100)
        time.sleep(6); wait('old respawn timer does not alter new lobby',lambda:phase('Lobby') and hp(client)['alive'] and hp(client)['hp']==100)
        if args.arena:
            start(15); move(client); pickup('client')
            wait('new-round carrier acquired',lambda:match()['carried'] and match()['carrier']==client)
            command('late','rejoin')
            wait('mid-match join receives round and crown',lambda:match('late').get('connected') and match('late').get('phase')=='Playing' and match('late').get('carrier')==client and len(match()['health'])==3 and all(h['round']==match()['round'] for h in match()['health']))
            command('client','leave')
            wait('carrier disconnect drops through real arena service',lambda:not match()['carried'] and match().get('safeArenaDrop') is True and not rows()[client]['Connected'])
            wait('disconnected client finishes shutdown',lambda:not match('client')['connected'])
            command('client','rejoin')
            wait('rejoin receives current round and health',lambda:match('client').get('connected') and len(match()['health'])==3 and all(h['round']==match()['round'] for h in match()['health']))
            wait('rosters remain unique on every connected peer',lambda:all(len(match(n)['scores'])==len({r['Id'] for r in match(n)['scores']}) for n in ('host','client','late')))
            wait('third round finishes on all peers',lambda:phase('Finished',('host','client','late')),20)
            reset()
        wait('real-health processes report zero runtime errors',lambda:all(read(n).get('errors',-1)==0 for n in processes))
    else:
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
        with ThreadPoolExecutor(max_workers=2) as pool:
            list(pool.map(pickup, ('host','client')))
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
        wait('zero runtime errors across processes',lambda:all(read(n).get('errors',-1)==0 for n in processes))
    summary=dict(passed=True,checks=checks,editorHost=args.editor,fullDuration=args.full_duration,realHealth=args.real_health,arena=args.arena,reports=str(OUT))
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
