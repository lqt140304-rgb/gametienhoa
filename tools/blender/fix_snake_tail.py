"""One-off fix for the Meshy Rắn Dung Nham: its tail came out as a separate red blade stuck on the side of the coil. Squashes the blade
into the coil's tube (coil numbers measured on that model) and repaints its texels black.

    python -I tools/blender/fix_snake_tail.py ran_tex.glb ran_fix.glb
"""
import bpy,sys,math,numpy as np
from mathutils import Vector
src,dst=sys.argv[-2],sys.argv[-1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
o=[o for o in bpy.data.objects if o.type=='MESH'][0]; me=o.data
CX,CY,R,Z0,RR=-0.02,0.113,0.58,-0.51,0.205    # coil: axis, core radius, core height, tube radius (measured)
moved=set()
# 1) the stray tail blade sits on the side of the coil: squash it into the tube so the surface is closed and smooth
for v in me.vertices:
    p=v.co; rho=math.hypot(p.x-CX,p.y-CY); az=math.degrees(math.atan2(p.y-CY,p.x-CX))
    if not (55<=az<=145): continue
    d=math.hypot(rho-R,p.z-Z0)
    if d>RR+0.03 and p.z<0.05 and rho>R-0.05:   # only the blade outside the coil, not the inner curl
        k=RR*0.97/d
        nr=R+(rho-R)*k; nz=Z0+(p.z-Z0)*k
        s=nr/max(rho,1e-6)
        v.co=Vector((CX+(p.x-CX)*s,CY+(p.y-CY)*s,nz)); moved.add(v.index)
print('blade vertices squashed',len(moved))
me.update()
# 3) repaint the squashed blade's texels with the coil's black scales so no red patch is left
mat=o.active_material
base=[l.from_node for l in mat.node_tree.links if l.to_socket.name=='Base Color'][0].image
W,H=base.size; px=np.array(base.pixels[:],dtype=np.float32).reshape(H,W,4)
uv=me.uv_layers.active.data
mask=np.zeros((H,W),bool)
def fill(a,b,c):
    xs=[a[0],b[0],c[0]]; ys=[a[1],b[1],c[1]]
    x0,x1=int(max(0,min(xs)*W-1)),int(min(W-1,max(xs)*W+1)); y0,y1=int(max(0,min(ys)*H-1)),int(min(H-1,max(ys)*H+1))
    if x1<=x0 or y1<=y0: return
    X,Y=np.meshgrid(np.arange(x0,x1+1)+0.5,np.arange(y0,y1+1)+0.5); X=X/W; Y=Y/H
    dd=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
    if abs(dd)<1e-12: return
    l1=((b[1]-c[1])*(X-c[0])+(c[0]-b[0])*(Y-c[1]))/dd; l2=((c[1]-a[1])*(X-c[0])+(a[0]-c[0])*(Y-c[1]))/dd
    mask[y0:y1+1,x0:x1+1]|=(l1>=-0.02)&(l2>=-0.02)&(1-l1-l2>=-0.02)
for poly in me.polygons:
    if any(v in moved for v in poly.vertices):
        lp=[uv[i].uv for i in poly.loop_indices]
        for k in range(1,len(lp)-1): fill(lp[0],lp[k],lp[k+1])
lum=px[...,:3].mean(axis=2)
dark=(~mask)&(lum<0.25)
col=px[dark,:3].mean(axis=0) if dark.any() else np.array([0.12,0.12,0.14],np.float32)
shade=np.clip(lum/max(float(lum[mask].mean()) if mask.any() else 1,1e-3),0.6,1.3)[...,None]
px[mask,:3]=np.clip(col*shade[mask],0,1)
base.pixels=px.ravel().tolist(); base.pack()
print('repainted px',int(mask.sum()))
bpy.ops.export_scene.gltf(filepath=dst,export_format='GLB')
