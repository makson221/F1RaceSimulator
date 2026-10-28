import json, os, sys, subprocess
sys.path.insert(0, os.path.dirname(__file__))
from events import EVENTS
DL = sys.argv[1]
out = {}
for tid,(yr,ev) in EVENTS.items():
    repo = os.path.join(DL, 'ti'+yr)
    tree = set(subprocess.run(['git','-c','core.quotepath=false','-C',repo,'ls-tree','-r','--name-only','HEAD','--',ev+'/'],capture_output=True,text=True).stdout.splitlines())
    best=None; cands=[]
    for ses in ['Race','Qualifying']:
        p=os.path.join(repo,ev,ses,'session_laptimes.json')
        if not os.path.exists(p): continue
        s=json.load(open(p))
        n=len(s['time'])
        for i in range(n):
            t=s['time'][i]
            if not isinstance(t,(int,float)): continue
            if s.get('pin',[None]*n)[i] not in (None,'None') or s.get('pout',[None]*n)[i] not in (None,'None'): continue
            if s.get('del',[False]*n)[i] is True: continue
            path=f"{ev}/{ses}/{s['drv'][i]}/{s['lap'][i]}_tel.json"
            if path not in tree: continue
            cands.append((0 if ses=='Race' else 1,t,path,ses))
            if best is None or t<best[0]: best=(t,path,ses)
    cands.sort()
    r=[c[1:] for c in cands if c[0]==0][:4]; q=[c[1:] for c in cands if c[0]==1][:3]
    out[tid]=(yr,best,r+q)
    print(tid, yr, best)
json.dump(out, open(os.path.join(os.path.dirname(__file__),'picked.json'),'w'), ensure_ascii=False, indent=1)
